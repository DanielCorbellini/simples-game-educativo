using System;
using UnityEngine;
using EducationalGame.Player;
using EducationalGame.Quiz;

namespace EducationalGame.School
{
    [Serializable]
    public struct SequenceToken
    {
        public string id;
        public string label;

        public SequenceToken(string tokenId, string tokenLabel)
        {
            id = tokenId;
            label = tokenLabel;
        }
    }

    /// <summary>
    /// Terminal da biblioteca. A conclusão não grava nota no SchoolProgressManager.
    /// </summary>
    public class LibraryFinalChallenge : MonoBehaviour
    {
        public static LibraryFinalChallenge Instance { get; private set; }
        public static bool IsSolved { get; private set; }
        public static event Action OnSolved;

        [SerializeField] private SequenceToken[] solution =
        {
            new SequenceToken("matematica", "Matemática"),
            new SequenceToken("portugues", "Português"),
            new SequenceToken("historia", "História"),
            new SequenceToken("logica", "Lógica")
        };

        [SerializeField] private float maxDistance = 3.2f;

        private bool _aimed;
        private bool _showingPrompt;

        public SequenceToken[] Solution => solution;

        public static void ResetSession()
        {
            IsSolved = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            if (solution == null || solution.Length == 0)
            {
                solution = new[]
                {
                    new SequenceToken("matematica", "Matemática"),
                    new SequenceToken("portugues", "Português"),
                    new SequenceToken("historia", "História"),
                    new SequenceToken("logica", "Lógica")
                };
            }
        }

        private void LateUpdate()
        {
            if (FinalChallengeUI.IsOpen || (ExamineController.Instance != null && ExamineController.Instance.IsPanelOpen))
            {
                return;
            }

            if (QuizUIManager.Instance != null && QuizUIManager.Instance.IsQuizOpen)
            {
                return;
            }

            RefreshAim();

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame && _aimed)
            {
                if (!AllSubjectsCompleted())
                {
                    return;
                }

                if (IsSolved)
                {
                    if (QuizUIManager.Instance != null)
                    {
                        QuizUIManager.Instance.ShowPrompt("Desafio final já concluído");
                    }
                    return;
                }

                FinalChallengeUI.Open(this);
            }
        }

        public bool Matches(string[] attempt)
        {
            if (attempt == null || solution == null || attempt.Length != solution.Length)
            {
                return false;
            }

            for (int i = 0; i < solution.Length; i++)
            {
                if (attempt[i] != solution[i].id)
                {
                    return false;
                }
            }

            return true;
        }

        public void MarkSolved()
        {
            if (IsSolved) return;
            IsSolved = true;
            OnSolved?.Invoke();
        }

        private void RefreshAim()
        {
            Camera camera = Camera.main;
            FirstPersonController player = FindAnyObjectByType<EducationalGame.Player.FirstPersonController>();
            if (player != null && player.PlayerCamera != null)
            {
                camera = player.PlayerCamera;
            }

            bool found = false;
            if (camera != null && AllSubjectsCompleted() && !IsSolved && !FinalChallengeUI.IsOpen)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<LibraryFinalChallenge>() == this)
                    {
                        found = true;
                    }
                }
            }

            if (found == _aimed) return;
            _aimed = found;

            if (QuizUIManager.Instance == null) return;

            if (_aimed)
            {
                QuizUIManager.Instance.ShowPrompt("Iniciar desafio final");
                _showingPrompt = true;
            }
            else if (_showingPrompt)
            {
                _showingPrompt = false;
                if (ExamineController.Instance != null && ExamineController.Instance.IsAimingAtClue)
                {
                    return;
                }

                QuizUIManager.Instance.HidePrompt();
            }
        }

        private static bool AllSubjectsCompleted()
        {
            return SchoolProgressManager.Instance != null && SchoolProgressManager.Instance.IsAllCompleted();
        }
    }
}
