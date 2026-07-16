using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Block;
using Game.BlockCrush.Grid;
using Game.BlockCrush.Shared;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Grid.Shared;
using Game.BlockCrush.Cannon.Shared;
using Game.BlockCrush.Level.Shared;

namespace Game.BlockCrush.Cannon
{
    public sealed class CannonGrid : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector2 _cellSize = Vector2.one;
        [SerializeField, Min(0f)] private float _cellHorizontalSpacing;
        [SerializeField, Min(0f)] private float _cellVerticalSpacing;
        [SerializeField] private Vector3 _localOrigin;
        [SerializeField] private Vector2 _slotSize = Vector2.one;
        [SerializeField, Min(0f)] private float _slotHorizontalSpacing = 0.1f;
        [SerializeField, Min(0f)] private float _slotYOffset = 1.25f;
        [SerializeField] private Transform _cellsRoot;
        [SerializeField] private Transform _slotsRoot;

        [Header("Cannon Placement")]
        [SerializeField]
        [Tooltip("Local Y position for cannons inside cells and slots. Can be negative when the prefab pivot is already above its floor.")]
        [FormerlySerializedAs("_cannonFloorOffset")]
        private float _cannonLocalYOffset;

        [Header("Behaviour")]
        [SerializeField] private bool _autoShootFromSlots = true;

        private CannonGridCell.Factory _cellFactory;
        private CannonSlot.Factory _slotFactory;
        private ColorCannon.Factory _cannonFactory;
        private ColorBullet.Factory _bulletFactory;
        private BlockGrid _blockGrid;
        private CannonGridCell[,] _cells;
        private CannonSlot[] _slots;
        private int _width;
        private int _height;
        private readonly HashSet<ColorCannon> _cannonsMovingToSlots = new HashSet<ColorCannon>();
        private readonly HashSet<ColorCannon> _shootingCannons = new HashSet<ColorCannon>();
        private readonly HashSet<ColorCannon> _cannonsFinishingShots = new HashSet<ColorCannon>();
        private readonly HashSet<ColorBlock> _targetedBlocks = new HashSet<ColorBlock>();
        private readonly Dictionary<BlockColorKey, TargetSweepState> _targetSweepStates =
            new Dictionary<BlockColorKey, TargetSweepState>();
        private int _runtimeVersion;

        private sealed class TargetSweepState
        {
            public int RowY = -1;
            public int NextX;
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public int SlotCount
        {
            get { return _slots != null ? _slots.Length : 0; }
        }

        public bool HasPendingTargetedBlocks
        {
            get { return _targetedBlocks.Count > 0; }
        }

        public bool AreAllSlotsOccupied
        {
            get
            {
                if (_slots == null || _slots.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] == null || _slots[i].IsEmpty)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [Inject]
        public void Construct(
            CannonGridCell.Factory cellFactory,
            CannonSlot.Factory slotFactory,
            ColorCannon.Factory cannonFactory,
            ColorBullet.Factory bulletFactory,
            [InjectOptional] BlockGrid blockGrid)
        {
            _cellFactory = cellFactory;
            _slotFactory = slotFactory;
            _cannonFactory = cannonFactory;
            _bulletFactory = bulletFactory;
            _blockGrid = blockGrid;
        }

        public async UniTask LoadLevelAsync(
            CannonLevelData levelData,
            bool immediate = true,
            CancellationToken cancellationToken = default)
        {
            if (levelData == null)
            {
                Debug.LogError("Cannot load null Block Crush cannon level.", this);
                return;
            }

            if (_cellFactory == null || _slotFactory == null || _cannonFactory == null)
            {
                Debug.LogError("CannonGrid requires Zenject factories. Add GameInstaller to the SceneContext.", this);
                return;
            }

            Clear();
            EnsureRoots();

            _width = Mathf.Max(1, levelData.width);
            _height = Mathf.Max(1, levelData.height);
            int slotCount = Mathf.Max(0, levelData.slotCount);

            _cells = new CannonGridCell[_width, _height];
            _slots = new CannonSlot[slotCount];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    CannonGridCell cell = _cellFactory.Create(position, _cellsRoot);
                    cell.SetCannonLocalYOffset(_cannonLocalYOffset);
                    _cells[x, y] = cell;
                }
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                CannonSlot slot = _slotFactory.Create(i, _slotsRoot);
                slot.SetCannonLocalYOffset(_cannonLocalYOffset);
                _slots[i] = slot;
            }

            ArrangeCells();
            ArrangeSlots();

            IEnumerable<CannonSpawnData> sourceCannons =
                levelData.cannons ?? new List<CannonSpawnData>();

            IEnumerable<CannonSpawnData> orderedCannons = sourceCannons
                .Where(IsInsideLevel)
                .OrderBy(cannon => cannon.x)
                .ThenBy(cannon => cannon.y);

            foreach (CannonSpawnData spawnData in orderedCannons)
            {
                CannonGridCell cell = _cells[spawnData.x, spawnData.y];
                if (!cell.IsEmpty)
                {
                    Debug.LogWarningFormat(
                        this,
                        "Skipping cannon because cell ({0}, {1}) is already occupied.",
                        spawnData.x,
                        spawnData.y);
                    continue;
                }

                ColorCannon cannon = _cannonFactory.Create(spawnData, cell.CannonRoot);
                cannon.name = string.Format("{0} Cannon ({1},{2})", spawnData.color, spawnData.x, spawnData.y);
                cannon.AssignOwnerGrid(this);
                cell.AddCannon(cannon, false);
            }

            await ArrangeAllCellsAsync(CannonArrangeMode.Immediate, 0, cancellationToken);

            if (!immediate)
            {
                await ApplyGravityAsync(false, cancellationToken);
            }

            if (_autoShootFromSlots)
            {
                await ResolveBoardAsync(cancellationToken);
            }
        }

        public async UniTask HandleCannonTappedAsync(ColorCannon cannon, CancellationToken cancellationToken = default)
        {
            if (cannon == null || _cannonsMovingToSlots.Contains(cannon))
            {
                return;
            }

            if (!CanMoveToShootingSlot(cannon))
            {
                return;
            }

            CannonSlot slot = GetFirstFreeSlot();
            if (slot == null)
            {
                return;
            }

            await MoveCannonToSlotAsync(cannon, slot, cancellationToken);
            await ApplyGravityAsync(false, cancellationToken);

            if (_autoShootFromSlots)
            {
                await ResolveBoardAsync(this.GetCancellationTokenOnDestroy());
            }
        }

        public UniTask ResolveBoardAsync(CancellationToken cancellationToken = default)
        {
            StartShootingLoopsForSlots(cancellationToken);
            return UniTask.CompletedTask;
        }

        public bool IsGameOverState()
        {
            return AreAllSlotsOccupied
                   && !HasPendingTargetedBlocks
                   && !HasAnyShootableTargetForSlottedCannons();
        }

        public bool HasAnyShootableTargetForSlottedCannons()
        {
            if (_slots == null || _blockGrid == null)
            {
                return false;
            }

            HashSet<ColorBlock> reservedBlocks = new HashSet<ColorBlock>(_targetedBlocks);

            for (int i = 0; i < _slots.Length; i++)
            {
                ColorCannon cannon = _slots[i] != null ? _slots[i].Cannon : null;
                if (cannon == null || !cannon.HasShots)
                {
                    continue;
                }

                if (_blockGrid.TryFindShootableBlock(cannon.ColorKey, reservedBlocks, out ColorBlock targetBlock))
                {
                    if (targetBlock != null)
                    {
                        reservedBlocks.Add(targetBlock);
                    }

                    return true;
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
                int targetY = _height - 1;

                for (int y = _height - 1; y >= 0; y--)
                {
                    CannonGridCell source = _cells[x, y];
                    if (source == null || source.IsEmpty)
                    {
                        continue;
                    }

                    if (targetY != y)
                    {
                        CannonGridCell target = _cells[x, targetY];
                        ColorCannon movingCannon = source.TakeCannon();
                        target.AddCannon(movingCannon, true);

                        int travelledCells = targetY - y;
                        CannonArrangeMode arrangeMode = immediate
                            ? CannonArrangeMode.Immediate
                            : CannonArrangeMode.GridRise;

                        moveTasks.Add(target.ArrangeAsync(arrangeMode, travelledCells, cancellationToken));
                        movedAny = true;
                    }

                    targetY--;
                }
            }

            if (moveTasks.Count > 0)
            {
                await UniTask.WhenAll(moveTasks);
            }

            return movedAny;
        }

        public bool TryGetCell(GridPosition position, out CannonGridCell cell)
        {
            if (!IsInside(position))
            {
                cell = null;
                return false;
            }

            cell = _cells[position.x, position.y];
            return cell != null;
        }

        public bool IsInside(GridPosition position)
        {
            return _cells != null
                   && position.x >= 0
                   && position.y >= 0
                   && position.x < _width
                   && position.y < _height;
        }

        public Vector3 GetLocalCellPosition(GridPosition position)
        {
            Vector2 cellSize = GetCellSize();
            return new Vector3(
                GetGridLeftEdge() + cellSize.x * 0.5f + position.x * GetCellStepX(),
                _localOrigin.y,
                GetGridBottomEdge() + cellSize.y * 0.5f + position.y * GetCellStepZ());
        }

        public Vector3 GetLocalSlotPosition(int slotIndex, int slotCount)
        {
            Vector2 slotSize = GetSlotSize();
            float gridLeft = GetGridLeftEdge();
            float slotsWidth = GetUniformSlotsWidth(slotCount, slotSize);
            float startX = gridLeft + (GetGridWidth() - slotsWidth) * 0.5f + slotSize.x * 0.5f;
            float slotZ = GetLayoutTopEdge() - slotSize.y * 0.5f;

            return new Vector3(
                startX + slotIndex * (slotSize.x + _slotHorizontalSpacing),
                _localOrigin.y,
                slotZ);
        }

        public void Clear()
        {
            _runtimeVersion++;
            _cannonsMovingToSlots.Clear();
            _shootingCannons.Clear();
            _cannonsFinishingShots.Clear();
            _targetedBlocks.Clear();
            _targetSweepStates.Clear();
            ClearCells();
            ClearSlots();
            _width = 0;
            _height = 0;
        }

        private void ArrangeSlots()
        {
            if (_slots == null || _slots.Length == 0)
            {
                return;
            }

            Vector2 baseSlotSize = GetSlotSize();
            Vector2[] slotSizes = new Vector2[_slots.Length];
            float slotsWidth = 0f;

            for (int i = 0; i < _slots.Length; i++)
            {
                slotSizes[i] = _slots[i] != null
                    ? _slots[i].GetFootprintSize(baseSlotSize)
                    : baseSlotSize;

                slotsWidth += slotSizes[i].x;
            }

            slotsWidth += Mathf.Max(0, _slots.Length - 1) * Mathf.Max(0f, _slotHorizontalSpacing);

            float cursorX = GetGridLeftEdge() + (GetGridWidth() - slotsWidth) * 0.5f;
            float layoutTopEdge = GetLayoutTopEdge();

            for (int i = 0; i < _slots.Length; i++)
            {
                CannonSlot slot = _slots[i];
                if (slot == null)
                {
                    cursorX += slotSizes[i].x + Mathf.Max(0f, _slotHorizontalSpacing);
                    continue;
                }

                Vector2 slotSize = slotSizes[i];
                slot.transform.localPosition = new Vector3(
                    cursorX + slotSize.x * 0.5f,
                    _localOrigin.y,
                    layoutTopEdge - slotSize.y * 0.5f);

                cursorX += slotSize.x + Mathf.Max(0f, _slotHorizontalSpacing);
            }
        }

        private void ArrangeCells()
        {
            if (_cells == null)
            {
                return;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    CannonGridCell cell = _cells[x, y];
                    if (cell != null)
                    {
                        cell.transform.localPosition = GetLocalCellPosition(new GridPosition(x, y));
                    }
                }
            }
        }

        private Vector2 GetCellSize()
        {
            return new Vector2(Mathf.Max(0.01f, _cellSize.x), Mathf.Max(0.01f, _cellSize.y));
        }

        private Vector2 GetSlotSize()
        {
            return new Vector2(Mathf.Max(0.01f, _slotSize.x), Mathf.Max(0.01f, _slotSize.y));
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

        private float GetGridDepth()
        {
            Vector2 cellSize = GetCellSize();
            return Mathf.Max(1, _height) * cellSize.y
                   + Mathf.Max(0, _height - 1) * Mathf.Max(0f, _cellVerticalSpacing);
        }

        private float GetLayoutTopEdge()
        {
            return _localOrigin.z;
        }

        private float GetSlotRowDepth()
        {
            if (_slots == null || _slots.Length == 0)
            {
                return 0f;
            }

            return GetSlotYOffset() + GetMaxSlotDepth();
        }

        private float GetMaxSlotDepth()
        {
            Vector2 baseSlotSize = GetSlotSize();
            float maxDepth = baseSlotSize.y;

            if (_slots == null)
            {
                return maxDepth;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    continue;
                }

                maxDepth = Mathf.Max(maxDepth, _slots[i].GetFootprintSize(baseSlotSize).y);
            }

            return maxDepth;
        }

        private float GetSlotYOffset()
        {
            return Mathf.Max(0f, _slotYOffset);
        }

        private float GetGridLeftEdge()
        {
            return _localOrigin.x - GetGridWidth() * 0.5f;
        }

        private float GetGridBottomEdge()
        {
            return GetGridTopEdge() - GetGridDepth();
        }

        private float GetGridTopEdge()
        {
            return GetLayoutTopEdge() - GetSlotRowDepth();
        }

        private float GetUniformSlotsWidth(int slotCount, Vector2 slotSize)
        {
            return Mathf.Max(0, slotCount) * slotSize.x
                   + Mathf.Max(0, slotCount - 1) * Mathf.Max(0f, _slotHorizontalSpacing);
        }

        private void StartShootingLoopsForSlots(CancellationToken cancellationToken)
        {
            if (!_autoShootFromSlots || _slots == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                CannonSlot slot = _slots[i];
                ColorCannon cannon = slot != null ? slot.Cannon : null;
                StartCannonShootingLoop(cannon, cancellationToken);
            }
        }

        private void StartCannonShootingLoop(ColorCannon cannon, CancellationToken cancellationToken)
        {
            if (cannon == null
                || cannon.CurrentSlot == null
                || _shootingCannons.Contains(cannon)
                || _cannonsMovingToSlots.Contains(cannon)
                || _cannonsFinishingShots.Contains(cannon))
            {
                return;
            }

            _shootingCannons.Add(cannon);
            RunCannonShootingLoopAsync(cannon, _runtimeVersion, cancellationToken).Forget();
        }

        private async UniTask RunCannonShootingLoopAsync(
            ColorCannon cannon,
            int runtimeVersion,
            CancellationToken cancellationToken)
        {
            bool finalShotStarted = false;

            try
            {
                while (IsCannonLoopActive(cannon, runtimeVersion))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!cannon.HasShots)
                    {
                        break;
                    }

                    if (!TryReserveTarget(cannon, out ColorBlock targetBlock))
                    {
                        if (_targetedBlocks.Count > 0)
                        {
                            await UniTask.Yield(cancellationToken);
                        }
                        else
                        {
                            await cannon.AimForwardAsync(cancellationToken);
                            await DelayByShootRateAsync(cannon, cancellationToken);
                        }

                        continue;
                    }

                    bool destroyCannonAfterShot = cannon.Shots <= 1;
                    if (destroyCannonAfterShot)
                    {
                        finalShotStarted = true;
                        _cannonsFinishingShots.Add(cannon);
                    }

                    cannon.ConsumeShot();
                    FireCannonAtTargetAsync(
                        cannon,
                        targetBlock,
                        destroyCannonAfterShot,
                        runtimeVersion,
                        cancellationToken).Forget();

                    if (destroyCannonAfterShot)
                    {
                        break;
                    }

                    await DelayByShootRateAsync(cannon, cancellationToken);
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            finally
            {
                _shootingCannons.Remove(cannon);
            }

            if (!cancellationToken.IsCancellationRequested
                && !finalShotStarted
                && IsCannonLoopActive(cannon, runtimeVersion)
                && !cannon.HasShots)
            {
                await DestroyCannonIfSlottedAsync(cannon, cancellationToken);
            }
        }

        private async UniTask FireCannonAtTargetAsync(
            ColorCannon cannon,
            ColorBlock targetBlock,
            bool destroyCannonAfterShot,
            int runtimeVersion,
            CancellationToken cancellationToken)
        {
            bool bulletStarted = false;

            try
            {
                if (!IsCannonLoopActive(cannon, runtimeVersion) || targetBlock == null)
                {
                    return;
                }

                Transform targetTransform = targetBlock.transform;
                await cannon.AimAtAsync(targetTransform.position, cancellationToken);

                if (!IsRuntimeVersion(runtimeVersion) || cannon == null || targetBlock == null)
                {
                    return;
                }

                ColorBullet bullet = _bulletFactory.Create(cannon.ColorKey, _slotsRoot);
                Vector3 startPosition = cannon.MuzzlePoint.position;

                FlyBulletAndDestroyTargetAsync(
                    bullet,
                    targetBlock,
                    startPosition,
                    runtimeVersion,
                    cancellationToken).Forget();
                bulletStarted = true;

                await cannon.PlayShootAsync(cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
            }
            finally
            {
                if (!bulletStarted)
                {
                    _targetedBlocks.Remove(targetBlock);
                }

                if (destroyCannonAfterShot)
                {
                    try
                    {
                        await DestroyCannonIfSlottedAsync(cannon, cancellationToken);
                    }
                    finally
                    {
                        _cannonsFinishingShots.Remove(cannon);
                    }
                }
            }
        }

        private async UniTask FlyBulletAndDestroyTargetAsync(
            ColorBullet bullet,
            ColorBlock targetBlock,
            Vector3 startPosition,
            int runtimeVersion,
            CancellationToken cancellationToken)
        {
            try
            {
                if (bullet != null && targetBlock != null)
                {
                    await bullet.FlyAsync(startPosition, targetBlock.transform, cancellationToken);
                }

                if (IsRuntimeVersion(runtimeVersion) && _blockGrid != null && targetBlock != null)
                {
                    await _blockGrid.DestroyBlocksAsync(new[] { targetBlock }, false, cancellationToken);
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            finally
            {
                _targetedBlocks.Remove(targetBlock);
            }
        }

        private bool TryReserveTarget(ColorCannon cannon, out ColorBlock targetBlock)
        {
            targetBlock = null;

            if (_blockGrid == null || _bulletFactory == null || cannon == null)
            {
                return false;
            }

            TargetSweepState sweepState = GetTargetSweepState(cannon.ColorKey);
            if (!_blockGrid.TryFindShootableBlock(
                    cannon.ColorKey,
                    _targetedBlocks,
                    sweepState.RowY,
                    sweepState.NextX,
                    out targetBlock))
            {
                return false;
            }

            _targetedBlocks.Add(targetBlock);
            AdvanceTargetSweep(cannon.ColorKey, targetBlock);
            return true;
        }

        private TargetSweepState GetTargetSweepState(BlockColorKey colorKey)
        {
            if (!_targetSweepStates.TryGetValue(colorKey, out TargetSweepState sweepState))
            {
                sweepState = new TargetSweepState();
                _targetSweepStates.Add(colorKey, sweepState);
            }

            return sweepState;
        }

        private void AdvanceTargetSweep(BlockColorKey colorKey, ColorBlock targetBlock)
        {
            if (targetBlock == null || targetBlock.CurrentCell == null)
            {
                return;
            }

            TargetSweepState sweepState = GetTargetSweepState(colorKey);
            GridPosition position = targetBlock.CurrentCell.Position;
            sweepState.RowY = position.y;
            sweepState.NextX = Mathf.Clamp(position.x + 1, 0, _blockGrid != null ? _blockGrid.Width : int.MaxValue);
        }

        private static UniTask DelayByShootRateAsync(
            ColorCannon cannon,
            CancellationToken cancellationToken)
        {
            float interval = cannon != null ? cannon.ShootInterval : 0.1f;
            int milliseconds = Mathf.Max(1, Mathf.RoundToInt(interval * 1000f));
            return UniTask.Delay(milliseconds, cancellationToken: cancellationToken);
        }

        private bool IsCannonLoopActive(ColorCannon cannon, int runtimeVersion)
        {
            return IsRuntimeVersion(runtimeVersion)
                   && cannon != null
                   && cannon.CurrentSlot != null
                   && !_cannonsMovingToSlots.Contains(cannon);
        }

        private bool IsRuntimeVersion(int runtimeVersion)
        {
            return runtimeVersion == _runtimeVersion;
        }

        private static async UniTask DestroyCannonIfSlottedAsync(
            ColorCannon cannon,
            CancellationToken cancellationToken)
        {
            if (cannon == null || cannon.CurrentSlot == null)
            {
                return;
            }

            try
            {
                await cannon.CurrentSlot.DestroyCannonAsync(cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        private async UniTask MoveCannonToSlotAsync(
            ColorCannon cannon,
            CannonSlot slot,
            CancellationToken cancellationToken)
        {
            _cannonsMovingToSlots.Add(cannon);

            try
            {
                slot.AddCannon(cannon, true);
                await slot.ArrangeAsync(CannonArrangeMode.SlotMove, cancellationToken);
            }
            finally
            {
                _cannonsMovingToSlots.Remove(cannon);
            }
        }

        private bool CanMoveToShootingSlot(ColorCannon cannon)
        {
            if (cannon.CurrentCell == null || GetFirstFreeSlot() == null)
            {
                return false;
            }

            return cannon.CurrentCell.Position.y == _height - 1;
        }

        private CannonSlot GetFirstFreeSlot()
        {
            if (_slots == null)
            {
                return null;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].IsEmpty)
                {
                    return _slots[i];
                }
            }

            return null;
        }

        private async UniTask ArrangeAllCellsAsync(
            CannonArrangeMode mode,
            int travelledCells,
            CancellationToken cancellationToken)
        {
            List<UniTask> arrangeTasks = new List<UniTask>();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    arrangeTasks.Add(_cells[x, y].ArrangeAsync(mode, travelledCells, cancellationToken));
                }
            }

            await UniTask.WhenAll(arrangeTasks);
        }

        private void EnsureRoots()
        {
            if (_cellsRoot == null)
            {
                GameObject cellsRoot = new GameObject("Cannon Cells");
                cellsRoot.transform.SetParent(transform, false);
                _cellsRoot = cellsRoot.transform;
            }

            if (_slotsRoot == null)
            {
                GameObject slotsRoot = new GameObject("Cannon Slots");
                slotsRoot.transform.SetParent(transform, false);
                _slotsRoot = slotsRoot.transform;
            }
        }

        private void ClearCells()
        {
            if (_cells == null)
            {
                return;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    CannonGridCell cell = _cells[x, y];
                    if (cell == null)
                    {
                        continue;
                    }

                    DestroyObject(cell.gameObject);
                }
            }

            _cells = null;
        }

        private void ClearSlots()
        {
            if (_slots == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    DestroyObject(_slots[i].gameObject);
                }
            }

            _slots = null;
        }

        private void DestroyObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private bool IsInsideLevel(CannonSpawnData cannon)
        {
            bool inside = cannon.x >= 0
                          && cannon.y >= 0
                          && cannon.x < Mathf.Max(1, _width)
                          && cannon.y < Mathf.Max(1, _height);

            if (!inside)
            {
                Debug.LogWarningFormat(
                    this,
                    "Skipping cannon outside level bounds: color {0}, position ({1}, {2}).",
                    cannon.color,
                    cannon.x,
                    cannon.y);
            }

            return inside;
        }
    }
}
