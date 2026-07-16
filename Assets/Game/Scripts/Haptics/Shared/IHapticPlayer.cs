using Game.Scripts.Haptics.Data;

namespace Game.Scripts.Haptics.Shared
{
    public interface IHapticPlayer
    {
        void Play(HapticFeedbackType feedbackType);
    }
}
