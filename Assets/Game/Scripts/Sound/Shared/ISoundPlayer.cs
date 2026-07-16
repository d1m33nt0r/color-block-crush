using Game.Scripts.Core.Common.Sound.Data;
using UnityEngine;

namespace Game.Scripts.Core.Common.Sound.Shared
{
    public interface ISoundPlayer
    {
        void Initialize(SoundKey soundKey, Transform parent = null);
        void SetVolume(float volume);
        void SetSound(SoundKey soundKey);
        void Play();
        void Stop();
        void Mute();
        void Unmute();
    }
}