using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RegisterRequest { public string username; public string password; }

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
public class UpdateDisplayNameRequest { public string displayName; }

[Serializable]
public class UserProfileResponse 
{ 
    public string username; 
    public string displayName; 
    public string characterType; 
}

[Serializable]
public class AddFriendRequest 
{ 
    public string friendUsername; 
    public string playerId; 
    public string friendId; 
    public string message;
}

[Serializable]
public class GameInviteRequest 
{ 
    public string friendUsername; 
    public string roomId; 
}

[Serializable]
public class RespondInviteRequest 
{ 
    public int inviteId; 
    public string status; 
}

[Serializable]
public class FriendData { public string username; public string displayName; public string status; public string characterType; }

[Serializable]
public class FriendRequestData { public string senderUsername; public string senderDisplayName; public string characterType; }

[Serializable]
public class GameInviteData { public int inviteId; public string senderUsername; public string roomId; public string status; }

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance;
    private string baseUrl = "http://127.0.0.1:5113/api"; 
    private string currentToken = "";
    private string currentUsername = ""; 
    private Coroutine sessionCheckCoroutine;

    public string CurrentDisplayName { get; private set; } = "";
    public string CurrentCharacterType { get; private set; } = "";
    public string CurrentRoomId { get; set; } = "";

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

    public void AddFriend(string friendUsername, Action<bool, string> callback = null)
    {
        StartCoroutine(PostFriendAction("add-friend", friendUsername, callback));
    }

    public void AcceptFriend(string friendUsername, Action<bool, string> callback = null)
    {
        StartCoroutine(PostFriendAction("accept-friend", friendUsername, callback));
    }

    public void RejectFriend(string friendUsername, Action<bool, string> callback = null)
    {
        StartCoroutine(PostFriendAction("reject-friend", friendUsername, callback));
    }

    public void DeleteFriend(string friendUsername, Action<bool, string> callback = null)
    {
        StartCoroutine(DeleteFriendRequest(friendUsername, callback));
    }

    public void InviteToGame(string friendUsername, string roomId, Action<bool, string> callback = null)
    {
        StartCoroutine(InviteToGameRoutine(friendUsername, roomId, callback));
    }

    public void GetGameInvites(Action<bool, GameInviteData[]> callback)
    {
        StartCoroutine(GetGameInvitesRoutine(callback));
    }

    public void RespondToInvite(int inviteId, string status, Action<bool, string> callback = null)
    {
        StartCoroutine(RespondToInviteRoutine(inviteId, status, callback));
    }

    public void GetFriends(Action<bool, FriendData[]> callback)
    {
        StartCoroutine(GetFriendsRequest(callback));
    }

    public void GetPendingRequests(Action<bool, FriendRequestData[]> callback)
    {
        StartCoroutine(GetPendingRequestsRoutine(callback));
    }

    public void GetMatchPlayers(string roomId, Action<bool, string> callback)
    {
        StartCoroutine(GetMatchPlayersRoutine(roomId, callback));
    }

    private delegate void RequestSetup(UnityWebRequest req);

    IEnumerator SendRequest(string url, string method, object body, RequestSetup setup, Action<bool, string> callback, string logTag)
    {
        Debug.Log($"[BackendManager][{logTag}] Sending {method} to {url}");
        
        using (UnityWebRequest req = new UnityWebRequest(url, method))
        {
            if (body != null)
            {
                string jsonBody = JsonUtility.ToJson(body);
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.uploadHandler.contentType = "application/json";
            }
            
            req.downloadHandler = new DownloadHandlerBuffer();
            req.certificateHandler = new BypassCertificate();
            
            setup?.Invoke(req);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string responseText = req.downloadHandler.text;
                Debug.Log($"[BackendManager][{logTag}] Success: {responseText}");
                callback?.Invoke(true, responseText);
            }
            else
            {
                string errorDetail = req.downloadHandler?.text;
                string errorMessage = string.IsNullOrEmpty(errorDetail) ? req.error : errorDetail;
                Debug.LogError($"[BackendManager][{logTag}] Error {req.responseCode}: {errorMessage}");
                
                if (req.responseCode == 401)
                {
                    callback?.Invoke(false, "Unauthorized: Vui lòng đăng nhập lại.");
                }
                else
                {
                    callback?.Invoke(false, errorMessage);
                }
            }
        }
    }

    IEnumerator RegisterRequest(string username, string password, Action<bool, string> callback)
    {
        string url = baseUrl + "/Auth/register";
        RegisterRequest reqData = new RegisterRequest { username = username, password = password };
        
        yield return SendRequest(url, "POST", reqData, null, (success, res) => {
            callback?.Invoke(success, success ? "Đăng ký thành công!" : res);
        }, "Register");
    }

    IEnumerator LoginRequest(string username, string password, Action<bool, string> callback)
    {
        string url = baseUrl + "/Auth/login";
        LoginRequest reqData = new LoginRequest { username = username, password = password };

        yield return SendRequest(url, "POST", reqData, null, (success, res) => {
            if (success)
            {
                LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(res);
                currentToken = loginResponse.token;
                currentUsername = username;
                OnLoginSuccess?.Invoke();
                callback?.Invoke(true, "Đăng nhập thành công!");
            }
            else
            {
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
                    callback?.Invoke(false, res);
                }
            }
        }, "Login");
    }

    IEnumerator GetUserProfileRequest(Action<bool, string> callback)
    {
        if (IsOfflineMode)
        {
            string name = !string.IsNullOrEmpty(CurrentDisplayName) ? CurrentDisplayName : currentUsername;
            callback?.Invoke(true, name);
            yield break;
        }

        if (string.IsNullOrEmpty(currentToken))
        {
            callback?.Invoke(false, "Chưa đăng nhập.");
            yield break;
        }

        string url = baseUrl + "/User/profile"; 
        yield return SendRequest(url, "GET", null, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            if (success)
            {
                UserProfileResponse profile = JsonUtility.FromJson<UserProfileResponse>(res);
                CurrentDisplayName = string.IsNullOrEmpty(profile.displayName) ? profile.username : profile.displayName;
                CurrentCharacterType = profile.characterType;
                callback?.Invoke(true, CurrentDisplayName);
            }
            else callback?.Invoke(false, res);
        }, "GetProfile");
    }

    IEnumerator UpdateDisplayNameRequestRoutine(string newName, Action<bool, string> callback)
    {
        string url = baseUrl + "/User/display-name"; 
        UpdateDisplayNameRequest reqData = new UpdateDisplayNameRequest { displayName = newName };

        yield return SendRequest(url, "PUT", reqData, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            if (success) CurrentDisplayName = newName;
            callback?.Invoke(success, success ? "Đổi tên thành công!" : res);
        }, "UpdateName");
    }

    IEnumerator PostFriendAction(string endpoint, string friendUsername, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/Auth/{endpoint}";
        AddFriendRequest reqData = new AddFriendRequest { friendUsername = friendUsername };

        yield return SendRequest(url, "POST", reqData, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            callback?.Invoke(success, success ? "Thao tác thành công!" : res);
        }, $"FriendAction:{endpoint}");
    }

    IEnumerator DeleteFriendRequest(string friendUsername, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/Auth/delete-friend";
        AddFriendRequest reqData = new AddFriendRequest { friendUsername = friendUsername };

        yield return SendRequest(url, "DELETE", reqData, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            callback?.Invoke(success, success ? "Đã xóa bạn bè!" : res);
        }, "DeleteFriend");
    }

    IEnumerator InviteToGameRoutine(string friendUsername, string roomId, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/Auth/invite-game";
        GameInviteRequest reqData = new GameInviteRequest { friendUsername = friendUsername, roomId = roomId };

        yield return SendRequest(url, "POST", reqData, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            callback?.Invoke(success, success ? "Đã gửi lời mời!" : res);
        }, "InviteGame");
    }

    IEnumerator GetGameInvitesRoutine(Action<bool, GameInviteData[]> callback)
    {
        string url = $"{baseUrl}/Auth/get-game-invites";
        yield return SendRequest(url, "GET", null, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            if (success) callback?.Invoke(true, JsonHelper.FromJson<GameInviteData>(res));
            else callback?.Invoke(false, null);
        }, "GetInvites");
    }

    IEnumerator RespondToInviteRoutine(int inviteId, string status, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/Auth/respond-invite";
        RespondInviteRequest reqData = new RespondInviteRequest { inviteId = inviteId, status = status };

        yield return SendRequest(url, "POST", reqData, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            callback?.Invoke(success, success ? "Đã phản hồi!" : res);
        }, "RespondInvite");
    }

    IEnumerator GetFriendsRequest(Action<bool, FriendData[]> callback)
    {
        string url = $"{baseUrl}/Auth/get-friends";
        yield return SendRequest(url, "GET", null, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            if (success) callback?.Invoke(true, JsonHelper.FromJson<FriendData>(res));
            else callback?.Invoke(false, null);
        }, "GetFriends");
    }

    IEnumerator GetPendingRequestsRoutine(Action<bool, FriendRequestData[]> callback)
    {
        string url = $"{baseUrl}/Auth/get-pending-requests";
        yield return SendRequest(url, "GET", null, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            if (success) callback?.Invoke(true, JsonHelper.FromJson<FriendRequestData>(res));
            else callback?.Invoke(false, null);
        }, "GetPending");
    }

    IEnumerator GetMatchPlayersRoutine(string roomId, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/Match/{roomId}/players";
        yield return SendRequest(url, "GET", null, (req) => {
            req.SetRequestHeader("Authorization", "Bearer " + currentToken);
        }, (success, res) => {
            callback?.Invoke(success, res);
        }, "GetMatchPlayers");
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