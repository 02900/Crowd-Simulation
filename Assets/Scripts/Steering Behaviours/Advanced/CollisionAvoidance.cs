using System.Collections;
using UnityEngine;
using SteeringBehaviours;

public class CollisionAvoidance : AgentBehaviour {

	// Holds a list of potential targets
	[SerializeField] private Transform[] targets;
	private SteeringBehaviours.Agent[] targetAgents = null;
	private Rigidbody[] targetRigidbodys = null;
    public bool avoidBullets;
    public bool useRigidbodyOfBullet;

	// Holds the collision radius of a character 
	// (we asume all characters have the same radius here)
	[SerializeField] private float radius = 0.5f;

	// Show or Hide Gizmos
	[SerializeField] private bool gizmos;

	public override void Awake () {
		base.Awake ();

        targetAgents = new SteeringBehaviours.Agent[targets.Length];
        targetRigidbodys = new Rigidbody[targets.Length];

        int i;

        for (i = 0; i < targets.Length; i++)
            targetAgents[i] = targets[i].gameObject.HasComponent<SteeringBehaviours.Agent>() ?
                targets[i].gameObject.GetComponent<SteeringBehaviours.Agent>() : null;

        for (i = 0; i < targets.Length; i++)
            targetRigidbodys[i] = targets[i].gameObject.HasComponent<Rigidbody>() ?
                targets[i].gameObject.GetComponent<Rigidbody>() : null;

	}

	public override Steering GetSteering () {
		Steering steering = new Steering ();

        if (avoidBullets) {
            GameObject[] bullets = GameObject.FindGameObjectsWithTag("bulletPlayer");
            targets = new Transform[bullets.Length];

            targetAgents = new SteeringBehaviours.Agent[targets.Length];
            targetRigidbodys = new Rigidbody[targets.Length];

            for (int j = 0; j < bullets.Length; j++)
            {
                targets[j] = bullets[j].transform;

                if (useRigidbodyOfBullet)
                {
                    targetRigidbodys[j] = targets[j].gameObject.HasComponent<Rigidbody>() ?
                    targets[j].gameObject.GetComponent<Rigidbody>() : null;
                }

                else {
                    Destroy(targets[j].gameObject.GetComponent<Rigidbody>());
                }

            }
        }

		// 1. Find the target that's closest to collision
		// Store the first collision time
		float shorestTime = Mathf.Infinity;
		// Store the target that collides then, and other data
		// that we will need and avoid recalculating
		Transform firstTarget = null;
		float firstMinSeparation = 0.0f;
		float firstDistance = 0.0f;
		Vector3 firstRelativePos = Vector3.zero;
		Vector3 firstRelativeVel = Vector3.zero;

		// Loop through each target
		int i=0;
		foreach (Transform new_target in targets) {
			// Calculate the time to collision
			Vector3 relativePos = new_target.position - transform.position;
            Vector3 relativeVel = targetAgents[i] != null ?
                targetAgents[i].velocity - agent.velocity :
                targetRigidbodys[i].velocity - agent.velocity;
			float relativeSpeed = relativeVel.magnitude;
			i++;
			if (relativeSpeed == 0) continue;
			float timeToCollision = Vector3.Dot(relativePos, relativeVel);
			timeToCollision /= relativeSpeed * relativeSpeed * -1;
			// Check if it is going to be a collision at all
			float distance = relativePos.magnitude;
			float minSeparation = distance - relativeSpeed * timeToCollision;
			if (minSeparation > 2*radius) continue;
			// Check if it is the shortest
			if (timeToCollision > 0 && timeToCollision < shorestTime) {
				// Store the time, target and other data
				shorestTime = timeToCollision;
				firstTarget = new_target;
				firstMinSeparation = minSeparation;
				firstDistance = distance;
				firstRelativePos = relativePos;
				firstRelativeVel = relativeVel;
			}
		}

		target = firstTarget!=null? firstTarget.gameObject : null;

		// 2. Calculate the steering
		// If we have no target, then exit
		if (firstTarget == null) 
			return steering;
        // Update the target

        // If we're going to hit exactly, or if we're already
        // colliding, then do the steering based on current position
        if (firstMinSeparation <= 0 || firstDistance < 2 * radius)
        {

            if (!avoidBullets)
            {
                firstRelativePos = firstTarget.position;
            }
            else {

                Vector3 heading = firstTarget.position - transform.position;
                int dir = ExtensionMethods.AngleDir(transform.forward, heading, transform.up);
                firstRelativePos = transform.right * dir * 250 + transform.forward * 20;

            }
        }
        // Otherwise calculate the future relative position
        else if (!avoidBullets)
        {
            firstRelativePos += firstRelativeVel * shorestTime;
        }

        else {
            return steering;
        }

        // Avoid the target
        firstRelativePos.Normalize();
        if (!avoidBullets)
        {
            steering.linear = -firstRelativePos * agent.maxAccel;
        }
        else {
            steering.linear = -firstRelativePos * agent.maxAccel * 80;
        }
        return steering;
	}

	public void ToogleGizmos () {
		gizmos = !gizmos;
	}

	void OnDrawGizmos () {
		if (gizmos) {
			Gizmos.color = Color.green;
			if (target != null)
				Gizmos.DrawLine (transform.position, target.transform.position);
		}
	}
}
