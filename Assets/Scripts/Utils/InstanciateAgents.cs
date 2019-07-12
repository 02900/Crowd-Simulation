using UnityEngine;
using CrowdSimulation;

public class InstanciateAgents : MonoBehaviour
{
    public GameObject agent;
    public Transform parent;

    // Circle
    public float nroAgents = 10;
    public float radius = 10;

    // Rectangle
    public float goal = 30;
    public int width = 5;
    public int height = 5;
    public int rotation = 0;

    [Range(0.5f, 3)] public float separationX = 1;
    [Range(0.5f, 3)] public float separationY = 1;

    public bool noisy;

    public enum Type { RECTANGLE, CIRCLE, VIRTUAL_AGENT }
    public Type type;

    public void Rectagle()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3 pos = transform.position + Vector3.right * i * separationX + Vector3.forward * j * separationY;
                if (noisy) pos += transform.right * Random.Range(0.1f, 0.7f) + transform.forward * Random.Range(0.1f, 0.7f);
                GameObject go = Instantiate(agent, pos, Quaternion.Euler(0, rotation, 0), parent);
                go.transform.GetChild(0).position = go.transform.GetChild(0).position + go.transform.forward * goal;
            }
        }
    }

    public void Circle()
    {
        Vector3 center = transform.position;
        for (int i = 0; i < nroAgents; i++)
        {
            float a = 360 / nroAgents * i;
            Vector3 pos = GetPosition(center, radius, a);
            Quaternion rot = Quaternion.LookRotation(center - pos);
            GameObject go = Instantiate(agent, pos, rot, transform);
            go.transform.GetChild(0).RotateAround(transform.position, Vector3.up, 180);
        }
    }

    public void VirtualAgent()
    {
        for (int i = 0; i < nroAgents; i++)
        {
            GameObject go = Instantiate(agent, Vector3.zero, Quaternion.identity, parent);
            go.GetComponent<VirtualAgent>().id += i; 
        }
    }

    private Vector3 GetPosition(Vector3 center, float radius, float a)
    {
        float ang = a;
        Vector3 pos;
        pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
        pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
        pos.y = center.y;
        return pos;
    }
}
