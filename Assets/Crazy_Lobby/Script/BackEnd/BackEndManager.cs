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



public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance;
    public static event Action OnForcedLogout;
    private string baseUrl = "http://127.0.0.1:5113/api";
    private string currentToken = "";
    private Coroutine sessionCheckCoroutine;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void Register(string username, string password)
    {
        StartCoroutine(RegisterRequest(username, password));
    }
    public void Login(string username, string password)
    {
        StartCoroutine(LoginRequest(username, password));
    }

    IEnumerator RegisterRequest(string username, string password)
    {
        string url = baseUrl + "/Auth/register";
        
        Debug.Log("Đang gửi Register Request tới: " + url);

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
                Debug.Log("<color=green>ĐĂNG KÝ THÀNH CÔNG!</color>");
            else
                Debug.Log("<color=red>ĐĂNG KÝ THẤT BẠI: " + req.error + "</color>");
        }
    }



    IEnumerator LoginRequest(string username, string password)
    {
        string url = baseUrl + "/Auth/login";
        Debug.Log("Đang gửi Login Request tới: " + url);

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
                Debug.Log("<color=green>ĐĂNG NHẬP THÀNH CÔNG!</color>");
                string jsonResponse = req.downloadHandler.text;
                Debug.Log("<color=cyan>Dữ liệu JSON từ Server: " + jsonResponse + "</color>");

                LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(jsonResponse);
                currentToken = loginResponse.token;
                
                Debug.Log("<color=yellow>Token nhận được: " + currentToken + "</color>");
                Debug.Log("<color=yellow>Player ID: " + loginResponse.playerId + "</color>");
            }
            else
            {
                Debug.LogError("<color=red>ĐĂNG NHẬP THẤT BẠI: " + req.error + "</color>");
                Debug.LogError("Mã lỗi (Response Code): " + req.responseCode);

                if (req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text))
                {
                    Debug.LogError("Chi tiết lỗi từ Server: " + req.downloadHandler.text);
                }
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
