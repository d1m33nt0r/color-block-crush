using System.Threading;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Shared;
using Game.Scripts.Core.Common.Sound.Data;
using Game.Scripts.Core.Common.Sound.Shared;
using TMPro;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Cannon.Shared;
using Game.BlockCrush.Level.Shared;

namespace Game.BlockCrush.Cannon
{
    public sealed class ColorCannon : MonoBehaviour
    {
        [SerializeField] private BlockColorKey _colorKey;
        [SerializeField, Min(0)] private int _shots = 1;
        [SerializeField, Min(0.01f)] private float _shootRate;
        [SerializeField] private TMP_Text _shotsText;
        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private SpriteRenderer[] _spriteRenderers;
        [SerializeField] private ColorCannonAnimator _animator;

        private CannonGrid _ownerGrid;
        private AnimationSettings _animationSettings;
        private ISoundPlayer _shootSoundPlayer;
        private Vector3 _parentIndependentScale = Vector3.one;
        private bool _isDisappearing;

        public sealed class Factory : PlaceholderFactory<CannonSpawnData, Transform, ColorCannon>
        {
        }

        public BlockColorKey ColorKey
        {
            get { return _colorKey; }
        }

        public int Shots
        {
            get { return _shots; }
        }

        public float ShootRate
        {
            get { return Mathf.Max(0.01f, _shootRate); }
        }

        public float ShootInterval
        {
            get { return 1f / ShootRate; }
        }

        public bool HasShots
        {
            get { return _shots > 0; }
        }

        public CannonGridCell CurrentCell { get; private set; }
        public CannonSlot CurrentSlot { get; private set; }

        public Transform MuzzlePoint
        {
            get { return _muzzlePoint != null ? _muzzlePoint : transform; }
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnMouseDown()
        {
            if (_ownerGrid != null)
            {
                _ownerGrid.HandleCannonTappedAsync(this, this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        [Inject]
        public void Construct([InjectOptional] ISoundPlayer shootSoundPlayer)
        {
            _shootSoundPlayer = shootSoundPlayer;
            _shootSoundPlayer?.Initialize(SoundKey.CannonShoot, transform);
        }

        public void Initialize(
            BlockColorKey colorKey,
            int shots,
            float shootRate,
            ColorBlockDefinition definition,
            AnimationSettings animationSettings)
        {
            CacheComponents();

            _animationSettings = animationSettings;
            _colorKey = colorKey;
            _shots = Mathf.Max(0, shots);
            _shootRate = shootRate > 0f ? shootRate : CannonSpawnData.DefaultShootRate;
            _parentIndependentScale = transform.localScale;
            ApplyParentIndependentScale();

            ApplyVisuals(definition ?? ColorBlockDefinition.CreateFallback(colorKey));
            UpdateShotsText();
            _animator.CaptureInitialScale();
        }

        public void AssignOwnerGrid(CannonGrid ownerGrid)
        {
            _ownerGrid = ownerGrid;
        }

        public int ConsumeShot()
        {
            _shots = Mathf.Max(0, _shots - 1);
            UpdateShotsText();
            return _shots;
        }

        public UniTask MoveLocalAsync(
            Vector3 targetLocalPosition,
            CannonArrangeMode mode,
            int travelledCells,
            CancellationToken cancellationToken = default)
        {
            return _animator.MoveLocalAsync(targetLocalPosition, mode, travelledCells, _animationSettings, cancellationToken);
        }

        public UniTask PlayShootAsync(CancellationToken cancellationToken = default)
        {
            _shootSoundPlayer?.Play();
            return _animator.PlayShootAsync(_animationSettings, cancellationToken);
        }

        public UniTask AimAtAsync(Vector3 targetWorldPosition, CancellationToken cancellationToken = default)
        {
            return _animator.AimAtAsync(targetWorldPosition, _animationSettings, cancellationToken);
        }

        public UniTask AimForwardAsync(CancellationToken cancellationToken = default)
        {
            return _animator.AimForwardAsync(_animationSettings, cancellationToken);
        }

        public Vector3 GetPlacementLocalPosition(float localYOffset)
        {
            return new Vector3(0f, localYOffset, 0f);
        }

        public void ApplyParentIndependentScale()
        {
            if (_parentIndependentScale == Vector3.zero)
            {
                _parentIndependentScale = transform.localScale;
            }

            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(
                DivideByScale(_parentIndependentScale.x, parentScale.x),
                DivideByScale(_parentIndependentScale.y, parentScale.y),
                DivideByScale(_parentIndependentScale.z, parentScale.z));

            if (_animator != null)
            {
                _animator.CaptureInitialScaleOnly();
            }
        }

        public async UniTask DestroyAnimatedAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisappearing)
            {
                return;
            }

            _isDisappearing = true;
            await _animator.PlayDisappearAsync(_animationSettings, cancellationToken);

            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        internal void SetCurrentCell(CannonGridCell cell)
        {
            CurrentCell = cell;
            if (cell != null)
            {
                CurrentSlot = null;
            }
        }

        internal void SetCurrentSlot(CannonSlot slot)
        {
            CurrentSlot = slot;
            if (slot != null)
            {
                CurrentCell = null;
            }
        }

        private void CacheComponents()
        {
            if (_animator == null)
            {
                _animator = GetComponent<ColorCannonAnimator>();
                if (_animator == null)
                {
                    _animator = gameObject.AddComponent<ColorCannonAnimator>();
                }
            }

            if (_shotsText == null)
            {
                _shotsText = GetComponentInChildren<TMP_Text>(true);
                if (_shotsText == null)
                {
                    _shotsText = CreateShotsText();
                }
            }

            if (_muzzlePoint == null)
            {
                _muzzlePoint = CreateMuzzlePoint();
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

        private TMP_Text CreateShotsText()
        {
            GameObject textObject = new GameObject("Shots Text");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = 3f;
            textMesh.color = Color.white;
            textMesh.enableAutoSizing = true;
            textMesh.fontSizeMin = 1f;
            textMesh.fontSizeMax = 3f;
            textMesh.rectTransform.sizeDelta = new Vector2(2f, 1f);
            return textMesh;
        }

        private Transform CreateMuzzlePoint()
        {
            GameObject muzzleObject = new GameObject("Muzzle Point");
            muzzleObject.transform.SetParent(transform, false);
            muzzleObject.transform.localPosition = new Vector3(0f, 0f, 0.65f);
            return muzzleObject.transform;
        }

        private void UpdateShotsText()
        {
            if (_shotsText != null)
            {
                _shotsText.text = _shots.ToString();
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

        private static float DivideByScale(float desiredScale, float parentScale)
        {
            return Mathf.Abs(parentScale) > 0.0001f ? desiredScale / parentScale : desiredScale;
        }
    }
}
