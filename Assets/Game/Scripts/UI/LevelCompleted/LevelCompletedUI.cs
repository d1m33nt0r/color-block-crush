using DG.Tweening;
using Game.Scripts.Core.Common.Sound.Data;
using Game.Scripts.Core.Common.Sound.Shared;
using Game.Scripts.LevelManagement.Shared;
using Game.Scripts.UI.LevelCompleted.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Scripts.UI.LevelCompleted
{
    public class LevelCompletedUI : MonoBehaviour, ILevelCompletedUI
    {
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TMP_Text _completedText;
        [SerializeField] private GameObject _root;

        [Header("Show Animation")]
        [SerializeField, Min(0f)] private float _backgroundDuration = 0.22f;
        [SerializeField, Min(0f)] private float _textDuration = 0.18f;
        [SerializeField, Min(0f)] private float _buttonDuration = 0.16f;
        [SerializeField, Range(0.01f, 1f)] private float _hiddenScaleMultiplier = 0.88f;

        private ILevelManager _levelManager;
        private ISoundPlayer _buttonClickSoundPlayer;
        private CanvasGroup _buttonCanvasGroup;
        private Sequence _showSequence;
        private Color _backgroundColor;
        private Color _textColor;
        private Vector3 _backgroundScale;
        private Vector3 _textScale;
        private Vector3 _buttonScale;
        private float _buttonAlpha = 1f;
        private bool _isCached;

        [Inject]
        public void Construct(
            [InjectOptional] ILevelManager levelManager,
            [InjectOptional] ISoundPlayer buttonClickSoundPlayer)
        {
            _levelManager = levelManager;
            _buttonClickSoundPlayer = buttonClickSoundPlayer;
            _buttonClickSoundPlayer?.Initialize(SoundKey.ButtonClick, transform);
        }

        private void Awake()
        {
            CacheState();

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.AddListener(OnClickNextLevelButton);
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.RemoveListener(OnClickNextLevelButton);
            }

            if (_showSequence != null)
            {
                _showSequence.Kill(false);
            }
        }

        private void OnClickNextLevelButton()
        {
            _buttonClickSoundPlayer?.Play();

            if (_nextLevelButton != null)
            {
                _nextLevelButton.interactable = false;
            }

            _levelManager?.LoadNextLevel();
        }

        public void Show()
        {
            CacheState();
            _root.SetActive(true);
            PlayShowAnimation();
        }

        private void PlayShowAnimation()
        {
            if (_showSequence != null)
            {
                _showSequence.Kill(false);
            }

            PrepareHiddenState();
            _showSequence = DOTween.Sequence().SetLink(gameObject);

            if (_backgroundImage != null)
            {
                _showSequence.Append(FadeGraphic(_backgroundImage, _backgroundColor.a, _backgroundDuration));
                _showSequence.Join(_backgroundImage.transform
                    .DOScale(_backgroundScale, _backgroundDuration)
                    .SetEase(Ease.OutBack));
            }

            if (_completedText != null)
            {
                _showSequence.Append(FadeGraphic(_completedText, _textColor.a, _textDuration));
                _showSequence.Join(_completedText.transform
                    .DOScale(_textScale, _textDuration)
                    .SetEase(Ease.OutBack));
            }

            if (_buttonCanvasGroup != null && _nextLevelButton != null)
            {
                _showSequence.Append(DOTween
                    .To(() => _buttonCanvasGroup.alpha, value => _buttonCanvasGroup.alpha = value, _buttonAlpha, _buttonDuration)
                    .SetEase(Ease.OutQuad));
                _showSequence.Join(_nextLevelButton.transform
                    .DOScale(_buttonScale, _buttonDuration)
                    .SetEase(Ease.OutBack));
            }

            _showSequence.OnComplete(() =>
            {
                if (_nextLevelButton != null)
                {
                    _nextLevelButton.interactable = true;
                }
            });
        }

        private void HideImmediate()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            _root.SetActive(false);
        }

        private void PrepareHiddenState()
        {
            if (_nextLevelButton != null)
            {
                _nextLevelButton.interactable = false;
            }

            SetGraphicAlpha(_backgroundImage, 0f);
            SetGraphicAlpha(_completedText, 0f);

            if (_backgroundImage != null)
            {
                _backgroundImage.transform.localScale = _backgroundScale * _hiddenScaleMultiplier;
            }

            if (_completedText != null)
            {
                _completedText.transform.localScale = _textScale * _hiddenScaleMultiplier;
            }

            if (_buttonCanvasGroup != null)
            {
                _buttonCanvasGroup.alpha = 0f;
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.transform.localScale = _buttonScale * _hiddenScaleMultiplier;
            }
        }

        private void CacheState()
        {
            if (_isCached)
            {
                return;
            }

            if (_root == null)
            {
                _root = gameObject;
            }

            if (_backgroundImage != null)
            {
                _backgroundColor = _backgroundImage.color;
                _backgroundScale = _backgroundImage.transform.localScale;
            }

            if (_completedText != null)
            {
                _textColor = _completedText.color;
                _textScale = _completedText.transform.localScale;
            }

            if (_nextLevelButton != null)
            {
                _buttonCanvasGroup = _nextLevelButton.GetComponent<CanvasGroup>();
                if (_buttonCanvasGroup == null)
                {
                    _buttonCanvasGroup = _nextLevelButton.gameObject.AddComponent<CanvasGroup>();
                }

                _buttonAlpha = _buttonCanvasGroup.alpha;
                _buttonScale = _nextLevelButton.transform.localScale;
            }

            _isCached = true;
        }

        private static Tween FadeGraphic(Graphic graphic, float targetAlpha, float duration)
        {
            if (graphic == null)
            {
                return null;
            }

            return DOTween.To(
                () => graphic.color,
                value => graphic.color = value,
                WithAlpha(graphic.color, targetAlpha),
                duration).SetEase(Ease.OutQuad);
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            graphic.color = WithAlpha(graphic.color, alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
