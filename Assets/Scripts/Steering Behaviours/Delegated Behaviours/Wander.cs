using UnityEngine;

namespace SteeringBehaviours
{
    public class Wander : Face
    {
        // Holds the radius forward offset of the wander
        // circule
        [SerializeField] private float wanderOffset = 0.5f;
        [SerializeField] private float wanderRadius = 10f;

        // Holds the maximun rate at which the wander orientation
        // can change
        [SerializeField] private float wanderRate = 0.2f;

        // Again we don't need a new target

        // Other data is derived from the superclass ...

        public override void Awake()
        {
            target = new GameObject();
            target.transform.position = transform.position;
            base.Awake();
        }

        public override Steering GetSteering()
        {
            Steering steering = new Steering();
            // 1. Calculate the target to delegate to face

            // Holds the current orientation of the wander target
            float wanderOrientation;
            // Update the wander orientation
            wanderOrientation = Random.Range(-1.0f, 1.0f) * wanderRate;

            // Calculate the combined target orientation
            float targetOrientation = wanderOrientation + agent.orientation;

            // Map the orientation of the agent to a Vector3
            Vector3 tarOrientation = Orientation2Vector(agent.orientation);

            // Calculate the center of the wander circle
            Vector3 tarPosition = transform.position + (wanderOffset * tarOrientation);

            //Calculate the target location
            tarPosition += wanderRadius * Orientation2Vector(targetOrientation);

            target.transform.position = tarPosition;

            // 2. Delegate to face
            steering = base.GetSteering();
            steering.linear = target.transform.position - transform.position;
            steering.linear.Normalize();
            steering.linear *= agent.maxAccel;
            return steering;
        }

        public Vector3 Orientation2Vector(float orientation)
        {
            Vector3 vector = Vector3.zero;
            vector.x = Mathf.Sin(orientation * Mathf.Deg2Rad) * 1.0f;
            vector.z = Mathf.Cos(orientation * Mathf.Deg2Rad) * 1.0f;
            return vector.normalized;
        }

        void OnDrawGizmos()
        {
            if (target)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(target.transform.position, 0.8f);
                Gizmos.DrawLine(transform.position, target.transform.position);
            }
        }
    }
}
