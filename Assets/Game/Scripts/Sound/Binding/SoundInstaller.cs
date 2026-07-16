using Game.Scripts.Core.Common.Sound.Data;
using Game.Scripts.Core.Common.Sound.Shared;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Core.Common.Sound.Binding
{
    public class SoundInstaller : MonoInstaller
    {
        [SerializeField] private SoundPlayer _soundPlayer;
        
        public override void InstallBindings()
        {
            Container
                .Bind<ISoundPlayer>()
                .To<SoundPlayer>()
                .FromComponentInNewPrefab(_soundPlayer)
                .AsTransient();
        }
    }
}