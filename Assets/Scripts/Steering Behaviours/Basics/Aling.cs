using UnityEngine;

namespace SteeringBehaviours
{
    public class Aling : AgentBehaviour
    {
        // Holds the max angular acceleration and rotation 
        // of the character

        private float angularAccel;

        [SerializeField]
        private float maxRotation = 360;

        // Holds the radius for arriving at the target
        [SerializeField]
        private float targetRadius = 1;

        // Holds the radius for beginning to slow down
        [SerializeField]
        private float slowRadius = 2;

        //Holds the time over which to achieve target speed
        [SerializeField]
        private float timeToTarget = 0.1f;

        //[SerializeField]
        private bool opposite = false;

        // Use this for initialization
        public override Steering GetSteering()
        {
            // Create the structure to hold our output
            Steering steering = new Steering();

            if (target == null)
            {
                enabled = false;
                return steering;
            }

            // Get the naive direction to the target
            float rotation = target.transform.eulerAngles.y - agent.orientation;
            //float rotation = target.transform.eulerAngles.y - transform.eulerAngles.y;
            rotation += opposite ? 180 : 0;

            // Map the result to the (-pi, pi) interval
            rotation %= 360.0f;
            if (rotation < -180 || rotation > 180)
                rotation += rotation < -180 ? 360 : -360;

            float rotationSize = Mathf.Abs(rotation);

            //Check if we are there, return no steering
            if (rotationSize < targetRadius)
                return steering;

            float desiredRotation;
            // If we are outside the slowRadius, then use
            // maximun rotation
            if (rotationSize > slowRadius)
                desiredRotation = maxRotation;

            // Otherwise calculate a scaled rotation
            else
                desiredRotation = maxRotation * rotationSize / slowRadius;

            // The final target rotation cambines speed
            // (already in the variable) and direction
            desiredRotation *= rotation / rotationSize;

            // Acceleration tries to get to the target rotation
            steering.angular = desiredRotation - transform.eulerAngles.y;
            steering.angular /= timeToTarget;


            // Check if the acceleration is too great
            angularAccel = Mathf.Abs(steering.angular);
            if (angularAccel > agent.maxAngularAccel)
            {
                steering.angular /= angularAccel;
                steering.angular *= agent.maxAngularAccel;
            }

            // Output the steering
            // steering.linear = Vector3.zero;
            return steering;
        }
    }
}
