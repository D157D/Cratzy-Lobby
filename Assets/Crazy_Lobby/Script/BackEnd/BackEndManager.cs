using System;
using System.Collections;
using System.Collections.Generic;
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

[Serializable]
public class FriendData
{
    public string username;
    public string displayName;
    public string status;
    public string characterType;

    public bool isOnline => status == "Online";
}

[Serializable]
public class FriendActionRequest
{
    public string friendUsername;
}

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance;
    // private string baseUrl = "http://127.0.0.1:5113/api"; local
    private string baseUrl = "https://webapiforgame-production.up.railway.app/api";
    private string currentToken = "";
    private string currentUsername = ""; 

    public string CurrentDisplayName { get; private set; } = "";
    public string PlayerId { get; private set; } = "";

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
    public void AddFriend(string friendName, Action<bool, string> callback = null)
    {
        StartCoroutine(UpdateFriendStatusRoutine(friendName, "add", callback));
    }

    public void RemoveFriend(string friendName, Action<bool, string> callback = null)
    {
        StartCoroutine(UpdateFriendStatusRoutine(friendName, "remove", callback));
    }
    public void FindFriendsInDB(string name, Action<bool, List<FriendData>> callback = null)
    {
        StartCoroutine(FindFriends(name, callback));
    }

    public void GetFriendsList(Action<bool, List<FriendData>> callback = null)
    {
        StartCoroutine(GetFriendsListRoutine(callback));
    }

    public void GetFriendRequestsList(Action<bool, List<FriendData>> callback = null)
    {
        StartCoroutine(GetFriendRequestsListRoutine(callback));
    }

    public void AcceptFriendRequest(string friendName, Action<bool, string> callback = null)
    {
        StartCoroutine(UpdateFriendRequestStatusRoutine(friendName, "accept", callback));
    }

    public void DeclineFriendRequest(string friendName, Action<bool, string> callback = null)
    {
        StartCoroutine(UpdateFriendRequestStatusRoutine(friendName, "decline", callback));
    }

    public void CreateRoom(string roomName, int maxPlayers, Action<bool, string> callback = null)
    {
        StartCoroutine(CreateRoomRoutine(roomName, maxPlayers, callback));
    }

    public void GetRoomsList(Action<bool, List<RoomData>> callback = null)
    {
        StartCoroutine(GetRoomsListRoutine(callback));
    }

    public void JoinRoom(string roomId, Action<bool, string> callback = null)
    {
        StartCoroutine(JoinLeaveRoomRoutine(roomId, "join", callback));
    }

    public void LeaveRoom(string roomId, Action<bool, string> callback = null)
    {
        StartCoroutine(JoinLeaveRoomRoutine(roomId, "leave", callback));
    }

    public void SendMatchResult(string roomId, int score, int maxCombo, int perfectHits, int missHits, Action<bool, string> callback = null)
    {
        StartCoroutine(SendMatchResultRoutine(roomId, score, maxCombo, perfectHits, missHits, callback));
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
                PlayerId = loginResponse.playerId;
                
                Debug.Log($"[BackendManager] Login thành công. PlayerId: {PlayerId}");
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
    IEnumerator UpdateFriendStatusRoutine(string friendName, string action, Action<bool, string> callback)
    {
        string endpoint = action == "add" ? "/Auth/add-friend" : "/Auth/delete-friend";
        string url = baseUrl + endpoint; 
        
        FriendActionRequest reqData = new FriendActionRequest { friendUsername = friendName };
        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        string httpMethod = action == "add" ? "POST" : "DELETE";

        using (UnityWebRequest req = new UnityWebRequest(url, httpMethod))
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
                callback?.Invoke(true, action == "add" ? "Thêm bạn thành công!" : "Xóa bạn thành công!");
            }
            else
            {
                Debug.LogError($"Lỗi {action} bạn: {req.downloadHandler.text}");
                callback?.Invoke(false, $"Lỗi {action} bạn: " + req.error);
            }
        }
    }

    IEnumerator FindFriends(string name, Action<bool, List<FriendData>> callback)
    {
        string url = baseUrl + "/User/search?query=" + UnityWebRequest.EscapeURL(name);
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))   
        {
            req.certificateHandler = new BypassCertificate();

            if (!string.IsNullOrEmpty(currentToken))
            {
                req.SetRequestHeader("Authorization", "Bearer " + currentToken);
            }

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                FriendData[] friends = JsonHelper.FromJson<FriendData>(jsonResponse);
                callback?.Invoke(true, new System.Collections.Generic.List<FriendData>(friends));
            }
            else
            {
                Debug.LogError($"Lỗi tìm bạn: {req.downloadHandler.text}");
                callback?.Invoke(false, null);
            }
        }
    }

    IEnumerator GetFriendsListRoutine(Action<bool, List<FriendData>> callback)
    {
        string url = baseUrl + "/Auth/get-friends";
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))   
        {
            req.certificateHandler = new BypassCertificate();

            if (!string.IsNullOrEmpty(currentToken))
            {
                req.SetRequestHeader("Authorization", "Bearer " + currentToken);
            }

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                FriendData[] friends = JsonHelper.FromJson<FriendData>(jsonResponse);
                callback?.Invoke(true, new System.Collections.Generic.List<FriendData>(friends));
            }
            else
            {
                Debug.LogError($"Lỗi lấy danh sách bạn: {req.downloadHandler.text}");
                callback?.Invoke(false, null);
            }
        }
    }

    IEnumerator GetFriendRequestsListRoutine(Action<bool, List<FriendData>> callback)
    {
        string url = baseUrl + "/Auth/get-friend-requests";
        
        using (UnityWebRequest req = UnityWebRequest.Get(url))   
        {
            req.certificateHandler = new BypassCertificate();

            if (!string.IsNullOrEmpty(currentToken))
            {
                req.SetRequestHeader("Authorization", "Bearer " + currentToken);
            }

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = req.downloadHandler.text;
                FriendData[] friends = JsonHelper.FromJson<FriendData>(jsonResponse);
                callback?.Invoke(true, new System.Collections.Generic.List<FriendData>(friends));
            }
            else
            {
                Debug.LogError($"Lỗi lấy danh sách yêu cầu kết bạn: {req.downloadHandler.text}");
                callback?.Invoke(false, null);
            }
        }
    }

    IEnumerator UpdateFriendRequestStatusRoutine(string friendName, string action, Action<bool, string> callback)
    {
        string endpoint = action == "accept" ? "/Auth/accept-friend" : "/Auth/decline-friend";
        string url = baseUrl + endpoint; 
        
        FriendActionRequest reqData = new FriendActionRequest { friendUsername = friendName };
        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
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
                callback?.Invoke(true, action == "accept" ? "Chấp nhận kết bạn thành công!" : "Từ chối kết bạn thành công!");
            }
            else
            {
                Debug.LogError($"Lỗi {action} bạn: {req.downloadHandler.text}");
                callback?.Invoke(false, $"Lỗi {action} bạn: " + req.error);
            }
        }
    }

    IEnumerator CreateRoomRoutine(string roomName, int maxPlayers, Action<bool, string> callback)
    {
        string url = baseUrl + "/Room/create";
        CreateRoomReq reqData = new CreateRoomReq { roomName = roomName, maxPlayers = maxPlayers };
        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                CreateRoomRes res = JsonUtility.FromJson<CreateRoomRes>(req.downloadHandler.text);
                callback?.Invoke(true, res.roomId);
            }
            else
            {
                callback?.Invoke(false, req.error);
            }
        }
    }

    IEnumerator GetRoomsListRoutine(Action<bool, List<RoomData>> callback)
    {
        string url = baseUrl + "/Room/list";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new BypassCertificate();
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                RoomData[] rooms = JsonHelper.FromJson<RoomData>(req.downloadHandler.text);
                callback?.Invoke(true, new List<RoomData>(rooms));
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }
    }

    IEnumerator JoinLeaveRoomRoutine(string roomId, string action, Action<bool, string> callback)
    {
        string url = baseUrl + "/Room/" + action;
        string jsonBody = "\"" + roomId + "\""; // Gửi raw string
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, action == "join" ? "Vào phòng thành công!" : "Rời phòng thành công!");
            }
            else
            {
                callback?.Invoke(false, req.error);
            }
        }
    }

    IEnumerator SendMatchResultRoutine(string roomId, int score, int maxCombo, int perfectHits, int missHits, Action<bool, string> callback)
    {
        string url = baseUrl + "/Match/result";
        MatchResultReq reqData = new MatchResultReq 
        { 
            roomId = roomId, 
            score = score, 
            maxCombo = maxCombo, 
            perfectHits = perfectHits, 
            missHits = missHits 
        };
        string jsonBody = JsonUtility.ToJson(reqData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, "Gửi kết quả thành công!");
            }
            else
            {
                callback?.Invoke(false, req.error);
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