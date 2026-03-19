using Fusion;
using UnityEngine;
using TMPro;

namespace Crazy_Lobby.UI
{
    public class PlayerNameUI : NetworkBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Kéo Component TextMeshProUGUI từ Canvas vào đây để hiển thị tên")]
        public TextMeshProUGUI playerNameText;

        // Biến đồng bộ mạng để lưu tên người chơi (tối đa 32 ký tự)
        [Networked] 
        public NetworkString<_32> PlayerName { get; set; }

        public override void Spawned()
        {
            // Chỉ client sở hữu nhân vật này (hoặc Server/Host) mới được phép gọi API lấy tên
            if (HasInputAuthority || HasStateAuthority)
            {
                FetchAndDisplayPlayerName();
            }
        }

        public void FetchAndDisplayPlayerName()
        {
            if (playerNameText != null) playerNameText.text = "Đang tải tên...";

            // Gọi Backend để lấy tên (sử dụng Token cục bộ của người chơi)
            BackendManager.Instance.GetUserProfile((isSuccess, resultString) =>
            {
                if (playerNameText == null) return;

                if (isSuccess)
                {
                    // Khi lấy được tên thành công, gửi RPC lên Server để gán tên này cho mọi người cùng thấy
                    RpcSetPlayerName(resultString);
                }
                else
                {
                    playerNameText.text = resultString; // Hiển thị thông báo lỗi nếu có
                }
            });
        }

        [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
        public void RpcSetPlayerName(string name)
        {
            PlayerName = name; // Server lưu tên và biến [Networked] sẽ tự động đồng bộ xuống tất cả các Client khác
        }

        // Hàm Render chạy ở Update thông thường trên mọi Client
        public override void Render()
        {
            if (playerNameText == null) return;
            
            string networkedName = PlayerName.ToString();
            // Chỉ cập nhật UI Text nếu tên đã có dữ liệu và nội dung thay đổi (giúp tối ưu hiệu suất, tránh giật lag UI)
            if (!string.IsNullOrEmpty(networkedName) && playerNameText.text != networkedName)
            {
                playerNameText.text = networkedName;
            }
        }
    }
}