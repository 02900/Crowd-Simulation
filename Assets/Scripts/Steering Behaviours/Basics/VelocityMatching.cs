using SteeringBehaviours;

public class VelocityMatching : AgentBehaviour {

	//Time over wich to achieve target speed
	public float timeToTarget = 0.1f;

	// the target is agent
	private SteeringBehaviours.Agent agentTarget;

	public override void Awake ()
	{
		base.Awake ();

		// si hay un objetivo y ese objetivo es un agente, entonces usaremos esa velocidad
		// sino, trabajaremos con la velocidad del rigidbody
		// sino tiene ninguno de estos componentes entonces dara error en tiempo de ejecucion
		if (target != null)
            agentTarget = target.HasComponent<SteeringBehaviours.Agent>()? target.GetComponent<SteeringBehaviours.Agent>() : null;
	}

	public override Steering GetSteering ()
	{
		// Create the structure to hold our output
		Steering steering = new Steering ();

		//Acceleration tries to get target velocity
		steering.linear = agentTarget != null? agentTarget.velocity - agent.velocity : rb.velocity - agent.velocity;
		steering.linear /= timeToTarget;

		//Check if the acceleration is too fast
		if (steering.linear.magnitude > agent.maxAccel) {
			steering.linear.Normalize ();
			steering.linear *= agent.maxAccel;
		}

		//Output the steering
		// steering.angular = 0;
		return steering;
	}
}
