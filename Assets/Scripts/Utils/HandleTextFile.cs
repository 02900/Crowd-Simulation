using UnityEngine;
using System.IO;

public class HandleTextFile : MonoBehaviour
{
    int i;
    [SerializeField] string testName;
    [SerializeField] string recName;

    public bool recordStats;
    public bool record;
    public bool loadRec;
    StreamReader reader;
    StreamWriter writer;

    public void Awake()
    {
        if (record && loadRec) loadRec = false;

        string path;
        string t = "";

        if (record) {
            t = "-trajectories";
            path = "Assets/Results/" + testName + "-" + i + "-trajectories.txt";
        }

        else path = "Assets/Results/" + testName + "-" + i + ".txt";

        while (File.Exists(path))
        {
            i++;
            path = "Assets/Results/" + testName + "-" + i + t + ".txt";
        }

        if (!loadRec)
        {
            if (record) IniTrajectoryFile(t);
        }
        else
        {
            reader = new StreamReader("Assets/Results/" + recName + "-trajectories.txt");
            reader.ReadLine();
        }
    }

    public void IniTrajectoryFile(string t)
    {
        string path = "Assets/Results/" + testName + "-" + i + "-trajectories.txt";
        writer = new StreamWriter(path, true);
        writer.WriteLine("Test " + testName + "-trayectorias");
    }

    public void RecordStep (Vector2 Position, Vector2 Velocity)
    {
        if (writer == null) return;
        writer.WriteLine(Position.ToString("F4"));
        writer.WriteLine(Velocity.ToString("F4"));
    }

    public void CloseRecord() {
        if (record && writer != null) writer.Close();
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

    void OnApplicationQuit()
    {
        CloseFile();
        CloseRecord();
    }
}