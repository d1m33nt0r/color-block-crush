using System;
using System.Collections.Generic;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Grid.Shared;

namespace Game.BlockCrush.Level.Shared
{
    [Serializable]
    public sealed class CannonLevelData
    {
        public string id = "Cannons 1";
        public int width = 5;
        public int height = 4;
        public int slotCount = 3;
        public List<CannonSpawnData> cannons = new List<CannonSpawnData>();
    }

    [Serializable]
    public sealed class CannonSpawnData
    {
        public const float DefaultShootRate = 12f;

        public int x;
        public int y;
        public BlockColorKey color;
        public int shots = 1;
        public float shootRate = DefaultShootRate;

        public GridPosition Position
        {
            get { return new GridPosition(x, y); }
        }
    }
}
