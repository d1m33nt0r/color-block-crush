using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;

namespace Game.BlockCrush.Cannon
{
    public sealed class ColorBullet : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private SpriteRenderer[] _spriteRenderers;
        [SerializeField] private TrailRenderer _trailRenderer;
        
        private AnimationSettings _animationSettings;

        public sealed class Factory : PlaceholderFactory<BlockColorKey, Transform, ColorBullet>
        {
        }

        public BlockColorKey ColorKey { get; private set; }

        private void Awake()
        {
            CacheComponents();
        }

        public void Initialize(
            BlockColorKey colorKey,
            ColorBlockDefinition definition,
            AnimationSettings animationSettings)
        {
            CacheComponents();

            ColorKey = colorKey;
            _animationSettings = animationSettings;

            ApplyVisuals(definition ?? ColorBlockDefinition.CreateFallback(colorKey));

            if (_animationSettings != null)
            {
                transform.localScale = Vector3.one * _animationSettings.bulletScale;
            }
        }

        public async UniTask FlyAsync(
            Vector3 startPosition,
            Vector3 targetPosition,
            CancellationToken cancellationToken = default)
        {
            await FlyInternalAsync(startPosition, targetPosition, null, cancellationToken);
        }

        public UniTask FlyAsync(
            Vector3 startPosition,
            Transform target,
            CancellationToken cancellationToken = default)
        {
            Vector3 targetPosition = target != null ? target.position : startPosition;
            return FlyInternalAsync(startPosition, targetPosition, target, cancellationToken);
        }

        private async UniTask FlyInternalAsync(
            Vector3 startPosition,
            Vector3 targetPosition,
            Transform target,
            CancellationToken cancellationToken)
        {
            transform.position = startPosition;
            FaceTarget(targetPosition);

            float duration = _animationSettings != null ? _animationSettings.bulletFlightDuration : 0.22f;
            Ease ease = _animationSettings != null ? _animationSettings.bulletFlightEase : Ease.InQuad;

            if (duration > 0f)
            {
                float progress = 0f;
                Vector3 lastTargetPosition = targetPosition;

                Tween tween = DOVirtual
                    .Float(0f, 1f, duration, value =>
                    {
                        progress = value;

                        if (target != null)
                        {
                            lastTargetPosition = target.position;
                        }

                        transform.position = Vector3.LerpUnclamped(startPosition, lastTargetPosition, progress);
                        FaceTarget(lastTargetPosition);
                    })
                    .SetEase(ease)
                    .SetLink(gameObject);

                await TweenUniTaskBridge.AwaitCompletionAsync(tween, cancellationToken);
            }
            else
            {
                transform.position = target != null ? target.position : targetPosition;
            }

            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private void CacheComponents()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            if (_spriteRenderers == null || _spriteRenderers.Length == 0)
            {
                _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private void ApplyVisuals(ColorBlockDefinition definition)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer targetRenderer = _renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                if (definition.material != null)
                {
                    targetRenderer.material = definition.material;
                }

                ApplyMaterialColor(targetRenderer.material, definition.color);
            }

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = _spriteRenderers[i];
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = definition.color;
                }
            }
            
            if (_trailRenderer != null)
            {
                var color = definition.color;
                _trailRenderer.startColor = new Color(color.r, color.g, color.b, 0.5f);
                _trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
