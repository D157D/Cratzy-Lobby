using UnityEngine;
using Fusion; // Để dùng NetworkRunner

namespace Crazy_Lobby.Player.Components
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayerAudio : MonoBehaviour
    {
        [Header("Audio Components")]
        public AudioSource audioSource;

        [Header("Audio Clips")]
        public AudioClip jumpSound;
        public AudioClip shootSound;
        public AudioClip deathSound;
        public AudioClip bounceSound;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        // Truyền Runner.IsForward vào để tránh lỗi lặp âm thanh do mạng (resimulation)
        public void PlayJump(bool isForward)
        {
            if (isForward && jumpSound != null) audioSource.PlayOneShot(jumpSound);
        }

        public void PlayShoot(bool isForward)
        {
            if (isForward && shootSound != null) audioSource.PlayOneShot(shootSound);
        }

        public void PlayDeath()
        {
            if (deathSound != null) audioSource.PlayOneShot(deathSound);
        }

        public void PlayBounce()
        {
            if (bounceSound != null) audioSource.PlayOneShot(bounceSound);
        }
    }
}