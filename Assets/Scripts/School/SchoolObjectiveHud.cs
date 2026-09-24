using UnityEngine;
using UnityEngine.UI;
using EducationalGame.Quiz;

namespace EducationalGame.School
{
    /// <summary>
    /// Faixa de objetivo. O texto vem do SchoolMissionManager, que lê o SchoolProgressManager.
    /// </summary>
    public class SchoolObjectiveHud : MonoBehaviour
    {
        private Text _label;

        private void Awake()
        {
            BuildLabel();
        }

        private void OnEnable()
        {
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged += Refresh;
            }

            LibraryFinalChallenge.OnSolved += Refresh;
        }

        private void Start()
        {
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged -= Refresh;
                SchoolProgressManager.Instance.OnProgressChanged += Refresh;
            }

            LibraryFinalChallenge.OnSolved -= Refresh;
            LibraryFinalChallenge.OnSolved += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged -= Refresh;
            }

            LibraryFinalChallenge.OnSolved -= Refresh;
        }

        private void Refresh()
        {
            if (_label == null) return;

            SchoolMissionManager mission = FindAnyObjectByType<SchoolMissionManager>();
            _label.text = mission != null
                ? mission.CurrentObjective
                : "Objetivo: Vá até a Sala 01 — Matemática";
        }

        private void BuildLabel()
        {
            GameObject canvasObj = new GameObject("Objective_Canvas");
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject("ObjectiveLabel");
            panel.transform.SetParent(canvasObj.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -22f);
            rect.sizeDelta = new Vector2(760f, 46f);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.06f, 0.08f, 0.13f, 0.88f);
            background.raycastTarget = false;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 4f);
            textRect.offsetMax = new Vector2(-16f, -4f);

            _label = textObj.AddComponent<Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _label.font = font;
            _label.fontSize = 20;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = new Color(0.95f, 0.95f, 1f);
            _label.raycastTarget = false;
            _label.text = "Objetivo: Vá até a Sala 01 — Matemática";
        }
    }
}
