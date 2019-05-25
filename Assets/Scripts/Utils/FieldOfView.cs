using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FieldOfView : MonoBehaviour
{
    public float viewRadius = 10;
    [Range(0, 180)] public float viewAngle = 90;

    public float viewRadiusBack = 5;
    [Range(0, 180)] public float viewAngleBack = 90;

    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public List<Transform> visibleTargets = new List<Transform>();

    void Start()
    {
        StartCoroutine("FindTargetsWithDelay", 0.2f);
    }

    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            if (target == transform) continue;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position + Vector3.up, dirToTarget, dstToTarget, obstacleMask))
                    visibleTargets.Add(target);
            }
        }

        Collider[] targetsInViewRadiusBack = Physics.OverlapSphere(transform.position, viewRadiusBack, targetMask);
        for (int i = 0; i < targetsInViewRadiusBack.Length; i++)
        {
            Transform target = targetsInViewRadiusBack[i].transform;
            if (target == transform) continue;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(-transform.forward, dirToTarget) < (360 - viewAngleBack) / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position + Vector3.up, dirToTarget, dstToTarget, obstacleMask))
                    visibleTargets.Add(target);
            }
        }
    }
}