/*
 *  Agent.h
 *  
 *  
 *  All rights are retained by the authors and the University of Minnesota.
 *  Please contact sjguy@cs.umn.edu for licensing inquiries.
 *  
 *  Authors: Ioannis Karamouzas, Brian Skinner, and Stephen J. Guy
 *  Contact: ioannis@cs.umn.edu
 */
/*!
 *  @file       Agent.h
 *  @brief      Contains the Agent class.
 */

using OpenSteer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PowerLaw
{
    public class Agent : ProximityDatabaseItem
    {
        const float EPSILON = 0.02f;
        const float goalRadius = 0.5f;
        const float maxAccel = 100f;
        private int id;                                                       // The id of the character. 
        private Vector2 position;                                             // The position of the character. 
        private Vector2 velocity;                                             // The velocity of the character. 
        private float radius;                                                 // The raidus of the character. 
        private Vector2 goal;                                                 // The goal of the character. 
        private Vector2 vPref;                                                // the preferred velocity of the character
        private float prefSpeed;                                              // The preferred speed of the character. 
        private LQProximityDatabase.TokenType proximityToken;                               // interface object for the proximity database
        private List<ProximityDatabaseItem> proximityNeighbors = new List<ProximityDatabaseItem>();     // The proximity neighbors

        // Additional parameters for the approach
        protected float neighborDist;                  // The maximum distance from the agent at which an object will be considered.
        protected Vector2 F;                           // The final force acting on the agent
        protected float k;                             // The scaling constant k of the anticipatory law
        protected float t0;                            // The exponential cutoff term tau_0
        protected float m;                             // The exponent of the power law (m = 2 in our analysis)
        protected float ksi;                           // Relaxation time for the driving force

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

        public Vector2 Position { get { return position; } set { position = value; } }
        public Vector2 Velocity { get { return velocity; } set { velocity = value; } }

        void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            m_Animator = GetComponent<Animator>();
        }

        public void Init(int id, Vector2 position, Vector2 goal)
        {
            this.id = id;

            k = 40f;
            ksi = 0.54f;
            m = 2.0f;
            t0 = 3.0f;
            neighborDist = 80.0f;
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

            // Add to the database
            proximityToken = Engine.Instance.GetSpatialDatabase.AllocateToken(this);

            // Notify proximity database that our position has changed
            proximityToken.UpdateForNewPosition(position);
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

        public bool DoStep()
        {
            vPref = goal - position;
            float distSqToGoal = vPref.sqrMagnitude;

            if (distSqToGoal <= goalRadius * goalRadius) {
                velocity = Vector2.zero;
                return true;
            }

            // compute preferred velocity
            vPref *= prefSpeed / Mathf.Sqrt(distSqToGoal);

            // compute the new velocity of the agent
            ComputeForces();

            F = Global.Clamp(F, maxAccel);
            velocity += F * Engine.timeStep;

            // Limit velocity to prefSpeed of agent
            //if (velocity.sqrMagnitude > (prefSpeed * prefSpeed))
            //{
            //    velocity.Normalize();
            //    velocity *= prefSpeed;
            //}

            position += velocity * Engine.timeStep;

            // notify proximity database that our position has changed
            proximityToken.UpdateForNewPosition(position);

            MoveInRealWorld();

            return false;
        }

        /// <summary>
        /// Computes the forces exerted on the agent at each simulation step
        /// Our simulation model includes the following three forces:
        /// A driving force F_i indicating the preference of the agent i
        /// to walk in a certain direction at a certain speed, as defined in 
        /// [D. Helbing, I. Farkas, and T. Vicsek, Nature 407, 487 (2000)], 
        /// 
        /// F_i = (V_iPref - V_i) / Epsilon 
        ///  
        /// This force can be replaced by a self-propelled force to simulate agents with 
        /// no preferred direction of motion following the approach of[D.Grossman, 
        /// I.S.Aranson, and E.B.Jacob, New J.Phys. 10, 023036(2008).]
        /// The agent-agent interaction force F_ij derived in Eq. (S2)
        /// of the Supplemental material. A similar force F_iO acting 
        /// on agent i as a result of interaction with each static obstacle O 
        /// present in the environment. 
        /// 
        /// In our simulations, we generally assume that 
        /// obstacles are modeled as a collection of line segments. Then, 
        /// 
        /// F_iO = -Delta_r * (K*T^(-2)*e^(-T/T_0))
        /// 
        /// but now T denotes the minimal intersection time between the ray 
        /// x_i + t*v_i, t>0 and the 2D capsule resulting after
        /// sweeping O with the disc of the agent.   
        /// </summary>
        protected void ComputeForces()
        {
            //driving force
            F = (vPref - velocity) / ksi;

            // Compute new neighbors of agent;
            proximityNeighbors.Clear();
            Vector3 center = new Vector3(position.x, 0, position.y);
            proximityToken.FindNeighbors(center, neighborDist, ref proximityNeighbors);

            // compute the anticipatory force from each neighbor
            for (int i = 0; i < proximityNeighbors.Count; ++i)
            {
                Agent other = (Agent)proximityNeighbors[i];
                float distanceSq = (other.position - position).sqrMagnitude;
                float radiusSq = Mathf.Sqrt(other.radius + radius);
                if (this != other && System.Math.Abs(distanceSq - radiusSq) > EPSILON)
                {
                    // if agents are actually colliding use their separation distance 
                    if (distanceSq < radiusSq)
                        radiusSq = Mathf.Sqrt(other.radius + radius - Mathf.Sqrt(distanceSq));

                    Vector2 w = other.position - position;
                    Vector2 v = velocity - other.velocity;
                    float a = Vector2.Dot(v, v);
                    float b = Vector2.Dot(w, v);
                    float c = Vector2.Dot(w, w) - radiusSq;
                    float discr = b * b - a * c;
                    if (discr > 0.0f && (a < -EPSILON || a > EPSILON))
                    {
                        discr = Mathf.Sqrt(discr);
                        float t = (b - discr) / a;
                        if (t > 0)
                            F += -k * Mathf.Exp(-t / t0) * (v - (b * v - a * w) / discr) / (a * Mathf.Pow(t, m)) * (m / t + 1 / t0);
                    }
                }
            }

            //anticipatory forces from static obstacles
            for (int i = 0; i < Engine.Instance.GetObstacles.Count; ++i)
            {
                LineObstacle obstacle = Engine.Instance.GetObstacle(i);
                Vector2 n_w = Global.ClosestPointLineSegment(obstacle.P1, obstacle.P2, position) - position;
                float d_w = n_w.sqrMagnitude;

                // Agent is moving away from obstacle, already colliding or obstacle too far away
                if (Vector2.Dot(velocity, n_w) < 0 || System.Math.Abs(d_w - (Mathf.Sqrt(radius))) < EPSILON || d_w > Mathf.Sqrt(neighborDist))
                    continue;

                // correct the radius, if the agent is already colliding
                float r = d_w < Mathf.Sqrt(radius) ? Mathf.Sqrt(d_w) : radius;
                float a = Vector2.Dot(velocity, velocity);
                bool discCollision = false, segmentCollision = false;
                float t_min = Mathf.Infinity;

                float b = 1, discr = 1;
                float b_temp, discr_temp, c_temp, D_temp;
                Vector2 w_temp, w = Vector2.zero, o1_temp, o2_temp, o_temp, o = Vector2.zero;

                // time-to-collision with disc_1 of the capped rectangle (capsule)
                w_temp = obstacle.P1 - position;
                b_temp = Vector2.Dot(w_temp, velocity);
                c_temp = Vector2.Dot(w_temp, w_temp) - (r * r);
                discr_temp = b_temp * b_temp - a * c_temp;
                if (discr_temp > .0f && (a < -EPSILON || a > EPSILON))
                {
                    discr_temp = Mathf.Sqrt(discr_temp);
                    float t = (b_temp - discr_temp) / a;
                    if (t > 0)
                    {
                        t_min = t;
                        b = b_temp;
                        discr = discr_temp;
                        w = w_temp;
                        discCollision = true;
                    }
                }

                // time-to-collision with disc_2 of the capsule
                w_temp = obstacle.P2 - position;
                b_temp = Vector2.Dot(w_temp, velocity);
                c_temp = Vector2.Dot(w_temp, w_temp) - (r * r);
                discr_temp = b_temp * b_temp - a * c_temp;
                if (discr_temp > 0 && (a < -EPSILON || a > EPSILON))
                {
                    discr_temp = Mathf.Sqrt(discr_temp);
                    float t = (b_temp - discr_temp) / a;
                    if (t > 0 && t < t_min)
                    {
                        t_min = t;
                        b = b_temp;
                        discr = discr_temp;
                        w = w_temp;
                        discCollision = true;
                    }
                }

                // time-to-collision with segment_1 of the capsule
                o1_temp = obstacle.P1 + r * obstacle.Normal;
                o2_temp = obstacle.P2 + r * obstacle.Normal;
                o_temp = o2_temp - o1_temp;

                D_temp = Global.Det(velocity, o_temp);
                if (System.Math.Abs(D_temp) > EPSILON)
                {
                    float inverseDet = 1.0f / D_temp;
                    float t = Global.Det(o_temp, position - o1_temp) * inverseDet;
                    float s = Global.Det(velocity, position - o1_temp) * inverseDet;
                    if (t > 0 && s >= 0 && s <= 1 && t < t_min)
                    {
                        t_min = t;
                        o = o_temp;
                        discCollision = false;
                        segmentCollision = true;
                    }
                }

                // time-to-collision with segment_2 of the capsule
                o1_temp = obstacle.P1 - r * obstacle.Normal;
                o2_temp = obstacle.P2 - r * obstacle.Normal;
                o_temp = o2_temp - o1_temp;

                D_temp = Global.Det(velocity, o_temp);
                if (System.Math.Abs(D_temp) > EPSILON)
                {
                    float inverseDet = 1.0f / D_temp;
                    float t = Global.Det(o_temp, position - o1_temp) * inverseDet;
                    float s = Global.Det(velocity, position - o1_temp) * inverseDet;
                    if (t > 0 && s >= 0 && s <= 1 && t < t_min)
                    {
                        t_min = t;
                        o = o_temp;
                        discCollision = false;
                        segmentCollision = true;
                    }
                }

                if (discCollision)
                {
                    F += -k * Mathf.Exp(-t_min / t0) * (velocity - (b * velocity - a * w) / discr) / (a * Mathf.Pow(t_min, m)) * (m / t_min + 1 / t0);
                }
                else if (segmentCollision)
                {
                    F += k * Mathf.Exp(-t_min / t0) / (Mathf.Pow(t_min, m) * Global.Det(velocity, o)) * (m / t_min + 1 / t0) * new Vector2(-o.y, o.x);
                }
            }
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
            m_Animator.SetFloat("Forward", 0, full ? 0 : 0.1f, Engine.timeStep);
            m_Animator.SetFloat("Turn", 0, full ? 0 : 0.1f, Engine.timeStep);
            ColorSpeed();
        }
    }
}
