using DG.Tweening;
using UnityEngine;

namespace Game.BlockCrush.Shared
{
    [CreateAssetMenu(fileName = "AnimationSettings", menuName = "Game/Block Crush/Animation Settings")]
    public sealed class AnimationSettings : ScriptableObject
    {
        [Header("Grid Fall")]
        [Min(0f)] public float cellMoveDuration = 0.18f;
        [Min(0f)] public float extraDurationPerCell = 0.035f;
        public Ease cellMoveEase = Ease.OutCubic;

        [Header("Cell Stack Reflow")]
        [Min(0f)] public float stackReflowDuration = 0.12f;
        public Ease stackReflowEase = Ease.OutBack;

        [Header("Destroy")]
        [Min(0f)] public float destroyDuration = 0.2f;
        [Range(0f, 1f)] public float destroyScaleMultiplier = 0.05f;
        public Ease destroyEase = Ease.InBack;

        [Header("Neighbor Shake")]
        [Min(0f)] public float neighborShakeDuration = 0.14f;
        [Min(0f)] public float neighborShakeHorizontalStrength = 0.04f;
        [Min(0f)] public float neighborShakeVerticalStrength = 0.01f;
        [Min(1)] public int neighborShakeVibrato = 6;
        public Ease neighborShakeEase = Ease.OutQuad;

        [Header("Cannon Move")]
        [Min(0f)] public float cannonGridMoveDuration = 0.18f;
        [Min(0f)] public float cannonExtraDurationPerCell = 0.035f;
        public Ease cannonGridMoveEase = Ease.OutCubic;
        [Min(0f)] public float cannonSlotMoveDuration = 0.2f;
        public Ease cannonSlotMoveEase = Ease.OutBack;

        [Header("Cannon Shoot")]
        [Min(0f)] public float cannonShootDuration = 0.1f;
        [Min(1f)] public float cannonShootPunchScale = 1.12f;
        public Ease cannonShootEase = Ease.OutQuad;
        [Min(0f)] public float cannonAimDuration = 0.14f;
        public Ease cannonAimEase = Ease.OutCubic;

        [Header("Cannon Disappear")]
        [Min(0f)] public float cannonDisappearDuration = 0.18f;
        [Range(0f, 1f)] public float cannonDisappearScaleMultiplier = 0.05f;
        public Ease cannonDisappearEase = Ease.InBack;

        [Header("Bullet")]
        [Min(0f)] public float bulletFlightDuration = 0.22f;
        public Ease bulletFlightEase = Ease.InQuad;
        [Min(0.01f)] public float bulletScale = 0.25f;

        public float GetGridFallDuration(int travelledCells)
        {
            int extraCells = Mathf.Max(0, travelledCells - 1);
            return Mathf.Max(0f, cellMoveDuration + extraDurationPerCell * extraCells);
        }

        public float GetCannonGridMoveDuration(int travelledCells)
        {
            int extraCells = Mathf.Max(0, travelledCells - 1);
            return Mathf.Max(0f, cannonGridMoveDuration + cannonExtraDurationPerCell * extraCells);
        }
    }
}
