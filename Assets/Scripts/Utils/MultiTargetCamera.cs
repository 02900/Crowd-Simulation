using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class MultiTargetCamera : MonoBehaviour {

    public Vector3 offset;
    public List<Transform> targets;
    public float smoothTime = 0.5f;

    public float minZoom = 40.0f;
    public float maxZoom = 10.0f;
    public float zoomLimiter = 15.0f;

    private Camera cam;

    private Vector3 velocity;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        FocusCamOnTargets();
    }

    private void FocusCamOnTargets() {

        if (targets.Count == 0)
            return;

        foreach (var t in targets)
            if (t == null)
                return;

        Move();
        Zoom();
    }

    private void Zoom()
    {
        float newZoom = Mathf.Lerp(maxZoom, minZoom, GetGreatestDistance() / zoomLimiter);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newZoom, Time.deltaTime);
    }

    private float GetGreatestDistance()
    {
        var bounds = new Bounds(targets[0].position, Vector3.zero);

        foreach (var target in targets)
            bounds.Encapsulate(target.position);

        return bounds.size.x;
    }

    private void Move()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 finalPosition = centerPoint + offset;
        transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref velocity, smoothTime);
    }

    // Center point of all targets in the scene
    private Vector3 GetCenterPoint()
    {
        if (targets.Count == 1 && targets[0])
            return targets[0].position;

        var bounds = new Bounds(targets[0].position, Vector3.zero);

        foreach (var target in targets)
                bounds.Encapsulate(target.position);

        return bounds.center;
    }
}
