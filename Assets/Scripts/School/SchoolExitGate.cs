using UnityEngine;

namespace EducationalGame.School
{
    /// <summary>
    /// Saída oficial. Nesta etapa permanece trancada mesmo depois da biblioteca.
    /// </summary>
    public class SchoolExitGate : MonoBehaviour
    {
        [SerializeField] private Collider lockCollider;
        [SerializeField] private RoomDoorLock doorLock;
        [SerializeField] private bool isLocked = true;

        public bool IsLocked => isLocked;
        public Collider LockCollider => lockCollider;

        public void Configure(Collider blocker, RoomDoorLock lockComponent)
        {
            lockCollider = blocker;
            doorLock = lockComponent;
            SetLocked(true);
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            if (lockCollider != null)
            {
                lockCollider.enabled = locked;
            }

            if (doorLock != null)
            {
                doorLock.SetBlocked(locked);
            }
        }
    }
}
