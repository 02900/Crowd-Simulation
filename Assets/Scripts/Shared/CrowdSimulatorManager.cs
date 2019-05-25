using UnityEngine;

public abstract class CrowdSimulatorManager<T> : MonoBehaviour
{
    public static CrowdSimulatorManager<T> Instance;

    [SerializeField] protected GameObject agentPrefab;
    [SerializeField] [Range(1, 10)] float speed = 3;
    [SerializeField] [Range(0.1f, 10)] protected float radius = 0.25f;

    private Vector2 mousePosition;
    private Plane m_hPlane = new Plane(Vector3.up, Vector3.zero);

    public float Speed { get { return speed; } set { speed = value; } }

    public Vector2 MousePosition
    {
        get { return mousePosition; }
        set { mousePosition = value; }
    }

    public virtual void Awake()
    {
        Instance = this;
        GetAgents();
    }

    //private void GetAgents()
    //{
    //    T agents = FindObjectsOfType<T>();
    //    foreach (T agent in agents)
    //        CreateAgent(agent.transform.position, agent.GetComponent<T>());
    //}

    public abstract void GetAgents();
    public abstract void DeleteAgent();
    public abstract void CreateAgent();
    public abstract void CreateAgent(Vector3 position, T agent);

    private void UpdateMousePosition()
    {
        float rayDistance;
        Vector3 position = Vector3.zero;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (m_hPlane.Raycast(mouseRay, out rayDistance))
            position = mouseRay.GetPoint(rayDistance);
        MousePosition = ExtensionMethods.ToXZ(position);
    }

    public virtual void Update()
    {
        // if the simulation is running then update mouse position
        if (Input.GetMouseButton(1))
            UpdateMousePosition();

        // update mouse position and instanciate a agent there
        if (Input.GetMouseButtonDown(0)) {
            UpdateMousePosition();
            CreateAgent();
        }

        // Delete a agent based in distance to mouse position
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            UpdateMousePosition();
            DeleteAgent();
        }
    }
}
