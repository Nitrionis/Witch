using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Storage
{
	internal class PatchesCollection
	{
		private readonly Dictionary<PatchKey, int> keyToSlots = new();
		private readonly Queue<int> freeKeySlots = new();
		private (PatchKey Key, PatchPointer Pointer)[] keyValueSlots = new (PatchKey, PatchPointer)[128];
		private int maxSlotIndex;

		public PatchesCollection()
		{
			for (int i = 0; i < keyValueSlots.Length; i++) {
				freeKeySlots.Enqueue(i);
			}
		}

		public void Add(PatchKey key, PatchPointer patch)
		{
			EnshureFreeSlot();
			if (!TryAdd(key, patch)) {
				throw new Exception("Dictionary Add failed");
			}
		}

		public bool TryAdd(PatchKey key, PatchPointer patch)
		{
			EnshureFreeSlot();
			int slotIndex = freeKeySlots.Peek();
			if (keyToSlots.TryAdd(key, slotIndex)) {
				keyValueSlots[slotIndex] = (key, patch);
				freeKeySlots.Dequeue();
				maxSlotIndex = Math.Max(maxSlotIndex, slotIndex);
				return true;
			}
			return false;
		}

		public bool TryGetValue(PatchKey key, out PatchPointer patch)
		{
			patch = default;
			if (keyToSlots.TryGetValue(key, out int slotIndex)) {
				patch = keyValueSlots[slotIndex].Pointer;
				return true;
			}
			return false;
		}

		public bool ContainsKey(PatchKey key) => keyToSlots.ContainsKey(key);

		public void Remove(PatchKey key)
		{
			int slotIndex = keyToSlots[key];
			keyToSlots.Remove(key);
			freeKeySlots.Enqueue(slotIndex);
			keyValueSlots[slotIndex] = default;
		}

		public void Trim()
		{
			if (freeKeySlots.Count == 0) {
				return;
			}
			int firstFreeSlot = -1;
			for (int i = 0; i < keyValueSlots.Length; i++) {
				if (keyValueSlots[i].Key.IsDefault) {
					firstFreeSlot = i;
					break;
				}
			}
			for (int i = firstFreeSlot + 1; i < keyValueSlots.Length; i++) {
				var key = keyValueSlots[i].Key;
				if (!key.IsDefault) {
					keyToSlots[key] = firstFreeSlot;
					firstFreeSlot++;
				}
			}
			freeKeySlots.Clear();
			for (int i = firstFreeSlot; i < keyValueSlots.Length; i++) {
				freeKeySlots.Enqueue(i);
				keyValueSlots[i] = default;
			}
			maxSlotIndex = firstFreeSlot;
		}

		private void EnshureFreeSlot()
		{
			if (freeKeySlots.Count > 0) {
				return; 
			}
			int newCapacity = 2 * keyValueSlots.Length;
			for (int i = keyValueSlots.Length; i < newCapacity; i++) {
				freeKeySlots.Enqueue(i);
			}
			Array.Resize(ref keyValueSlots, newCapacity);
		}

		public struct CyclicEnumerator : IEnumerator<KeyValuePair<PatchKey, PatchPointer>>
		{
			private int currentSlotIndex;
			private int missCount;
			private int moveNextCount;

			public readonly PatchesCollection Collection;

			public CyclicEnumerator(PatchesCollection collection) : this() => Collection = collection;

			object IEnumerator.Current => Current;

			public KeyValuePair<PatchKey, PatchPointer> Current { get; private set; }

			public bool MoveNext()
			{
				if (Collection.keyToSlots.Count == 0) {
					return false;
				}
				while (true) {
					currentSlotIndex = (currentSlotIndex + 1) % Collection.maxSlotIndex;
					var (key, value) = Collection.keyValueSlots[currentSlotIndex];
					Current = new(key, value);
					if (!Current.Key.IsDefault) {
						break;
					}
					missCount++;
				}
				moveNextCount++;
				int itemCount = Collection.keyToSlots.Count;
				if (moveNextCount >= itemCount) {
					moveNextCount = 0;
					if ((float)missCount / itemCount > 2f) {
						Collection.Trim();
					}
				}
				return true;
			}

			public void Reset() { }

			public void Dispose() { }

		}
	}
}
