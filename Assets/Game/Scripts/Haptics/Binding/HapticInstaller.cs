using Game.Scripts.Haptics;
using Game.Scripts.Haptics.Data;
using Game.Scripts.Haptics.Shared;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Haptics.Binding
{
    public sealed class HapticInstaller : MonoInstaller
    {
        [SerializeField] private HapticSettings _settings = new HapticSettings();

        public override void InstallBindings()
        {
            Container
                .Bind<HapticSettings>()
                .FromInstance(_settings)
                .AsSingle();

            Container
                .Bind<IHapticPlayer>()
                .To<HapticPlayer>()
                .AsTransient();
        }
    }
}
