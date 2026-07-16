using System.Threading;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Grid;
using Game.BlockCrush.Shared;
using Game.Scripts.Core.Common.Sound.Data;
using Game.Scripts.Core.Common.Sound.Shared;
using Game.Scripts.Haptics.Data;
using Game.Scripts.Haptics.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;

namespace Game.BlockCrush.Block
{
    public sealed class ColorBlock : MonoBehaviour
    {
        [SerializeField] private BlockColorKey _colorKey;
        [SerializeField, Min(0.01f)] private float _height = 1f;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private SpriteRenderer[] _spriteRenderers;
        [SerializeField] private ColorBlockAnimator _animator;
        [SerializeField] private HapticFeedbackType _destroyHapticFeedback = HapticFeedbackType.MediumImpact;

        private AnimationSettings _animationSettings;
        private IHapticPlayer _hapticPlayer;
        private ISoundPlayer _destroySoundPlayer;
        private Color _effectColor = Color.white;
        private bool _isDestroying;

        public sealed class Factory : PlaceholderFactory<ColorBlockDefinition, Transform, ColorBlock>
        {
        }

        public BlockColorKey ColorKey
        {
            get { return _colorKey; }
        }

        public float Height
        {
            get { return Mathf.Max(0.01f, _height); }
            set { _height = Mathf.Max(0.01f, value); }
        }

        public bool IsDestroying
        {
            get { return _isDestroying; }
        }

        public Color EffectColor
        {
            get { return _effectColor; }
        }

        public BlockCell CurrentCell { get; private set; }

        private void Awake()
        {
            CacheComponents();
        }

        [Inject]
        public void Construct(
            [InjectOptional] IHapticPlayer hapticPlayer,
            [InjectOptional] ISoundPlayer destroySoundPlayer)
        {
            _hapticPlayer = hapticPlayer;
            _destroySoundPlayer = destroySoundPlayer;
            _destroySoundPlayer?.Initialize(SoundKey.BlockDestroy, transform);
        }

        public void Initialize(ColorBlockDefinition definition, AnimationSettings animationSettings)
        {
            CacheComponents();

            if (definition == null)
            {
                definition = ColorBlockDefinition.CreateFallback(_colorKey);
            }

            _animationSettings = animationSettings;
            _colorKey = definition.key;
            Height = definition.height;
            _effectColor = definition.color;

            ApplyVisuals(definition);
            _animator.CaptureInitialScale();
        }

        public async UniTask DestroyAnimatedAsync(CancellationToken cancellationToken = default)
        {
            if (_isDestroying)
            {
                return;
            }

            _isDestroying = true;
            _hapticPlayer?.Play(_destroyHapticFeedback);
            _destroySoundPlayer?.Play();

            if (_animator != null)
            {
                await _animator.PlayDestroyAsync(_animationSettings, cancellationToken);
            }

            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        public UniTask PlayNeighborShakeAsync(CancellationToken cancellationToken = default)
        {
            if (_isDestroying || _animator == null)
            {
                return UniTask.CompletedTask;
            }

            return _animator.PlayNeighborShakeAsync(_animationSettings, cancellationToken);
        }

        internal void SetCurrentCell(BlockCell cell)
        {
            CurrentCell = cell;
        }

        private void CacheComponents()
        {
            if (_animator == null)
            {
                _animator = GetComponent<ColorBlockAnimator>();
                if (_animator == null)
                {
                    _animator = gameObject.AddComponent<ColorBlockAnimator>();
                }
            }

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
