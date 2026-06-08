using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class KineticPlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Grounded,
        Air,
        WallRunning,
        Dashing
    }

    [Header("State Tracking")]
    [SerializeField] private PlayerState currentState;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("References")]
    [SerializeField, Tooltip("Camera used for dynamic FOV")]
    private Camera playerCamera;
    [SerializeField, Tooltip("Transform to define forward direction")]
    private Transform orientation;

    [Header("Ground Movement")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float runSpeed = 15f;
    [SerializeField] private float accelerationTime = 1.5f;
    [SerializeField, Tooltip("Curve from 0 to 1 for walking to running transition")] 
    private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float groundDrag = 5f;
    
    [Header("Air Movement (Fix)")]
    [SerializeField, Tooltip("Strict max speed to prevent flying/momentum bleeding")]
    private float maxAirSpeed = 12f;
    [SerializeField, Tooltip("Force applied when moving in the air")]
    private float airAcceleration = 10f;
    [SerializeField] private float airDrag = 1f;

    [Header("Jumping & Double Jump")]
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float playerHeight = 2f;

    [Header("Stamina System")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRechargeRate = 20f;
    [SerializeField] private float dashStaminaCost = 25f;
    [SerializeField] private float wallRunStaminaCost = 30f; // per second

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 40f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Wall Running")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private float wallDistance = 0.6f;
    [SerializeField] private float wallJumpUpForce = 7f;
    [SerializeField] private float wallJumpForwardForce = 10f;
    [SerializeField, Tooltip("Force applied strictly OUTWARD from the wall to prevent infinite scaling on a flat surface")]
    private float wallJumpSideForce = 12f;
    [SerializeField, Tooltip("Cooldown preventing re-attaching to a wall immediately after jumping off")]
    private float wallJumpCooldown = 0.25f;

    [Header("Camera Dynamics")]
    [SerializeField] private float minFOV = 60f;
    [SerializeField] private float maxFOV = 95f;
    [SerializeField] private float fovTransitionSpeed = 5f;

    [Header("VFX")]
    [SerializeField] private KineticVFXManager vfxManager;

    // State Variables
    private Rigidbody rb;
    private Vector2 inputDirection;
    private Vector3 moveDirection;

    private float currentStamina;
    private float timeMoving;
    
    private bool isGrounded;
    private bool canDoubleJump;
    
    private float dashTimeLeft;
    private Vector3 dashDirection;

    private bool isWallRight;
    private bool isWallLeft;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;

    private float wallJumpCooldownTimer;

    // --- Public Getters for UI and Camera ---
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public PlayerState CurrentState => currentState;
    public float CurrentSpeed => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
    public Vector3 Velocity => rb.linearVelocity;
    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        currentStamina = maxStamina;
        currentState = PlayerState.Air;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();

        if (jumpAction != null) 
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJumpPerformed;
        }

        if (dashAction != null) 
        {
            dashAction.action.Enable();
            dashAction.action.performed += OnDashPerformed;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();

        if (jumpAction != null) 
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJumpPerformed;
        }

        if (dashAction != null) 
        {
            dashAction.action.Disable();
            dashAction.action.performed -= OnDashPerformed;
        }
    }

    private void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        ReadInput();
        DetermineState();
        UpdateStamina();
        // UpdateCameraFOV(); // Disabled for Third Person transition
        
        // Auto-sync orientation with camera yaw for true 3rd person movement
        if (orientation != null && playerCamera != null)
        {
            orientation.rotation = Quaternion.Euler(0f, playerCamera.transform.eulerAngles.y, 0f);
        }
        
        // Handle Drag based strictly on state
        if (currentState == PlayerState.Grounded)
            rb.linearDamping = groundDrag;
        else if (currentState == PlayerState.Air)
            rb.linearDamping = airDrag;
        else
            rb.linearDamping = 0; // Dash and WallRun ignore drag
            
        if (wallJumpCooldownTimer > 0)
            wallJumpCooldownTimer -= Time.deltaTime;

        SpeedControl();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case PlayerState.Dashing:
                PerformDash();
                break;
            case PlayerState.WallRunning:
                PerformWallRun();
                break;
            case PlayerState.Grounded:
                MoveGrounded();
                break;
            case PlayerState.Air:
                MoveAir();
                break;
        }
    }

    private void ReadInput()
    {
        if (moveAction != null)
            inputDirection = moveAction.action.ReadValue<Vector2>();
    }

    private void DetermineState()
    {
        // Dash state completely locks out normal state transitions until it completes
        if (currentState == PlayerState.Dashing) 
            return;

        if (orientation != null)
        {
            isWallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance, whatIsWall);
            isWallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance, whatIsWall);

            // Cannot start wall run if the cooldown is active
            if ((isWallRight || isWallLeft) && !isGrounded && inputDirection.y > 0 && wallJumpCooldownTimer <= 0)
            {
                if (currentState != PlayerState.WallRunning && currentStamina > 0)
                {
                    StartWallRun();
                    return;
                }
                else if (currentState == PlayerState.WallRunning)
                {
                    return; // Remain in WallRun state
                }
            }
            else if (currentState == PlayerState.WallRunning)
            {
                StopWallRun();
            }
        }

        if (isGrounded)
        {
            currentState = PlayerState.Grounded;
            canDoubleJump = true; // Refresh double jump on ground
        }
        else
        {
            currentState = PlayerState.Air;
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (currentState == PlayerState.WallRunning)
        {
            WallJump();
        }
        else if (currentState == PlayerState.Grounded)
        {
            Jump();
        }
        else if (canDoubleJump)
        {
            DoubleJump();
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (currentState != PlayerState.Dashing && currentStamina >= dashStaminaCost)
        {
            StartDash();
        }
    }

    private void CalculateMoveDirection()
    {
        if (orientation == null) return;
        moveDirection = orientation.forward * inputDirection.y + orientation.right * inputDirection.x;
        moveDirection.y = 0f;
        moveDirection.Normalize();
    }

    private void MoveGrounded()
    {
        CalculateMoveDirection();

        if (moveDirection.magnitude > 0)
            timeMoving += Time.fixedDeltaTime;
        else
            timeMoving -= Time.fixedDeltaTime * 2f; // Decelerate 

        timeMoving = Mathf.Clamp(timeMoving, 0f, accelerationTime);

        float t = timeMoving / accelerationTime;
        float currentTargetSpeed = Mathf.Lerp(walkSpeed, runSpeed, accelerationCurve.Evaluate(t));

        rb.AddForce(moveDirection * currentTargetSpeed * 10f, ForceMode.Force);
    }

    private void MoveAir()
    {
        CalculateMoveDirection();

        // Standard air control applies a set acceleration force, no dash multiplier here
        rb.AddForce(moveDirection * airAcceleration * 10f, ForceMode.Force);
    }
    
    private void SpeedControl()
    {
        // Dash and WallRun manually manage their rigidbodies, ignore generic speed control
        if (currentState == PlayerState.Dashing || currentState == PlayerState.WallRunning) 
            return;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentMaxSpeed = 0f;

        if (currentState == PlayerState.Grounded)
        {
            float t = timeMoving / accelerationTime;
            currentMaxSpeed = Mathf.Lerp(walkSpeed, runSpeed, accelerationCurve.Evaluate(t));
        }
        else if (currentState == PlayerState.Air)
        {
            // Strict Air Movement Max Speed
            currentMaxSpeed = maxAirSpeed;
        }

        // Clamp the horizontal velocity to strictly decouple dash speeds from standard movement
        if (flatVel.magnitude > currentMaxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentMaxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        if (vfxManager != null) vfxManager.PlayBurst(transform.up, vfxManager.JumpBurstCount);
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float jumpVel = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
        rb.AddForce(transform.up * jumpVel, ForceMode.VelocityChange);
    }

    private void DoubleJump()
    {
        if (vfxManager != null) vfxManager.PlayBurst(transform.up, vfxManager.JumpBurstCount);
        
        canDoubleJump = false;
        
        // Preserve X and Z velocity (could be dash velocity or air velocity), apply exact Y jump
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float jumpVel = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
        rb.AddForce(transform.up * jumpVel, ForceMode.VelocityChange);
    }

    private void StartDash()
    {
        currentStamina -= dashStaminaCost;
        currentState = PlayerState.Dashing;
        dashTimeLeft = dashDuration;
        rb.useGravity = false;
        
        CalculateMoveDirection();

        // Dash in movement direction, or forward if no input
        if (moveDirection == Vector3.zero && orientation != null) 
            dashDirection = orientation.forward;
        else 
            dashDirection = moveDirection;

        if (vfxManager != null) vfxManager.PlayBurst(dashDirection, vfxManager.DashBurstCount);
    }

    private void PerformDash()
    {
        dashTimeLeft -= Time.fixedDeltaTime;
        
        // Lock X and Z to dash properties, but allow Y velocity to exist if they Double Jumped during dash
        rb.linearVelocity = new Vector3(
            dashDirection.x * dashSpeed, 
            rb.linearVelocity.y, 
            dashDirection.z * dashSpeed
        );

        if (dashTimeLeft <= 0f)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        rb.useGravity = true;
        currentState = PlayerState.Air;
        
        // Once dash duration is over, immediately drop velocity to normal air max speed.
        // This ensures air movement properties never inherit dash velocity.
        rb.linearVelocity = new Vector3(
            dashDirection.x * maxAirSpeed, 
            rb.linearVelocity.y, 
            dashDirection.z * maxAirSpeed
        );
    }

    private void UpdateStamina()
    {
        // Recharge if walking or standing still (grounded and not using advanced moves)
        if (currentState == PlayerState.Grounded)
        {
            currentStamina += staminaRechargeRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    private void StartWallRun()
    {
        currentState = PlayerState.WallRunning;
        rb.useGravity = false;
        canDoubleJump = true; 

        if (vfxManager != null) 
        {
            Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

            if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
                wallForward = -wallForward;

            vfxManager.StartWallRunVFX(wallForward);
        }
    }

    private void PerformWallRun()
    {
        currentStamina -= wallRunStaminaCost * Time.fixedDeltaTime;

        if (currentStamina <= 0)
        {
            StopWallRun();
            return; // Gravity re-enables naturally in state transition
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallJumpForwardForce * 10f, ForceMode.Force);
        rb.AddForce(-wallNormal * 100f, ForceMode.Force);
    }

    private void StopWallRun()
    {
        rb.useGravity = true;
        currentState = PlayerState.Air;
        if (vfxManager != null) vfxManager.StopWallRunVFX();
    }

    private void WallJump()
    {
        Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
        
        // Apply upward and OUTWARD force to push player away from the wall
        Vector3 forceToApply = orientation.forward * wallJumpForwardForce + wallNormal * wallJumpSideForce + transform.up * wallJumpUpForce;

        if (vfxManager != null) vfxManager.PlayBurst(forceToApply.normalized, vfxManager.JumpBurstCount);
        
        // Explicitly refresh double jump, ensuring infinite wall jumps
        canDoubleJump = true; 

        // Apply force
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
        
        // Start cooldown to prevent instant re-attachment
        wallJumpCooldownTimer = wallJumpCooldown;

        StopWallRun();
    }

    private void UpdateCameraFOV()
    {
        if (playerCamera == null) return;

        float targetFOV = minFOV;
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentSpeed = flatVel.magnitude;

        if (currentState == PlayerState.Grounded && currentSpeed > walkSpeed)
        {
            float speedRatio = Mathf.Clamp01((currentSpeed - walkSpeed) / (runSpeed - walkSpeed));
            targetFOV = Mathf.Lerp(minFOV, maxFOV, speedRatio);
        }

        if (currentState == PlayerState.Dashing) 
            targetFOV = maxFOV + 15f;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }
}
