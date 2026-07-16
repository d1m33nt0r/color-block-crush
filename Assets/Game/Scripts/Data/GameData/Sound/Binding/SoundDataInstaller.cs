using Game.Scripts.Core.Common.Sound.Shared;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Core.Common.Sound.Data.Binding
{
    public class SoundDataInstaller : MonoInstaller
    {
        [SerializeField] private SoundDatabase _soundDatabase;
        
        public override void InstallBindings()
        {
            Container
                .Bind<ISoundDatabase>()
                .To<SoundDatabase>()
                .FromInstance(_soundDatabase)
                .AsSingle();
        }
    }
}