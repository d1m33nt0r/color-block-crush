using System.Threading;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Grid.Shared;
using Game.BlockCrush.Cannon.Shared;

namespace Game.BlockCrush.Cannon
{
    public sealed class CannonGridCell : MonoBehaviour
    {
        [SerializeField] private Transform _cannonRoot;
        private float _cannonLocalYOffset;

        public sealed class Factory : PlaceholderFactory<GridPosition, Transform, CannonGridCell>
        {
        }

        public GridPosition Position { get; private set; }
        public ColorCannon Cannon { get; private set; }

        public bool IsEmpty
        {
            get { return Cannon == null; }
        }

        public Transform CannonRoot
        {
            get { return _cannonRoot != null ? _cannonRoot : transform; }
        }

        public void Initialize(GridPosition position, AnimationSettings animationSettings)
        {
            Position = position;
            name = string.Format("Cannon Cell {0},{1}", position.x, position.y);
            if (_cannonRoot == null)
            {
                _cannonRoot = transform;
            }
        }

        public void SetCannonLocalYOffset(float localYOffset)
        {
            _cannonLocalYOffset = localYOffset;
        }

        public void AddCannon(ColorCannon cannon, bool worldPositionStays)
        {
            if (cannon == null)
            {
                return;
            }

            if (cannon.CurrentCell != null && cannon.CurrentCell != this)
            {
                cannon.CurrentCell.RemoveCannon(cannon);
            }

            if (cannon.CurrentSlot != null)
            {
                cannon.CurrentSlot.RemoveCannon(cannon);
            }

            Cannon = cannon;
            cannon.SetCurrentCell(this);
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
            cannon.SetCurrentCell(null);
            return true;
        }

        public ColorCannon TakeCannon()
        {
            ColorCannon cannon = Cannon;
            if (cannon != null)
            {
                RemoveCannon(cannon);
            }

            return cannon;
        }

        public UniTask ArrangeAsync(
            CannonArrangeMode mode,
            int travelledCells,
            CancellationToken cancellationToken = default)
        {
            if (Cannon == null)
            {
                return UniTask.CompletedTask;
            }

            return Cannon.MoveLocalAsync(
                Cannon.GetPlacementLocalPosition(_cannonLocalYOffset),
                mode,
                travelledCells,
                cancellationToken);
        }
    }
}
