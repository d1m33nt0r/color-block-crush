using System.Threading;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Cannon.Shared;

namespace Game.BlockCrush.Cannon
{
    public sealed class CannonSlot : MonoBehaviour
    {
        [SerializeField] private Transform _cannonRoot;
        private float _cannonLocalYOffset;

        public sealed class Factory : PlaceholderFactory<int, Transform, CannonSlot>
        {
        }

        public int Index { get; private set; }
        public ColorCannon Cannon { get; private set; }

        public bool IsEmpty
        {
            get { return Cannon == null; }
        }

        public Transform CannonRoot
        {
            get { return _cannonRoot != null ? _cannonRoot : transform; }
        }

        public void Initialize(int index, AnimationSettings animationSettings)
        {
            Index = index;
            name = string.Format("Cannon Slot {0}", index);
            if (_cannonRoot == null)
            {
                _cannonRoot = transform;
            }
        }

        public void SetCannonLocalYOffset(float localYOffset)
        {
            _cannonLocalYOffset = localYOffset;
        }

        public Vector2 GetFootprintSize(Vector2 baseSize)
        {
            Vector3 scale = transform.localScale;
            return new Vector2(
                Mathf.Max(0.01f, baseSize.x * Mathf.Abs(scale.x)),
                Mathf.Max(0.01f, baseSize.y * Mathf.Abs(scale.z)));
        }

        public void AddCannon(ColorCannon cannon, bool worldPositionStays)
        {
            if (cannon == null)
            {
                return;
            }

            if (cannon.CurrentCell != null)
            {
                cannon.CurrentCell.RemoveCannon(cannon);
            }

            if (cannon.CurrentSlot != null && cannon.CurrentSlot != this)
            {
                cannon.CurrentSlot.RemoveCannon(cannon);
            }

            Cannon = cannon;
            cannon.SetCurrentSlot(this);
            cannon.transform.SetParent(CannonRoot, worldPositionStays);
            cannon.ApplyParentIndependentScale();
        }

        public bool RemoveCannon(ColorCannon cannon)
        {
            if (cannon == null || Cannon != cannon)
            {
                return false;
            }

            Cannon = null;
            cannon.SetCurrentSlot(null);
            return true;
        }

        public async UniTask DestroyCannonAsync(CancellationToken cancellationToken = default)
        {
            ColorCannon cannon = Cannon;
            if (cannon == null)
            {
                return;
            }

            RemoveCannon(cannon);
            await cannon.DestroyAnimatedAsync(cancellationToken);
        }

        public UniTask ArrangeAsync(CannonArrangeMode mode, CancellationToken cancellationToken = default)
        {
            if (Cannon == null)
            {
                return UniTask.CompletedTask;
            }

            return Cannon.MoveLocalAsync(Cannon.GetPlacementLocalPosition(_cannonLocalYOffset), mode, 1, cancellationToken);
        }
    }
}
