using UnityEngine;

namespace AnticipatoryModel
{
    public class AMVirtualAgent : Agent
    {
        [SerializeField] bool used;
        [SerializeField] int usedByID;
        [SerializeField] bool debug;

        public bool Used { get { return used; } set { used = value; } }

        public void SetupAgent(float radius, Vector2 position, Vector2 velocity)
        {
            this.radius = radius;
            this.position = position;
            this.velocity = velocity;
            used = true;
            isVirtual = true;
        }

        void OnDrawGizmos()
        {
            if (!debug) return;

            int i = 0;
            Gizmos.color = Color.green;
            float theta = 0;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = new Vector3(position.x + x, 0, position.y + y);
            Vector3 newPos = pos;
            Vector3 lastPos = pos;

            for (theta = 0.1f; theta < Mathf.PI * 2; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);
                newPos  = new Vector3(position.x + x, 0, position.y + y);
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
            i++;
        }
    }
}
