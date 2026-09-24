using UnityEngine;
using EducationalGame.Quiz;

namespace EducationalGame.School
{
    /// <summary>
    /// Traduz a conclusão das quatro disciplinas em estados de porta.
    /// As notas continuam só no SchoolProgressManager.
    /// </summary>
    public class SchoolMissionManager : MonoBehaviour
    {
        private static SchoolMissionManager _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnEnable()
        {
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged += ApplyProgression;
            }
        }

        private void Start()
        {
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged -= ApplyProgression;
                SchoolProgressManager.Instance.OnProgressChanged += ApplyProgression;
            }

            ApplyProgression();
        }

        private void OnDisable()
        {
            if (SchoolProgressManager.Instance != null)
            {
                SchoolProgressManager.Instance.OnProgressChanged -= ApplyProgression;
            }
        }

        public void ApplyProgression()
        {
            bool mathDone = IsCompleted(ClassroomSubject.Matematica);
            bool portugueseDone = IsCompleted(ClassroomSubject.Portugues);
            bool historyDone = IsCompleted(ClassroomSubject.Historia);
            bool logicDone = IsCompleted(ClassroomSubject.Logica);

            RoomMarker[] markers = FindObjectsByType<RoomMarker>(FindObjectsSortMode.None);
            for (int i = 0; i < markers.Length; i++)
            {
                RoomMarker marker = markers[i];
                if (marker.Role == RoomRole.Ambience)
                {
                    marker.SetAccessState(RoomAccessState.Free);
                    continue;
                }

                if (marker.Role == RoomRole.FinalChallenge)
                {
                    marker.SetAccessState(logicDone ? RoomAccessState.Available : RoomAccessState.Locked);
                    continue;
                }

                if (!marker.HasSubject) continue;

                switch (marker.Subject)
                {
                    case ClassroomSubject.Matematica:
                        marker.SetAccessState(mathDone ? RoomAccessState.Completed : RoomAccessState.Available);
                        break;
                    case ClassroomSubject.Portugues:
                        marker.SetAccessState(portugueseDone ? RoomAccessState.Completed : mathDone ? RoomAccessState.Available : RoomAccessState.Locked);
                        break;
                    case ClassroomSubject.Historia:
                        marker.SetAccessState(historyDone ? RoomAccessState.Completed : portugueseDone ? RoomAccessState.Available : RoomAccessState.Locked);
                        break;
                    case ClassroomSubject.Logica:
                        marker.SetAccessState(logicDone ? RoomAccessState.Completed : historyDone ? RoomAccessState.Available : RoomAccessState.Locked);
                        break;
                }
            }

            SchoolExitGate exit = FindAnyObjectByType<SchoolExitGate>();
            if (exit != null)
            {
                exit.SetLocked(true);
            }
        }

        private static bool IsCompleted(ClassroomSubject subject)
        {
            if (SchoolProgressManager.Instance == null) return false;
            SubjectProgressData progress = SchoolProgressManager.Instance.GetProgress(subject);
            return progress != null && progress.isCompleted;
        }
    }
}
