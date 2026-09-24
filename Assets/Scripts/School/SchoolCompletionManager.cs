using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EducationalGame.Player;
using EducationalGame.Quiz;

namespace EducationalGame.School
{
    /// <summary>
    /// Libera a saída quando o desafio final termina e encerra o jogo ao atravessá-la.
    /// </summary>
    public class SchoolCompletionManager : MonoBehaviour
    {
        private bool _escapeFinished;
        private GameObject _endPanel;

        private void OnEnable()
        {
            LibraryFinalChallenge.OnSolved += HandleChallengeSolved;
        }

        private void Start()
        {
            EnsureExitSensor();
            if (LibraryFinalChallenge.IsSolved)
            {
                HandleChallengeSolved();
            }
        }

        private void OnDisable()
        {
            LibraryFinalChallenge.OnSolved -= HandleChallengeSolved;
        }

        private void HandleChallengeSolved()
        {
            SchoolExitGate exit = FindAnyObjectByType<SchoolExitGate>();
            if (exit != null)
            {
                exit.SetLocked(false);
            }

            SchoolMissionManager mission = FindAnyObjectByType<SchoolMissionManager>();
            if (mission != null)
            {
                mission.ApplyProgression();
            }
        }

        private void EnsureExitSensor()
        {
            SchoolExitGate exit = FindAnyObjectByType<SchoolExitGate>();
            if (exit == null || exit.GetComponentInChildren<SchoolExitSensor>() != null)
            {
                return;
            }

            GameObject sensor = new GameObject("Exit_Sensor");
            sensor.transform.SetParent(exit.transform, false);
            sensor.transform.localPosition = new Vector3(0f, 1.1f, -1.4f);
            BoxCollider trigger = sensor.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.2f, 2.4f, 1.6f);
            sensor.AddComponent<SchoolExitSensor>().Bind(this, exit);
        }

        public void NotifyPlayerExited(SchoolExitGate exit)
        {
            if (_escapeFinished || exit == null || exit.IsLocked) return;
            _escapeFinished = true;
            ShowEndScreen();

            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            if (player != null)
            {
                player.enabled = false;
                player.SetCursorLock(false);
            }
        }

        private void ShowEndScreen()
        {
            if (_endPanel != null)
            {
                _endPanel.SetActive(true);
                return;
            }

            GameObject canvasObj = new GameObject("Completion_Canvas");
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            _endPanel = new GameObject("EndPanel");
            _endPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rect = _endPanel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(780f, 420f);
            Image image = _endPanel.AddComponent<Image>();
            image.color = new Color(0.06f, 0.08f, 0.13f, 0.97f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            CreateText(_endPanel.transform, font, "Escape concluído!", 34, new Vector2(0f, 120f), new Vector2(700f, 50f));
            CreateText(_endPanel.transform, font, "Você concluiu todas as atividades e conseguiu sair da escola.", 20, new Vector2(0f, 40f), new Vector2(680f, 80f));

            CreateButton(_endPanel.transform, font, "Jogar novamente", new Vector2(-130f, -120f), Restart);
            CreateButton(_endPanel.transform, font, "Sair", new Vector2(130f, -120f), QuitGame);
        }

        private void Restart()
        {
            LibraryFinalChallenge.ResetSession();
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.ResetProgress();
            }

            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.name);
        }

        private static void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private static void CreateText(Transform parent, Font font, string value, int size, Vector2 position, Vector2 dimensions)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = obj.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
        }

        private static void CreateButton(Transform parent, Font font, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = new GameObject(label);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 52f);
            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.16f, 0.28f, 0.46f, 1f);
            Button button = obj.AddComponent<Button>();
            button.onClick.AddListener(action);
            CreateText(obj.transform, font, label, 18, Vector2.zero, new Vector2(200f, 40f));
        }
    }

    public class SchoolExitSensor : MonoBehaviour
    {
        private SchoolCompletionManager _manager;
        private SchoolExitGate _exit;

        public void Bind(SchoolCompletionManager manager, SchoolExitGate exit)
        {
            _manager = manager;
            _exit = exit;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_manager == null || _exit == null || _exit.IsLocked) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<FirstPersonController>() == null)
            {
                return;
            }

            _manager.NotifyPlayerExited(_exit);
        }
    }
}
