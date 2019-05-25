using UnityEngine;
using System.IO;

public class HandleTextFile : MonoBehaviour
{
    int i;
    [SerializeField] string testName;
    [SerializeField] string recName;

    public bool record;
    public bool loadRec;
    StreamReader reader;

    public void Awake()
    {
        string path = "Assets/Results/" + testName + "-" + i + "-trajectories.txt";

        while (File.Exists(path))
        {
            i++;
            path = "Assets/Results/" + testName + "-" + i + "-trajectories.txt";
        }

        if (!loadRec)
        {
            if (record) IniTrajectoryFile();
        }
        else
        {
            reader = new StreamReader("Assets/Results/" + recName + "-trajectories.txt");
            reader.ReadLine();
        }
    }

    public void IniTrajectoryFile()
    {
        string path = "Assets/Results/" + testName + "-" + i + "-trajectories.txt";

        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine("Test " + testName + "-trayectorias");
        writer.Close();
    }

    public void RecordStep (Vector2 Position, Vector2 Velocity)
    {
        string path = "Assets/Results/" + testName + "-" + i + "-trajectories.txt";
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(Position.ToString("F4"));
        writer.WriteLine(Velocity.ToString("F4"));
        writer.Close();
    }


    public void WriteString(float time_mean, float dst_mean, float ek_mean, float totalTime)
    {
        string path = "Assets/Results/" + testName + "-" + i + ".txt";
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine("Test " + testName);
        writer.WriteLine("Tiempo total " + totalTime);
        writer.WriteLine("Tiempo promedio " + time_mean);
        writer.WriteLine("Distancia media " + dst_mean);
        writer.WriteLine("Energia cinetica media " + ek_mean);
        writer.Close();
    }

    public Vector2 LoadVector()
    {
        string data = reader.ReadLine();
        return StringToVector2(data);
    }

    public void CloseFile()
    {
        if(reader != null)
            reader.Close();
    }

    public bool EndStream() {
        return reader.EndOfStream;
    }

    public static Vector2 StringToVector2(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            sVector = sVector.Substring(1, sVector.Length - 2);

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        return new Vector2(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]));
    }
}