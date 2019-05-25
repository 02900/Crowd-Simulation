using UnityEngine;

namespace SteeringBehaviours
{
    public class Separation : AgentBehaviour
    {
        // Holds a list of potential targets
        [SerializeField] private Transform[] targets;

        // Holds the threshold to take action
        [SerializeField] private float threhold = 0.5f;

        // Holds the constant coefficiente of decay for
        // the inverse square law force
        [SerializeField] private float decayCoefficient = 0.6f;

        public override Steering GetSteering()
        {
            // The steering variable holds the output
            Steering steering = new Steering();

            // Loop through each target
            foreach (Transform target in targets)
            {
                // Check if the target is close
                Vector3 direction = transform.position - target.position;
                float distance = direction.magnitude;
                if (distance < threhold)
                {
                    // Calculate the strength of repulsion
                    float strength = Mathf.Min(decayCoefficient / distance * distance, agent.maxAccel);

                    // Add the acceleration
                    direction.Normalize();
                    steering.linear += strength * direction;
                }

            }

            // We've gone through all targets, return result
            return steering;
        }
    }
}
