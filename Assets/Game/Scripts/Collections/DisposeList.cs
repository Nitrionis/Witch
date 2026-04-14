using System;
using System.Collections.Generic;

namespace Game.Collections
{
	internal class DisposeList : IDisposable
	{
		private bool disposedValue;
		private List<IDisposable> list = new();

		public void Add(IDisposable disposable)
		{
			if (disposable == null) {
				throw new ArgumentNullException("null disposable");
			}
			list.Add(disposable);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue) {
				if (disposing) {
					foreach (var disposable in list) {
						disposable.Dispose();
					}
				} else {
					UnityEngine.Debug.LogException(new Exception("DisposeList not disposed manually!"));
				}
				disposedValue = true;
			}
		}

		~DisposeList()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
