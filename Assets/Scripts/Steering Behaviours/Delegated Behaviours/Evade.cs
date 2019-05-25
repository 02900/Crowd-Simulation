using UnityEngine;

namespace SteeringBehaviours
{
    public class Evade : Flee
    {
        public float maxPrediction = 0.6f;
        private GameObject targetAux;

        private Agent agentTarget;

        public override void Awake()
        {
            base.Awake();
            targetAux = target.gameObject;
            target = new GameObject();

            if (targetAux != null)
                agentTarget = targetAux.HasComponent<Agent>() ? targetAux.GetComponent<Agent>() : null;
        }

        void OnDestroy()
        {
            Destroy(targetAux);
        }

        public override Steering GetSteering()
        {
            Vector3 direction = targetAux.transform.position - transform.position;
            float distance = direction.magnitude;
            float speed = agent.velocity.magnitude;
            float prediction;

            if (speed <= distance / maxPrediction)
                prediction = maxPrediction;
            else
                prediction = distance / speed;

            target.transform.position = targetAux.transform.position;
            target.transform.position += agentTarget != null ?
                agentTarget.velocity * prediction :
                rb.velocity * prediction;
            return base.GetSteering();
        }
    }
}
