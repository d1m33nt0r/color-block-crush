using System;
using Cysharp.Threading.Tasks;
using Game.BlockCrush.Cannon;
using Game.BlockCrush.Grid;
using Game.BlockCrush.Shared;
using Game.Scripts.LevelManagement.Shared;
using UnityEngine;
using Zenject;
using GameLevelData = Game.Scripts.Data.GameData.Levels.Data.LevelData;
using Game.BlockCrush.Level.Shared;

namespace Game.BlockCrush.Level
{
    public sealed class LevelBootstrapper : MonoBehaviour
    {
        [SerializeField] private TextAsset _levelJson;
        [SerializeField] private TextAsset _cannonLevelJson;
        [SerializeField] private BlockGrid _grid;
        [SerializeField] private CannonGrid _cannonGrid;
        [SerializeField] private bool _loadOnStart = true;
        [SerializeField] private bool _applyGravityAfterLoad;
        [SerializeField] private bool _applyCannonGravityAfterLoad = true;
        [SerializeField] private bool _resolveCannonsAfterLoad = true;

        private LevelJsonParser _jsonParser;
        private CannonJsonParser _cannonJsonParser;
        private ILevelManager _levelManager;

        public bool IsLoaded { get; private set; }

        [Inject]
        public void Construct(
            LevelJsonParser jsonParser,
            CannonJsonParser cannonJsonParser,
            [InjectOptional] BlockGrid grid,
            [InjectOptional] CannonGrid cannonGrid,
            [InjectOptional] ILevelManager levelManager)
        {
            _jsonParser = jsonParser;
            _cannonJsonParser = cannonJsonParser;
            _levelManager = levelManager;

            if (_grid == null)
            {
                _grid = grid;
            }

            if (_cannonGrid == null)
            {
                _cannonGrid = cannonGrid;
            }
        }

        private void Awake()
        {
            if (_jsonParser == null)
            {
                _jsonParser = new LevelJsonParser();
            }

            if (_cannonJsonParser == null)
            {
                _cannonJsonParser = new CannonJsonParser();
            }

            if (_grid == null)
            {
                _grid = GetComponent<BlockGrid>();
            }

            if (_cannonGrid == null)
            {
                _cannonGrid = GetComponent<CannonGrid>();
            }
        }

        private void Start()
        {
            if (_loadOnStart)
            {
                LoadAsync().Forget();
            }
        }

        public async UniTask LoadAsync()
        {
            IsLoaded = false;

            TextAsset levelJson = _levelJson;
            TextAsset cannonLevelJson = _cannonLevelJson;

            if (_levelManager != null && _levelManager.TryGetCurrentLevel(out GameLevelData levelData))
            {
                levelJson = levelData.BlocksData;
                cannonLevelJson = levelData.CannonsData;
            }

            if (levelJson == null && cannonLevelJson == null)
            {
                Debug.LogWarning("Block Crush level JSON and cannon JSON are not assigned.", this);
                return;
            }

            if (levelJson != null && _grid == null)
            {
                Debug.LogError("Block Crush grid is not assigned.", this);
                return;
            }

            if (cannonLevelJson != null && _cannonGrid == null)
            {
                Debug.LogError("Block Crush cannon grid is not assigned.", this);
                return;
            }

            try
            {
                if (levelJson != null)
                {
                    Game.BlockCrush.Level.Shared.LevelData blockLevelData = _jsonParser.FromJson(levelJson.text);
                    await _grid.LoadLevelAsync(blockLevelData, true, this.GetCancellationTokenOnDestroy());

                    if (_applyGravityAfterLoad)
                    {
                        await _grid.ApplyGravityAsync(false, this.GetCancellationTokenOnDestroy());
                    }
                }

                if (cannonLevelJson != null)
                {
                    CannonLevelData cannonLevelData = _cannonJsonParser.FromJson(cannonLevelJson.text);
                    await _cannonGrid.LoadLevelAsync(
                        cannonLevelData,
                        !_applyCannonGravityAfterLoad,
                        this.GetCancellationTokenOnDestroy());

                    if (_resolveCannonsAfterLoad)
                    {
                        await _cannonGrid.ResolveBoardAsync(this.GetCancellationTokenOnDestroy());
                    }
                }

                IsLoaded = true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to load Block Crush level JSON: " + exception, this);
            }
        }
    }
}
