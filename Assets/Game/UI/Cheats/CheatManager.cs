using UnityEngine;
using UnityEngine.SceneManagement;

public class CheatManager : MonoBehaviour
{
	[Header("Cheat Menu Settings")]
	[SerializeField] private string cheatMenuSceneName = "CheatMenu";
	[SerializeField] private bool enableCheats = true;

	[Header("Debug")]
	[SerializeField] private bool showDebugLogs = true;

	private bool isCheatMenuLoaded = false;

	public static CheatManager Instance { get; private set; }

	void Awake()
	{
		// Singleton pattern
		if (Instance == null) {
			Instance = this;
			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
		}
	}

	void Update()
	{
		if (!enableCheats) return;

		HandleKeyboardInput();
		HandleMobileInput();
	}

	private void HandleKeyboardInput()
	{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.F1)) {
			ToggleCheatMenu();
		}
#endif
	}

	private void HandleMobileInput()
	{
#if UNITY_IOS || UNITY_ANDROID
        // Check for exactly 3 simultaneous touches
        if (Input.touchCount == 3)
        {
            // Verify all three touches just began in the same frame
            bool allTouchesBegan = true;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase != TouchPhase.Began)
                {
                    allTouchesBegan = false;
                    break;
                }
            }
            
            if (allTouchesBegan)
            {
                ToggleCheatMenu();
            }
        }
#endif
	}

	private void ToggleCheatMenu()
	{
		if (isCheatMenuLoaded) {
			UnloadCheatMenu();
		} else {
			LoadCheatMenu();
		}
	}

	private void LoadCheatMenu()
	{
		if (!IsSceneLoaded(cheatMenuSceneName)) {
			try {
				SceneManager.LoadScene(cheatMenuSceneName, LoadSceneMode.Additive);
				isCheatMenuLoaded = true;
				if (showDebugLogs) Debug.Log("Cheat menu loaded additively");
			} catch (System.Exception e) {
				Debug.LogError($"Failed to load cheat menu: {e.Message}");
			}
		}
	}

	private void UnloadCheatMenu()
	{
		if (IsSceneLoaded(cheatMenuSceneName)) {
			SceneManager.UnloadSceneAsync(cheatMenuSceneName);
			isCheatMenuLoaded = false;
			if (showDebugLogs) Debug.Log("Cheat menu unloaded");
		}
	}

	private bool IsSceneLoaded(string sceneName)
	{
		for (int i = 0; i < SceneManager.sceneCount; i++) {
			Scene scene = SceneManager.GetSceneAt(i);
			if (scene.name == sceneName && scene.isLoaded) {
				return true;
			}
		}
		return false;
	}

	// Public API
	public void EnableCheats(bool enable)
	{
		enableCheats = enable;
		if (!enable && isCheatMenuLoaded) {
			UnloadCheatMenu();
		}
	}

	public void ShowCheatMenu()
	{
		if (!isCheatMenuLoaded && enableCheats) {
			LoadCheatMenu();
		}
	}

	public void HideCheatMenu()
	{
		if (isCheatMenuLoaded) {
			UnloadCheatMenu();
		}
	}
}