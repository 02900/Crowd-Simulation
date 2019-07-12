using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using RVO;

namespace RVO2
{
    public class RVO2Manager : MonoBehaviour
    {
        public const float timeStep = 0.01f;
        const float moreDelayedTimeStep = 0.2f;
        const float goalRadius = 0.5f;
        public static RVO2Manager Instance;
        const float mass_half = 33;
        int framesCount;
        int framesCountDelayed;
        int hits;

        RVO2Agent[] agents;
        public RVO2Agent[] Agents { get { return agents; } }
    
        float speed;
        [SerializeField] protected float radius = 0.25f;
        [SerializeField] float neighborDist = 15;
        [SerializeField] int maxNeighbor = 10;
        [SerializeField] float timeHorizon = 10;
        [SerializeField] float timeHorizonObst = 10;
        Vector2[] prevPos;

        float[] DST_TRAVEL;
        float[] TIME_TRAVEL;
        float[] EKinematic;
        bool[] finish;
        Vector2[] goal;

        float time_mean;
        float dst_mean;
        float ek_mean;

        List<float> TimesTravel;
        List<float> DistancesTravel;

        HandleTextFile results;
        bool playingRec = true;

        void Awake()
        {
            Instance = this;
            Simulator.Instance.setTimeStep(timeStep);
            Simulator.Instance.setAgentDefaults(neighborDist, maxNeighbor, timeHorizon,
                timeHorizonObst, radius, speed, new Vector2(0.0f, 0.0f));

            results = FindObjectOfType<HandleTextFile>();
            CreateAgents();
        }

        void CreateAgents()
        {
            Vector2 position;
            agents = FindObjectsOfType<RVO2Agent>();
            goal = new Vector2[agents.Length];

            for (int i = 0; i < agents.Length; i++)
            {
                position = ExtensionMethods.ToXZ(agents[i].transform.position);
                goal[i] = ExtensionMethods.ToXZ(agents[i].transform.GetChild(0).position);
                agents[i].id = Simulator.Instance.addAgent(position);
                speed = Random.Range(1.3f, 1.6f);
                Simulator.Instance.setAgentMaxSpeed(agents[i].id, speed);
            }

            if (results.recordStats)
            {
                DST_TRAVEL = new float[agents.Length];
                TIME_TRAVEL = new float[agents.Length];
                EKinematic = new float[agents.Length];
            }
            finish = new bool[agents.Length];

            TimesTravel = new List<float>();
            DistancesTravel = new List<float>();
            prevPos = new Vector2[agents.Length];
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
                    TimesTravel[agents.Length-1], hits, framesCount);

                results.CloseRecord();
            }
        }

        void Start()
        {
            StartCoroutine(FrameMoreDelayedUpdateRoutine());
            StartCoroutine(FrameDelayedUpdateRoutine());
        }

        public void SetCustomPrefVel(int id, float prefSpeed) {
            Simulator.Instance.setAgentMaxSpeed(id, prefSpeed);
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
                Vector2 vel;
                yield return new WaitForSeconds(moreDelayedTimeStep);
                if (!results.loadRec && Input.GetKey(KeyCode.Z))
                {
                    for (int i = 0; i < agents.Length && results.recordStats; i++)
                    {
                        vel = Simulator.Instance.getAgentVelocity(i);
                        if (!finish[i])
                            EKinematic[i] += mass_half * vel.sqrMagnitude;
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
                agents[a.id].ResetAnimParameters(Vector2.zero, GetPrefSpeed(a.id), true);
                finish[a.id] = true;
            }
        }


        /// <summary>
        /// Performs a simulation/integration step i.e. updates the acceleration, 
        /// velocity and position of the simulated characters.
        /// </summary>
        public void UpdateSimulation()
        {
            if (!results.loadRec)
            {
                framesCount++;
                for (int i = 0; i < agents.Length; ++i)
                    prevPos[i] = GetPosition(i);

                SetPreferredVelocities();
                Simulator.Instance.doStep();

                for (int i = 0; i < agents.Length; i++)
                {
                    if (results.record) results.RecordStep(GetPosition(i), GetVelocity(i));
                    if (finish[i]) continue;

                    for (int j = 0; j < agents.Length; j++)
                    {
                        if (i == j) continue;
                        float rA, rB;
                        Vector2 posA, posB;
                        rA = GetRadius(i);
                        if (finish[j]) rB = rA;
                        else rB = GetRadius(j);
                        posA = GetPosition(i);
                        posB = GetPosition(j);
                        IsColliding(posA, rA, posB, rB);
                    }

                    agents[i].MoveInRealWorld(GetPosition(i), GetVelocity(i));

                    if (results.recordStats && GetVelocity(i) != Vector2.zero)
                    {
                        float dstChange = Vector2.Distance(prevPos[i], GetPosition(i));
                        DST_TRAVEL[i] += dstChange;
                    }

                    finish[i] = ReachGoal(i);

                    if (finish[i])
                    {
                        Simulator.Instance.setAgentFinish(i);
                        if (results.recordStats) {
                            TIME_TRAVEL[i] = framesCount * timeStep;
                            EKinematic[i] /= framesCountDelayed;
                            AddAgentStat(TIME_TRAVEL[i], DST_TRAVEL[i]);
                        }
                        agents[i].ResetAnimParameters(Vector2.zero, GetPrefSpeed(i), true);
                    }
                }
            }

            else if (playingRec)
            {
                for (int i = 0; i < agents.Length && !results.EndStream(); i++)
                {
                    agents[i].RecMove(results.LoadVector(), results.LoadVector());
                }

                if (results.EndStream())
                {
                    playingRec = false;
                    results.CloseFile();
                    for (int i = 0; i < Agents.Length; i++)
                        agents[i].ResetAnimParameters(Vector2.zero, 1.4f, true);
                }
            }
        }

        public void PauseAnimation()
        {
            for (int i = 0; i < agents.Length; ++i)
                agents[i].PauseAnimParameters();
        }

        bool ReachGoal(int id)
        {
            float distSqToGoal = (goal[id] - GetPosition(id)).sqrMagnitude;
            return distSqToGoal <= goalRadius * goalRadius;
        }

        Vector2 GetPosition(int id) {
            return Simulator.Instance.getAgentPosition(id);
        }

        Vector2 GetVelocity(int id) {
            if (finish[id]) return Vector2.zero;
            return Simulator.Instance.getAgentVelocity(id);
        }

        float GetPrefSpeed(int id) {
            return Simulator.Instance.getAgentMaxSpeed(id);
        }

        float GetRadius(int id)
        {
            return Simulator.Instance.getAgentRadius(id);
        }

        void SetPreferredVelocities()
        {
            for (int i = 0; i < agents.Length; i++)
            {
                Vector2 goalVector = goal[i] - GetPosition(i);
                goalVector *= GetPrefSpeed(i) / goalVector.magnitude;
                Simulator.Instance.setAgentPrefVelocity(i, goalVector);
            }
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

