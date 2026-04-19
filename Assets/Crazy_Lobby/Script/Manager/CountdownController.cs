using Fusion;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CountdownController : NetworkBehaviour
{
    // 👉 Biến tĩnh toàn cục để khóa/mở di chuyển
    public static bool IsGameStarted = false; 
    
    // 👉 Singleton để các script khác dễ dàng gọi hàm tăng số người về đích
    public static CountdownController Instance;

    [Header("UI Cấu hình Đếm Ngược")]
    public Image countdownImage; 
    public Sprite sprite3;       
    public Sprite sprite2;       
    public Sprite sprite1;       
    public Sprite spriteGo;      

    [Header("UI Cấu hình Trong Game (Thời gian & Về đích)")]
    public TextMeshProUGUI timerText;             
    public TextMeshProUGUI finishCountText;       
    public int totalPlayersToFinish = 5;          
    public float levelDuration = 120f;            

    [Header("UI Cấu hình Kết Quả")]
    public GameObject resultPanel;
    public Image resultImage;         
    public Sprite victorySprite;     
    public Sprite completeSprite;        
    public Sprite failSprite;        

    [Header("Âm thanh")]
    public AudioSource mysfx;
    public AudioClip startsfx;
    public AudioClip gosfx;

    // --- BIẾN MẠNG ĐỂ ĐỒNG BỘ THỜI GIAN VÀ TIẾN ĐỘ ---
    [Networked] private TickTimer CountdownTimer { get; set; }
    [Networked] private int CurrentCount { get; set; }
    
    [Networked] public float TimeRemaining { get; set; }  // 👉 Thời gian CÒN LẠI
    [Networked] public int FinishedCount { get; set; }  // Số người đã về đích

    private int lastPlayedCount = -1;

    private void Awake()
    {
        // Gán Instance để script khác có thể gọi CountdownController.Instance...
        if (Instance == null) Instance = this;
    }

    public override void Spawned()
    {
        IsGameStarted = false; 
        countdownImage.gameObject.SetActive(false);

        if (HasStateAuthority)
        {
            CurrentCount = 4; 
            CountdownTimer = TickTimer.CreateFromSeconds(Runner, 1f);
            
            TimeRemaining = levelDuration; // 👉 Gán thời gian ban đầu bằng thời lượng màn chơi
            FinishedCount = 0;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 1. LOGIC ĐẾM NGƯỢC ĐẦU GAME
        if (HasStateAuthority && CountdownTimer.Expired(Runner))
        {
            if (CurrentCount > 0)
            {
                CurrentCount--;
                CountdownTimer = TickTimer.CreateFromSeconds(Runner, 1f);
            }
        }

        if (CurrentCount != lastPlayedCount)
        {
            lastPlayedCount = CurrentCount;
            UpdateCountdownUI(CurrentCount);
        }

        // 2. LOGIC THỜI GIAN VÀ UI TRONG GAME (Chạy sau khi có chữ GO)
        if (IsGameStarted)
        {
            // Server làm nhiệm vụ trừ dần thời gian
            if (HasStateAuthority && TimeRemaining > 0)
            {
                TimeRemaining -= Runner.DeltaTime; // 👉 Trừ dần thời gian

                // Khi hết giờ
                if (TimeRemaining <= 0)
                {
                    TimeRemaining = 0;
                    Debug.Log("HẾT GIỜ! Game Over!");
                    IsGameStarted = false; // Khóa di chuyển toàn map
                    
                    RPC_OnGameEnd(false); // Kết thúc do hết giờ (Fail)
                }
            }

            // Mọi Client đều cập nhật UI liên tục
            UpdateInGameUI();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnGameEnd(bool slotsFilled)
    {
        bool isLocalFinished = false;
        foreach (var p in PlayerController.ActivePlayers)
        {
            if (p.HasInputAuthority && p.HasFinished)
            {
                isLocalFinished = true;
                break;
            }
        }

        if (!isLocalFinished)
        {
            // Nếu chưa về đích:
            // - Nếu slots full -> COMPLETE (Hoặc Eliminated)
            // - Nếu hết giờ -> FAIL
            StartCoroutine(ShowResultRoutine(slotsFilled ? GameResult.Complete : GameResult.Fail));
        }
    }

    private enum GameResult { Victory, Complete, Fail }

    private IEnumerator ShowResultRoutine(GameResult result)
    {
        if (resultPanel != null && resultImage != null)
        {
            switch (result)
            {
                case GameResult.Victory:
                    resultImage.sprite = victorySprite;
                    break;
                case GameResult.Complete:
                    resultImage.sprite = completeSprite;
                    break;
                case GameResult.Fail:
                    resultImage.sprite = failSprite;
                    break;
            }
            resultPanel.SetActive(true);
        }

        yield return new WaitForSeconds(3f);

        if (resultPanel != null) resultPanel.SetActive(false);

        if (result == GameResult.Complete)
        {
            var camera = FindObjectOfType<CameraP>();
            if (camera != null) camera.OnPlayerDied();
        }
        else if (result == GameResult.Victory || result == GameResult.Fail)
        {
            // TẢI SANG SCENE TIẾP THEO nếu là server 
            // (Áp dụng cho trường hợp Thắng hoặc Thua do hết thời gian)
            if (Runner.IsServer)
            {
                int nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
                Runner.LoadScene(SceneRef.FromIndex(nextSceneIndex));
            }
        }
    }

    private void UpdateCountdownUI(int count)
    {
        // Chỉ hiển thị UI đếm ngược khi số nằm trong khoảng từ 3 đến 0
        if (count > 3 || count < 0)
        {
            countdownImage.gameObject.SetActive(false);
            return;
        }

        countdownImage.gameObject.SetActive(true);

        switch (count)
        {
            case 3:
                countdownImage.sprite = sprite3;
                if (mysfx != null && startsfx != null) mysfx.PlayOneShot(startsfx);
                break;
            case 2:
                countdownImage.sprite = sprite2;
                if (mysfx != null && startsfx != null) mysfx.PlayOneShot(startsfx);
                break;
            case 1:
                countdownImage.sprite = sprite1;
                if (mysfx != null && startsfx != null) mysfx.PlayOneShot(startsfx);
                break;
            case 0:
                countdownImage.sprite = spriteGo;
                if (mysfx != null && gosfx != null) mysfx.PlayOneShot(gosfx);
                
                IsGameStarted = true; 
                StartCoroutine(HideUIAfterDelay()); 
                break;
        }
    }

    private void UpdateInGameUI()
    {
        // Cập nhật Text thời gian đếm ngược (Định dạng Phút:Giây)
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(TimeRemaining / 60F);
            int seconds = Mathf.FloorToInt(TimeRemaining % 60F);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // Cập nhật Text số người về đích
        if (finishCountText != null)
        {
            finishCountText.text = $"{FinishedCount}/{totalPlayersToFinish}";
        }

        // 👉 HIỆN TRẠNG THÁI COMPLETE / FAIL DƯỚI DẠNG ẢNH
        UpdateResultUI();
    }


    private IEnumerator HideUIAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        countdownImage.gameObject.SetActive(false);
    }

    // 👉 HÀM DÀNH CHO CÁI VẠCH ĐÍCH (FINISH LINE) GỌI VÀO
    public void AddFinishedPlayer()
    {
        if (HasStateAuthority)
        {
            FinishedCount++;
            Debug.Log($"Đã có {FinishedCount} người về đích!");

            // Nếu đã đạt số lượng cần thiết → Kết thúc game (Lưu ý: Game kết thúc cho tất cả những người chưa về đích)
            if (FinishedCount >= totalPlayersToFinish)
            {
                Debug.Log("Tất cả chỗ đã được lấp đầy! Hết Game!");
                IsGameStarted = false; 
                RPC_OnGameEnd(true); // Kết thúc do hết chỗ
            }
        }
    }

    public void TriggerLocalVictorySequence()
    {
        StartCoroutine(ShowResultRoutine(GameResult.Victory));
    }
    private void UpdateResultUI()
    {
        // Không còn cần thiết vì logic hiển thị đã chuyển sang ShowResultRoutine
    }

}