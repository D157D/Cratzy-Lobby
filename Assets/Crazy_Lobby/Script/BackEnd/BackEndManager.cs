using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Giữ nguyên các class DTO (Data Transfer Objects) của bạn ở đây
#region Data Classes
[Serializable] public class SignUpRequest { public string username; public string password; }
[Serializable] public class LoginRequest { public string username; public string password; }
[Serializable] public class LoginResponse { public string token; public string playerId; }
[Serializable] public class CreateRoomReq { public string roomName; public int maxPlayers; }
[Serializable] public class CreateRoomRes { public string roomId; }
[Serializable] public class RoomData { public string roomId; public string roomName; public int maxPlayers; public string hostId; }
[Serializable] public class MatchResultReq { public string roomId; public int score; public int maxCombo; public int perfectHits; public int missHits; }
[Serializable] public class UpdateNameRequest { public string displayName; }
[Serializable] public class UserProfileResponse { public string username; public string displayName; }
[Serializable] public class FriendActionRequest { public string friendUsername; }
[Serializable] public class FriendData
{
    public string username;
    public string displayName;
    public string status;
    public string characterType;
    public bool isOnline => status == "Online";
}
#endregion

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance;
    
    // private string baseUrl = "http://127.0.0.1:5113/api";
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

    #region Core Network Function
    /// <summary>
    /// Hàm Generic xử lý toàn bộ boilerplate của UnityWebRequest.
    /// </summary>
    private IEnumerator SendRequest(string endpoint, string method, object bodyData, Action<bool, string> callback)
    {
        string url = baseUrl + endpoint;
        using (UnityWebRequest req = new UnityWebRequest(url, method))
        {
            // Xử lý Body Data nếu có
            if (bodyData != null)
            {
                // Nếu data đã là chuỗi (ví dụ truyền raw string roomId), thì giữ nguyên, ngược lại serialize ra JSON
                string jsonBody = bodyData is string s ? s : JsonUtility.ToJson(bodyData);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.uploadHandler.contentType = "application/json";
            }

            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();

            // Tự động đính kèm Token nếu đã đăng nhập
            if (!string.IsNullOrEmpty(currentToken))
            {
                req.SetRequestHeader("Authorization", "Bearer " + currentToken);
            }

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"[BackendManager] API {method} {endpoint} Failed: {req.error}\nResponse: {req.downloadHandler?.text}");
                callback?.Invoke(false, req.error);
            }
        }
    }
    #endregion

    #region Auth & User Profile
    public void Register(string username, string password, Action<bool, string> callback = null)
    {
        var data = new SignUpRequest { username = username, password = password };
        StartCoroutine(SendRequest("/Auth/register", "POST", data, (success, res) => {
            callback?.Invoke(success, success ? "Đăng ký thành công!" : "Đăng ký thất bại: " + res);
        }));
    }

    public void Login(string username, string password, Action<bool, string> callback = null)
    {
        var data = new LoginRequest { username = username, password = password };
        StartCoroutine(SendRequest("/Auth/login", "POST", data, (success, res) => {
            if (success)
            {
                LoginResponse loginRes = JsonUtility.FromJson<LoginResponse>(res);
                currentToken = loginRes.token;
                currentUsername = username;
                PlayerId = loginRes.playerId;
                
                Debug.Log($"[BackendManager] Login thành công. PlayerId: {PlayerId}");
                OnLoginSuccess?.Invoke();
                callback?.Invoke(true, "Đăng nhập thành công!");
            }
            else
            {
                // Logic Offline Fallback
                if (username == "aaa" && password == "aaa")
                {
                    currentToken = "offline_token_aaa";
                    currentUsername = username;
                    CurrentDisplayName = username; 
                    Debug.Log("[BackendManager] Chuyển sang chế độ Offline.");
                    OnLoginSuccess?.Invoke();
                    callback?.Invoke(true, "Đăng nhập thành công (Offline)!");
                }
                else
                {
                    callback?.Invoke(false, "Đăng nhập thất bại: " + res);
                }
            }
        }));
    }

    public void GetUserProfile(Action<bool, string> callback = null)
    {
        if (IsOfflineMode)
        {
            CurrentDisplayName = !string.IsNullOrEmpty(CurrentDisplayName) ? CurrentDisplayName : currentUsername;
            callback?.Invoke(true, CurrentDisplayName);
            return;
        }

        if (!IsLoggedIn)
        {
            callback?.Invoke(false, "Chưa đăng nhập.");
            return;
        }

        StartCoroutine(SendRequest("/User/profile", "GET", null, (success, res) => {
            if (success)
            {
                var profile = JsonUtility.FromJson<UserProfileResponse>(res);
                CurrentDisplayName = string.IsNullOrEmpty(profile.displayName) ? profile.username : profile.displayName;
                callback?.Invoke(true, CurrentDisplayName);
            }
            else callback?.Invoke(false, "Lỗi tải tên: " + res);
        }));
    }

    public void UpdateDisplayName(string newName, Action<bool, string> callback = null)
    {
        var data = new UpdateNameRequest { displayName = newName };
        StartCoroutine(SendRequest("/User/display-name", "PUT", data, (success, res) => {
            if (success) CurrentDisplayName = newName;
            callback?.Invoke(success, success ? "Đổi tên thành công!" : "Đổi tên thất bại: " + res);
        }));
    }
    #endregion

    #region Friends System
    public void AddFriend(string friendName, Action<bool, string> callback = null) 
        => UpdateFriendAction("/Auth/add-friend", "POST", friendName, "Thêm bạn", callback);

    public void RemoveFriend(string friendName, Action<bool, string> callback = null) 
        => UpdateFriendAction("/Auth/delete-friend", "DELETE", friendName, "Xóa bạn", callback);

    public void AcceptFriendRequest(string friendName, Action<bool, string> callback = null) 
        => UpdateFriendAction("/Auth/accept-friend", "POST", friendName, "Chấp nhận kết bạn", callback);

    public void DeclineFriendRequest(string friendName, Action<bool, string> callback = null) 
        => UpdateFriendAction("/Auth/decline-friend", "POST", friendName, "Từ chối kết bạn", callback);

    private void UpdateFriendAction(string endpoint, string method, string friendName, string actionName, Action<bool, string> callback)
    {
        var data = new FriendActionRequest { friendUsername = friendName };
        StartCoroutine(SendRequest(endpoint, method, data, (success, res) => {
            callback?.Invoke(success, success ? $"{actionName} thành công!" : $"Lỗi {actionName}: {res}");
        }));
    }

    public void FindFriendsInDB(string name, Action<bool, List<FriendData>> callback = null)
    {
        string endpoint = "/User/search?query=" + UnityWebRequest.EscapeURL(name);
        FetchFriendsList(endpoint, callback);
    }

    public void GetFriendsList(Action<bool, List<FriendData>> callback = null) 
        => FetchFriendsList("/Auth/get-friends", callback);

    public void GetFriendRequestsList(Action<bool, List<FriendData>> callback = null) 
        => FetchFriendsList("/Auth/get-friend-requests", callback);

    private void FetchFriendsList(string endpoint, Action<bool, List<FriendData>> callback)
    {
        StartCoroutine(SendRequest(endpoint, "GET", null, (success, res) => {
            if (success)
            {
                FriendData[] friends = JsonHelper.FromJson<FriendData>(res);
                callback?.Invoke(true, new List<FriendData>(friends));
            }
            else callback?.Invoke(false, null);
        }));
    }
    #endregion

    #region Rooms & Matches
    public void CreateRoom(string roomName, int maxPlayers, Action<bool, string> callback = null)
    {
        var data = new CreateRoomReq { roomName = roomName, maxPlayers = maxPlayers };
        StartCoroutine(SendRequest("/Room/create", "POST", data, (success, res) => {
            if (success) callback?.Invoke(true, JsonUtility.FromJson<CreateRoomRes>(res).roomId);
            else callback?.Invoke(false, res);
        }));
    }

    public void GetRoomsList(Action<bool, List<RoomData>> callback = null)
    {
        StartCoroutine(SendRequest("/Room/list", "GET", null, (success, res) => {
            if (success) callback?.Invoke(true, new List<RoomData>(JsonHelper.FromJson<RoomData>(res)));
            else callback?.Invoke(false, null);
        }));
    }

    public void JoinRoom(string roomId, Action<bool, string> callback = null) 
        => StartCoroutine(SendRequest("/Room/join", "POST", $"\"{roomId}\"", (success, res) => callback?.Invoke(success, success ? "Vào phòng thành công!" : res)));

    public void LeaveRoom(string roomId, Action<bool, string> callback = null) 
        => StartCoroutine(SendRequest("/Room/leave", "POST", $"\"{roomId}\"", (success, res) => callback?.Invoke(success, success ? "Rời phòng thành công!" : res)));

    public void SendMatchResult(string roomId, int score, int maxCombo, int perfectHits, int missHits, Action<bool, string> callback = null)
    {
        var data = new MatchResultReq { roomId = roomId, score = score, maxCombo = maxCombo, perfectHits = perfectHits, missHits = missHits };
        StartCoroutine(SendRequest("/Match/result", "POST", data, (success, res) => {
            callback?.Invoke(success, success ? "Gửi kết quả thành công!" : res);
        }));
    }
    #endregion

    #region Helpers
    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
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
    #endregion
}