using UnityEngine;
using SteeringBehaviours;

public class Pursue : Arrive {
	public float maxPrediction = 0.8f;
	private GameObject targetAux;
	private SteeringBehaviours.Agent agentTarget;

	public override void Awake () {
		base.Awake();
		targetAux = target.gameObject;
		target = new GameObject();

		if (targetAux != null)
            agentTarget = targetAux.HasComponent<SteeringBehaviours.Agent>()? targetAux.GetComponent<SteeringBehaviours.Agent>() : null;
	}

	void OnDestroy () {
		Destroy(targetAux);
	}

	public override Steering GetSteering () {
        if (targetAux == null) {
            return base.GetSteering();
        }

        Vector3 direction = targetAux.transform.position - transform.position;
		float distance = direction.magnitude;
		float speed = agent.velocity.magnitude;
		float prediction;
		if (speed <= distance / maxPrediction) {
			prediction = maxPrediction;
		} else {
			prediction = distance / speed;
		}
		target.transform.position = targetAux.transform.position;
		target.transform.position += agentTarget!=null? agentTarget.velocity * prediction : rb.velocity * prediction;
		return base.GetSteering();
	}
}
