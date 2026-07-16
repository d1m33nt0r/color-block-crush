using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.BlockCrush.Block;
using Game.BlockCrush.Shared;
using UnityEngine;
using Game.BlockCrush.Grid.Shared;

namespace Game.BlockCrush.Grid
{
    public sealed class GridCellStackAnimator : MonoBehaviour
    {
        [SerializeField] private AnimationSettings _settingsOverride;

        public async UniTask ArrangeAsync(
            IReadOnlyList<ColorBlock> blocks,
            AnimationSettings fallbackSettings,
            GridCellArrangeMode mode,
            int travelledCells,
            CancellationToken cancellationToken = default)
        {
            AnimationSettings settings = _settingsOverride != null ? _settingsOverride : fallbackSettings;
            List<UniTask> moveTasks = null;
            float stackOffset = 0f;

            for (int i = 0; i < blocks.Count; i++)
            {
                ColorBlock block = blocks[i];
                if (block == null)
                {
                    continue;
                }

                float blockHeight = block.Height;
                Vector3 targetLocalPosition = new Vector3(0f, stackOffset + blockHeight * 0.5f, 0f);
                stackOffset += blockHeight;

                block.transform.DOKill(false);

                if (mode == GridCellArrangeMode.Immediate)
                {
                    block.transform.localPosition = targetLocalPosition;
                    continue;
                }

                float duration = GetDuration(settings, mode, travelledCells);
                if (duration <= 0f)
                {
                    block.transform.localPosition = targetLocalPosition;
                    continue;
                }

                Ease ease = GetEase(settings, mode);
                Tween tween = block.transform
                    .DOLocalMove(targetLocalPosition, duration)
                    .SetEase(ease)
                    .SetLink(block.gameObject);

                if (moveTasks == null)
                {
                    moveTasks = new List<UniTask>();
                }

                moveTasks.Add(TweenUniTaskBridge.AwaitCompletionAsync(tween, cancellationToken));
            }

            if (moveTasks != null && moveTasks.Count > 0)
            {
                await UniTask.WhenAll(moveTasks);
            }
        }

        private static float GetDuration(AnimationSettings settings, GridCellArrangeMode mode, int travelledCells)
        {
            if (settings == null)
            {
                return mode == GridCellArrangeMode.GridFall ? 0.18f : 0.12f;
            }

            return mode == GridCellArrangeMode.GridFall
                ? settings.GetGridFallDuration(travelledCells)
                : settings.stackReflowDuration;
        }

        private static Ease GetEase(AnimationSettings settings, GridCellArrangeMode mode)
        {
            if (settings == null)
            {
                return mode == GridCellArrangeMode.GridFall ? Ease.OutCubic : Ease.OutBack;
            }

            return mode == GridCellArrangeMode.GridFall
                ? settings.cellMoveEase
                : settings.stackReflowEase;
        }
    }
}
