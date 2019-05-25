using UnityEngine;

namespace SteeringBehaviours
{
    public class LookWhereYoureGoing : Aling
    {
        // ... Other data is derived from the superclass ...

        public override void Awake()
        {
            target = new GameObject();
            target.transform.position = transform.position;
            base.Awake();
        }

        public override Steering GetSteering()
        {
            // 1. Calculate the target to delegate to 

            // Check for a zero direction, and make no change if so
            // Otherwise set the target based on the velocity
            if (agent.velocity.sqrMagnitude > 0)
            {
                float targetOrientation = Mathf.Atan2(-agent.velocity.x, agent.velocity.z);
                targetOrientation *= Mathf.Rad2Deg;
                target.transform.eulerAngles = new Vector3(target.transform.eulerAngles.x,
                    targetOrientation, target.transform.eulerAngles.z);
            }

            // 2. Delegate to aling
            return base.GetSteering();
        }
    }
}
