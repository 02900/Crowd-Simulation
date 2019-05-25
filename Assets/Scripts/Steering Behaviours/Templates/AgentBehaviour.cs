using UnityEngine;

namespace SteeringBehaviours
{
    public class AgentBehaviour : MonoBehaviour
    {
        //Holds the kinematic data for the target
        public GameObject target;
        protected Agent agent;

        [HideInInspector]
        public Rigidbody rb;

        public float weight = 1.0f;
        public int priority = 1;

        public virtual void Awake()
        {
            if (target != null)
                rb = target.HasComponent<Rigidbody>() ? target.GetComponent<Rigidbody>() : null;

            agent = GetComponent<Agent>();
        }

        // Update is called once per frame
        public virtual void Update()
        {
            agent.SetSteering(GetSteering(), weight, priority);
        }

        public virtual Steering GetSteering()
        {
            return new Steering();
        }
    }
}