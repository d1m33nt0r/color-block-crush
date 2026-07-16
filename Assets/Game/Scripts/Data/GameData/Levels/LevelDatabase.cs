using System.Collections.Generic;
using Game.Scripts.Data.GameData.Levels.Data;
using Game.Scripts.Data.GameData.Levels.Shared;
using UnityEngine;

namespace Game.Scripts.Data.GameData.Levels
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject, ILevelDatabase
    {
        public IReadOnlyList<LevelData> Levels => _levels;
        
        [SerializeField] private List<LevelData> _levels;
    }
}