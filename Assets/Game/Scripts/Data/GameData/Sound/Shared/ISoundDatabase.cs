using System.Collections.Generic;
using Game.Scripts.Core.Common.Sound.Data;
using UnityEngine;

namespace Game.Scripts.Core.Common.Sound.Shared
{
    public interface ISoundDatabase
    {
        bool TryGetSoundData(SoundKey key, out SoundData soundData);
    }
}