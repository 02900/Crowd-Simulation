using UnityEngine;

namespace SteeringBehaviours
{
    public class Flee : AgentBehaviour
    {
        public float secureDistance = 3;  //Secure distance to flee and stop

        public override Steering GetSteering()
        {
            // Create the structure to hold our output
            Steering steering = new Steering();

            // Get the direction to the target
            steering.linear = transform.position - target.transform.position;

            // Set secure distance to stop
            if (steering.linear.magnitude <= secureDistance)
            {
                //Give full acceleration along this direction
                steering.linear.Normalize();
                steering.linear *= agent.maxAccel;

                // steering.angular = 0;
            }
            else
            {
                //Reduce speed until it is small enough to set velocity to zero
                steering.linear = -agent.velocity / 1.5f;
                if (steering.linear.magnitude < 1 / 8)
                {
                    agent.velocity = Vector3.zero;
                }
            }

            return steering;
        }
    }
}
