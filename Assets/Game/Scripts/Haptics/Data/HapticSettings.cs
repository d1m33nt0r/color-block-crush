using System;
using UnityEngine;

namespace Game.Scripts.Haptics.Data
{
    [Serializable]
    public sealed class HapticSettings
    {
        [SerializeField] private bool _isEnabled = true;

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }
    }
}
