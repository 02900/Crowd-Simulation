using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SocialForces
{
    // Keep track of 'crowd' vector index in 'SocialForce.h'
    public class Agent : MonoBehaviour
    {
        // Constant Values Based on (Moussaid et al., 2009)
        const float lambda = 2.0f;   // Weight reflecting relative importance of velocity vector against position vector
        const float gamma = 0.35f;  // Speed interaction
        const float n_prime = 3.0f;  // Angular interaction
        const float n = 2.0f;        // Angular interaction
        const float A = 4.5f;        // Modal parameter A

        Vector2 dst, dir, interactionDirection, interactionDirectionLeftNormal, f_ij;
        float B, theta, forceVelocityAmount, forceAngleAmount;
        float K;

        const float EPSILON = 0.02f;
        const float goalRadius = 0.5f;
        const float T = 0.54F;  // Relaxation time based on (Moussaid et al., 2009)
        const float neighborDist = 15;

        int id;
        float radius;
        float prefSpeed;

        Vector2 position;
        Vector2 velocity;
        Vector2 goal;

        public Vector2 Position { get { return position; } set { position = value; } }
        public Vector2 Velocity { get { return velocity; } set { velocity = value; } }

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

        void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            m_Animator = GetComponent<Animator>();
        }

        public void Init(int id, Vector2 position, Vector2 goal)
        {
            this.id = id;
            radius = 0.25f;
            prefSpeed = 3f;

            //gaussian distributed speed
            float u;
            do
            {
                u = Random.value / Random.value;
            } while (u >= 1.0);

            prefSpeed += Mathf.Sqrt(-2.0f * Mathf.Log(1.0f - u)) * 0.1f * Mathf.Cos(2.0f * Mathf.PI * Random.value / Random.value);
            this.position = position;
            this.goal = goal;
        }

        void Start()
        {
            #region real word
            target = transform.childCount > 3 ? transform.GetChild(0) : null;
            if (target) goal3d = target.position;

            if (navmesh && target)
            {
                goals = new Queue(GetWaypoints(target, GetComponent<NavMeshAgent>()));
                if (goals.Count < 1) return;
                goal3d = (Vector3)goals.Dequeue();
            }

            if (System.Math.Abs(fixedPrefSpeed) > EPSILON) prefSpeed = fixedPrefSpeed;
            #endregion
        }

        // Compute Driving Force
        Vector2 DrivingForce()
        {
            // Formula: f_i = ((desiredSpeed * e_i) - velocity_i) / T
            return (prefSpeed * (goal - position).normalized - velocity) / T;
        }

        // Computes f_ij
        Vector2 AgentInteractForce()
        {
            f_ij = Vector2.zero;

            foreach (Agent neighbor in Engine.Instance.Agents)
            {
                // Do Not Compute Interaction Force to Itself
                if (neighbor.id == id) continue;
                // Compute Distance Between Agent j and i
                dst = neighbor.position - position;

                // Skip Computation if Agents i and j are Too Far Away
                if (dst.sqrMagnitude > neighborDist * neighborDist)
                    continue;

                // Compute Direction of Agent j from i
                // Formula: e_ij = (position_j - position_i) / ||position_j - position_i||
                dir = dst.normalized;

                // Compute Interaction Vector Between Agent i and j
                // Formula: D = lambda * (velocity_i - velocity_j) + e_ij
                interactionDirection = lambda * (velocity - neighbor.velocity) + dir;

                // Compute Modal Parameter B
                // Formula: B = gamma * ||D_ij||
                B = gamma * interactionDirection.magnitude;

                // Compute Interaction Direction
                // Formula: t_ij = D_ij / ||D_ij||
                interactionDirection = interactionDirection.normalized;

                // Compute Angle Between Interaction Direction (t_ij) and Vector Pointing from Agent i to j (e_ij)
                theta = Vector2.Angle(interactionDirection, dir) * Mathf.Deg2Rad;

                // Compute Sign of Angle 'theta'
                // Formula: K = theta / |theta|
                K = (System.Math.Abs(theta) < EPSILON) ? 0 : Mathf.Sign(theta);

                // Compute Amount of Deceleration
                // Formula: f_v = -A * exp(-distance_ij / B - ((n_prime * B * theta) * (n_prime * B * theta)))
                float t1, t2, t3;
                t1 = -dst.magnitude / B;
                t2 = Mathf.Pow(n_prime * B * theta, 2);
                t3 = Mathf.Exp(t1 - t2);

                forceVelocityAmount = -A * t3;

                // Compute Amount of Directional Changes
                // Formula: f_theta = -A * K * exp(-distance_ij / B - ((n * B * theta) * (n * B * theta)))
                forceAngleAmount = -A * K * Mathf.Exp(-dst.magnitude / B - ((n * B * theta) * (n * B * theta)));

                // Compute Normal Vector of Interaction Direction Oriented to the Left
                interactionDirectionLeftNormal = new Vector2(-interactionDirection.y, interactionDirection.x);

                // Compute Interaction Force
                // Formula: f_ij = f_v * t_ij + f_theta * n_ij
                f_ij += forceVelocityAmount * interactionDirection + forceAngleAmount * interactionDirectionLeftNormal;
            }

            return f_ij;
        }

        // Computes f_iw
        Vector2 WallInteractForce()
        {
            // Repulsion range based on (Moussaid et al., 2009)
            const int a = 3;
            const float b = 1.5f;

            Vector2 nearestPoint;
            Vector2 vector_wi, minVector_wi = Vector2.zero;
            float distanceSquared, minDistanceSquared = Mathf.Infinity, d_w, f_iw;

            foreach (Wall wall in Engine.Instance.GetWalls)
            {
                nearestPoint = wall.GetNearestPoint(position);
                vector_wi = position - nearestPoint;    // Vector from wall to agent i
                distanceSquared = vector_wi.sqrMagnitude;

                // Store Nearest Wall Distance
                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    minVector_wi = vector_wi;
                }
            }

            d_w = Mathf.Sqrt(minDistanceSquared) - radius;    // Distance between wall and agent i

            // Compute Interaction Force
            // Formula: f_iw = a * exp(-d_w / b)
            f_iw = a * Mathf.Exp(-d_w / b);
            minVector_wi.Normalize();

            return f_iw * minVector_wi;
        }

        //public float GetOrientation() { return (Mathf.Atan2(velocity.y, velocity.x) * (180 / Mathf.PI)); }
        //public Vector2 GetAheadVector() { return (velocity + position); }

        public bool DoStep()
        {
            float distSqToGoal = (goal - position).sqrMagnitude;
            if (distSqToGoal <= goalRadius * goalRadius) {
                velocity = Vector2.zero;
                return true;
            }

            // Compute Social Force
            Vector2 acceleration = DrivingForce() + AgentInteractForce() + WallInteractForce();

            // Compute New Velocity
            velocity += acceleration * Engine.timeStep;

            // Truncate Velocity if Exceed Maximum Speed (Magnitude)
            if (velocity.sqrMagnitude > (prefSpeed * prefSpeed))
            {
                velocity.Normalize();
                velocity *= prefSpeed;
            }

            position += velocity * Engine.timeStep;
            MoveInRealWorld();

            return false;
        }

        /// <summary>
        /// Real World
        /// </summary>
        void MoveInRealWorld()
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

            // the anim speed multiplier allows the overall speed of
            // walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            //if (move.magnitude > 0) m_Animator.speed = GetPercentSpeed();
        }

        public void ResetAnimParameters(bool full = false)
        {
            m_Animator.SetFloat("Forward", 0, full? 0 : 0.1f, Engine.timeStep);
            m_Animator.SetFloat("Turn", 0, full? 0 : 0.1f, Engine.timeStep);
            ColorSpeed();
        }
    }
}