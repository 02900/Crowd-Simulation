using UnityEngine;

public class AnimationController : MonoBehaviour {

    const float EPSILON = 0.02f;
    const float m_MovingTurnSpeed = 360;
    const float m_StationaryTurnSpeed = 180;

    Animator m_Animator;
    float m_TurnAmount;
    float m_ForwardAmount;
    Vector3 m_GroundNormal = Vector3.zero;

    MaterialPropertyBlock _propBlock;
    [SerializeField] Color Color1 = Color.cyan, Color2 = Color.blue;
    [SerializeField] Renderer _renderer = null;

    void Awake()
    {
        m_Animator = GetComponent<Animator>();
        _propBlock = new MaterialPropertyBlock();
    }

    float GetPercentSpeed(Vector3 velocity)
    {
        if (System.Math.Abs(velocity.magnitude) < EPSILON) return 0.01f;
        return velocity.magnitude / 3;
    }

    public void Move(Vector3 velocity, float timeStep)
    {
        Vector3 dir = velocity.normalized;
        // convert the world relative moveInput vector into a local-relative
        // turn amount and forward amount required to head in the desired
        // direction.
        dir = transform.InverseTransformDirection(dir);
        dir = Vector3.ProjectOnPlane(dir, m_GroundNormal);

        m_TurnAmount = Mathf.Atan2(dir.x, dir.z);
        m_ForwardAmount = dir.z * GetPercentSpeed(velocity);

        LookWhereImGoing(timeStep);
        UpdateAnimator(dir, timeStep);
    }

    private void LookWhereImGoing(float timeStep)
    {
        float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
        transform.Rotate(0, m_TurnAmount * turnSpeed * timeStep, 0);
    }

    private void UpdateAnimator(Vector3 move, float timeStep)
    {
        m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, timeStep);
        m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, timeStep);
    }

    public void ResetAnimParameters(float timeStep, bool full = false)
    {
        m_Animator.SetFloat("Forward", 0, full ? 0 : 0.1f, timeStep);
        m_Animator.SetFloat("Turn", 0, full ? 0 : 0.1f, timeStep);
        ColorSpeed(Vector2.zero);
    }

    public void ColorSpeed(Vector3 velocity)
    {
        // Get the current value of the material properties in the renderer.
        _renderer.GetPropertyBlock(_propBlock);
        // Assign our new value.
        _propBlock.SetColor("_Color", Color.Lerp(Color1, Color2, GetPercentSpeed(velocity)));
        // Apply the edited values to the renderer.
        _renderer.SetPropertyBlock(_propBlock);
    }
}