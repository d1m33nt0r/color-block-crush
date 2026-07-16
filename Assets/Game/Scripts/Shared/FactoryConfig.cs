using Game.BlockCrush.Block;
using Game.BlockCrush.Cannon;
using Game.BlockCrush.Effects;
using Game.BlockCrush.Grid;
using Game.BlockCrush.Shared;
using UnityEngine;

namespace Game.Scripts.Shared
{
    public sealed class FactoryConfig
    {
        public FactoryConfig(
            BlockCell cellPrefab,
            ColorBlock defaultBlockPrefab,
            CannonGridCell cannonCellPrefab,
            CannonSlot cannonSlotPrefab,
            ColorCannon defaultCannonPrefab,
            ColorBullet defaultBulletPrefab,
            BlockDestroyEffect blockDestroyEffectPrefab,
            Transform runtimeRoot,
            AnimationSettings animationSettings)
        {
            CellPrefab = cellPrefab;
            DefaultBlockPrefab = defaultBlockPrefab;
            CannonCellPrefab = cannonCellPrefab;
            CannonSlotPrefab = cannonSlotPrefab;
            DefaultCannonPrefab = defaultCannonPrefab;
            DefaultBulletPrefab = defaultBulletPrefab;
            BlockDestroyEffectPrefab = blockDestroyEffectPrefab;
            RuntimeRoot = runtimeRoot;
            AnimationSettings = animationSettings;
        }

        public BlockCell CellPrefab { get; private set; }
        public ColorBlock DefaultBlockPrefab { get; private set; }
        public CannonGridCell CannonCellPrefab { get; private set; }
        public CannonSlot CannonSlotPrefab { get; private set; }
        public ColorCannon DefaultCannonPrefab { get; private set; }
        public ColorBullet DefaultBulletPrefab { get; private set; }
        public BlockDestroyEffect BlockDestroyEffectPrefab { get; private set; }
        public Transform RuntimeRoot { get; private set; }
        public AnimationSettings AnimationSettings { get; private set; }
    }
}
