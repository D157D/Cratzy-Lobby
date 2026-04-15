using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class LoadLobby : MonoBehaviour
{

    [SerializeField] private string lobbySceneName = "Login_CrazyLobby";
    void Awake()
    {
        StartCoroutine(LoadLobbyScene());
    }
    IEnumerator LoadLobbyScene()
    {
        yield return new WaitForSeconds(5f);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(lobbySceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }   
    }
    
}
