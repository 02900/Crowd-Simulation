using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace SocialForces
{
    public class Engine : MonoBehaviour
    {
        public static Engine Instance;
        public const float timeStep = 0.01f;
        const float moreDelayedTimeStep = 0.2f;
        const float mass_half = 33;
        int framesCount;
        int framesCountDelayed;
        float hits;

        Agent[] agents;
        public Agent[] Agents { get { return agents; } }
        public List<Wall> walls = new List<Wall>();

        public void AddWall(Wall wall) { walls.Add(wall); }
        public List<Wall> GetWalls { get { return walls; } }
        public int GetNumWalls() { return walls.Count; }

        float[] DST_TRAVEL;
        float[] TIME_TRAVEL;
        float[] EKinematic;
        bool[] finish;

        float time_mean;
        float dst_mean;
        float ek_mean;

        List<float> TimesTravel = new List<float>();
        List<float> DistancesTravel = new List<float>();

        HandleTextFile results;
        bool playingRec = true;

        void Awake()
        {
            Instance = this;
            results = FindObjectOfType<HandleTextFile>();
            CreateAgents();
        }

        void CreateAgents()
        {
            agents = FindObjectsOfType<Agent>();
            Vector2 position, goal;
            for (int i = 0; i < agents.Length; i++)
            {
                position = ExtensionMethods.ToXZ(agents[i].transform.position);
                goal = ExtensionMethods.ToXZ(agents[i].transform.GetChild(0).position);
                agents[i].Init(i, position, goal);
            }

            if (results.recordStats)
            {
                DST_TRAVEL = new float[agents.Length];
                TIME_TRAVEL = new float[agents.Length];
                EKinematic = new float[agents.Length];
            }

            finish = new bool[agents.Length];
        }

        void AddAgentStat(float time, float dst)
        {
            TimesTravel.Add(time);
            DistancesTravel.Add(dst);

            if (TimesTravel.Count == agents.Length)
            {
                for (int i = 0; i < agents.Length; i++)
                {
                    time_mean += TimesTravel[i];
                    dst_mean += DistancesTravel[i];
                    ek_mean += EKinematic[i];
                }
                time_mean /= TimesTravel.Count;
                dst_mean /= DistancesTravel.Count;
                ek_mean /= agents.Length;
                results.WriteString(time_mean, dst_mean, ek_mean,
                    TimesTravel[agents.Length - 1], hits, framesCount);

                results.CloseRecord();
            }
        }

        void Start()
        {
            StartCoroutine(FrameMoreDelayedUpdateRoutine());
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
                yield return new WaitForSeconds(timeStep);
                if (Input.GetKey(KeyCode.Q))
                {
                    ForceFinish();
                    yield break;
                }
                if (Input.GetKey(KeyCode.Z)) UpdateSimulation();
                else PauseAnimation();
            }
        }

        /// <summary>
        /// A loop which executes logic after waiting for an amount of frames to pass.
        /// </summary>
        IEnumerator FrameMoreDelayedUpdateRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(moreDelayedTimeStep);
                if (!results.loadRec && Input.GetKey(KeyCode.Z))
                {
                    for (int i = 0; i < agents.Length && results.recordStats; i++)
                    {
                        if (agents[i].Velocity != Vector2.zero)
                            EKinematic[i] += mass_half * agents[i].Velocity.sqrMagnitude;
                    }
                    framesCountDelayed++;
                }
            }
        }

        void ForceFinish()
        {
            foreach (var a in agents)
            {
                if (finish[a.id]) continue;
                if (results.recordStats)
                {
                    TIME_TRAVEL[a.id] = framesCount * timeStep;
                    EKinematic[a.id] /= framesCountDelayed;
                    AddAgentStat(TIME_TRAVEL[a.id], DST_TRAVEL[a.id]);
                }
                agents[a.id].ResetAnimParameters(true);
                finish[a.id] = true;
            }
        }

        /// <summary>
        /// Performs a simulation/integration step i.e. updates the acceleration, 
        /// velocity and position of the simulated characters.
        /// </summary>
        void UpdateSimulation()
        {
            if (!results.loadRec)
            {
                framesCount++;
                Vector2 prevPos;
                for (int i = 0; i < agents.Length; i++)
                {
                    if (results.record) results.RecordStep(Agents[i].Position, Agents[i].Velocity);
                    if (finish[i]) continue;
                    prevPos = agents[i].Position;
                    finish[i] = agents[i].DoStep();

                    for (int j = 0; j < agents.Length; j++)
                    {
                        if (i == j) continue;
                        float rA, rB;
                        Vector2 posA, posB;
                        rA = Agents[i].Radius;
                        rB = Agents[j].Radius;
                        posA = Agents[i].Position;
                        posB = Agents[j].Position;
                        IsColliding(posA, rA, posB, rB);
                    }

                    if (results.recordStats && agents[i].Velocity != Vector2.zero)
                    {
                        float dstChange = Vector2.Distance(prevPos, agents[i].Position);
                        DST_TRAVEL[i] += dstChange;
                    }

                    if (finish[i]) {
                        if (results.recordStats) {
                            TIME_TRAVEL[i] = framesCount * timeStep;
                            EKinematic[i] /= framesCountDelayed;
                            AddAgentStat(TIME_TRAVEL[i], DST_TRAVEL[i]);
                        }
                        agents[i].ResetAnimParameters(true);
                    }
                }
            }
            else if (playingRec){
                for (int i = 0; i < Agents.Length && !results.EndStream(); i++)
                {
                    agents[i].Position = results.LoadVector();
                    agents[i].Velocity = results.LoadVector();
                    agents[i].RecMove();
                }

                if (results.EndStream())
                {
                    playingRec = false;
                    results.CloseFile();
                    for (int i = 0; i < Agents.Length; i++)
                        agents[i].ResetAnimParameters(true);
                }
            }
        }

        void PauseAnimation()
        {
            for (int i = 0; i < agents.Length; i++)
                agents[i].ResetAnimParameters();
        }


        void IsColliding(Vector2 posA, float radA, Vector2 posB, float radB)
        {
            float r = radA + radB;
            Vector2 w = posB - posA;
            float c = Vector2.Dot(w, w) - r * r;
            if (c < 0) hits += 1;
        }
    }
}