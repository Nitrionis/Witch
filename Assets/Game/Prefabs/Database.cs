using UnityEngine;

namespace Assets.Game.Prefabs
{
	[CreateAssetMenu(fileName = "Database", menuName = "Scriptable Objects/Database")]
	public class Database : ScriptableObject
	{
		[SerializeField]
		private GameObject player;

		public GameObject Player => player;
	}
}
