using UnityEngine;
using SteeringBehaviours;

public class Face : Aling {

	protected GameObject targetAux;
	private SteeringBehaviours.Agent auxAgent;

	public override void Awake ()
	{
        base.Awake();
        Setup();
    }

    public void Setup() {
        targetAux = target;
        target = new GameObject();
        target.AddComponent<SteeringBehaviours.Agent>();
        auxAgent = target.GetComponent<SteeringBehaviours.Agent>();
    }

	public override Steering GetSteering ()
	{
        if (targetAux == null) {
            return base.GetSteering();
        }

		// 1. Calculate the target to delegate to aling

		// Work out the direction to target
		Vector3 direction = targetAux.transform.position - transform.position;

		// Check for a zero direction, and make no change if so
		// Otherwise, put the target toogether
		if (direction.sqrMagnitude > 0.0f) {
			float targetOrientation = Mathf.Atan2 (direction.x, direction.z);
			targetOrientation *= Mathf.Rad2Deg;
            auxAgent.orientation = targetOrientation;
		}
		// 2. Delegate to aling
		return base.GetSteering();
	}

	void OnDestoy () {
		Destroy (target);
	}
}