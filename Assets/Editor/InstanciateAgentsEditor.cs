using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InstanciateAgents))]
public class InstanciateAgentsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //DrawDefaultInspector();
        InstanciateAgents instanciateAgents = (InstanciateAgents)target;
        instanciateAgents.agent = (GameObject)EditorGUILayout.ObjectField
            ("Agent Prefab", instanciateAgents.agent, typeof(GameObject), false);

        instanciateAgents.parent = (Transform)EditorGUILayout.ObjectField
            ("Parent", instanciateAgents.parent, typeof(Transform), true);

        instanciateAgents.type = (InstanciateAgents.Type)EditorGUILayout.
            EnumPopup("Instanciation Type", instanciateAgents.type);

        if (instanciateAgents.type == InstanciateAgents.Type.RECTANGLE)
        {
            instanciateAgents.goal = EditorGUILayout.FloatField("Distance to goal", instanciateAgents.goal);
            instanciateAgents.width = EditorGUILayout.IntField("Width", instanciateAgents.width);
            instanciateAgents.height = EditorGUILayout.IntField("Height", instanciateAgents.height);
            instanciateAgents.rotation = EditorGUILayout.IntSlider("Rotation on Axis Y", instanciateAgents.rotation, 0, 360);
            instanciateAgents.separationX = EditorGUILayout.Slider("Separation X", instanciateAgents.separationX, 0.5f, 5);
            instanciateAgents.separationY = EditorGUILayout.Slider("Separation Y", instanciateAgents.separationY, 0.5f, 5);
            instanciateAgents.noisy = EditorGUILayout.Toggle("Noisy", instanciateAgents.noisy);

            EditorGUILayout.LabelField("Nro of agents", (instanciateAgents.width * instanciateAgents.height).ToString());


            if (GUILayout.Button("Instanciate agents as grid"))
                instanciateAgents.Rectagle();
        }

        else if (instanciateAgents.type == InstanciateAgents.Type.CIRCLE)
        {
            instanciateAgents.nroAgents =
                EditorGUILayout.FloatField
                ("Nro Virtual Agents", instanciateAgents.nroAgents);

            instanciateAgents.radius = 
                EditorGUILayout.FloatField("Radius", instanciateAgents.radius);

            if (GUILayout.Button("Instanciate agents around of a circle"))
                instanciateAgents.Circle();
        }

        else {
            instanciateAgents.nroAgents = 
                EditorGUILayout.FloatField
                ("Nro Virtual Agents", instanciateAgents.nroAgents);

            if (GUILayout.Button("Create " + instanciateAgents.nroAgents + " virtual agents"))
                instanciateAgents.VirtualAgent();
        }


        if (GUILayout.Button("Kill all childs"))
            while (instanciateAgents.parent.transform.childCount != 0)
                DestroyImmediate(instanciateAgents.parent.transform.GetChild(0).gameObject);

    }
}
