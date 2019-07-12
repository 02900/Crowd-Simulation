using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenSteer;

namespace CrowdSimulation
{
    public class Engine : MonoBehaviour
    {
        public static Engine Instance;
        public const float timeStep = 0.01f;
        const float moreDelayedTimeStep = 0.5f;
        const float mass_half = 33;
        int framesCount;
        int framesCountDelayed;

        float hits;
        public void AddHit () { hits +=0.5f; }
        public float GetHits { get { return hits; } }

        #region Groups
        const float ed = 1.25f;               // distance        
        const float ev = 0.5f;                // velocity
        const float eav = 30;                 // angle
        #endregion

        AAgent[] agents;
        public AAgent[] Agents { get { return agents; } set { agents = value; } }
        public VirtualAgent[] VirtualAgents { get; set; }

        /// <param name="id">The id of the agent.</param>
        /// <returns>Returns the corresponding agent given its id/</returns>
        public AAgent GetAgent(int id) { return Agents[id]; }

        /// <param name="id">The id of the agent.</param>
        /// <returns>Returns the corresponding agent given its id/</returns>
        public VirtualAgent GetVirtualAgent(int id) { return VirtualAgents[id]; }

        /// Returns the number of agents in the simulation. 
        public int GetNumAgents() { return Agents.Length; }

        float[] DST_TRAVEL;
        float[] TIME_TRAVEL;
        float[] EKinematic;
        bool[] finish;

        List<float> TimesTravel = new List<float>();
        List<float> DistancesTravel = new List<float>();

        float time_mean;
        float dst_mean;
        float ek_mean;

        HandleTextFile results;
        bool playingRec = true;

        // Hold pairs of agents in same group
        List<Tuple<int, int>> pairs = new List<Tuple<int, int>>();
        List<List<int>> groups = new List<List<int>>();

        // The proximity database.
        protected LQProximityDatabase spatialDatabase;

        ///  Returns a pointer to the proximity database of the engine.
        public LQProximityDatabase GetSpatialDatabase { get { return spatialDatabase; } }

        void AddAgentStat(float time, float dst) {
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

        void Awake()
        {
            Instance = this;
            spatialDatabase = new LQProximityDatabase(Vector3.zero, new Vector3(500, 0, 500), new Vector3(10, 0, 10));
            results = FindObjectOfType<HandleTextFile>();
            CreateAgents();
            CreateVirtualAgents();
        }

        void Start()
        {
            StartCoroutine(FrameDelayedUpdateGroupsRoutine());
            StartCoroutine(FrameDelayedUpdateRoutine());
        }

        void CreateVirtualAgents() {
            VirtualAgents = FindObjectsOfType<VirtualAgent>();
            for (int i = 0; i < VirtualAgents.Length; i++)
                VirtualAgents[i].id = i;
        }

        void CreateAgents()
        {
            Agents = FindObjectsOfType<AAgent>();
            Vector2 position, goal;
            for (int i = 0; i < Agents.Length; i++)
            {
                position = ExtensionMethods.ToXZ(Agents[i].transform.position);
                goal = ExtensionMethods.ToXZ(Agents[i].transform.GetChild(0).position);
                Agents[i].Init(i, position, goal);
            }

            if (results.recordStats)
            {
                DST_TRAVEL = new float[agents.Length];
                TIME_TRAVEL = new float[agents.Length];
                EKinematic = new float[agents.Length];
            }

            finish = new bool[agents.Length];
        }

        /// <summary>
        /// A loop which executes logic after waiting for an amount of frames to pass.
        /// </summary>
        IEnumerator FrameDelayedUpdateRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(timeStep);
                if (Input.GetKey(KeyCode.Q)) {
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
        IEnumerator FrameDelayedUpdateGroupsRoutine()
        {
            DetectingGroups();
            while (true)
            {
                yield return new WaitForSeconds(moreDelayedTimeStep);
                if (!results.loadRec && Input.GetKey(KeyCode.Z))
                {
                    for (int i = 0; i < Agents.Length && results.recordStats; i++)
                    {
                        if (agents[i].velocity != Vector2.zero)
                            EKinematic[i] += mass_half * Agents[i].velocity.sqrMagnitude;
                    }
                    DetectingGroups();
                    framesCountDelayed++;
                }
            }
        }

        void ForceFinish() {
            foreach (var a in agents) {
                if (finish[a.id]) continue;
                if (results.recordStats)
                {
                    TIME_TRAVEL[a.id] = framesCount * timeStep;
                    EKinematic[a.id] /= framesCountDelayed;
                    AddAgentStat(TIME_TRAVEL[a.id], DST_TRAVEL[a.id]);
                }
                agents[a.id].Anim.ResetAnimParameters(timeStep, true);
                finish[a.id] = true;
            }
        }

        /// <summary>
        /// Performs a simulation/integration step i.e. updates the acceleration, 
        /// velocity and position of the simulated characters.
        /// </summary>
        void UpdateSimulation()
        {
            if (!results.loadRec) {
                framesCount++;
                Vector2 prevPos;

                for (int i = 0; i < Agents.Length; i++)
                {
                    if (results.record) results.RecordStep(Agents[i].position, Agents[i].velocity);
                    if (finish[i]) continue;
                    prevPos = agents[i].position;
                    finish[i] = agents[i].DoStep();

                    if (results.recordStats && agents[i].velocity != Vector2.zero)
                    {
                        float dstChange = Vector2.Distance(prevPos, agents[i].position);
                        DST_TRAVEL[i] += dstChange;
                    }

                    if (finish[i])
                    { 
                        if (results.recordStats) {
                            TIME_TRAVEL[i] = framesCount * timeStep;
                            EKinematic[i] /= framesCountDelayed;
                            AddAgentStat(TIME_TRAVEL[i], DST_TRAVEL[i]);
                        }
                        agents[i].Anim.ResetAnimParameters(timeStep, true);
                    }
                }
                DesactivateVirtualAgents();
            }

            else if (playingRec) {
                for (int i = 0; i < Agents.Length && !results.EndStream(); i++)
                {
                    if (!results.EndStream()) {
                        agents[i].position = results.LoadVector();
                        agents[i].velocity = results.LoadVector();
                        agents[i].RecMove();
                    }
                }

                if (results.EndStream())
                {
                    playingRec = false;
                    results.CloseFile();
                    for (int i = 0; i < Agents.Length; i++)
                        agents[i].Anim.ResetAnimParameters(timeStep, true);
                }
            }
        }

        void PauseAnimation()
        {
            for (int i = 0; i < Agents.Length; i++)
                Agents[i].Anim.ResetAnimParameters(timeStep);
        }

        public List<List<int>> GetGroups { get { return groups; } set { groups = value; } }

        void DetectingGroups()
        {
            pairs.Clear();

            for (int j = 0; j < agents.Length; j++)
            {
                for (int i = 0; i < agents.Length; i++)
                {
                    if (i == j) continue;

                    AAgent a = agents[i].GetComponent<AAgent>();
                    AAgent b = agents[j].GetComponent<AAgent>();

                    var t1 = new Tuple<int, int>(a.id, b.id);
                    var t2 = new Tuple<int, int>(b.id, a.id);

                    if (BelongToSameGroup(a, b)
                        && !pairs.Contains(t1)
                        && !pairs.Contains(t2))
                        pairs.Add(new Tuple<int, int>(a.id, b.id));
                }
            }

            CalculateTransitions(pairs);
        }

        bool BelongToSameGroup(AAgent a, AAgent b)
        {
            bool belong = false;

            // Condition 1: same position
            float dist = Vector2.Distance(a.position, b.position);
            belong = dist < ed;

            //// Condition 2: same velocity magnitude
            belong &= Mathf.Abs(a.velocity.sqrMagnitude - b.velocity.sqrMagnitude) < ev * ev;

            //// Condition 3: same move direction
            Vector2 dirA = (a.goal - a.position).normalized;
            Vector2 dirB = (b.goal - b.position).normalized;
            belong &= Vector2.Angle(dirA, dirB) < eav;

            return belong;
        }

        void CalculateTransitions(List<Tuple<int, int>> pairs)
        {
            GetGroups.Clear();

            while (pairs.Count > 0)
            {
                GetGroups.Add(new List<int>());
                GetGroups[GetGroups.Count - 1].Add(pairs[0].Item1);
                GetGroups[GetGroups.Count - 1].Add(pairs[0].Item2);
                pairs.RemoveAt(0);

                bool change;
                do
                {
                    change = false;
                    int n = pairs.Count;
                    for (int i = 0; i < n; i++)
                    {
                        foreach (var group in GetGroups)
                        {
                            bool transition = false;
                            transition = group.Contains(pairs[i].Item1);
                            transition = transition || group.Contains(pairs[i].Item2);

                            if (transition)
                            {
                                if (!group.Contains(pairs[i].Item1)) group.Add(pairs[i].Item1);
                                if (!group.Contains(pairs[i].Item2)) group.Add(pairs[i].Item2);
                                pairs.RemoveAt(i);
                                i--;
                                n--;
                                change = true;
                            }
                        }
                    }
                } while (change);
            }

            //int p = 0;

            //Debug.Log("Cantidad de grupos? : " + groups.Count);

            //foreach (var group in groups)
            //{
            //    Debug.Log("Grupo " + p);
            //    foreach (int id in group)
            //        Debug.Log("AgentID: " + id);

            //    Debug.Log("\n");
            //    p++;
            //}
        }

        /// <summary>
        /// Destruir agentes virtuales, debido a que hay que recalcular su 
        /// posicion en el siguiente step, a menos que los mantenga por mas de un frame
        /// </summary>
        void DesactivateVirtualAgents()
        {
            foreach (VirtualAgent agenteVirtual in VirtualAgents)
                if (agenteVirtual.Used) agenteVirtual.Reset();
        }
    }
}