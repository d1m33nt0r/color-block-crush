using UnityEngine;
using Zenject;

namespace Game.Scripts.UI.LevelCompleted.Binding
{
    public class UILevelCompletedInstaller : MonoInstaller
    {
        [SerializeField] private LevelCompletedUI _levelCompletedUI;
        
        public override void InstallBindings()
        {
            Container
                .BindInterfacesTo<LevelCompletedUI>()
                .FromComponentInNewPrefab(_levelCompletedUI)
                .AsSingle();
        }
    }
}