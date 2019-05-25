using UnityEngine;

namespace SteeringBehaviours
{
    public class Arrive : AgentBehaviour
    {
        public float targetRadius = 3;                  //Radius for arriving at the target
        public float slowRadius = 20;                   //Radius for beginning to slow down
        public float timeToTarget = 0.1f;               //Time over wich to achieve target speed
        public bool onTarget;

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
            Vector3 direction = target.transform.position - transform.position;
            float distance = direction.magnitude;

            //Check if we are there, return no steering
            if (distance < targetRadius)
            {
                onTarget = true;
                return steering;
            }

            if (distance > targetRadius + 2.4f)
                onTarget = false;

            if (!onTarget)
            {
                float targetSpeed;

                //If we're outside de slowRadius, then go max speed
                if (distance > slowRadius)
                    targetSpeed = agent.maxSpeed;

                //Otherwise we calculate a scaled speed
                else
                    targetSpeed = agent.maxSpeed * distance / slowRadius;

                Vector3 targetVelocity = direction;
                //desired velocity combines speed and direction
                targetVelocity = direction;
                targetVelocity.Normalize();
                targetVelocity *= targetSpeed;

                //Acceleration tries to get target velocity
                steering.linear = targetVelocity - agent.velocity;
                steering.linear /= timeToTarget;

                //Check if the acceleration is too fast
                if (steering.linear.magnitude > agent.maxAccel)
                {
                    steering.linear.Normalize();
                    steering.linear *= agent.maxAccel;
                }
            }

            return steering;
        }
    }
}
