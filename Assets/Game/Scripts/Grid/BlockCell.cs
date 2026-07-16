using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Block;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Grid.Shared;

namespace Game.BlockCrush.Grid
{
    public sealed class BlockCell : MonoBehaviour
    {
        [SerializeField] private Transform _stackRoot;
        [SerializeField] private GridCellStackAnimator _stackAnimator;

        private readonly List<ColorBlock> _blocks = new List<ColorBlock>();
        private AnimationSettings _animationSettings;

        public sealed class Factory : PlaceholderFactory<GridPosition, Transform, BlockCell>
        {
        }

        public GridPosition Position { get; private set; }

        public Transform StackRoot
        {
            get { return _stackRoot != null ? _stackRoot : transform; }
        }

        public IReadOnlyList<ColorBlock> Blocks
        {
            get
            {
                PruneInvalidBlocks();
                return _blocks;
            }
        }

        public bool IsEmpty
        {
            get
            {
                PruneInvalidBlocks();
                return _blocks.Count == 0;
            }
        }

        public int ActiveBlockCount
        {
            get
            {
                PruneInvalidBlocks();
                return _blocks.Count;
            }
        }

        public void Initialize(GridPosition position, AnimationSettings animationSettings)
        {
            Position = position;
            _animationSettings = animationSettings;
            name = string.Format("Cell {0},{1}", position.x, position.y);
            EnsureComponents();
        }

        public void AddBlock(ColorBlock block, bool worldPositionStays)
        {
            if (block == null)
            {
                return;
            }

            if (block.CurrentCell != null && block.CurrentCell != this)
            {
                block.CurrentCell.RemoveBlock(block);
            }

            if (!_blocks.Contains(block))
            {
                _blocks.Add(block);
            }

            block.SetCurrentCell(this);
            block.transform.SetParent(StackRoot, worldPositionStays);
        }

        public void AddBlocks(IReadOnlyList<ColorBlock> blocks, bool worldPositionStays)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                AddBlock(blocks[i], worldPositionStays);
            }
        }

        public bool RemoveBlock(ColorBlock block)
        {
            if (block == null)
            {
                return false;
            }

            bool removed = _blocks.Remove(block);
            if (removed)
            {
                block.SetCurrentCell(null);
            }

            return removed;
        }

        public List<ColorBlock> TakeAllBlocks()
        {
            PruneInvalidBlocks();

            List<ColorBlock> blocks = new List<ColorBlock>(_blocks);
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                {
                    blocks[i].SetCurrentCell(null);
                }
            }

            _blocks.Clear();
            return blocks;
        }

        public async UniTask DestroyBlockAsync(ColorBlock block, CancellationToken cancellationToken = default)
        {
            if (!_blocks.Contains(block))
            {
                return;
            }

            RemoveBlock(block);

            UniTask destroyTask = block.DestroyAnimatedAsync(cancellationToken);
            UniTask arrangeTask = ArrangeAsync(GridCellArrangeMode.StackReflow, 0, cancellationToken);
            await UniTask.WhenAll(destroyTask, arrangeTask);
        }

        public UniTask ArrangeImmediateAsync(CancellationToken cancellationToken = default)
        {
            return ArrangeAsync(GridCellArrangeMode.Immediate, 0, cancellationToken);
        }

        public UniTask ArrangeAsync(GridCellArrangeMode mode, int travelledCells, CancellationToken cancellationToken = default)
        {
            EnsureComponents();
            PruneInvalidBlocks();
            return _stackAnimator.ArrangeAsync(_blocks, _animationSettings, mode, travelledCells, cancellationToken);
        }

        private void PruneInvalidBlocks()
        {
            for (int i = _blocks.Count - 1; i >= 0; i--)
            {
                ColorBlock block = _blocks[i];
                if (block != null && !block.IsDestroying)
                {
                    continue;
                }

                if (block != null && block.CurrentCell == this)
                {
                    block.SetCurrentCell(null);
                }

                _blocks.RemoveAt(i);
            }
        }

        private void EnsureComponents()
        {
            if (_stackRoot == null)
            {
                _stackRoot = transform;
            }

            if (_stackAnimator == null)
            {
                _stackAnimator = GetComponent<GridCellStackAnimator>();
                if (_stackAnimator == null)
                {
                    _stackAnimator = gameObject.AddComponent<GridCellStackAnimator>();
                }
            }
        }
    }
}
