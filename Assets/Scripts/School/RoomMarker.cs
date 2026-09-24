using UnityEngine;
using EducationalGame.Quiz;

namespace EducationalGame.School
{
    public enum RoomRole
    {
        Mission,
        Ambience,
        FinalChallenge
    }

    public enum RoomAccessState
    {
        Locked,
        Available,
        Completed,
        Free
    }

    /// <summary>
    /// Metadados de uma sala e o bloqueio da porta associado.
    /// O estado Locked liga o collider; os demais deixam a passagem livre.
    /// </summary>
    public class RoomMarker : MonoBehaviour
    {
        [SerializeField] private int roomNumber;
        [SerializeField] private string displayName;
        [SerializeField] private RoomRole role;
        [SerializeField] private RoomAccessState accessState;
        [SerializeField] private bool hasSubject;
        [SerializeField] private ClassroomSubject subject;
        [SerializeField] private RoomDoorLock doorLock;

        public int RoomNumber => roomNumber;
        public string DisplayName => displayName;
        public RoomRole Role => role;
        public RoomAccessState AccessState => accessState;
        public bool HasSubject => hasSubject;
        public ClassroomSubject Subject => subject;
        public RoomDoorLock DoorLock => doorLock;

        public void BindDoorLock(RoomDoorLock lockComponent)
        {
            doorLock = lockComponent;
            SetAccessState(accessState);
        }

        public void SetAccessState(RoomAccessState state)
        {
            accessState = state;
            if (doorLock != null)
            {
                doorLock.SetBlocked(state == RoomAccessState.Locked);
            }
        }

        public void Configure(int number, string name, RoomRole roomRole, RoomAccessState state, bool withSubject, ClassroomSubject classroomSubject)
        {
            roomNumber = number;
            displayName = name;
            role = roomRole;
            accessState = state;
            hasSubject = withSubject;
            if (withSubject)
            {
                subject = classroomSubject;
            }
        }
    }
}
