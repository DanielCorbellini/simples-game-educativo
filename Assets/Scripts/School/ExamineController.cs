using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EducationalGame.Player;
using EducationalGame.Quiz;

namespace EducationalGame.School
{
    /// <summary>
    /// Raycast a partir da câmera do jogador. Não altera o FirstPersonController:
    /// só desliga o componente enquanto o painel da pista está aberto.
    /// </summary>
    public class ExamineController : MonoBehaviour
    {
        public static ExamineController Instance { get; private set; }

        private const float RayDistance = 4f;

        private ExaminableClue _aimed;
        private bool _showingPrompt;
        private bool _panelOpen;
        private FirstPersonController _player;
        private GameObject _panel;
        private Text _titleText;
        private Text _bodyText;

        public bool IsPanelOpen => _panelOpen;
        public bool IsAimingAtClue => _aimed != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildPanel();
        }

        private void Update()
        {
            if (FinalChallengeUI.IsOpen)
            {
                return;
            }

            if (_panelOpen)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    ClosePanel();
                }
                return;
            }

            if (QuizUIManager.Instance != null && QuizUIManager.Instance.IsQuizOpen)
            {
                ClearAim();
                return;
            }

            RefreshAim();

            Keyboard keys = Keyboard.current;
            if (keys != null && keys.eKey.wasPressedThisFrame && _aimed != null)
            {
                OpenPanel(_aimed);
            }
        }

        public void RefreshAim()
        {
            if (_panelOpen) return;

            Camera camera = ResolveCamera();
            ExaminableClue found = null;
            if (camera != null)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, RayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    ExaminableClue clue = hit.collider.GetComponentInParent<ExaminableClue>();
                    if (clue != null && hit.distance <= clue.MaxDistance)
                    {
                        found = clue;
                    }
                }
            }

            if (found == _aimed) return;

            _aimed = found;
            if (_aimed != null)
            {
                if (QuizUIManager.Instance != null)
                {
                    QuizUIManager.Instance.ShowPrompt("Examinar");
                    _showingPrompt = true;
                }
            }
            else
            {
                ClearAim();
            }
        }

        private void ClearAim()
        {
            _aimed = null;
            if (!_showingPrompt) return;

            _showingPrompt = false;
            if (QuizUIManager.Instance != null)
            {
                QuizUIManager.Instance.HidePrompt();
            }

            ClassroomInteractionZone[] zones = FindObjectsByType<ClassroomInteractionZone>(FindObjectsSortMode.None);
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i].UpdatePromptMessage();
            }
        }

        private void OpenPanel(ExaminableClue clue)
        {
            _panelOpen = true;
            _aimed = null;
            _showingPrompt = false;
            if (QuizUIManager.Instance != null)
            {
                QuizUIManager.Instance.HidePrompt();
            }

            _titleText.text = clue.Title;
            _bodyText.text = clue.Body;
            _panel.SetActive(true);

            _player = FindAnyObjectByType<FirstPersonController>();
            if (_player != null)
            {
                _player.enabled = false;
                _player.SetCursorLock(false);
            }
        }

        private void ClosePanel()
        {
            _panelOpen = false;
            if (_panel != null) _panel.SetActive(false);

            if (_player != null)
            {
                _player.enabled = true;
                _player.SetCursorLock(true);
            }
        }

        private static Camera ResolveCamera()
        {
            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            if (player != null && player.PlayerCamera != null)
            {
                return player.PlayerCamera;
            }

            return Camera.main;
        }

        private void BuildPanel()
        {
            GameObject canvasObj = new GameObject("Examine_Canvas");
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("ExaminePanel");
            _panel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 340f);

            Image background = _panel.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.15f, 0.96f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _titleText = CreateText(_panel.transform, font, 26, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(28f, 252f), new Vector2(-28f, -16f));
            _bodyText = CreateText(_panel.transform, font, 20, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(28f, 78f), new Vector2(-28f, -96f));

            GameObject buttonObj = new GameObject("CloseButton");
            buttonObj.transform.SetParent(_panel.transform, false);
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-24f, 20f);
            buttonRect.sizeDelta = new Vector2(180f, 48f);
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.45f, 0.85f, 1f);
            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(ClosePanel);
            Text buttonLabel = CreateText(buttonObj.transform, font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            buttonLabel.text = "Fechar";

            _panel.SetActive(false);
        }

        private static Text CreateText(Transform parent, Font font, int size, FontStyle style, TextAnchor anchor, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Text text = textObj.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
