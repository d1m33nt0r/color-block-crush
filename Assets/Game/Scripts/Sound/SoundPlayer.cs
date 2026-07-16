using Game.Scripts.Core.Common.Sound.Data;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Core.Common.Sound.Shared
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour, ISoundPlayer
    {
        [Inject] protected ISoundDatabase SoundDatabase;
        
        private AudioSource source;
        private float maxVolume;
        private SoundData soundData;
        private bool isMuted;
        
        public void Initialize(SoundKey soundKey, Transform parent = null)
        {
            if (parent != null)
            {
                transform.SetParent(parent, false);
                transform.localPosition = Vector3.zero;
            }
            
            source = GetComponent<AudioSource>();
            SetSound(soundKey);
        }

        public void SetSound(SoundKey soundKey)
        {
            if (SoundDatabase == null
                || soundKey == SoundKey.None
                || !SoundDatabase.TryGetSoundData(soundKey, out var resolvedSoundData)
                || resolvedSoundData == null)
            {
                soundData = null;
                return;
            }

            soundData = resolvedSoundData;
            
            source.loop = soundData.SoundPlayerParams.IsLoop;
            source.playOnAwake = soundData.SoundPlayerParams.PlayOnAwake;
            source.volume = soundData.SoundPlayerParams.Volume;
            maxVolume = soundData.SoundPlayerParams.Volume;
        }
        
        public void SetVolume(float volume)
        {
            source.volume = maxVolume * Mathf.Clamp01(volume);
        }

        private int _index = 0;
        private int _maxIndex => soundData.SoundList.Count - 1;
        
        public void Play()
        {
            if (soundData == null
                || soundData.SoundList == null
                || soundData.SoundList.Count == 0)
            {
                return;
            }

            if (soundData.SoundPlayerParams.ConsistentMode)
            {
                if (_index > _maxIndex)
                {
                    _index = 0;
                }

                source.clip = soundData.SoundList[_index];
                _index++;
                if (_index > _maxIndex) _index = 0;
            }
            else
            {
                source.clip = soundData.GetRandomSound();
            }

            if (source.clip == null)
            {
                return;
            }
            
            source.Play();
        }
        
        public void Stop()
        {
            source.Stop();
        }

        public void Mute()
        {
            source.mute = true;
        }

        public void Unmute()
        {
            source.mute = false;
        }
    }
}
