/*
 *  SimulationEngine.h
 *  
 *  
 *  All rights are retained by the authors and the University of Minnesota.
 *  Please contact sjguy@cs.umn.edu for licensing inquiries.
 *  
 *  Authors: Ioannis Karamouzas, Brian Skinner, and Stephen J. Guy
 *  Contact: ioannis@cs.umn.edu
 */

/*!
 *  @file       SimulationEngine.h
 *  @brief      Contains the SimulationEngine class.
 */

using OpenSteer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerLaw
{
    public class Engine : MonoBehaviour
    {
        public const float timeStep = 0.01f;
        const float moreDelayedTimeStep = 0.2f;
        public static Engine Instance;
        const float mass_half = 33;
        float framesCount;
        float framesCountDelayed;

        // The proximity database.
        protected LQProximityDatabase spatialDatabase;

        ///  Returns a pointer to the proximity database of the engine.
        public LQProximityDatabase GetSpatialDatabase { get { return spatialDatabase; } }

        Agent[] agents;                           // The agents in the simulation/
        List<LineObstacle> obstacles = new List<LineObstacle>();          // The obstacles in the simulation.

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

        void Awake(){
            Instance = this;
            spatialDatabase = new LQProximityDatabase(Vector3.zero, new Vector3(500, 0, 500), new Vector3(10, 0, 10));
            CreateAgents();
            results = FindObjectOfType<HandleTextFile>();
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

        public void AddAgentStat(float time, float dst)
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
                    TimesTravel[agents.Length - 1]);

                results.CloseRecord();
            }
        }

        void Start()
        {
            StartCoroutine(FrameDelayedUpdateRoutine());
            StartCoroutine(FrameMoreDelayedUpdateRoutine());
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
                    if (results.record) results.RecordStep(agents[i].Position, agents[i].Velocity);
                    if (finish[i]) continue;
                    prevPos = agents[i].Position;
                    finish[i] = agents[i].DoStep();

                    if (results.recordStats && agents[i].Velocity != Vector2.zero)
                    {
                        float dstChange = Vector2.Distance(prevPos, agents[i].Position);
                        DST_TRAVEL[i] += dstChange;
                    }

                    if (finish[i])
                    {
                        if (results.recordStats) {
                            TIME_TRAVEL[i] = framesCount * timeStep;
                            EKinematic[i] /= framesCountDelayed;
                            AddAgentStat(TIME_TRAVEL[i], DST_TRAVEL[i]);
                        }
                        agents[i].ResetAnimParameters(true);
                    }
                }
            }

            else if (playingRec)
            {

                for (int i = 0; i < agents.Length && !results.EndStream(); i++)
                {
                    if (!results.EndStream())
                    {
                        agents[i].Position = results.LoadVector();
                        agents[i].Velocity = results.LoadVector();
                        agents[i].RecMove();
                    }
                }

                if (results.EndStream())
                {
                    playingRec = false;
                    results.CloseFile();
                    for (int i = 0; i < agents.Length; i++)
                        agents[i].ResetAnimParameters(true);
                }
            }
        }

        void PauseAnimation()
        {
            for (int i = 0; i < agents.Length; i++)
                agents[i].ResetAnimParameters();
        }

        /// <summary>
        /// Add a new line obstacle to the simulation.
        /// </summary>
        /// <param name="start">The start point of the line segment</param>
        /// <param name="end">The end point of the line segment</param>
        public void AddObstacle(Vector2 start, Vector2 end)
        {
            LineObstacle l = new LineObstacle(start, end);
            obstacles.Add(l);
        }

        ///  Returns the list of obstacles. 
        public List<LineObstacle> GetObstacles { get { return obstacles; } }

        /// <param name="id">id The id of the obstacle.</param>
        /// <returns>Returns the corresponding line obstacle given its id.</returns>
        public LineObstacle GetObstacle(int id) { return obstacles[id]; }
    }
}