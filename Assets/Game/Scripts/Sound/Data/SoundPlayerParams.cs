using System;
using UnityEngine;

namespace Game.Scripts.Core.Common.Sound.Data
{
    [Serializable]
    public class SoundPlayerParams
    {
        public bool IsLoop => _isLoop;
        public float Volume => _volume;
        public bool PlayOnAwake => _playOnAwake;
        public bool ConsistentMode => _consistentMode;
        
        [SerializeField] private bool _isLoop = false;
        [SerializeField] private bool _consistentMode = false;
        [SerializeField] private float _volume = 1f;
        [SerializeField] private bool _playOnAwake = false;
    }
}