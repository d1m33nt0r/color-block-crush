using System.Collections.Generic;
using Game.Scripts.Core.Common.Sound.Shared;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Scripts.Core.Common.Sound.Data
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "SoundDatabase")]
    public class SoundDatabase : SerializedScriptableObject, ISoundDatabase
    {
        [SerializeField] private Dictionary<SoundKey, SoundData> soundsData;
        
        public bool TryGetSoundData(SoundKey key, out SoundData soundData)
        {
            soundData = null;
    
            if (!soundsData.TryGetValue(key, out var res))
            {
                Debug.LogError($"Does not contain sound key: {key}");
                return false;
            }

            soundData = res;
            return true;
        }
    }
}