using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Core.Common.Sound.Data
{
    [Serializable]
    public class SoundData
    {
        public SoundPlayerParams SoundPlayerParams => _soundPlayerParams;
        public List<AudioClip> SoundList => _soundList;
        
        [SerializeField] private SoundPlayerParams _soundPlayerParams;
        [SerializeField] private List<AudioClip> _soundList;
        
        public AudioClip GetRandomSound()
        {
            if (_soundList == null || _soundList.Count == 0)
            {
                return null;
            }

            return _soundList[UnityEngine.Random.Range(0, _soundList.Count)];
        }
    }
}
