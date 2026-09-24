using UnityEngine;

namespace EducationalGame.School
{
    /// <summary>
    /// Pista examinável. A interação fica no ExamineController, por mirada e proximidade.
    /// </summary>
    public class ExaminableClue : MonoBehaviour
    {
        [SerializeField] private string title = "Pista";
        [SerializeField] private string body = "";
        [SerializeField] private float maxDistance = 3f;

        public string Title => title;
        public string Body => body;
        public float MaxDistance => maxDistance;

        public void Configure(string clueTitle, string clueBody, float distance)
        {
            title = clueTitle;
            body = clueBody;
            maxDistance = distance;
        }
    }
}
