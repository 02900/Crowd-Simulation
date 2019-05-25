using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bearingAngleTest : MonoBehaviour
{

    public Transform other;

    void Start()
    {
        StartCoroutine(FrameDelayedUpdateRoutine());
    }

    /// <summary>
    /// A loop which executes logic after waiting for an amount of frames to pass.
    /// </summary>
    IEnumerator FrameDelayedUpdateRoutine()
    {
        // Always run
        while (true)
        {
            yield return new WaitForSeconds(1f);
            aUpdate();
        }
    }

    void aUpdate()
    {
        Vector2 velocity = Vector2.up;
        Vector2 dir = ExtensionMethods.ToXZ(other.position - transform.position);
        float a = -Vector2.SignedAngle(velocity, dir);
        if (a < 0) a += 360;

        Vector2 otherVelocity = ExtensionMethods.ToXZ(transform.position - other.position);
        float angle = Vector2.Angle(velocity, otherVelocity);

        Debug.Log(name +  ", BA is: " + a + " and the VA is: " + angle);

        //if (System.Math.Abs(angle - 180) < 7) Debug.Log("VELOCIDADES OPUESTAS");
        //else if (System.Math.Abs(angle) < 7) Debug.Log("VELOCIDADES PARALELAS");
        //else Debug.Log("VELOCIDADES DISTINTAS");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, other.position);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.forward);
    }
}

