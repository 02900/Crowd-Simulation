using System.Collections.Generic;
using UnityEngine;

namespace SteeringBehaviours
{
    public class Seek : AgentBehaviour
    {
        public float targetRadius = 3;
        [HideInInspector] public Queue<Vector3> targets = new Queue<Vector3>();

        public override Steering GetSteering()
        {
            // Create the structure to hold our output
            Steering steering = new Steering();

            if (target == null)
            {
                enabled = false;
                return steering;
            }

            // Get the direction to the target
            Vector3 direction = target != null ? target.transform.position - transform.position : Vector3.zero;
            float distance = direction.magnitude;

            //Check if we are there, return no steering
            if (distance < targetRadius)
            {
                if (targets.Count > 0)
                {
                    target.transform.position = targets.Dequeue();
                }
                else
                {
                    Destroy(target);
                    enabled = false;
                }

                return steering;
            }

            // Get the direction to the target
            steering.linear = direction;

            //Give full acceleration along this direction
            steering.linear.Normalize();
            steering.linear *= agent.maxAccel;

            return steering;
        }
    }
}
