using Zenject;

namespace Game.Scripts.LevelManagement.Binding
{
    public sealed class LevelManagementInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesAndSelfTo<LevelManager>()
                .AsSingle();
        }
    }
}
