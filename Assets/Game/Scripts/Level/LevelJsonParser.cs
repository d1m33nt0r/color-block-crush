using System;
using Game.BlockCrush.Shared;
using UnityEngine;
using Game.BlockCrush.Level.Shared;

namespace Game.BlockCrush.Level
{
    public sealed class LevelJsonParser
    {
        public LevelData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Level JSON is empty.", nameof(json));
            }

            LevelData levelData = JsonUtility.FromJson<LevelData>(json);
            if (levelData == null)
            {
                throw new ArgumentException("Level JSON could not be parsed.", nameof(json));
            }

            if (levelData.blocks == null)
            {
                levelData.blocks = new System.Collections.Generic.List<BlockSpawnData>();
            }

            levelData.width = Mathf.Max(1, levelData.width);
            levelData.height = Mathf.Max(1, levelData.height);

            return levelData;
        }

        public string ToJson(LevelData levelData, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(levelData, prettyPrint);
        }
    }
}
