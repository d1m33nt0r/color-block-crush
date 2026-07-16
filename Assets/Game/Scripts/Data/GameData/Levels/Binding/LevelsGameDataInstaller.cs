using Game.Scripts.Data.GameData.Levels.Shared;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Data.GameData.Levels.Binding
{
    public class LevelsGameDataInstaller : MonoInstaller
    {
        [SerializeField] private LevelDatabase _levelDatabase;
        
        public override void InstallBindings()
        {
            Container
                .Bind<ILevelDatabase>()
                .To<LevelDatabase>()
                .FromInstance(_levelDatabase)
                .AsSingle();
        }
    }
}