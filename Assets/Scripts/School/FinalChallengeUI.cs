using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EducationalGame.Player;

namespace EducationalGame.School
{
    /// <summary>
    /// Painel do desafio final. Não usa o QuizUIManager.
    /// </summary>
    public class FinalChallengeUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        private static FinalChallengeUI _instance;

        private LibraryFinalChallenge _challenge;
        private readonly List<string> _attempt = new List<string>();
        private GameObject _panel;
        private Text _sequenceText;
        private Text _feedbackText;
        private RectTransform _tokenRow;
        private Font _font;
        private FirstPersonController _player;
        private bool _tokensBuilt;

        public static void Open(LibraryFinalChallenge challenge)
        {
            if (_instance == null)
            {
                GameObject host = new GameObject("Final_Challenge_UI");
                _instance = host.AddComponent<FinalChallengeUI>();
            }

            _instance.Show(challenge);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Build();
        }

        private void Update()
        {
            if (!IsOpen) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void Show(LibraryFinalChallenge challenge)
        {
            _challenge = challenge;
            _attempt.Clear();
            EnsureTokens();
            _feedbackText.text = "Escolha as quatro salas na ordem em que foram concluídas.";
            RefreshSequence();
            _panel.SetActive(true);
            IsOpen = true;

            _player = FindAnyObjectByType<FirstPersonController>();
            if (_player != null)
            {
                _player.enabled = false;
                _player.SetCursorLock(false);
            }
        }

        private void Close()
        {
            IsOpen = false;
            if (_panel != null) _panel.SetActive(false);
            if (_player != null)
            {
                _player.enabled = true;
                _player.SetCursorLock(true);
            }
        }

        private void AddToken(SequenceToken token)
        {
            if (_attempt.Count >= _challenge.Solution.Length) return;
            if (_attempt.Contains(token.id)) return;
            _attempt.Add(token.id);
            _feedbackText.text = "";
            RefreshSequence();
        }

        private void ClearAttempt()
        {
            _attempt.Clear();
            _feedbackText.text = "Sequência limpa. Tente novamente.";
            RefreshSequence();
        }

        private void Confirm()
        {
            if (_challenge.Matches(_attempt.ToArray()))
            {
                _feedbackText.text = "Sequência correta.";
                _challenge.MarkSolved();
                Close();
                return;
            }

            _feedbackText.text = "Sequência incorreta. Tente novamente.";
            _attempt.Clear();
            RefreshSequence();
        }

        private void RefreshSequence()
        {
            if (_challenge == null) return;
            string[] labels = new string[_challenge.Solution.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = i < _attempt.Count ? LabelFor(_attempt[i]) : "—";
            }

            _sequenceText.text = string.Join("  →  ", labels);
        }

        private string LabelFor(string id)
        {
            SequenceToken[] solution = _challenge.Solution;
            for (int i = 0; i < solution.Length; i++)
            {
                if (solution[i].id == id) return solution[i].label;
            }

            return id;
        }

        private void Build()
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 140;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = _panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(820f, 460f);
            Image panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Font font = _font;

            Text title = CreateLabel(_panel.transform, font, 28, FontStyle.Bold, new Vector2(0f, 180f), new Vector2(760f, 40f));
            title.text = "Desafio Final";
            title.alignment = TextAnchor.MiddleCenter;

            _sequenceText = CreateLabel(_panel.transform, font, 22, FontStyle.Bold, new Vector2(0f, 120f), new Vector2(760f, 40f));
            _sequenceText.alignment = TextAnchor.MiddleCenter;

            _feedbackText = CreateLabel(_panel.transform, font, 18, FontStyle.Normal, new Vector2(0f, -150f), new Vector2(740f, 50f));
            _feedbackText.alignment = TextAnchor.MiddleCenter;

            GameObject row = new GameObject("Tokens");
            row.transform.SetParent(_panel.transform, false);
            _tokenRow = row.AddComponent<RectTransform>();
            _tokenRow.anchoredPosition = new Vector2(0f, 40f);
            _tokenRow.sizeDelta = new Vector2(760f, 52f);

            CreateButton(_panel.transform, font, "Confirmar", new Vector2(80f, -80f), new Vector2(180f, 48f), new Color(0.12f, 0.45f, 0.28f), Confirm);
            CreateButton(_panel.transform, font, "Limpar", new Vector2(-120f, -80f), new Vector2(160f, 48f), new Color(0.28f, 0.2f, 0.16f), ClearAttempt);
            CreateButton(_panel.transform, font, "Fechar", new Vector2(280f, -190f), new Vector2(150f, 44f), new Color(0.25f, 0.28f, 0.36f), Close);

            _panel.SetActive(false);
        }

        private void EnsureTokens()
        {
            if (_tokensBuilt || _challenge == null || _tokenRow == null) return;
            _tokensBuilt = true;

            SequenceToken[] tokens = _challenge.Solution;
            float step = tokens.Length > 1 ? 720f / tokens.Length : 0f;
            float x = -step * (tokens.Length - 1) * 0.5f;
            for (int i = 0; i < tokens.Length; i++)
            {
                SequenceToken token = tokens[i];
                CreateButton(_tokenRow, _font, token.label, new Vector2(x, 0f), new Vector2(Mathf.Min(170f, step - 10f), 48f), new Color(0.16f, 0.22f, 0.34f), () => AddToken(token));
                x += step;
            }
        }

        private static Text CreateLabel(Transform parent, Font font, int size, FontStyle style, Vector2 position, Vector2 dimensions)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = obj.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private static void CreateButton(Transform parent, Font font, string label, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject(label);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = obj.AddComponent<Image>();
            image.color = color;
            Button button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            Text text = CreateLabel(obj.transform, font, 18, FontStyle.Bold, Vector2.zero, size);
            text.text = label;
        }
    }
}
