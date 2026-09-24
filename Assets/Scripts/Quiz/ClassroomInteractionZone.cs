using UnityEngine;
using UnityEngine.InputSystem;
using EducationalGame.Player;

namespace EducationalGame.Quiz
{
    /// <summary>
    /// Zona de interação 3D no chão em frente à lousa interativa de cada sala de aula.
    /// Detecta a entrada do jogador, exibe o prompt na tela e abre o minigame ao pressionar E.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ClassroomInteractionZone : MonoBehaviour
    {
        [Header("Configurações da Sala")]
        [Tooltip("Disciplina e tema curricular desta sala de aula.")]
        [SerializeField] private ClassroomSubject subject = ClassroomSubject.Matematica;

        [Tooltip("Indica se o minigame desta sala já foi concluído pelo jogador.")]
        [SerializeField] private bool isCompleted = false;

        [Header("Elementos Visuais")]
        [Tooltip("Ponto de luz suave temático da plataforma.")]
        [SerializeField] private Light zoneLight;

        [Tooltip("Renderer do círculo/disco emissivo no chão.")]
        [SerializeField] private Renderer markerRenderer;

        private bool _playerInside = false;

        public ClassroomSubject Subject
        {
            get => subject;
            set => subject = value;
        }

        public bool IsCompleted => isCompleted;

        public Light ZoneLight
        {
            get => zoneLight;
            set => zoneLight = value;
        }

        public Renderer MarkerRenderer
        {
            get => markerRenderer;
            set => markerRenderer = value;
        }

        private void Start()
        {
            if (SchoolProgressManager.Instance != null)
            {
                var progress = SchoolProgressManager.Instance.GetProgress(subject);
                if (progress != null && progress.isCompleted)
                {
                    isCompleted = true;
                }
            }
            UpdateVisualColors();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                _playerInside = true;
                UpdatePromptMessage();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                _playerInside = false;
                if (QuizUIManager.Instance != null)
                {
                    QuizUIManager.Instance.HidePrompt();
                }
            }
        }

        private QuizUIManager GetOrCreateQuizManager()
        {
            if (QuizUIManager.Instance != null) return QuizUIManager.Instance;
            var existing = FindAnyObjectByType<QuizUIManager>();
            if (existing != null) return existing;
            GameObject obj = new GameObject("Quiz_Manager");
            return obj.AddComponent<QuizUIManager>();
        }

        private void Update()
        {
            if (!_playerInside) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                var mgr = GetOrCreateQuizManager();
                if (mgr != null && !mgr.IsQuizOpen)
                {
                    mgr.StartQuiz(subject, this);
                }
            }
        }

        public void UpdatePromptMessage()
        {
            if (!_playerInside) return;

            string subjectName = QuizDatabase.GetSubjectDisplayName(subject);
            string message = isCompleted
                ? $"[E] Revisar Desafio de {subjectName} (Concluído ★)"
                : $"[E] Iniciar Desafio de {subjectName}";

            var mgr = GetOrCreateQuizManager();
            if (mgr != null)
            {
                mgr.ShowPrompt(message);
            }
        }

        /// <summary>
        /// Marca o desafio da sala como concluído e atualiza a iluminação para dourado triunfante.
        /// </summary>
        public void SetCompleted(bool completed)
        {
            isCompleted = completed;
            UpdateVisualColors();
            UpdatePromptMessage();
        }

        private void UpdateVisualColors()
        {
            Color activeColor = isCompleted
                ? new Color(1.0f, 0.85f, 0.20f) // Dourado vitorioso
                : QuizDatabase.GetSubjectThemeColor(subject);

            if (zoneLight != null)
            {
                zoneLight.color = activeColor;
            }

            if (markerRenderer != null && markerRenderer.material != null)
            {
                markerRenderer.material.color = activeColor;
                if (markerRenderer.material.HasProperty("_EmissionColor"))
                {
                    markerRenderer.material.SetColor("_EmissionColor", activeColor * 0.75f);
                    markerRenderer.material.EnableKeyword("_EMISSION");
                }
            }
        }

        private bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") ||
                   other.GetComponent<FirstPersonController>() != null ||
                   other.GetComponentInParent<FirstPersonController>() != null;
        }
    }
}
