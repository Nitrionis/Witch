using UnityEngine;

namespace Game
{
	public class GameSessionHolder : MonoBehaviour
	{
		public GameSession Session { get; private set; }

        private void Start()
        {
			DontDestroyOnLoad(this);
        }

        public void StartLocalSession()
		{

		}

		public void ConnectToRemoteSession()
		{

		}
	}
}
