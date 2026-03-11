using UnityEngine;
using System.Collections.Generic;

public class Camera : MonoBehaviour
{
    public UnityEngine.Camera TargetCamera;
    public Vector3 Offset = new Vector3(0, 8, -10);
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

    private void Start()
    {
        if (TargetCamera == null)
        {
            TargetCamera = GetComponent<UnityEngine.Camera>();
            if (TargetCamera == null) TargetCamera = UnityEngine.Camera.main;
        }

        // Khóa chuột vào giữa màn hình để xoay camera
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        // Dành cho việc test offline: nếu chưa có mục tiêu, thử tìm người chơi.
        if (_currentTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
            if (playerObject != null)
            {
                SetLocalPlayer(playerObject.transform);
            }
        }

        if (_isSpectating && Input.GetMouseButtonDown(0))
        {
            SwitchToNextSpectatorTarget();
        }

        if (_currentTarget != null && TargetCamera != null)
        {
            // Xử lý input chuột để xoay camera
            _yaw += Input.GetAxis("Mouse X") * Sensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * Sensitivity;
            _pitch = Mathf.Clamp(_pitch, PitchLimits.x, PitchLimits.y);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);

            // Tính toán vị trí: Target + Chiều cao (Offset.y) - Hướng nhìn * Khoảng cách
            Vector3 targetPos = _currentTarget.position + Vector3.up * Offset.y;
            Vector3 position = targetPos - (rotation * Vector3.forward * Distance);

            TargetCamera.transform.rotation = rotation;
            TargetCamera.transform.position = position;
        }
    }


    public void SetLocalPlayer(Transform playerTransform)
    {
        _localPlayer = playerTransform;
        _currentTarget = _localPlayer; 
        StopSpectating();
    }

    public void OnPlayerDied()
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
        GameObject[] players = GameObject.FindGameObjectsWithTag(PlayerTag);
        
        foreach (var p in players)
        {
            // Add all players except the local one
            if (p.transform != _localPlayer)
            {
                _spectatablePlayers.Add(p.transform);
            }
        }
    }

    private void SwitchToNextSpectatorTarget()
    {
        if (_spectatablePlayers.Count == 0)
        {
            RefreshSpectatorList();
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
