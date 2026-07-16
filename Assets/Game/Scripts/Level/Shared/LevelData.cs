using System;
using System.Collections.Generic;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Grid.Shared;

namespace Game.BlockCrush.Level.Shared
{
    [Serializable]
    public sealed class LevelData
    {
        public string id = "Level 1";
        public int width = 6;
        public int height = 8;
        public List<BlockSpawnData> blocks = new List<BlockSpawnData>();
    }

    [Serializable]
    public sealed class BlockSpawnData
    {
        public int x;
        public int y;
        public int layer;
        public BlockColorKey color;

        public GridPosition Position
        {
            get { return new GridPosition(x, y); }
        }
    }
}
