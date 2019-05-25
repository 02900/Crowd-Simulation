using System.Collections;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (FieldOfView))]
public class FieldOfViewEditor : Editor {
	
	void OnSceneGUI () {
		FieldOfView fow = (FieldOfView) target;
		Handles.color = Color.white;

        Handles.DrawWireArc (fow.transform.position, Vector3.up, Vector3.forward, 360, fow.viewRadius);
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.viewRadiusBack);

        Handles.color = Color.blue;
        Vector3 viewAngleA = ExtensionMethods.Orientation2Vector (-fow.viewAngle/2, fow.transform.eulerAngles.y);
		Vector3 viewAngleB = ExtensionMethods.Orientation2Vector (fow.viewAngle/2, fow.transform.eulerAngles.y);
		Handles.DrawLine (fow.transform.position, fow.transform.position + viewAngleA * fow.viewRadius);
		Handles.DrawLine (fow.transform.position, fow.transform.position + viewAngleB * fow.viewRadius);

        Handles.color = Color.green;
        Vector3 viewAngleC = ExtensionMethods.Orientation2Vector(-fow.viewAngleBack / 2, fow.transform.eulerAngles.y);
        Vector3 viewAngleD = ExtensionMethods.Orientation2Vector(fow.viewAngleBack / 2, fow.transform.eulerAngles.y);
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleD * fow.viewRadiusBack);
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleC * fow.viewRadiusBack);

        Handles.color = Color.red;
		foreach (Transform visibleTarget in fow.visibleTargets) {
			Handles.DrawLine (fow.transform.position, visibleTarget.position);
		}
	}
}
