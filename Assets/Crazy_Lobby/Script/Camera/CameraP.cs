using UnityEngine;
using System.Collections.Generic;
using Fusion;
using Crazy_Lobby.Enemy; // 👉 Thêm dòng này để Camera nhận diện được EnemyAI

public class CameraP : MonoBehaviour
{
    public UnityEngine.Camera TargetCamera;
    public Vector3 Offset = new Vector3(0, 4, -10);
    public float Distance = 10f;
    public float Sensitivity = 2f;
    public Vector2 PitchLimits = new Vector2(-30, 60);

    public string PlayerTag = "Player";

    private Transform _currentTarget;
    private Transform _localPlayer;
    private List<Transform> _spectatablePlayers = new List<Transform>();
    private bool _isSpectating = false;
    private int _spectatorIndex = -1;
    
    private float _yaw;
    private float _pitch;
    private bool _isReversedView = false;
    public bool IsTargetLocked = false; 

    public float CurrentYaw => _yaw;

    public void SetYawPitch(float yaw, float pitch)
    {
        _yaw = yaw;
        _pitch = pitch > 180f ? pitch - 360f : pitch;
    }

    private void Start()
    {
        if (TargetCamera == null)
        {
            TargetCamera = GetComponent<Camera>();
            if (TargetCamera == null) TargetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (_currentTarget == null)
        {
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag(PlayerTag);
            foreach (var p in playerObjects)
            {
                var networkObj = p.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj.HasInputAuthority)
                {
                    SetLocalPlayer(p.transform);
                    break; 
                }
            }
        }

        if (_isSpectating)
        {
            // Bấm chuột trái để chuyển góc nhìn
            if (Input.GetMouseButtonDown(0))
            {
                SwitchToNextSpectatorTarget();
            }
            else if (_currentTarget != null)
            {
                // 👉 Tự động nhảy sang mục tiêu khác nếu người mình đang xem BỊ CHẾT hoặc VỪA VỀ ĐÍCH
                var targetPC = _currentTarget.GetComponent<PlayerController>();
                if (targetPC != null && (targetPC.IsDead || targetPC.HasFinished))
                {
                    SwitchToNextSpectatorTarget();
                }
                else
                {
                    var targetEnemy = _currentTarget.GetComponent<EnemyAI>();
                    if (targetEnemy != null && (targetEnemy.IsDead || targetEnemy.HasFinished))
                    {
                        SwitchToNextSpectatorTarget();
                    }
                }
            }
        }

        if (_currentTarget != null && TargetCamera != null)
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                _isReversedView = !_isReversedView;
            }

            if (!IsTargetLocked)
            {
                float mouseX = Input.GetAxis("Mouse X") * Sensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * Sensitivity;

                _yaw += mouseX;
                _pitch -= mouseY;
                
                _pitch = Mathf.Clamp(_pitch, PitchLimits.x, PitchLimits.y);
            }

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);

            Vector3 targetPos = _currentTarget.position + Vector3.up * Offset.y;

            if (_isReversedView)
            {
                Vector3 position = targetPos + (rotation * Vector3.forward * Distance);
                TargetCamera.transform.rotation = rotation * Quaternion.Euler(0, 180, 0);
                TargetCamera.transform.position = position;
            }
            else
            {
                Vector3 position = targetPos - (rotation * Vector3.forward * Distance);
                TargetCamera.transform.rotation = rotation;
                TargetCamera.transform.position = position;
            }
        }
    }

    public void SetLocalPlayer(Transform playerTransform)
    {
        _localPlayer = playerTransform;
        _currentTarget = _localPlayer; 
        StopSpectating();

        _isReversedView = false;
        _yaw = playerTransform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnPlayerDied()
    {
        _isSpectating = true;
        RefreshSpectatorList();
        SwitchToNextSpectatorTarget();
    }

    // 👉 HÀM MỚI: ĐƯỢC GỌI KHI NHÂN VẬT VỀ ĐÍCH
    public void OnPlayerFinished()
    {
        _isSpectating = true;
        RefreshSpectatorList();
        SwitchToNextSpectatorTarget();
    }

    
    public void OnPlayerRespawned()
    {
        StopSpectating();
    }

    private void StopSpectating()
    {
        _isSpectating = false;
        if (_localPlayer != null)
        {
            _currentTarget = _localPlayer;
        }
    }

    private void RefreshSpectatorList()
    {
        _spectatablePlayers.Clear();
        
        // 1. Quét tìm những Người chơi khác (chưa chết và chưa về đích)
        foreach (var p in PlayerController.ActivePlayers)
        {
            if (p != null && p.transform != _localPlayer && !p.IsDead && !p.HasFinished)
            {
                _spectatablePlayers.Add(p.transform);
            }
        }

        // 2. Quét tìm những Quái vật (chưa chết và chưa về đích)
        foreach (var e in EnemyAI.ActiveEnemies)
        {
            if (e != null && !e.IsDead && !e.HasFinished)
            {
                _spectatablePlayers.Add(e.transform);
            }
        }
    }

    private void SwitchToNextSpectatorTarget()
    {
        if (_spectatablePlayers.Count == 0)
        {
            RefreshSpectatorList();
            // Nếu quét xong vẫn không có ai (tất cả đều đã chết hoặc về đích), thì giữ nguyên camera
            if (_spectatablePlayers.Count == 0) return; 
        }

        _spectatorIndex = (_spectatorIndex + 1) % _spectatablePlayers.Count;
        var target = _spectatablePlayers[_spectatorIndex];

        if (target != null)
        {
            _currentTarget = target;
        }
        else
        {
            _spectatablePlayers.RemoveAt(_spectatorIndex);
            SwitchToNextSpectatorTarget();
        }
    }
}