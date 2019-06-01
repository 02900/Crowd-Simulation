using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace RVO2
{

    public class RVO2Agent : MonoBehaviour
    {
        [HideInInspector] public int id;
        const float goalRadius = 0.5f;
        const float EPSILON = 0.02f;

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

            if (System.Math.Abs(fixedPrefSpeed) > EPSILON) RVO2Manager.Instance.SetCustomPrefVel(id, fixedPrefSpeed);
            #endregion
        }

        /// <summary>
        /// Real World
        /// </summary>
        public void MoveInRealWorld(Vector2 position, Vector2 velocity)
        {
            ColorSpeed(velocity);

            transform.localPosition = new Vector3(position.x, 0, position.y);
            float distanceToTarget = Vector3.Distance(goal3d, transform.localPosition);

            Move(velocity);

            // Agent is within one radius of its goal then go to next waypoint
            if (distanceToTarget < goalRadius * 2)
            {
                if (goals.Count < 1) return;
                goal3d = (Vector3)goals.Dequeue();
                //goal = new Vector2(goal3d.x, goal3d.z);
            }
        }

        public void RecMove(Vector2 position, Vector2 velocity)
        {
            ColorSpeed(velocity);
            transform.localPosition = new Vector3(position.x, 0, position.y);
            Move(velocity);
        }

        void ColorSpeed(Vector2 velocity)
        {
            // Get the current value of the material properties in the renderer.
            _renderer.GetPropertyBlock(_propBlock);
            // Assign our new value.
            _propBlock.SetColor("_Color", Color.Lerp(Color1, Color2, GetPercentSpeed(velocity)));
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

        float GetPercentSpeed(Vector2 velocity)
        {
            if (System.Math.Abs(velocity.magnitude) < EPSILON) return 0.01f;
            return velocity.magnitude / 1.5f;
        }

        void Move(Vector2 velocity)
        {
            Vector3 dir = ExtensionMethods.Vector2ToVector3(velocity.normalized);

            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            dir = transform.InverseTransformDirection(dir);
            dir = Vector3.ProjectOnPlane(dir, m_GroundNormal);

            m_TurnAmount = Mathf.Atan2(dir.x, dir.z);
            m_ForwardAmount = dir.z * GetPercentSpeed(velocity);

            LookWhereImGoing();
            UpdateAnimator();
        }

        void LookWhereImGoing()
        {
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount * turnSpeed * RVO2Manager.timeStep, 0);
        }

        void UpdateAnimator()
        {
            m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, RVO2Manager.timeStep);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, RVO2Manager.timeStep);
        }

        public void ResetAnimParameters(Vector2 velocity, float prefSpeed, bool full = false)
        {
            m_Animator.SetFloat("Forward", 0, full ? 0 : 0.1f, RVO2Manager.timeStep);
            m_Animator.SetFloat("Turn", 0, full ? 0 : 0.1f, RVO2Manager.timeStep);
            ColorSpeed(velocity);
        }

        public void PauseAnimParameters()
        {
            m_Animator.SetFloat("Forward", 0, 0.1f, RVO2Manager.timeStep);
            m_Animator.SetFloat("Turn", 0, 0.1f, RVO2Manager.timeStep);
            ColorSpeed(Vector2.zero);
        }
    }
}