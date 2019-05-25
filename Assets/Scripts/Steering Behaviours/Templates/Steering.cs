using UnityEngine;

namespace SteeringBehaviours
{
    public class Steering
    {
        public float angular;
        public Vector3 linear;

        public Steering()
        {
            angular = 0.0f;
            linear = new Vector3();
        }
    }
}
