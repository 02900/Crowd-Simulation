﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AnticipatoryModel
{
    public class AMAgent : Agent
    {
        const float EPSILON = 0.02f;
        public const float goalRadius = 0.5f;

        #region definition of agent
        float prefSpeed;               // The max speed of the  
        float prefSpeedCache;               // The max speed of the  
        float timeHorizon;             // given a certain time tH, the agent will ignore any 
                                      // collisions that will happen more than tH seconds from now

        // Field Of View
        float neighborDist;
        float viewAngle = 135;
        float personalSpace = 3;

        public LayerMask targetMask;
        public LayerMask obstacleMask;

        [SerializeField] bool debugLog = false;
        [SerializeField] bool debugGroups = false;
        [SerializeField] bool useGroups = false;
        #endregion

        #region state of agent
        public Vector2 goal { get; set; }                  // The goal of the character. 
        Agent neighbor;
        Agent SetNeigbor { set { neighbor = value; ResetStrategy(); } }
        float min_ttc;
        Dictionary<int, float> ttc = new Dictionary<int, float>();
        Dictionary<int, float> group_ttc = new Dictionary<int, float>();
        #endregion

        #region eval strategy
        int cur_low_ttc;
        const float ttc_umbral = 1;
        const int MAX_INMINENT_COLISION = 7;
        const float thresholdCol = 7;

        public enum strategies { DCC, CH, F, A, N, NULL }
        strategies curStrategy = strategies.NULL;
        public strategies GetCurrentStrategy { get { return curStrategy; } }

        float duration;
        float cacheDuration;
        #endregion

        #region for navigation
        Transform target;                 // Empty gameObject attached to a agent to set his goal point
        Vector3 goal3d;                           // The goal position of the  
        Queue goals = new Queue();               // a list of waypoints to navigate to the goal 
        [SerializeField] bool navmesh = false;          // Use navigation mesh to get intermediate waypoints
        [SerializeField] float fixedPrefSpeed = 0;      // The goal position of the  
        #endregion

        AnimationController anim;
        public AnimationController Anim { get { return anim; } }
        DrawCircleGizmos drawCircles;

        void Awake()
        {
            anim = GetComponent<AnimationController>();
            drawCircles = GetComponent<DrawCircleGizmos>();
        }

        void Start()
        {
            name = "Agent " + id;
            drawCircles.allRadius[0] = radius;

            target = transform.childCount > 3 ? transform.GetChild(0) : null;
            if (target) goal3d = target.position;

            if (navmesh && target)
            {
                goals = new Queue(GetWaypoints(target, GetComponent<NavMeshAgent>()));
                if (goals.Count < 1) return;
                goal3d = (Vector3)goals.Dequeue();
            }

            if (System.Math.Abs(fixedPrefSpeed) > EPSILON) prefSpeed = fixedPrefSpeed;
            prefSpeedCache = prefSpeed;
        }

        public void Init(int id, Vector2 position, Vector2 goal)
        {
            base.id = id;

            // Gaussian distributed speed
            float rnd;
            do rnd = Random.value / Random.value; while (rnd >= 1.0);
            rnd = Mathf.Sqrt(-2.0f * Mathf.Log(1 - rnd)) *
                Mathf.Cos(2.0f * Mathf.PI * Random.value / Random.value);

            base.position = position;
            this. goal = goal;
            radius = Random.Range(0.25f, 0.5f);
            prefSpeed = 3 + Mathf.Abs(rnd);
            timeHorizon = Random.Range(3.0f, 8.0f);
            neighborDist = Random.Range(8.0f, 12.0f);

            duration = Random.Range(1.5f, 3.0f);
			cacheDuration = duration;
        }

        public bool DoStep()
        {
            if (debugLog) Debug.DrawRay(new Vector3(position.x, 1.5f, position.y),
                new Vector3(velocity.x, 0, velocity.y), Color.magenta);

            Vector2 dir = goal - position;
            float distSqToGoal = dir.sqrMagnitude;

            if (distSqToGoal <= goalRadius * goalRadius)
            {
                velocity = Vector2.zero;
                return true;
            }

            DetectingNeighbors();
            if (useGroups) DetectingGroups();
            Evaluate();

            // Limit velocity to prefSpeed of agent
            if (velocity.sqrMagnitude > (prefSpeed * prefSpeed))
            {
                velocity.Normalize();
                velocity *= prefSpeed;
            }

            position += velocity * Engine.timeStep;

            MoveInRealWorld();
            return false;
        }

        void DetectingNeighbors()
        {
            ttc.Clear();
            Collider[] targetsInViewRadius = Physics.OverlapSphere
                (transform.position, neighborDist, targetMask);

            for (int i = 0; i < targetsInViewRadius.Length; i++)
            {
                var n = targetsInViewRadius[i].GetComponent<AMAgent>();
                if (id == n.id) continue;
                Vector3 dirToTarget =
                    (targetsInViewRadius[i].transform.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance
                        (transform.position, targetsInViewRadius[i].transform.position);
                    if (!Physics.Raycast(transform.position + Vector3.up,
                        dirToTarget, dstToTarget, obstacleMask))
                    {
                        // compute time to collision
                        ttc.Add(n.id, Global.TTC(position, velocity, radius, 
                            n.position, n.velocity, n.radius));
                    }
                }
            }

            Collider[] targetsInViewRadiusBack = Physics.OverlapSphere
                (transform.position, personalSpace, targetMask);
            for (int i = 0; i < targetsInViewRadiusBack.Length; i++)
            {
                var n = targetsInViewRadius[i].GetComponent<AMAgent>();
                if (id == n.id) continue;
                Vector3 dirToTarget =
                    (targetsInViewRadiusBack[i].transform.position - transform.position).normalized;
                if (Vector3.Angle(-transform.forward, dirToTarget) < (360 - viewAngle) / 2)
                {
                    float dstToTarget =
                        Vector3.Distance(transform.position, targetsInViewRadiusBack[i].transform.position);
                    if (!Physics.Raycast(transform.position + Vector3.up,
                        dirToTarget, dstToTarget, obstacleMask))
                    {
                        if (!ttc.ContainsKey(n.id))
                        {
                            // compute time to collision
                            ttc.Add(n.id, Global.TTC(position, velocity, radius,
                                n.position, n.velocity, n.radius));
                        }
                    }
                }
            }
        }  
 
        void ResetStrategy()
        {
            curStrategy = strategies.NULL;
            duration = cacheDuration;
            prefSpeed = prefSpeedCache;
        }

        void Evaluate()
        {
            duration -= Engine.timeStep;
            if (duration <= 0) ResetStrategy();

            bool targetIsGroup = false;
            int j = -1; // id del agente o grupo con el ttc mas bajo
            min_ttc = Mathf.Infinity;

            foreach(var key in ttc.Keys) {
                if (ttc[key] < min_ttc)
                {
                    j = key;
                    min_ttc = ttc[key];
                }
            }

            foreach (var key in group_ttc.Keys)
            {
                if (group_ttc[key] < min_ttc)
                {
                    j = key;
                    min_ttc = group_ttc[key];
                    targetIsGroup = true;
                }
            }

            if (j != -1 && min_ttc < timeHorizon)
            {
                if (!targetIsGroup) neighbor = Engine.Instance.GetAgent(j);
                else neighbor = Engine.Instance.GetVirtualAgent(j);
                ApplyStrategy();
            }
            else { velocity = Behaviours.GetSteering(position, goal, prefSpeed); }
        }

        void DetectingGroups() {
            group_ttc.Clear();
            if (Engine.Instance.GetGroups.Count == 0) return;

            foreach (List<int> group in Engine.Instance.GetGroups) {
                Vector2 gPos, gVel;
                float gRad;

                if (Groups.PercivingGroups(position, goal - position, radius, group, ttc, out gPos, out gVel, out gRad, debugGroups)) {
                    var vAgents = Engine.Instance.VirtualAgents;
                    for (int i = 0; i < vAgents.Length; i++)
                    {
                        if (!vAgents[i].Used)
                        {
                            vAgents[i].SetupAgent(gRad, gPos, gVel);
                            group_ttc.Add(vAgents[i].id, Global.TTC(position, velocity, radius,
                                vAgents[i].position, vAgents[i].velocity, vAgents[i].radius));
                        }
                    }
                }
            }
        }

        void ApplyStrategy()
        {
            int[] s = { 3 };
            DetermineStrategy(s);

            //float vAngle = Vector2.Angle(velocity, neighbor.velocity);
            //if (System.Math.Abs(vAngle - 180) < thresholdCol) FrontCollision();
            //else if (System.Math.Abs(vAngle) < thresholdCol) RearCollision();
            //else LateralCollision();
        }

        void FrontCollision()
        {
            //Debug.Log("Colision de frontal de ambos agentes");
            if (debugLog) Debug.Log("My id:" + id + ", Colision Front-Mutual");

            int[] s = { 1 };

            if (min_ttc < 3)
                s = new[] { 1 };

            if (neighbor.IsVirtual || 
                neighbor.velocity.sqrMagnitude < prefSpeed) s = new[] { 1 };

            DetermineStrategy(s);
        }

        void RearCollision()
        {
            float bearingAngle = Behaviours.BearingAngle(velocity, neighbor.position - position);

            if (bearingAngle <= 90 || bearingAngle > 270)
            {
                if (debugLog) Debug.Log("My id:" + id + ", Colision Back");
                int[] s = { 1 };
                DetermineStrategy(s);
            }

            // (bearingAngle > 90 && bearingAngle <= 270)
            else
            {
                if (debugLog) Debug.Log("My id:" + id + ", Colision Front-1");
                int[] s = { 1, 2 };
                if (neighbor.velocity.sqrMagnitude < 1) s = new int[] { 1 };
                DetermineStrategy(s);
            }
        }

        void LateralCollision()
        {
            if (debugLog) Debug.Log("My id:" + id + ", Colision Lateral");
            int[] s = { 1 };
            DetermineStrategy(s, true);
        }

        void DetermineStrategy(int[] s, bool lateral = false)
        {
            bool sameStrategy = false;
            foreach (var ss in s)
            {
                if (curStrategy == (strategies)ss)
                {
                    sameStrategy = true;
                    break;
                }
            }

            if (!sameStrategy && curStrategy == strategies.NULL && System.Array.IndexOf(s, curStrategy) == -1)
            {
                int i = Random.Range(0, s.Length);
                curStrategy = (strategies)s[i];

                if (debugLog) Debug.Log(id + " , The current strategy is: " + curStrategy);
            }

            switch (curStrategy)
            {
                case strategies.DCC:
                    velocity = Behaviours.GetSteering(position, goal, prefSpeed);
                    velocity = Behaviours.DecelerationStrategy(min_ttc, velocity);
                    break;

                case strategies.CH:
                    velocity = Behaviours.GetSteering(position, goal, prefSpeed);
                    velocity = Behaviours.ChangeDirectionStrategy(velocity,
                        neighbor.position - position, lateral, min_ttc, debugLog);
                    break;

                case strategies.F:
                    velocity = Behaviours.GetSteering(position, goal, prefSpeed);
                    velocity = Behaviours.FollowStrategy(radius, prefSpeed, position, 
                        velocity, neighbor.position, neighbor.velocity);
                    break;

                case strategies.A:
                    velocity += Behaviours.CollisionAvoidance(position, velocity, 
                    Behaviours.GetSteering(position, goal, prefSpeed), timeHorizon, ttc, group_ttc);
                    break;

                case strategies.N:
                    velocity = Behaviours.GetSteering(position, goal, prefSpeed);
                    break;
            }

            if (debugLog) Debug.DrawRay(new Vector3(position.x, 1.5f, position.y),
                new Vector3(velocity.x, 0, velocity.y), Color.green);
        }

        public void MoveInRealWorld()
        {
            Vector3 vel = ExtensionMethods.Vector2ToVector3(velocity);
            anim.ColorSpeed(vel);

            transform.localPosition = new Vector3(position.x, 0, position.y);
            float distanceToTarget = Vector3.Distance(goal3d, transform.localPosition);
            anim.Move(vel, Engine.timeStep);

            // Agent is within one radius of its goal then go to next waypoint
            if (distanceToTarget < goalRadius * 2)
            {
                if (goals.Count < 1) return;
                goal3d = (Vector3)goals.Dequeue();
                goal = new Vector2(goal3d.x, goal3d.z);
            }
        }

        public void RecMove()
        {
            Vector3 vel = ExtensionMethods.Vector2ToVector3(velocity);
            anim.ColorSpeed(vel);
            transform.localPosition = new Vector3(position.x, 0, position.y);
            anim.Move(vel, Engine.timeStep);
        }

        Vector3[] GetWaypoints(Transform goalTransform, NavMeshAgent agent)
        {
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(goalTransform.position, path);

            if (path.status == NavMeshPathStatus.PathComplete)
                return path.corners;

            return new Vector3[] { };
        }
    }
}