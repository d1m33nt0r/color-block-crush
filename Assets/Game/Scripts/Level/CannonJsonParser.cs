using System;
using Game.BlockCrush.Shared;
using UnityEngine;
using Game.BlockCrush.Level.Shared;

namespace Game.BlockCrush.Level
{
    public sealed class CannonJsonParser
    {
        public CannonLevelData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Cannon level JSON is empty.", nameof(json));
            }

            CannonLevelData levelData = JsonUtility.FromJson<CannonLevelData>(json);
            if (levelData == null)
            {
                throw new ArgumentException("Cannon level JSON could not be parsed.", nameof(json));
            }

            if (levelData.cannons == null)
            {
                levelData.cannons = new System.Collections.Generic.List<CannonSpawnData>();
            }

            levelData.width = Mathf.Max(1, levelData.width);
            levelData.height = Mathf.Max(1, levelData.height);
            levelData.slotCount = Mathf.Max(0, levelData.slotCount);

            for (int i = 0; i < levelData.cannons.Count; i++)
            {
                levelData.cannons[i].shots = Mathf.Max(0, levelData.cannons[i].shots);
                if (levelData.cannons[i].shootRate <= 0f)
                {
                    levelData.cannons[i].shootRate = CannonSpawnData.DefaultShootRate;
                }
            }

            return levelData;
        }

        public string ToJson(CannonLevelData levelData, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(levelData, prettyPrint);
        }
    }
}
