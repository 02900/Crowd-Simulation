using UnityEngine;

namespace AnticipatoryModel
{
    public abstract class Agent : MonoBehaviour
    {
        public int id { get; set; }
        public float radius { get; set; }
        [HideInInspector] public Vector2 position;
        [HideInInspector] public Vector2 velocity;

        protected bool isVirtual;
        public bool IsVirtual { get { return isVirtual; } }
    }
}