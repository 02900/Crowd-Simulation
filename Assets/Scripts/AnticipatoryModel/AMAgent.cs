using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AnticipatoryModel
{
    public class AMAgent : Agent
    {
        const float EPSILON = 0.02f;
        const float goalRadius = 0.75f;
    
        #region definition of agent
        float prefSpeed;               // The max speed of the  
        float timeHorizon;             // given a certain time tH, the agent will ignore any 
                                      // collisions that will happen more than tH seconds from now

        float neighborDist;
        float viewAngle = 180;
        float personalSpace = 1;
        const float thresholdCol = 12;
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
        float duration;
        float cacheDuration;
        public enum strategies { DCC, CH, F, A, N, NULL }
        strategies curStrategy = strategies.NULL;
        public strategies GetCurrentStrategy { get { return curStrategy; } }
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

        [SerializeField] bool debugLog = false;
        [SerializeField] bool debugFollowing= false;
        [SerializeField] bool debugGroups = false;
        [SerializeField] bool useGroups = false;

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
        }

        public void Init(int id, Vector2 position, Vector2 goal)
        {
            base.id = id;
            base.position = position;
            this. goal = goal;
            radius = 0.25f;
            prefSpeed = Random.Range(1.3f, 1.6f);
            timeHorizon = Random.Range(3, 4);
            neighborDist = Random.Range(8, 9);
            duration = Random.Range(3f, 4f);
            cacheDuration = duration;
            AddToDB();
        }

        public bool DoStep()
        {
            Vector2 dir = goal - position;
            float distSqToGoal = dir.sqrMagnitude;

            if (distSqToGoal <= goalRadius * goalRadius)
            {
                velocity = Vector2.zero;
                return true;
            }

            velocity = Behaviours.GetSteering(position, goal, prefSpeed);

            ttc.Clear();
            DetectingNeighbors(neighborDist, dir, viewAngle);
            DetectingNeighbors(personalSpace, -dir, 360 - viewAngle);

            if (useGroups) DetectingGroups();
            Evaluate();

            // Limit velocity to prefSpeed of agent
            if (velocity.sqrMagnitude > (prefSpeed * prefSpeed))
            {
                velocity.Normalize();
                velocity *= prefSpeed;
            }

            position += velocity * Engine.timeStep;
            UpdateDB();

            MoveInRealWorld();
            return false;
        }

        void DetectingNeighbors(float inRadius, Vector2 forward, float viewAngle)
        {
            SearchNeighbors(inRadius);
            for (int i = 0; i < ProximityNeighbors.Count; i++)
            {
                Agent n = (Agent)ProximityNeighbors[i];
                if (id == n.id) continue;

                Vector2 dirToNeighbor = n.position - position;
                if (Vector2.Angle(forward, dirToNeighbor) < viewAngle / 2)
                {
                    if (!ttc.ContainsKey(n.id))
                        ttc.Add(n.id, Global.TTC(position, velocity, radius,
                        n.position, n.velocity, n.radius));
                }
            }
        }

        void DetectingGroups()
        {
            group_ttc.Clear();
            if (Engine.Instance.GetGroups.Count == 0) return;

            foreach (List<int> group in Engine.Instance.GetGroups)
            {
                List<int> members;
                Vector2 gPos, gVel;
                float gRad;
                int turn;

                if (Groups.PercivingGroups(id, position, goal - position, radius,
                    group, ttc, out gPos, out gVel, out gRad, out turn, out members, debugGroups))
                {
                    var vAgents = Engine.Instance.VirtualAgents;
                    for (int i = 0; i < vAgents.Length; i++)
                    {
                        if (!vAgents[i].Used)
                        {
                            vAgents[i].SetupAgent(gRad, gPos, gVel, turn);
                            group_ttc.Add(vAgents[i].id, Global.TTC(position, velocity, radius,
                                gPos, gVel, gRad));

                            foreach (int id in members)
                                ttc[id] = Mathf.Infinity;
                            break;
                        }
                    }
                }
            }
        }

        void ResetStrategy()
        {
            curStrategy = strategies.NULL;
            duration = cacheDuration;
            TurnTo = 0;
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

        void ApplyStrategy()
        {
            if (neighbor.velocity.magnitude < 0.3f)
                StaticObstacleCollision();

            else {
                float vAngle = Vector2.Angle(velocity, neighbor.velocity);
                if (System.Math.Abs(vAngle - 180) < thresholdCol) FrontCollision();
                else if (System.Math.Abs(vAngle) < thresholdCol) RearCollision();
                else LateralCollision();

                if (debugLog)
                {
                    Vector2 VELA = ExtensionMethods.RotateVector(velocity, thresholdCol) * 4;
                    Vector2 VELB = ExtensionMethods.RotateVector(velocity, -thresholdCol) * 4;
                    Debug.DrawRay(new Vector3(position.x, 2, position.y), ExtensionMethods.Vector2ToVector3(velocity), Color.blue);
                    Debug.DrawRay(new Vector3(position.x, 2, position.y), ExtensionMethods.Vector2ToVector3(VELA), Color.red);
                    Debug.DrawRay(new Vector3(position.x, 2, position.y), ExtensionMethods.Vector2ToVector3(VELB), Color.red);
                    Debug.DrawRay(new Vector3(position.x, 2, position.y), ExtensionMethods.Vector2ToVector3(neighbor.velocity) * 4, Color.cyan);
                }
            }
        }

        void FrontCollision()
        {
            // circle only 3 xd
            if (debugLog) DebugCollisionType(0);
            int[] s = { 3 };
            DetermineStrategy(s, 0);
        }

        void RearCollision()
        {
            Vector2 dir = velocity;
            if (velocity.sqrMagnitude < EPSILON) dir = goal - position;

            float bearingAngle = Behaviours.BearingAngle(dir, neighbor.position - position);
            int[] s;

            // Front
            if (bearingAngle <= 90 || bearingAngle > 270)
            {
                if (debugLog) DebugCollisionType(1);
                s = new []{ 2 };
                if (neighbor.velocity.sqrMagnitude < EPSILON) s = new int[] { 1 };
            }

            // Back
            // (bearingAngle > 90 && bearingAngle <= 270)
            else
            {
                if (debugLog) DebugCollisionType(2);
                s = new[] { 3 };
            }

            DetermineStrategy(s);
        }

        void LateralCollision()
        {
            if (debugLog) DebugCollisionType(3);
            int[] s = new[] { 3 };
            DetermineStrategy(s, 2);
        }

        void StaticObstacleCollision()
        {
            if (debugLog) DebugCollisionType(4);
            int[] s = { 3 };
            DetermineStrategy(s);
        }

        void DebugCollisionType(int i)
        {
            string type = "???";
            switch (i) {
                case 0: type = "Front-dual"; break;
                case 1: type = "Front-only";  break;
                case 2: type = "Back"; break;
                case 3: type = "Lateral"; break;
                case 4: type = "Static Obstacle"; break;
            }
            Debug.Log("<" + id + ", " + neighbor.id + ">, " + type + " and itsVirtual is " + neighbor.IsVirtual);
        }

        void DetermineStrategy(int[] s, int type=-1)
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

            if (!sameStrategy && System.Array.IndexOf(s, curStrategy) == -1)
            {
                ResetStrategy();
                int i = Random.Range(0, s.Length);
                curStrategy = (strategies)s[i];
                if (debugLog) Debug.Log(id + " , The current strategy is: " + curStrategy);
            }

            int turnTo = TurnTo;
            // Other options ??
            //Vector2 dir = neighbor.position - position;
            //Vector2 dir = neighbor.velocity - velocity;
            Vector2 dir = neighbor.position - position + (neighbor.velocity - velocity) * Engine.timeStep;
            switch (curStrategy)
            {
                case strategies.DCC:
                    velocity = Behaviours.DecelerationStrategy(min_ttc, velocity);
                    break;

                case strategies.CH:
                    velocity = Behaviours.ChangeDirectionStrategy(velocity,
                        dir, min_ttc, timeHorizon,
                        neighbor.TurnTo, out turnTo, type);
                    TurnTo = turnTo;

                    //if (type == 0)
                    //    foreach (var t in ttc.Keys)
                    //        if (ttc[t] < 1)
                    //            Engine.Instance.GetAgent(t).TurnTo = turnTo;
                    break;

                case strategies.F:
                    velocity = Behaviours.FollowStrategy(radius, prefSpeed, position, 
                        velocity, neighbor.position, neighbor.velocity, debugFollowing);
                    break;

                case strategies.A:
                    velocity = Behaviours.ChangeDirectionStrategy(velocity,
                        dir, min_ttc, timeHorizon,
                        neighbor.TurnTo, out turnTo, type);
                    TurnTo = turnTo;

                    if (type == 2)
                        foreach (var t in ttc.Keys)
                            if (ttc[t] < 1)
                                Engine.Instance.GetAgent(t).TurnTo = turnTo;

                    velocity += Behaviours.CollisionAvoidance(position, velocity, 
                    Behaviours.GetSteering(position, goal, prefSpeed), timeHorizon, ttc, group_ttc);
                    break;

                case strategies.N:
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
