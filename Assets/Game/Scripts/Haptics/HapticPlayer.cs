using Game.Scripts.Haptics.Data;
using Game.Scripts.Haptics.Shared;
using Solo.MOST_IN_ONE;
using Zenject;

namespace Game.Scripts.Haptics
{
    public sealed class HapticPlayer : IHapticPlayer
    {
        private readonly HapticSettings _settings;

        public HapticPlayer([InjectOptional] HapticSettings settings)
        {
            _settings = settings;
        }

        public void Play(HapticFeedbackType feedbackType)
        {
            if (feedbackType == HapticFeedbackType.None
                || (_settings != null && !_settings.IsEnabled))
            {
                return;
            }

            MOST_HapticFeedback.Generate(ToMostHapticType(feedbackType));
        }

        private static MOST_HapticFeedback.HapticTypes ToMostHapticType(HapticFeedbackType feedbackType)
        {
            switch (feedbackType)
            {
                case HapticFeedbackType.Selection:
                    return MOST_HapticFeedback.HapticTypes.Selection;

                case HapticFeedbackType.LightImpact:
                    return MOST_HapticFeedback.HapticTypes.LightImpact;

                case HapticFeedbackType.HeavyImpact:
                    return MOST_HapticFeedback.HapticTypes.HeavyImpact;

                case HapticFeedbackType.Success:
                    return MOST_HapticFeedback.HapticTypes.Success;

                case HapticFeedbackType.Warning:
                    return MOST_HapticFeedback.HapticTypes.Warning;

                case HapticFeedbackType.Failure:
                    return MOST_HapticFeedback.HapticTypes.Failure;

                case HapticFeedbackType.RigidImpact:
                    return MOST_HapticFeedback.HapticTypes.RigidImpact;

                case HapticFeedbackType.SoftImpact:
                    return MOST_HapticFeedback.HapticTypes.SoftImpact;

                case HapticFeedbackType.MediumImpact:
                default:
                    return MOST_HapticFeedback.HapticTypes.MediumImpact;
            }
        }
    }
}
