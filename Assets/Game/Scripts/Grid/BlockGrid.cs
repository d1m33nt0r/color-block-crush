using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Block;
using Game.BlockCrush.Effects;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Grid.Shared;
using Game.BlockCrush.Level.Shared;

namespace Game.BlockCrush.Grid
{
    public sealed class BlockGrid : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector2 _cellSize = Vector2.one;
        [SerializeField, Min(0f)] private float _cellHorizontalSpacing;
        [SerializeField, Min(0f)] private float _cellVerticalSpacing;
        [SerializeField] private Vector3 _localOrigin;
        [SerializeField] private Transform _cellsRoot;

        private BlockCell.Factory _cellFactory;
        private ColorBlock.Factory _blockFactory;
        private ColorBlockDatabase _blockDatabase;
        private AnimationSettings _animationSettings;
        private BlockDestroyEffectSpawner _destroyEffectSpawner;
        private BlockCell[,] _cells;
        private int _width;
        private int _height;

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public int ActiveBlockCount
        {
            get
            {
                if (_cells == null)
                {
                    return 0;
                }

                int count = 0;
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        BlockCell cell = _cells[x, y];
                        if (cell != null)
                        {
                            count += cell.ActiveBlockCount;
                        }
                    }
                }

                return count;
            }
        }

        [Inject]
        public void Construct(
            BlockCell.Factory cellFactory,
            ColorBlock.Factory blockFactory,
            [InjectOptional] ColorBlockDatabase blockDatabase,
            [InjectOptional] AnimationSettings animationSettings,
            [InjectOptional] BlockDestroyEffectSpawner destroyEffectSpawner)
        {
            _cellFactory = cellFactory;
            _blockFactory = blockFactory;
            _blockDatabase = blockDatabase;
            _animationSettings = animationSettings;
            _destroyEffectSpawner = destroyEffectSpawner;
        }

        public async UniTask LoadLevelAsync(
            LevelData levelData,
            bool immediate = true,
            CancellationToken cancellationToken = default)
        {
            if (levelData == null)
            {
                Debug.LogError("Cannot load null Block Crush level.", this);
                return;
            }

            if (_cellFactory == null || _blockFactory == null)
            {
                Debug.LogError("BlockGrid requires Zenject factories. Add GameInstaller to the SceneContext.", this);
                return;
            }

            Clear();
            EnsureRoots();

            _width = Mathf.Max(1, levelData.width);
            _height = Mathf.Max(1, levelData.height);
            _cells = new BlockCell[_width, _height];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    BlockCell cell = _cellFactory.Create(position, _cellsRoot);
                    cell.transform.localPosition = GetLocalCellPosition(position);
                    _cells[x, y] = cell;
                }
            }

            IEnumerable<BlockSpawnData> sourceBlocks = levelData.blocks ?? new List<BlockSpawnData>();
            IEnumerable<BlockSpawnData> orderedBlocks = sourceBlocks
                .Where(IsInsideLevel)
                .OrderBy(block => block.x)
                .ThenBy(block => block.y)
                .ThenBy(block => block.layer);

            foreach (BlockSpawnData spawnData in orderedBlocks)
            {
                BlockCell cell = _cells[spawnData.x, spawnData.y];
                ColorBlockDefinition definition = _blockDatabase != null
                    ? _blockDatabase.GetDefinitionOrFallback(spawnData.color)
                    : ColorBlockDefinition.CreateFallback(spawnData.color);

                ColorBlock block = _blockFactory.Create(definition, cell.StackRoot);
                block.name = string.Format("{0} Block ({1},{2}:{3})", spawnData.color, spawnData.x, spawnData.y, spawnData.layer);
                cell.AddBlock(block, false);
            }

            List<UniTask> arrangeTasks = new List<UniTask>();
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    arrangeTasks.Add(_cells[x, y].ArrangeImmediateAsync(cancellationToken));
                }
            }

            await UniTask.WhenAll(arrangeTasks);

            if (!immediate)
            {
                await ApplyGravityAsync(false, cancellationToken);
            }
        }

        public async UniTask DestroyBlockAsync(ColorBlock block, CancellationToken cancellationToken = default)
        {
            if (block == null || block.CurrentCell == null)
            {
                return;
            }

            await DestroyBlocksAsync(new[] { block }, true, cancellationToken);
        }

        public UniTask DestroyBlocksAsync(
            IReadOnlyList<ColorBlock> blocks,
            CancellationToken cancellationToken = default)
        {
            return DestroyBlocksAsync(blocks, true, cancellationToken);
        }

        public async UniTask DestroyBlocksAsync(
            IReadOnlyList<ColorBlock> blocks,
            bool waitForGravity,
            CancellationToken cancellationToken = default)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return;
            }

            HashSet<ColorBlock> uniqueBlocks = new HashSet<ColorBlock>();

            for (int i = 0; i < blocks.Count; i++)
            {
                ColorBlock block = blocks[i];
                if (block == null || block.CurrentCell == null || block.IsDestroying || !uniqueBlocks.Add(block))
                {
                    continue;
                }
            }

            if (uniqueBlocks.Count == 0)
            {
                return;
            }

            List<UniTask> animationTasks = CreateNeighborShakeTasks(uniqueBlocks, cancellationToken);
            foreach (ColorBlock block in uniqueBlocks)
            {
                if (block != null && block.CurrentCell != null)
                {
                    SpawnDestroyEffect(block, cancellationToken);
                    animationTasks.Add(block.CurrentCell.DestroyBlockAsync(block, cancellationToken));
                }
            }

            if (animationTasks.Count == 0)
            {
                return;
            }

            await UniTask.WhenAll(animationTasks);

            UniTask gravityTask = ApplyGravityAsync(false, cancellationToken);
            if (waitForGravity)
            {
                await gravityTask;
            }
            else
            {
                gravityTask.Forget();
            }
        }

        public bool TryFindFirstRowBlock(BlockColorKey colorKey, out ColorBlock block)
        {
            return TryFindFirstRowBlock(colorKey, null, out block);
        }

        public bool TryFindFirstRowBlock(
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks,
            out ColorBlock block)
        {
            block = null;

            if (_cells == null || _height <= 0)
            {
                return false;
            }

            const int firstRowY = 0;
            for (int x = 0; x < _width; x++)
            {
                BlockCell cell = _cells[x, firstRowY];
                if (cell == null || cell.IsEmpty)
                {
                    continue;
                }

                if (!TryGetTopBlock(cell, out ColorBlock topBlock, out _)
                    || topBlock.ColorKey != colorKey
                    || (reservedBlocks != null && reservedBlocks.Contains(topBlock)))
                {
                    continue;
                }

                block = topBlock;
                return true;
            }

            return false;
        }

        public bool TryFindShootableBlock(
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks,
            out ColorBlock block)
        {
            return TryFindShootableBlock(colorKey, reservedBlocks, -1, 0, out block);
        }

        public bool TryFindShootableBlock(
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks,
            int preferredRowY,
            int minX,
            out ColorBlock block)
        {
            block = null;

            if (_cells == null || _height <= 0)
            {
                return false;
            }

            minX = Mathf.Clamp(minX, 0, _width);

            if (preferredRowY >= 0 && preferredRowY < _height)
            {
                if (TryFindShootableBlockInRow(
                        colorKey,
                        reservedBlocks,
                        preferredRowY,
                        minX,
                        out block,
                        out bool rowHasMatchingShootableTarget))
                {
                    return true;
                }

                if (rowHasMatchingShootableTarget)
                {
                    return false;
                }
            }

            for (int y = 0; y < _height; y++)
            {
                if (y == preferredRowY)
                {
                    continue;
                }

                if (TryFindShootableBlockInRow(
                        colorKey,
                        reservedBlocks,
                        y,
                        0,
                        out block,
                        out bool rowHasMatchingShootableTarget))
                {
                    return true;
                }

                if (rowHasMatchingShootableTarget)
                {
                    return false;
                }
            }

            return false;
        }

        public async UniTask<bool> ApplyGravityAsync(bool immediate = false, CancellationToken cancellationToken = default)
        {
            if (_cells == null)
            {
                return false;
            }

            List<UniTask> moveTasks = new List<UniTask>();
            bool movedAny = false;

            for (int x = 0; x < _width; x++)
            {
                int targetY = 0;

                for (int y = 0; y < _height; y++)
                {
                    BlockCell source = _cells[x, y];
                    if (source == null || source.IsEmpty)
                    {
                        continue;
                    }

                    if (targetY != y)
                    {
                        BlockCell target = _cells[x, targetY];
                        List<ColorBlock> movingBlocks = source.TakeAllBlocks();
                        target.AddBlocks(movingBlocks, true);

                        int travelledCells = y - targetY;
                        GridCellArrangeMode arrangeMode = immediate
                            ? GridCellArrangeMode.Immediate
                            : GridCellArrangeMode.GridFall;

                        moveTasks.Add(target.ArrangeAsync(arrangeMode, travelledCells, cancellationToken));
                        movedAny = true;
                    }

                    targetY++;
                }
            }

            if (moveTasks.Count > 0)
            {
                await UniTask.WhenAll(moveTasks);
            }

            return movedAny;
        }

        public bool TryGetCell(GridPosition position, out BlockCell cell)
        {
            if (!IsInside(position))
            {
                cell = null;
                return false;
            }

            cell = _cells[position.x, position.y];
            return cell != null;
        }

        public Vector3 GetLocalCellPosition(GridPosition position)
        {
            Vector2 cellSize = GetCellSize();
            return new Vector3(
                GetGridLeftEdge() + cellSize.x * 0.5f + position.x * GetCellStepX(),
                _localOrigin.y,
                _localOrigin.z + position.y * GetCellStepZ());
        }

        public bool IsInside(GridPosition position)
        {
            return _cells != null
                   && position.x >= 0
                   && position.y >= 0
                   && position.x < _width
                   && position.y < _height;
        }

        public void Clear()
        {
            if (_cells == null)
            {
                return;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    BlockCell cell = _cells[x, y];
                    if (cell == null)
                    {
                        continue;
                    }

                    if (Application.isPlaying)
                    {
                        Destroy(cell.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(cell.gameObject);
                    }
                }
            }

            _cells = null;
            _width = 0;
            _height = 0;
        }

        private void EnsureRoots()
        {
            if (_cellsRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Cells");
            root.transform.SetParent(transform, false);
            _cellsRoot = root.transform;
        }

        private Vector2 GetCellSize()
        {
            return new Vector2(Mathf.Max(0.01f, _cellSize.x), Mathf.Max(0.01f, _cellSize.y));
        }

        private float GetCellStepX()
        {
            return GetCellSize().x + Mathf.Max(0f, _cellHorizontalSpacing);
        }

        private float GetCellStepZ()
        {
            return GetCellSize().y + Mathf.Max(0f, _cellVerticalSpacing);
        }

        private float GetGridWidth()
        {
            Vector2 cellSize = GetCellSize();
            return Mathf.Max(1, _width) * cellSize.x
                   + Mathf.Max(0, _width - 1) * Mathf.Max(0f, _cellHorizontalSpacing);
        }

        private float GetGridLeftEdge()
        {
            return _localOrigin.x - GetGridWidth() * 0.5f;
        }

        private List<UniTask> CreateNeighborShakeTasks(
            ISet<ColorBlock> destroyingBlocks,
            CancellationToken cancellationToken)
        {
            HashSet<ColorBlock> neighborBlocks = new HashSet<ColorBlock>();

            foreach (ColorBlock block in destroyingBlocks)
            {
                CollectNeighborBlocks(block, destroyingBlocks, neighborBlocks);
            }

            List<UniTask> shakeTasks = new List<UniTask>(neighborBlocks.Count);
            foreach (ColorBlock neighborBlock in neighborBlocks)
            {
                if (neighborBlock != null && !neighborBlock.IsDestroying)
                {
                    shakeTasks.Add(neighborBlock.PlayNeighborShakeAsync(cancellationToken));
                }
            }

            return shakeTasks;
        }

        private void CollectNeighborBlocks(
            ColorBlock block,
            ISet<ColorBlock> destroyingBlocks,
            ISet<ColorBlock> neighborBlocks)
        {
            if (block == null || block.CurrentCell == null)
            {
                return;
            }

            BlockCell cell = block.CurrentCell;
            AddStackNeighborBlocks(cell, block, destroyingBlocks, neighborBlocks);

            GridPosition position = cell.Position;
            AddCellBlocksAsNeighbors(position.x - 1, position.y, destroyingBlocks, neighborBlocks);
            AddCellBlocksAsNeighbors(position.x + 1, position.y, destroyingBlocks, neighborBlocks);
            AddCellBlocksAsNeighbors(position.x, position.y - 1, destroyingBlocks, neighborBlocks);
            AddCellBlocksAsNeighbors(position.x, position.y + 1, destroyingBlocks, neighborBlocks);
        }

        private static void AddStackNeighborBlocks(
            BlockCell cell,
            ColorBlock block,
            ISet<ColorBlock> destroyingBlocks,
            ISet<ColorBlock> neighborBlocks)
        {
            IReadOnlyList<ColorBlock> blocks = cell.Blocks;
            int blockIndex = -1;

            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] == block)
                {
                    blockIndex = i;
                    break;
                }
            }

            if (blockIndex < 0)
            {
                return;
            }

            AddNeighborBlock(blockIndex > 0 ? blocks[blockIndex - 1] : null, destroyingBlocks, neighborBlocks);
            AddNeighborBlock(blockIndex < blocks.Count - 1 ? blocks[blockIndex + 1] : null, destroyingBlocks, neighborBlocks);
        }

        private void AddCellBlocksAsNeighbors(
            int x,
            int y,
            ISet<ColorBlock> destroyingBlocks,
            ISet<ColorBlock> neighborBlocks)
        {
            GridPosition position = new GridPosition(x, y);
            if (!TryGetCell(position, out BlockCell cell))
            {
                return;
            }

            IReadOnlyList<ColorBlock> blocks = cell.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                AddNeighborBlock(blocks[i], destroyingBlocks, neighborBlocks);
            }
        }

        private static void AddNeighborBlock(
            ColorBlock block,
            ISet<ColorBlock> destroyingBlocks,
            ISet<ColorBlock> neighborBlocks)
        {
            if (block == null || block.IsDestroying || destroyingBlocks.Contains(block))
            {
                return;
            }

            neighborBlocks.Add(block);
        }

        private void SpawnDestroyEffect(ColorBlock block, CancellationToken cancellationToken)
        {
            if (_destroyEffectSpawner == null || block == null)
            {
                return;
            }

            _destroyEffectSpawner.Spawn(block.transform.position, block.EffectColor, cancellationToken);
        }

        private bool TryFindShootableBlockInRow(
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks,
            int y,
            int startX,
            out ColorBlock block,
            out bool rowHasMatchingShootableTarget)
        {
            block = null;
            rowHasMatchingShootableTarget = false;

            if (TryFindShootableBlockInRowRange(
                    colorKey,
                    reservedBlocks,
                    y,
                    startX,
                    _width,
                    out block,
                    out rowHasMatchingShootableTarget))
            {
                return true;
            }

            if (rowHasMatchingShootableTarget || startX <= 0)
            {
                return false;
            }

            if (HasReservedMatchingShootableBlockInRow(colorKey, reservedBlocks, y))
            {
                rowHasMatchingShootableTarget = true;
                return false;
            }

            return TryFindShootableBlockInRowRange(
                colorKey,
                reservedBlocks,
                y,
                0,
                startX,
                out block,
                out rowHasMatchingShootableTarget);
        }

        private bool TryFindShootableBlockInRowRange(
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks,
            int y,
            int startX,
            int endX,
            out ColorBlock block,
            out bool rowHasMatchingShootableTarget)
        {
            block = null;
            rowHasMatchingShootableTarget = false;

            startX = Mathf.Clamp(startX, 0, _width);
            endX = Mathf.Clamp(endX, startX, _width);

            for (int x = startX; x < endX; x++)
            {
                if (!TryGetMatchingShootableTopBlock(
                        x,
                        y,
                        colorKey,
                        reservedBlocks,
                        out ColorBlock topBlock))
                {
                    continue;
                }

                rowHasMatchingShootableTarget = true;
                if (reservedBlocks != null && reservedBlocks.Contains(topBlock))
                {
                    continue;
                }

                block = topBlock;
                return true;
            }

            return false;
        }

        private bool HasReservedMatchingShootableBlockInRow(
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks,
            int y)
        {
            if (reservedBlocks == null || reservedBlocks.Count == 0)
            {
                return false;
            }

            for (int x = 0; x < _width; x++)
            {
                if (TryGetMatchingShootableTopBlock(
                        x,
                        y,
                        colorKey,
                        reservedBlocks,
                        out ColorBlock topBlock)
                    && reservedBlocks.Contains(topBlock))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetMatchingShootableTopBlock(
            int x,
            int y,
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks,
            out ColorBlock topBlock)
        {
            topBlock = null;

            if (!TryGetTopBlock(_cells[x, y], out ColorBlock candidate, out _)
                || candidate.ColorKey != colorKey
                || !HasOpenLineToCell(x, y, colorKey, reservedBlocks))
            {
                return false;
            }

            topBlock = candidate;
            return true;
        }

        private bool HasOpenLineToCell(
            int x,
            int targetY,
            BlockColorKey colorKey,
            ISet<ColorBlock> reservedBlocks)
        {
            for (int y = 0; y < targetY; y++)
            {
                if (!TryGetTopBlock(_cells[x, y], out ColorBlock topBlock, out int blockCount))
                {
                    continue;
                }

                if (topBlock.ColorKey != colorKey
                    || reservedBlocks == null
                    || !reservedBlocks.Contains(topBlock)
                    || blockCount > 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetTopBlock(
            BlockCell cell,
            out ColorBlock topBlock,
            out int blockCount)
        {
            topBlock = null;
            blockCount = 0;

            if (cell == null || cell.IsEmpty)
            {
                return false;
            }

            IReadOnlyList<ColorBlock> blocks = cell.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                {
                    blockCount++;
                }
            }

            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                ColorBlock candidate = blocks[i];
                if (candidate == null)
                {
                    continue;
                }

                topBlock = candidate;
                return true;
            }

            return false;
        }

        private bool IsInsideLevel(BlockSpawnData block)
        {
            bool inside = block.x >= 0
                          && block.y >= 0
                          && block.x < Mathf.Max(1, _width)
                          && block.y < Mathf.Max(1, _height);

            if (!inside)
            {
                Debug.LogWarningFormat(
                    this,
                    "Skipping block outside level bounds: color {0}, position ({1}, {2}).",
                    block.color,
                    block.x,
                    block.y);
            }

            return inside;
        }
    }
}
