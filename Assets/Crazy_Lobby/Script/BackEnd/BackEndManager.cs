using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SignUpRequest { public string username; public string password; }

[Serializable]
public class LoginRequest { public string username; public string password; }

[Serializable]
public class LoginResponse { public string token; public string playerId; }

[Serializable]
public class CreateRoomReq { public string roomName; public int maxPlayers; }

[Serializable]
public class CreateRoomRes { public string roomId; }

[Serializable]
public class RoomData { public string roomId; public string roomName; public int maxPlayers; public string hostId; }

[Serializable]
public class MatchResultReq 
{ 
    public string roomId; 
    public int score; 
    public int maxCombo;
    public int perfectHits;
    public int missHits;
}

[Serializable]
public class UpdateNameRequest { public string displayName; }

[Serializable]
public class UserProfileResponse 
{ 
    public string username; 
    public string displayName; 
}

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance;
    private string baseUrl = "http://127.0.0.1:5113/api";
    private string currentToken = "";
    private string currentUsername = ""; 
    private Coroutine sessionCheckCoroutine;

    public string CurrentDisplayName { get; private set; } = "";

    public static event Action OnLoginSuccess;

    public bool IsLoggedIn => !string.IsNullOrEmpty(currentToken);
    public bool IsOfflineMode => currentToken == "offline_token_aaa";

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void Register(string username, string password, Action<bool, string> callback = null)
    {
        StartCoroutine(RegisterRequest(username, password, callback));
    }
    public void Login(string username, string password, Action<bool, string> callback = null)
    {
        StartCoroutine(LoginRequest(username, password, callback));
    }

    public void GetUserProfile(Action<bool, string> callback = null)
    {
        StartCoroutine(GetUserProfileRequest(callback));
    }

    public void UpdateDisplayName(string newName, Action<bool, string> callback = null)
    {
        StartCoroutine(UpdateDisplayNameRequestRoutine(newName, callback));
    }

    IEnumerator RegisterRequest(string username, string password, Action<bool, string> callback)
    {
        string url = baseUrl + "/Auth/register";
        
        SignUpRequest reqData = new SignUpRequest { username = username, password = password };
        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {   
            UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw);
            uploadHandler.contentType = "application/json"; 
            req.uploadHandler = uploadHandler;
            req.downloadHandler = new DownloadHandlerBuffer();      
            req.certificateHandler = new BypassCertificate();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, "Đăng ký thành công!");
            }
            else
            {
                callback?.Invoke(false, "Đăng ký thất bại: " + req.error);
            }
        }
    }

    IEnumerator LoginRequest(string username, string password, Action<bool, string> callback)
    {
        string url = baseUrl + "/Auth/login";
        
        LoginRequest reqData = new LoginRequest { username = username, password = password };
        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw);
            uploadHandler.contentType = "application/json";
            req.uploadHandler = uploadHandler;
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(jsonResponse);
                currentToken = loginResponse.token;
                currentUsername = username;
                
                Debug.Log($"[BackendManager] Login thành công. Token: {currentToken.Substring(0, Mathf.Min(20, currentToken.Length))}...");
                callback?.Invoke(true, "Đăng nhập thành công!");
                OnLoginSuccess?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[BackendManager] Login API thất bại: HTTP {req.responseCode} - {req.error}");

                if (username == "aaa" && password == "aaa")
                {
                    currentToken = "offline_token_aaa";
                    currentUsername = username;
                    CurrentDisplayName = username; 
                    Debug.Log("[BackendManager] Chuyển sang chế độ Offline.");
                    callback?.Invoke(true, "Đăng nhập thành công (Offline)!");
                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    callback?.Invoke(false, "Đăng nhập thất bại: " + req.error);
                }
            }
        }
    }

    IEnumerator GetUserProfileRequest(Action<bool, string> callback)
    {
        if (IsOfflineMode)
        {
            Debug.Log($"[BackendManager] Offline mode → trả về tên: {CurrentDisplayName}");
            string offlineName = !string.IsNullOrEmpty(CurrentDisplayName) ? CurrentDisplayName : currentUsername;
            CurrentDisplayName = offlineName;
            callback?.Invoke(true, offlineName);
            yield break;
        }

        if (string.IsNullOrEmpty(currentToken))
        {
            Debug.LogWarning("[BackendManager] GetUserProfile thất bại: Chưa đăng nhập (token rỗng).");
            callback?.Invoke(false, "Chưa đăng nhập.");
            yield break;
        }

        string url = baseUrl + "/User/profile"; 
        Debug.Log($"[BackendManager] Gọi API: GET {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
            req.certificateHandler = new BypassCertificate();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                Debug.Log($"[BackendManager] Profile response: {jsonResponse}");

                UserProfileResponse res = JsonUtility.FromJson<UserProfileResponse>(jsonResponse);
                
                CurrentDisplayName = string.IsNullOrEmpty(res.displayName) ? res.username : res.displayName;
                
                callback?.Invoke(true, CurrentDisplayName);
            }
            else
            {
                Debug.LogError($"[BackendManager] GetProfile thất bại: HTTP {req.responseCode} - {req.error}\nResponse: {req.downloadHandler?.text}");
                callback?.Invoke(false, $"Lỗi tải tên (HTTP {req.responseCode}): {req.error}");
            }
        }
    }

    IEnumerator UpdateDisplayNameRequestRoutine(string newName, Action<bool, string> callback)
    {
        string url = baseUrl + "/User/display-name"; 
        
        UpdateNameRequest reqData = new UpdateNameRequest { displayName = newName };
        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "PUT"))
        {
            UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw);
            uploadHandler.contentType = "application/json";
            req.uploadHandler = uploadHandler;
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();

            if (!string.IsNullOrEmpty(currentToken))
            {
                req.SetRequestHeader("Authorization", "Bearer " + currentToken);
            }

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                CurrentDisplayName = newName;
                callback?.Invoke(true, "Đổi tên thành công!");
            }
            else
            {
                Debug.LogError($"Lỗi đổi tên: {req.downloadHandler.text}");
                callback?.Invoke(false, "Đổi tên thất bại: " + req.error);
            }
        }
    }

    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }
        [Serializable] private class Wrapper<T> { public T[] array; }
    }
}