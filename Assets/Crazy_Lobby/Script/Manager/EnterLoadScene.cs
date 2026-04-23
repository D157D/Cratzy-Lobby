using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Crazy_Lobby.UI;

public class EnterLoadScene : MonoBehaviour
{
    private void Update()
    {
        // Detect Enter or Return key
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            LoadLobby();
        }
    }

    private void LoadLobby()
    {
        // Set the flag for Menu to automatically start
        Menu.ShouldAutoStart = true;
        
        // Shutdown Fusion if it's running
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            runner.Shutdown();
        }
        
        // Load the Lobby scene (usually at index 0 or named "Login_Crazy")
        // We'll use index 0 as it's common for entry scenes
        SceneManager.LoadScene(0);
    }
}
