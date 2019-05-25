using System.Collections;
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

        // Collision avoidance parameters
        Agent neighbor;                              // Agent to collide
        Agent SetNeigbor { set { neighbor = value; ResetStrategy(); } }

        Dictionary<int, float> ttc = new Dictionary<int, float>();
        Dictionary<int, float> group_ttc = new Dictionary<int, float>();
        float min_ttc;
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

        #region for Color Speed
        [SerializeField] Color Color1 = Color.blue, Color2 = Color.cyan;
        [SerializeField] Renderer _renderer = null;
        MaterialPropertyBlock _propBlock;
        #endregion

        #region for animation
        Animator m_Animator;
        float m_TurnAmount;
        float m_ForwardAmount;
        Vector3 m_GroundNormal;

        const float m_MovingTurnSpeed = 360;
        const float m_StationaryTurnSpeed = 180;
        #endregion

        #region init real word
        void Awake() { 
            _propBlock = new MaterialPropertyBlock();
            m_Animator = GetComponent<Animator>();
        }

        void Start()
        {
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
        #endregion

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
                        ttc.Add(n.id, Global.TTC(this, n));
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
                            ttc.Add(n.id, Global.TTC(this, n));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Only move to goal direction with vpref
        /// </summary>
        Vector2 GetSteering()
        {
            Vector2 dir = goal - position;
            velocity += dir.normalized * prefSpeed;
            return velocity;
        }

        /// <summary>
        /// Basic algorithm to avoidance collisions
        /// </summary>
        void Anticipatory()
        {
            Vector2 FAvoid;

            // vg is the agent’s goal velocity
            Vector2 goalVelocity = GetSteering();
            // k is a tunable parameter that controls the strength of the goal force
            float k = 2;

            // Compute goal force
            Vector2  acceleration = k * (goalVelocity - velocity);

            foreach (var key in ttc.Keys) {
            
                // compute time to collision
                float t = ttc[key];
                if (float.IsInfinity(t)) continue;

                AMAgent agent_tmp = Engine.Instance.GetAgent(key);

                // Compute collision avoidance force
                FAvoid = position + velocity * t
                    - agent_tmp.position - agent_tmp.velocity * t;

                FAvoid *= 1 / (t + 0.1f);

                if (System.Math.Abs(FAvoid.x) > EPSILON &&
                    System.Math.Abs(FAvoid.y) > EPSILON)
                    FAvoid /= Mathf.Sqrt(Vector2.Dot(FAvoid, FAvoid));

                // Force Magnitude
                float mag = 0;
                if (t >= 0 && t <= timeHorizon)
                    mag = (timeHorizon - t) / t + 0.1f;
                if (mag > 100) mag = 100;
                FAvoid *= mag;
                acceleration += FAvoid;
            }

            velocity += acceleration * Engine.timeStep;
        }

        void FollowStrategy()
        {
            float magnitude;
            float ttr = 1.5f;                     // reaction time
            float df = radius * 3 + 1.5f;         // zone contact + personal distances

            // Posicion futura del leader
            Vector2 pl = neighbor.position + neighbor.velocity * Engine.timeStep;

            // distance to future position of leader
            float dstLeader = (pl - position).magnitude;
            float vf = (dstLeader - df) / (Engine.timeStep + ttr);

            magnitude = vf > prefSpeed ? prefSpeed : vf;
            velocity = velocity.normalized * magnitude;
        }

        float BearingAngle()
        {
            float a = -Vector2.SignedAngle(velocity, neighbor.position - position);
            if (a < 0) a += 360;
            return a;
        }

        void ChangeDirectionStrategy(bool lateral)
        {
            // dir equal 1 is left, -1 is right
            float bearingAngle = BearingAngle(), dir;

            if (debugLog) Debug.Log(bearingAngle);

            if (!lateral) {
                if (bearingAngle < 180) dir = 1;
                else dir = -1;

                if (System.Math.Abs(bearingAngle) < EPSILON 
                    || System.Math.Abs(bearingAngle - 360) < EPSILON
                    || System.Math.Abs(bearingAngle - 180) < EPSILON) {
                    dir = Random.Range(0, 11) > 5  ? -1 : 1;
                }
            }

            else {
                if (bearingAngle >= 10 && bearingAngle < 170
                    || bearingAngle >= 185 && bearingAngle < 350) dir = 1;
                else {
                    if (bearingAngle < 180) dir = 1;
                    else dir = -1;
                }
            }

            float w = 10 / (Mathf.Pow(min_ttc, 2) + 0.25f);
            Vector2 velOrtho = ExtensionMethods.RotateVector(velocity.normalized, 90);
            velocity = ExtensionMethods.RotateVector(velocity, dir * w);
            velocity += dir * velOrtho * 0.1f;
        }

        void DecelerationStrategy()
        {
            float k = min_ttc < 2 ? 2 : min_ttc;
            k = Mathf.Exp(-0.15f * k * k);
            velocity = velocity * (1 - k);
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
            else { GetSteering(); }
        }

        void DetectingGroups() {
            group_ttc.Clear();
            if (Engine.Instance.GetGroups.Count == 0) return;

            foreach (List<int> group in Engine.Instance.GetGroups) {
                Vector2 gPos, gVel;
                float gRad;

                if (Groups.PercivingGroups(position, goal - position, radius, group, ttc, out gPos, out gVel, out gRad, debugGroups)) {
                    Debug.Log(id + " i can see a group");
                    var vAgents = Engine.Instance.VirtualAgents;
                    for (int i = 0; i < vAgents.Length; i++)
                    {
                        if (!vAgents[i].Used)
                        {
                            vAgents[i].SetupAgent(gRad, gPos, gVel);
                            group_ttc.Add(vAgents[i].id, Global.TTC(this, vAgents[i]));
                        }
                    }
                }
            }
        }

        void ApplyStrategy()
        {
            float vAngle = Vector2.Angle(velocity, neighbor.velocity);
            if (System.Math.Abs(vAngle - 180) < thresholdCol) FrontCollision();
            else if (System.Math.Abs(vAngle) < thresholdCol) RearCollision();
            else LateralCollision();
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
            float bearingAngle = BearingAngle();

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
                case strategies.DCC: GetSteering(); DecelerationStrategy(); break;
                case strategies.CH: GetSteering(); ChangeDirectionStrategy(lateral); break;
                case strategies.F: GetSteering(); FollowStrategy(); break;
                case strategies.A: Anticipatory(); break;
                case strategies.N: GetSteering(); break;
            }
        }

        /// <summary>
        /// Real World
        /// </summary>
        public void MoveInRealWorld()
        {
            ColorSpeed();

            transform.localPosition = new Vector3(position.x, 0, position.y);
            float distanceToTarget = Vector3.Distance(goal3d, transform.localPosition);

            Move(ExtensionMethods.Vector2ToVector3(velocity.normalized));

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
            ColorSpeed();
            transform.localPosition = new Vector3(position.x, 0, position.y);
            Move(ExtensionMethods.Vector2ToVector3(velocity.normalized));
        }

        void ColorSpeed()
        {
            // Get the current value of the material properties in the renderer.
            _renderer.GetPropertyBlock(_propBlock);
            // Assign our new value.
            _propBlock.SetColor("_Color", Color.Lerp(Color1, Color2, GetPercentSpeed()));
            // Apply the edited values to the renderer.
            _renderer.SetPropertyBlock(_propBlock);
        }

        Vector3[] GetWaypoints(Transform goalTransform, NavMeshAgent agent)
        {
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(goalTransform.position, path);

            if (path.status == NavMeshPathStatus.PathComplete)
                return path.corners;

            return new Vector3[] { };
        }

        float GetPercentSpeed()
        {
            if (System.Math.Abs(velocity.magnitude) < EPSILON) return 0.01f;
            return velocity.magnitude / 3;
        }

        public void Move(Vector3 dir)
        {
            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            dir = transform.InverseTransformDirection(dir);
            dir = Vector3.ProjectOnPlane(dir, m_GroundNormal);

            m_TurnAmount = Mathf.Atan2(dir.x, dir.z);
            m_ForwardAmount = dir.z * GetPercentSpeed();

            LookWhereImGoing();
            UpdateAnimator(dir);
        }

        private void LookWhereImGoing()
        {
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount * turnSpeed * Engine.timeStep, 0);
        }

        private void UpdateAnimator(Vector3 move)
        {
            m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Engine.timeStep);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Engine.timeStep);
        }

        public void ResetAnimParameters(bool full = false)
        {
            m_Animator.SetFloat("Forward", 0, full ? 0 : 0.1f, Engine.timeStep);
            m_Animator.SetFloat("Turn", 0, full ? 0 : 0.1f, Engine.timeStep);
            ColorSpeed();
        }
    }
}
