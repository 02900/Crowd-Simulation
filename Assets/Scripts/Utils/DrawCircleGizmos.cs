using UnityEngine;

public class DrawCircleGizmos : MonoBehaviour {

	public float[] allRadius = new float[2] {4, 8};
	public Color[] colors = new Color[] {Color.green, Color.magenta};

	void OnDrawGizmos () {
		int i = 0;
		foreach (float radius in allRadius) {
			Transform agent = GetComponent<Transform>();
			Gizmos.color = colors[i];
			float theta = 0;
			float x = radius * Mathf.Cos(theta);
			float y = radius * Mathf.Sin(theta);
			Vector3 pos = agent.position + new Vector3 (x, 0, y);
			Vector3 newPos = pos;
			Vector3 lastPos = pos;

			for (theta=0.1f; theta<Mathf.PI*2; theta+=0.1f) {
				x = radius * Mathf.Cos (theta);
				y = radius * Mathf.Sin (theta);

				newPos = agent.position + new Vector3 (x, 0, y);
				Gizmos.DrawLine (pos, newPos);
				pos = newPos;
			}
			Gizmos.DrawLine (pos, lastPos);
			i++;
		}
	}
}
