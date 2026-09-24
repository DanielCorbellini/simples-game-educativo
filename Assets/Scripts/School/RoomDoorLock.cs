using UnityEngine;
using EducationalGame.Player;
using EducationalGame.Quiz;

namespace EducationalGame.School
{
    /// <summary>
    /// Bloqueio físico de uma porta já existente. Não recria a escola.
    /// </summary>
    public class RoomDoorLock : MonoBehaviour
    {
        [SerializeField] private Collider solidBlocker;
        [SerializeField] private Collider promptTrigger;
        [SerializeField] private string lockedMessage;
        [SerializeField] private Transform[] doorLeaves;

        private bool _blocked;
        private bool _playerInside;

        public bool IsBlocked => _blocked;
        public string LockedMessage => lockedMessage;

        public void Build(Collider blocker, Collider trigger, string message, bool blocked, Transform[] leaves)
        {
            solidBlocker = blocker;
            promptTrigger = trigger;
            lockedMessage = message;
            doorLeaves = leaves;
            SetBlocked(blocked);
        }

        public void SetBlocked(bool blocked)
        {
            _blocked = blocked;

            if (solidBlocker != null)
            {
                solidBlocker.enabled = blocked;
            }

            ApplyDoorPose(blocked);

            if (promptTrigger != null)
            {
                promptTrigger.enabled = blocked;
            }

            if (!blocked && _playerInside && QuizUIManager.Instance != null)
            {
                QuizUIManager.Instance.HidePrompt();
                _playerInside = false;
            }
        }

        private void ApplyDoorPose(bool blocked)
        {
            if (doorLeaves == null) return;

            for (int i = 0; i < doorLeaves.Length; i++)
            {
                Transform leaf = doorLeaves[i];
                if (leaf == null) continue;

                // O prefab da porta nasce estático. Sem isso, a malha não acompanha a rotação.
                leaf.gameObject.isStatic = false;

                // Abertura no espaço local da folha. O pai já está girado para a esquerda,
                // a direita e a biblioteca, então o mesmo ângulo abre para o corredor nos dois lados.
                float openYaw = (i % 2 == 0) ? 75f : -75f;
                leaf.localRotation = Quaternion.Euler(0f, blocked ? 0f : openYaw, 0f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_blocked || !IsPlayer(other)) return;

            _playerInside = true;
            if (QuizUIManager.Instance != null && !string.IsNullOrEmpty(lockedMessage))
            {
                QuizUIManager.Instance.ShowPrompt(lockedMessage);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;

            _playerInside = false;
            if (QuizUIManager.Instance != null)
            {
                QuizUIManager.Instance.HidePrompt();
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") ||
                   other.GetComponent<FirstPersonController>() != null ||
                   other.GetComponentInParent<FirstPersonController>() != null;
        }
    }
}
