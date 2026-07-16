using Zenject;

namespace Game.Scripts.Data.UserData.Levels.Binding
{
    public class LevelsUserDataInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container
                .BindInterfacesTo<LevelsUserData>()
                .AsSingle();
        }
    }
}