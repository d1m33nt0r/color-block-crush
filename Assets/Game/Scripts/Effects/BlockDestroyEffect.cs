using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Game.BlockCrush.Effects
{
    public sealed class BlockDestroyEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] _particleSystems;

        public sealed class Factory : PlaceholderFactory<Transform, BlockDestroyEffect>
        {
        }

        private void Awake()
        {
            CacheParticleSystems();
        }

        public async UniTask PlayAsync(Color color, CancellationToken cancellationToken = default)
        {
            CacheParticleSystems();
            ApplyColor(color);

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(true);
            }

            float duration = GetMaxLifetime();
            if (duration > 0f)
            {
                await UniTask.Delay(
                    Mathf.CeilToInt(duration * 1000f),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await UniTask.Yield(cancellationToken);
            }
        }

        public void StopAndClear()
        {
            CacheParticleSystems();

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem != null)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        public void SetParticleSystems(ParticleSystem[] particleSystems)
        {
            _particleSystems = particleSystems;
        }

        private void CacheParticleSystems()
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }
        }

        private void ApplyColor(Color color)
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
                colorOverLifetime.enabled = true;
                colorOverLifetime.color = Recolor(colorOverLifetime.color, color);
            }
        }

        private float GetMaxLifetime()
        {
            float maxLifetime = 0f;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particleSystem.main;
                float duration = main.duration
                                 + GetMaxConstant(main.startDelay)
                                 + GetMaxConstant(main.startLifetime);

                maxLifetime = Mathf.Max(maxLifetime, duration);
            }

            return maxLifetime;
        }

        private static ParticleSystem.MinMaxGradient Recolor(
            ParticleSystem.MinMaxGradient source,
            Color color)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(WithRgb(color, source.color.a));

                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        WithRgb(color, source.colorMin.a),
                        WithRgb(color, source.colorMax.a));

                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(RecolorGradient(source.gradient, color));

                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        RecolorGradient(source.gradientMin, color),
                        RecolorGradient(source.gradientMax, color));

                case ParticleSystemGradientMode.RandomColor:
                    return new ParticleSystem.MinMaxGradient(RecolorGradient(source.gradient, color));

                default:
                    return new ParticleSystem.MinMaxGradient(color);
            }
        }

        private static Gradient RecolorGradient(Gradient source, Color color)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = source != null ? source.colorKeys : null;
            GradientAlphaKey[] alphaKeys = source != null ? source.alphaKeys : null;

            if (colorKeys == null || colorKeys.Length == 0)
            {
                colorKeys = new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                };
            }
            else
            {
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i].color = WithRgb(color, colorKeys[i].color.a);
                }
            }

            if (alphaKeys == null || alphaKeys.Length == 0)
            {
                alphaKeys = new[]
                {
                    new GradientAlphaKey(color.a, 0f),
                    new GradientAlphaKey(color.a, 1f)
                };
            }

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static Color WithRgb(Color source, float alpha)
        {
            return new Color(source.r, source.g, source.b, alpha);
        }

        private static float GetMaxConstant(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;

                case ParticleSystemCurveMode.TwoConstants:
                    return curve.constantMax;

                default:
                    return curve.constantMax > 0f ? curve.constantMax : curve.constant;
            }
        }
    }
}
