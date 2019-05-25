using UnityEngine;

namespace SteeringBehaviours
{
    public class ObstacleAvoidance : SeekAlt
    {
        // head of the agent
        public Transform body;

        // Holds the minimum distance to a wall. 
        // (i.e., how far to avoid) should be
        // greater than the radius of the character
        [SerializeField] private float avoidDistance = 3.5f;

        // Holds the distance to look ahead for a collision
        // (i.e, the length of the collision ray)
        [SerializeField] private float lookAhead = 6;
        [SerializeField] [Range(1, 2)] private float coefficientForce = 1;

        [SerializeField] private bool gizmos = false;

        // ... Other data is derived from the superclass ...

        Vector3 targetAux;

        public override void Awake()
        {
            base.Awake();
            target = new GameObject();
        }

        public override Steering GetSteering()
        {
            Steering steering = new Steering();

            // 1. Calculate the target to delegate to seek
            // Calculate the collision ray vector
            Vector3 rayDirection = agent.velocity.normalized * lookAhead;
            rayDirection.y = 0;
            RaycastHit hit;

            // Find the collision
            if (gizmos)
            {
                Debug.DrawRay(body != null ? body.position : transform.position, rayDirection, Color.blue);
                Debug.DrawRay(body != null ? body.position : transform.position, rayDirection + Vector3.right * 1.25f, Color.red);
                Debug.DrawRay(body != null ? body.position : transform.position, rayDirection - Vector3.right * 1.25f, Color.red);
            }

            if (Physics.Raycast(transform.position, rayDirection, out hit, lookAhead)
                || Physics.Raycast(transform.position, rayDirection + Vector3.right * 1.25f, out hit, lookAhead)
                || Physics.Raycast(transform.position, rayDirection - Vector3.right * 1.25f, out hit, lookAhead))
            {
                if (hit.transform.tag != "vertex" && hit.transform.tag != "Player")
                {
                    target.transform.position = hit.point + hit.normal * avoidDistance;
                    targetAux = target.transform.position;
                }
                else
                {
                    targetAux = Vector3.zero;
                    return steering;
                }
            }

            else
            {
                targetAux = Vector3.zero;
                return steering;
            }

            steering = base.GetSteering();
            steering.linear *= 100 * coefficientForce;

            return steering;
        }

        void OnDrawGizmos()
        {
            if (gizmos)
            {
                Gizmos.color = Color.red;
                if (targetAux != Vector3.zero && target.transform.position != Vector3.zero)
                {
                    Gizmos.DrawLine(body != null ? body.position : transform.position + Vector3.up * 0.2f, target.transform.position);
                    Gizmos.DrawLine(body != null ? body.position : transform.position + Vector3.up, target.transform.position);
                    Gizmos.DrawSphere(target.transform.position, 0.12f);
                }
            }
        }
    }
}
