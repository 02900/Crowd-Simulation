using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public abstract class Agent<T> : MonoBehaviour
{
    [SerializeField] protected int id;                                           // Id of the agent
    protected Vector3 goal;                                     // Goal position 
    protected Transform target;                                 // Empty gameObject attached to a agent to set his goal point
    protected Queue goals = new Queue();                        // a list of waypoints to navigate to the goal 
    protected AnimationController anim;                         // Animation Controller
    [SerializeField] protected bool navmesh;                    // Use navigation mesh to get intermediate waypoints
    [SerializeField] protected float targetRadius = 1;          // Radius for arriving at the target
    [SerializeField] protected float prefSpeed;                    // Max speed of the agent

    public Color Color1, Color2;
    public Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    public int Id { get { return id; } set { id = value; } }

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        anim = GetComponent<AnimationController>();
        target = transform.childCount > 3 ? transform.GetChild(0) : null;
        if (target) goal = target.position;

        if (navmesh && target)
        {
            goals = new Queue(GetWaypoints(target, GetComponent<NavMeshAgent>()));
            if (goals.Count < 1) return;
            goal = (Vector3) goals.Dequeue();
        }

        if (prefSpeed != 0) SetPrefSpeed();
    }

    public abstract void UpdateNewGoal();
    public abstract Vector3 GetAgentPosition();
    public abstract void StopSimulation();
    public abstract void DoStep();
    public abstract float GetPrefSpeed();
    public abstract void SetPrefSpeed();
    public abstract Vector3 GetVelocity();
    public float GetPercentSpeed() { return GetVelocity().magnitude/GetPrefSpeed(); }

    void Update()
    {
        ColorSpeed();

        // if no exist a target then go to where aim the mouse
        if (target == null)
        {
            goal = ExtensionMethods.Vector2ToVector3
            (CrowdSimulatorManager<T>.Instance.MousePosition);
            UpdateNewGoal();
        }

        Vector3 agentPos = GetAgentPosition();
        transform.localPosition = agentPos;
        float distanceToTarget = Vector3.Distance(goal, agentPos);

        // Agent is within one radius of its goal, set preferred velocity to zero
        if ((distanceToTarget < targetRadius && goals.Count == 0) || !Input.GetMouseButton(1))
        {
            Animate();
            StopSimulation();
            return;
        }

        // Agent is within one radius of its goal then go to next waypoint
        if (distanceToTarget < targetRadius)
        {
            if (goals.Count < 1) return;
            goal = (Vector3)goals.Dequeue();
            UpdateNewGoal();
        }

        DoStep();
        Animate();
    }

    void Animate() { if (anim) anim.Move(GetVelocity()); }

    void ColorSpeed()
    {
        // Get the current value of the material properties in the renderer.
        _renderer.GetPropertyBlock(_propBlock);
        // Assign our new value.
        _propBlock.SetColor("_Color", Color.Lerp(Color1, Color2, GetPercentSpeed()));
        // Apply the edited values to the renderer.
        _renderer.SetPropertyBlock(_propBlock);
    }

    public Vector3[] GetWaypoints(Transform goal, NavMeshAgent agent)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(goal.position, path);

        if (path.status == NavMeshPathStatus.PathComplete)
            return path.corners;

        return new Vector3[] { };
    }
}
