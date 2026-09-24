using System;
using System.Collections.Generic;
using UnityEngine;

namespace EducationalGame.Quiz
{
    [Serializable]
    public class SubjectProgressData
    {
        public ClassroomSubject subject;
        public bool isCompleted;
        public int bestScore;
        public int totalQuestions;
        public int stars; // 0 = Não realizado, 1 a 3 estrelas

        public SubjectProgressData(ClassroomSubject subj)
        {
            subject = subj;
            isCompleted = false;
            bestScore = 0;
            totalQuestions = 4;
            stars = 0;
        }

        public float ScorePercentage => totalQuestions > 0 ? (float)bestScore / totalQuestions : 0f;
    }

    /// <summary>
    /// Gerenciador centralizado do progresso escolar do jogador.
    /// Acompanha o status de conclusão, pontuação máxima e estrelas em cada uma das 4 salas de aula.
    /// Dispara eventos de atualização em tempo real para o HUD e o Boletim Escolar (TAB).
    /// </summary>
    public class SchoolProgressManager : MonoBehaviour
    {
        private static SchoolProgressManager _instance;
        public static SchoolProgressManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SchoolProgressManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SchoolProgressManager");
                        _instance = go.AddComponent<SchoolProgressManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Progresso por Disciplina")]
        [SerializeField] private List<SubjectProgressData> progressList = new List<SubjectProgressData>();

        private readonly Dictionary<ClassroomSubject, SubjectProgressData> _progressDict = new Dictionary<ClassroomSubject, SubjectProgressData>();

        public event Action OnProgressChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            InitializeProgress();
        }

        private void InitializeProgress()
        {
            _progressDict.Clear();

            // Garantir que todas as disciplinas do enum tenham registro inicial
            foreach (ClassroomSubject subject in Enum.GetValues(typeof(ClassroomSubject)))
            {
                SubjectProgressData data = progressList.Find(p => p.subject == subject);
                if (data == null)
                {
                    data = new SubjectProgressData(subject);
                    progressList.Add(data);
                }
                _progressDict[subject] = data;
            }
        }

        /// <summary>
        /// Registra a pontuação e conclusão de um desafio escolar.
        /// </summary>
        public void RecordCompletion(ClassroomSubject subject, int score, int total)
        {
            if (!_progressDict.ContainsKey(subject))
            {
                InitializeProgress();
            }

            SubjectProgressData data = _progressDict[subject];
            data.totalQuestions = total;

            if (score > data.bestScore)
            {
                data.bestScore = score;
            }

            float ratio = (float)score / total;

            if (ratio >= 0.75f)
            {
                data.stars = Mathf.Max(data.stars, 3);
            }
            else if (ratio >= 0.50f)
            {
                data.stars = Mathf.Max(data.stars, 2);
            }
            else
            {
                data.stars = Mathf.Max(data.stars, 1);
            }

            if (ratio >= 0.50f)
            {
                data.isCompleted = true;
            }

            OnProgressChanged?.Invoke();
        }

        /// <summary>
        /// Retorna os dados de progresso de uma matéria específica.
        /// </summary>
        public SubjectProgressData GetProgress(ClassroomSubject subject)
        {
            if (!_progressDict.ContainsKey(subject))
            {
                InitializeProgress();
            }
            return _progressDict[subject];
        }

        /// <summary>
        /// Retorna a quantidade de salas de aula já concluídas com sucesso.
        /// </summary>
        public int GetCompletedCount()
        {
            int count = 0;
            foreach (var kvp in _progressDict)
            {
                if (kvp.Value.isCompleted) count++;
            }
            return count;
        }

        /// <summary>
        /// Retorna a quantidade total de salas temáticas da escola (4 salas).
        /// </summary>
        public int GetTotalRoomsCount()
        {
            return Enum.GetValues(typeof(ClassroomSubject)).Length;
        }

        /// <summary>
        /// Retorna a porcentagem global de conclusão da escola (0.0f a 1.0f).
        /// </summary>
        public float GetGlobalCompletionPercentage()
        {
            int total = GetTotalRoomsCount();
            return total > 0 ? (float)GetCompletedCount() / total : 0f;
        }

        /// <summary>
        /// Retorna verdadeiro se todas as 4 salas foram concluídas com sucesso.
        /// </summary>
        public bool IsAllCompleted()
        {
            return GetCompletedCount() >= GetTotalRoomsCount();
        }

        /// <summary>
        /// Reinicia o progresso escolar para novo ano letivo (opcional).
        /// </summary>
        public void ResetProgress()
        {
            progressList.Clear();
            InitializeProgress();
            OnProgressChanged?.Invoke();
        }
    }
}
