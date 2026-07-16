using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.BlockCrush.Shared;
using UnityEngine;
using Game.BlockCrush.Cannon.Shared;

namespace Game.BlockCrush.Cannon
{
    public sealed class ColorCannonAnimator : MonoBehaviour
    {
        [SerializeField] private AnimationSettings _settingsOverride;
        [SerializeField] private Transform _rotationRoot;

        private Vector3 _initialScale;
        private Quaternion _initialLocalRotation;

        private void Awake()
        {
            CaptureInitialScale();
        }

        public void CaptureInitialScale()
        {
            _initialScale = transform.localScale;
            _initialLocalRotation = RotationRoot.localRotation;
        }

        public void CaptureInitialScaleOnly()
        {
            _initialScale = transform.localScale;
        }

        public async UniTask MoveLocalAsync(
            Vector3 targetLocalPosition,
            CannonArrangeMode mode,
            int travelledCells,
            AnimationSettings fallbackSettings,
            CancellationToken cancellationToken = default)
        {
            AnimationSettings settings = _settingsOverride != null ? _settingsOverride : fallbackSettings;
            transform.DOKill(false);

            if (mode == CannonArrangeMode.Immediate)
            {
                transform.localPosition = targetLocalPosition;
                return;
            }

            float duration = GetMoveDuration(settings, mode, travelledCells);
            if (duration <= 0f)
            {
                transform.localPosition = targetLocalPosition;
                return;
            }

            Ease ease = mode == CannonArrangeMode.SlotMove
                ? GetSlotEase(settings)
                : GetGridEase(settings);

            Tween tween = transform
                .DOLocalMove(targetLocalPosition, duration)
                .SetEase(ease)
                .SetLink(gameObject);

            await TweenUniTaskBridge.AwaitCompletionAsync(tween, cancellationToken);
        }

        public async UniTask PlayShootAsync(AnimationSettings fallbackSettings, CancellationToken cancellationToken = default)
        {
            AnimationSettings settings = _settingsOverride != null ? _settingsOverride : fallbackSettings;
            float duration = settings != null ? settings.cannonShootDuration : 0.1f;
            float punchScale = settings != null ? settings.cannonShootPunchScale : 1.12f;
            Ease ease = settings != null ? settings.cannonShootEase : Ease.OutQuad;

            if (duration <= 0f)
            {
                return;
            }

            transform.DOKill(false);
            Sequence sequence = DOTween.Sequence()
                .Append(transform.DOScale(_initialScale * punchScale, duration * 0.5f).SetEase(ease))
                .Append(transform.DOScale(_initialScale, duration * 0.5f).SetEase(ease))
                .SetLink(gameObject);

            await TweenUniTaskBridge.AwaitCompletionAsync(sequence, cancellationToken);
        }

        public UniTask AimAtAsync(
            Vector3 targetWorldPosition,
            AnimationSettings fallbackSettings,
            CancellationToken cancellationToken = default)
        {
            Transform rotationRoot = RotationRoot;
            Vector3 direction = targetWorldPosition - rotationRoot.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return UniTask.CompletedTask;
            }

            Quaternion targetWorldRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Quaternion targetLocalRotation = rotationRoot.parent != null
                ? Quaternion.Inverse(rotationRoot.parent.rotation) * targetWorldRotation
                : targetWorldRotation;

            return RotateLocalAsync(targetLocalRotation, fallbackSettings, cancellationToken);
        }

        public UniTask AimForwardAsync(
            AnimationSettings fallbackSettings,
            CancellationToken cancellationToken = default)
        {
            return RotateLocalAsync(_initialLocalRotation, fallbackSettings, cancellationToken);
        }

        public async UniTask PlayDisappearAsync(AnimationSettings fallbackSettings, CancellationToken cancellationToken = default)
        {
            AnimationSettings settings = _settingsOverride != null ? _settingsOverride : fallbackSettings;
            float duration = settings != null ? settings.cannonDisappearDuration : 0.18f;
            float scaleMultiplier = settings != null ? settings.cannonDisappearScaleMultiplier : 0.05f;
            Ease ease = settings != null ? settings.cannonDisappearEase : Ease.InBack;

            transform.DOKill(false);

            if (duration <= 0f)
            {
                transform.localScale = _initialScale * scaleMultiplier;
                return;
            }

            Tween tween = transform
                .DOScale(_initialScale * scaleMultiplier, duration)
                .SetEase(ease)
                .SetLink(gameObject);

            await TweenUniTaskBridge.AwaitCompletionAsync(tween, cancellationToken);
        }

        private Transform RotationRoot
        {
            get { return _rotationRoot != null ? _rotationRoot : transform; }
        }

        private async UniTask RotateLocalAsync(
            Quaternion targetLocalRotation,
            AnimationSettings fallbackSettings,
            CancellationToken cancellationToken)
        {
            AnimationSettings settings = _settingsOverride != null ? _settingsOverride : fallbackSettings;
            float duration = settings != null ? settings.cannonAimDuration : 0.14f;
            Ease ease = settings != null ? settings.cannonAimEase : Ease.OutCubic;
            Transform rotationRoot = RotationRoot;

            rotationRoot.DOKill(false);

            if (duration <= 0f)
            {
                rotationRoot.localRotation = targetLocalRotation;
                return;
            }

            Tween tween = rotationRoot
                .DOLocalRotateQuaternion(targetLocalRotation, duration)
                .SetEase(ease)
                .SetLink(gameObject);

            await TweenUniTaskBridge.AwaitCompletionAsync(tween, cancellationToken);
        }

        private static float GetMoveDuration(AnimationSettings settings, CannonArrangeMode mode, int travelledCells)
        {
            if (settings == null)
            {
                return mode == CannonArrangeMode.SlotMove ? 0.2f : 0.18f;
            }

            return mode == CannonArrangeMode.SlotMove
                ? settings.cannonSlotMoveDuration
                : settings.GetCannonGridMoveDuration(travelledCells);
        }

        private static Ease GetSlotEase(AnimationSettings settings)
        {
            return settings != null ? settings.cannonSlotMoveEase : Ease.OutBack;
        }

        private static Ease GetGridEase(AnimationSettings settings)
        {
            return settings != null ? settings.cannonGridMoveEase : Ease.OutCubic;
        }
    }
}
