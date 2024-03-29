﻿using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]

abstract public class Character : MonoBehaviour
{
    public enum CharacterState
    {
        none,
        idle_OutCombat,
        idle_InCombat,
        run,
        jump_up,
        jump_air,
        jump_down,
        aim,
        shoot,
        attack,
        adjustPosition,
        hit,
        dodge,
        roll,
        aimMove
    }

    public AudioSource charAudio;
    public AudioSource charCombatAudio;
    [SerializeField] protected AudioClip footsteps1;
    [SerializeField] protected AudioClip footsteps2;
    [SerializeField] protected AudioClip footsteps3;
    [SerializeField] protected AudioClip footsteps4;
    [SerializeField] protected AudioClip jumpFX;
    [SerializeField] protected AudioClip landFX;

    [SerializeField]
    protected float m_JumpPower;
    [Range(1f, 20f)]
    [SerializeField]
    protected float m_GravityMultiplier = 2f;
    [SerializeField]
    protected float m_BaseSpeedMultiplier;
    
    [SerializeField]
    protected float m_SlopeSpeedMultiplier = 0.18f;
    [SerializeField]
    protected float m_GroundCheckDistance;
    [SerializeField]
    protected float m_GroundCheckRadius;


    protected float m_MoveSpeedMultiplier;
    protected float m_DashSpeedMultiplier;
    protected float m_RunSpeedMultiplier;
    protected float m_CrouchSpeedMultiplier;


    public GameObject charBody;
    public new GameObject camera;

    protected Humanoid humanoid;
    public CombatManager m_combat;
    protected CapstoneAnimation animator;

    protected Rigidbody m_Rigidbody;

    public bool m_IsGrounded;
    protected bool m_Crouching;
    protected bool m_jump;
    protected bool m_dashing;
    protected bool m_moving;
    protected bool frozen = false;
    private bool hasJumped = false;

    protected float turnMod;
    protected float m_OrigGroundCheckDistance;

    protected int animationParameter;
    public int AnimationParameter
    { set { animationParameter = value; } }

    protected Vector3 m_GroundNormal;
    protected Vector3 move;

    protected Quaternion charBodyRotation;
    protected Quaternion m_Rotation;
    protected Quaternion camRotation;

    protected bool hasEffect;
    public bool HasEffect
    {
        get { return hasEffect; }
        set { hasEffect = value; }
    }


    [SerializeField]
    protected CharacterState currentState;
    protected CharacterState lastState;

    protected float stateTimer;
    public float StateTimer
    {
        get { return stateTimer; }
        set { stateTimer = value; }
    }

    public CharacterState CurrentState
    {
        get { return currentState; }
        set { currentState = value; }
    }
    public CharacterState LastState
    {
        get { return lastState; }
        set { lastState = value; }
    }

    // Use this for initialization
    virtual protected void Start ()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        m_OrigGroundCheckDistance = m_GroundCheckDistance;
        charBodyRotation = charBody.transform.rotation;
        m_Rotation = m_Rigidbody.transform.rotation;
        turnMod = 90.0f / 200.0f;

        animator = GetComponentInChildren<CapstoneAnimation>();
        m_combat = GetComponentInChildren<CombatManager>();
        humanoid = GetComponentInChildren<Humanoid>();

        m_JumpPower = humanoid.JumpPower;
        m_BaseSpeedMultiplier = humanoid.SpeedMove;
        m_DashSpeedMultiplier = humanoid.SpeedDash;
        m_RunSpeedMultiplier = humanoid.SpeedRun;
        m_CrouchSpeedMultiplier = humanoid.SpeedCrouch;

        stateTimer = 0;
        m_MoveSpeedMultiplier = m_BaseSpeedMultiplier;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    #region Movement
    /// <summary>
    /// Move the player character.
    /// </summary>
    /// <param name="vert">forward/backward motion</param>
    /// <param name="hori">side to side motion</param>
    /// <param name="charRotation">rotation of player</param>
    /// <param name="crouch">is player crouched</param>
    /// <param name="jump">should player jump</param>
    /// <param name="running">is the player running</param>
    /// <param name="dash">is the player dashing</param>
    abstract public void Move(float vert, float hori, Quaternion camRot, bool crouch, bool jump, bool running, bool dash, bool aiming);

    /// <summary>
    /// Move AI Character
    /// </summary>
    /// <param name="_isMoving">Is the character moving?</param>
    virtual public void Move(bool _isMoving)
    {
        m_combat.IsMoving = _isMoving;
    }

    /// <summary>
    /// freeze the character body rotation
    /// </summary>
    protected void freezeChar()
    {
        charBody.transform.rotation = charBodyRotation;
        frozen = true;
    }

    /// <summary>
    /// unfreeze the character body rotation
    /// </summary>
    protected void unFreezeChar()
    {
        if (frozen)
        {
            charBody.transform.rotation = m_Rotation;
            frozen = false;
        }
    }

    /// <summary>
    /// Handle movement on the ground
    /// </summary>
    /// <param name="crouch">Is the character crouched</param>
    /// <param name="jump">Is the character jumping</param>
    protected void HandleGroundedMovement(bool crouch, bool jump)
    {
        // check whether conditions are right to allow a jump:
        if (jump && !crouch && m_IsGrounded)
        {
            charAudio.Stop();
            charAudio.PlayOneShot(jumpFX);
            // jump!
            m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
            m_IsGrounded = false;

            //jump state
            m_combat.IsJumping = true;
            m_jump = true;
            stateTimer = 0;
        }
    }//end ground movement

    /// <summary>
    /// handle airborne movement
    /// </summary>
    protected void HandleAirborneMovement()
    {
        // apply extra gravity from multiplier:
        Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
        m_Rigidbody.AddForce(extraGravityForce);

        m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
    }//end airborne movement

    /// <summary>
    /// check to see if player is on the ground and its status
    /// </summary>
    protected void CheckGroundStatus()
    {
        RaycastHit hitInfo;

        // Check to see if player is hiting a curb or collidier directly on and would have trouble navigating over it.
        if (Physics.Raycast(transform.position + (Vector3.up * 0.08f), new Vector3(0, 0, 1f), 0.3f) ||
            Physics.Raycast(transform.position + (Vector3.up * 0.08f), new Vector3(0, 0, -1f), 0.3f) ||
            Physics.Raycast(transform.position + (Vector3.up * 0.08f), new Vector3(1, 0, 0f), 0.3f) ||
            Physics.Raycast(transform.position + (Vector3.up * 0.08f), new Vector3(-1, 0, 0f), 0.3f))
        {
            // Bump character up a bit to overcome curb slopes
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.08f, transform.position.z);
        }

        if (Physics.SphereCast(transform.position + (Vector3.up * 0.1f), m_GroundCheckRadius, Vector3.down, out hitInfo, m_GroundCheckDistance))
        //if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
        {
            //Debug.Log(hitInfo.normal.x + "  " + hitInfo.normal.y + "  " + hitInfo.normal.z);
            //Debug.DrawLine(transform.position + (Vector3.up * 0.1f), hitInfo.point);
            if (hitInfo.normal.y < 0.9f)
            {
                m_MoveSpeedMultiplier = m_SlopeSpeedMultiplier;
            }
            m_GroundNormal = hitInfo.normal;

            if(!m_IsGrounded && hasJumped)
            {
                charAudio.Stop();
                charAudio.PlayOneShot(landFX);
                hasJumped = false;
            }
            m_IsGrounded = true;
        }
        else
        {
            m_IsGrounded = false;
            m_GroundNormal = Vector3.up;
        }
    }//end CheckGroundStatus
    #endregion

    /// <summary>
    /// 
    /// </summary>
    protected void UpdateState()
    {
        animationParameter = 0;

        if (stateTimer >= 0)
        {
            stateTimer += Time.deltaTime;
        }

        lastState = currentState;


        if (!m_combat.UpdateState(stateTimer))
        {
            if (m_jump)
            {
                if (stateTimer < m_combat.JumpUpTime)
                {
                    hasJumped = true;
                    currentState = CharacterState.jump_up;
                }
                else if (stateTimer >= m_combat.JumpUpTime && stateTimer < m_combat.JumpUpTime + m_combat.JumpAirTime)
                {
                    currentState = CharacterState.jump_air;
                }
                else if (stateTimer >= m_combat.JumpUpTime + m_combat.JumpAirTime && stateTimer < m_combat.JumpUpTime + m_combat.JumpAirTime + m_combat.JumpDownTime)
                {
                    currentState = CharacterState.jump_down;
                }
                else
                {
                    stateTimer = -1;
                    m_jump = false;
                    m_combat.IsJumping = false;
                }

            }
            else if (m_combat.IsDodging)
            {
                if (stateTimer < m_combat.DodgeTime)
                {
                    currentState = CharacterState.dodge;
                    animationParameter = m_combat.DodgeDirection;
                }
                else
                {
                    stateTimer = -1;
                    m_combat.IsDodging = false;
                }
            }
            else if (m_combat.IsRolling)
            {
                if (stateTimer < m_combat.RollTime)
                {
                    currentState = CharacterState.roll;
                    ForceMove(m_combat.RollSpeed, 1);
                }
                else
                {
                    stateTimer = -1;
                    m_combat.IsRolling = false;
                }
            }
            else if (m_combat.IsAttacking)
            {
                if (stateTimer < m_combat.CurrentAttackTime)
                {
                    currentState = CharacterState.attack;
                    animationParameter = (int)m_combat.CurrentCombat;
                    if (stateTimer >= m_combat.CurrentEffectTime && !hasEffect)
                    {
                        hasEffect = true;
                        m_combat.Effect();
                    }
                    if (m_combat.ResetAttack)
                    {
                        currentState = CharacterState.none;
                        m_combat.ResetAttack = false;
                    }
                }
                else
                {
                    stateTimer = -1;
                    m_combat.IsAttacking = false;
                    hasEffect = false;
                    m_combat.ComboTimer = 0;
                }

            }
            else if (m_combat.IsAdjusting)
            {
                if (m_combat.CheckTarget())
                {
                    m_combat.IsAdjusting = false;
                    m_combat.Attack();
                }
                else
                {
                    //look at target
                    charBody.transform.forward = m_combat.CurrentTarget.transform.position - transform.position;
                    currentState = CharacterState.adjustPosition;
                    ForceMove(m_combat.AdjustSpeed, 1);
                }
            }
            else if (m_combat.IsAimming && m_combat.moveDir > -1)
            {
                animationParameter = m_combat.moveDir;
                currentState = CharacterState.aimMove;
            }
            else
            {

                if (m_moving)
                {
                    currentState = CharacterState.run;
                    if (m_dashing)
                    {
                        animationParameter = 1;
                    }
                }
                else
                {
                    if (m_combat.InCombat)
                    {
                        currentState = CharacterState.idle_InCombat;
                    }
                    else
                    {
                        currentState = CharacterState.idle_OutCombat;
                    }

                }
            }
        }


        if (lastState != currentState)
        {
            animator.Play(currentState, animationParameter);
        }


    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="speed"></param>
    /// <param name="direction"></param>
    public void ForceMove(float speed, int direction)
    {
        if (direction == 0)
        {
            transform.position -= charBody.transform.forward * speed * Time.deltaTime;
        }
        else if (direction == 1)
        {
            transform.position += charBody.transform.forward * speed * Time.deltaTime;
        }
        else if (direction == 2)
        {
            transform.position += charBody.transform.right * speed * Time.deltaTime;
        }
        else if (direction == 3)
        {
            transform.position -= charBody.transform.right * speed * Time.deltaTime;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="speed"></param>
    /// <param name="direction"></param>
    public void ForceMove(float speed, Vector3 direction)
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    /*
    void ScaleCapsuleForCrouching(bool crouch)
		{
			if (m_IsGrounded && crouch)
			{
				if (m_Crouching) return;
				m_Capsule.height = m_Capsule.height / 2f;
				m_Capsule.center = m_Capsule.center / 2f;
				m_Crouching = true;
			}
			else
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength))
				{
					m_Crouching = true;
					return;
				}
				m_Capsule.height = m_CapsuleHeight;
				m_Capsule.center = m_CapsuleCenter;
				m_Crouching = false;
			}
		}

		void PreventStandingInLowHeadroom()
		{
			// prevent standing up in crouch-only zones
			if (!m_Crouching)
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength))
				{
					m_Crouching = true;
				}
			}
		}
     */

}// End Character
