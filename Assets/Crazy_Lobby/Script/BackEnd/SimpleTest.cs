using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SimpleTest : MonoBehaviour
{
    // 1. Tạo class chứa dữ liệu (Bắt buộc phải có [System.Serializable] và biến public)
    [System.Serializable]
    public class LoginRequestModel
    {
        public string Username;
        public string Password;
    }

    void Start()
    {
        StartCoroutine(LoginTest());
    }

    IEnumerator LoginTest()
    {
        // Đảm bảo URL này khớp chính xác với Port đang chạy trên Server của bạn
        string url = "http://127.0.0.1:5113/api/Auth/login";
        
        Debug.Log("Đang gửi Request tới: " + url);

        // 2. Khởi tạo dữ liệu người dùng
        LoginRequestModel loginData = new LoginRequestModel 
        { 
            Username = "aaa", 
            Password = "aaa" 
        };

        // 3. Chuyển đổi dữ liệu thành chuỗi JSON và mảng byte
        string jsonBody = JsonUtility.ToJson(loginData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            // 4. Cấu hình UploadHandler (Gắn cứng định dạng JSON để trị lỗi 415)
            UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw);
            uploadHandler.contentType = "application/json"; 
            req.uploadHandler = uploadHandler;
            
            // 5. Cấu hình DownloadHandler để nhận dữ liệu Server trả về
            req.downloadHandler = new DownloadHandlerBuffer();      
            
            // Bỏ qua cảnh báo chứng chỉ SSL khi test ở localhost
            req.certificateHandler = new BypassCertificate();

            // Gửi đi và đợi phản hồi
            yield return req.SendWebRequest();

            // 6. Xử lý kết quả trả về
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=green>THÀNH CÔNG! Unity đã đăng nhập qua Server!</color>");
                Debug.Log("<color=cyan>Dữ liệu JSON từ Server: " + req.downloadHandler.text + "</color>");
            }
            else
            {
                Debug.LogError("<color=red>LỖI KẾT NỐI: " + req.error + "</color>");
                Debug.LogError("Mã lỗi (Response Code): " + req.responseCode);
                
                // In thêm chi tiết lỗi từ Server (nếu Server có trả về text giải thích lỗi)
                if (req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text))
                {
                    Debug.LogError("Chi tiết lỗi từ Server: " + req.downloadHandler.text);
                }
            }
        }
    }

    // Class giúp Unity chấp nhận mọi chứng chỉ (Rất hữu ích khi test localhost có HTTPS)
    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}