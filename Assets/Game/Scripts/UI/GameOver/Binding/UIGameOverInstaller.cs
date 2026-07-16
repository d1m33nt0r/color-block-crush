using UnityEngine;
using Zenject;

namespace Game.Scripts.UI.GameOver.Binding
{
    public class UIGameOverInstaller : MonoInstaller
    {
        [SerializeField] private GameOverUI _gameOverUI;
        
        public override void InstallBindings()
        {
            Container
                .BindInterfacesTo<GameOverUI>()
                .FromComponentInNewPrefab(_gameOverUI)
                .AsSingle();
        }
    }
}