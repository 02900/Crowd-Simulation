using UnityEngine;
using System.Collections.Generic;

namespace SteeringBehaviours
{
    public class PairWeightSteering
    {
        public float weight;
        public Steering steering;

        public PairWeightSteering()
        {
            weight = 0;
            steering = new Steering();
        }
    }

    public class Agent : MonoBehaviour
    {
        public float maxSpeed = 10;
        public float maxAccel = 40;             //Holds the maximun acceleration of the character
        public float maxAngularAccel = 75;      //Holds the maximun angular acceleration of the character
        [HideInInspector] public float orientation;
        [HideInInspector] public float rotation;
        [HideInInspector] public Vector3 velocity;
        protected Steering steering;

        [HideInInspector] public Rigidbody rb;

        // Minimum steering value to consider a group of behaviors
        [Tooltip("Set to 1 to turn off priority blending")]
        [Range(0f, 1)] public float priorityThreshold = 0.0f;

        // Hold the group of behaviors results
        private Dictionary<int, List<PairWeightSteering>> groups;

        // Use this for initialization
        void Awake()
        {
            velocity = Vector3.zero;
            steering = new Steering();

            rb = gameObject.HasComponent<Rigidbody>() ? GetComponent<Rigidbody>() : null;
        }

        void Start()
        {
            if (priorityThreshold < 1)
                groups = new Dictionary<int, List<PairWeightSteering>>();
        }

        public virtual void Update()
        {
            Vector3 displacement = velocity * Time.deltaTime;
            displacement.y = 0;
            orientation += rotation * Time.deltaTime;

            //we need to limit the orientation values
            // to be in the range (0 - 360)
            if (orientation < 0.0f)
                orientation += 360.0f;
            else if (orientation > 360.0f)
                orientation -= 360.0f;

            transform.Translate(displacement, Space.World);
            transform.rotation = new Quaternion();
            transform.Rotate(Vector3.up, orientation);
        }

        public virtual void LateUpdate()
        {
            if (priorityThreshold < 1)
            {
                // funnelled steering throug priorities
                steering = GetPrioritySteering();
                groups.Clear();
            }

            velocity += steering.linear * Time.deltaTime;
            rotation += steering.angular * Time.deltaTime;

            if (velocity.magnitude > maxSpeed)
            {
                velocity.Normalize();
                velocity *= maxSpeed;
            }

            if (steering.angular == 0.0f)
                rotation = 0.0f;

            if (steering.linear.sqrMagnitude == 0.0f)
                velocity = Vector3.zero;

            steering = new Steering();
        }

        public void SetSteering(Steering steering, float weight, int priority)
        {
            if (priorityThreshold == 1)
            {
                this.steering.linear += weight * steering.linear;
                this.steering.angular += weight * steering.angular;
            }
            else
            {
                if (!groups.ContainsKey(priority))
                    groups.Add(priority, new List<PairWeightSteering>());

                PairWeightSteering pair = new PairWeightSteering();
                pair.weight = weight;
                pair.steering = steering;
                groups[priority].Add(pair);
            }
        }

        // Function to funnel the steering group
        private Steering GetPrioritySteering()
        {
            Steering steering = new Steering();
            float sqrThreshold = priorityThreshold * priorityThreshold;
            foreach (List<PairWeightSteering> group in groups.Values)
            {
                steering = new Steering();
                foreach (PairWeightSteering singleSteering in group)
                {
                    steering.linear += singleSteering.weight != 0 ?
                        singleSteering.steering.linear * singleSteering.weight :
                        singleSteering.steering.linear;

                    steering.angular += singleSteering.weight != 0 ?
                        singleSteering.steering.angular * singleSteering.weight :
                        singleSteering.steering.angular;
                }
                if (steering.linear.sqrMagnitude > sqrThreshold ||
                    Mathf.Abs(steering.angular) > priorityThreshold)
                    return steering;
            }
            return steering;
        }
    }
}