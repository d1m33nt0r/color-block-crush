using System;
using UnityEngine;

namespace Game.Scripts.Data.GameData.Levels.Data
{
    [Serializable]
    public class LevelData
    {
        public TextAsset BlocksData => _blocksData;
        public TextAsset CannonsData => _cannonsData;
        
        [SerializeField] private TextAsset _blocksData;
        [SerializeField] private TextAsset _cannonsData;
    }
}