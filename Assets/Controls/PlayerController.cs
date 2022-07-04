using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    //Initialise all player action necessities
    private PlayerInputActions playerInputActions;
    public InputAction move;
    public InputAction jump;
    public InputAction crouch;

    public Transform orientation;
    public Rigidbody rb;

    Vector3 moveDirection;

    [Header("Overall Speed")]
    public float maxSpeed;

    [Header("Movement")]
    public float moveSpeed;
    public float maxMoveSpeed;
    public float groundDrag;

    public float jumpForce;
    private float lastJumpTime = 0f;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask isGround;
    public bool grounded;

    public float jumpBufferTime;
    private float jumpbufferCounter;
    private float lastGroundedTime;

    [Header("Crouching")]
    public float crouchModifier;
    public float crouchYscale;
    public float maxCrouchSpeed;
    private float startYscale;
    private bool isCrouching;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    public float slopeMultiplier;
    private RaycastHit slopeHit;

    [Header("Sliding")]
    public float minSlideSpeed;
    public float slideForce;
    public float maxSlideSpeed;
    private bool isSliding;

    [Header("Air movement")]
    public float airMultiplier;
    public float fastFallForce;
    public float maxAirSpeed;

    [Header("Wallrunning")]
    //Base Wall movement
    public LayerMask isWall;
    public float wallRunForce;
    public float maxWallrunSpeed;
    public float maxWallRunTime;
    public bool isWallRunning;
    private float wallRunTimer;

    //Wall Dettection
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    public bool wallLeft;
    public bool wallRight;



    [Header("States")]
    public MovementState state;

    public enum MovementState
    {
        walking,
        crouching,
        sliding,
        wallrunning,
        air,
    }


    [Header("UI Handling")]
    public TextMeshProUGUI speedText;



    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
    }

    private void Start()
    {
        startYscale = transform.localScale.y;
    }

    private void OnEnable()
    {
        move = playerInputActions.Player.Move;
        move.Enable();

        jump = playerInputActions.Player.Jump;
        jump.Enable();
        jump.performed += Jump;
        jump.performed += StartWallRun;
        jump.canceled += StopWallRun;

        crouch = playerInputActions.Player.Crouch;
        crouch.Enable();
        crouch.performed += Crouch;
        crouch.performed += StartSlide;
        crouch.canceled += StopCrouch;
        crouch.canceled += StopSlide;
    }

    private void OnDisable()
    {
        move.Disable();
        jump.Disable();
        crouch.Disable();
    }


    private void Update()
    {
        //Non-physics stuff here

        //Ground handling
        CheckForWall();
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, isGround);

        if (grounded)
        {
            rb.drag = groundDrag;
            lastGroundedTime = Time.time;
        }
        else
        {
            rb.drag = 0;
        }

        jumpbufferCounter -= Time.deltaTime;

        if (isSliding)
        {

        }

        
    }


    private void FixedUpdate()
    {
        //Use physics based stuff here
        MovePlayer();
        StateHandler();

        if (rb.velocity.magnitude < 0.1f)
        {
            rb.velocity = Vector3.zero;
        }
        speedText.text = "Current Speed: " + Mathf.Round(rb.velocity.magnitude).ToString();

        if (isSliding)
            Slide();

        if (isWallRunning)
        {
            WallRun();
        }
    }


    private void StateHandler()
    {

        //Mode - Wallrunning
        if (isWallRunning)
        {
            state = MovementState.wallrunning;
            maxSpeed = maxWallrunSpeed;
        }
        //Mode - Air
        else if (!grounded)
        {
            state = MovementState.air;
            maxSpeed = maxAirSpeed;
        }
        
        //Mode - Crouching and sliding
        else if (grounded && isCrouching)
        {
            if (isSliding)
            {
                state = MovementState.sliding;
                maxSpeed = maxSlideSpeed;
            }
            else
            {
                state = MovementState.crouching;
                maxSpeed = maxCrouchSpeed;
            }
        }
        
        //Mode - Walking
        else
        {
            state = MovementState.walking;
            maxSpeed = maxMoveSpeed;
        }
    }

    //Basic movement group
    private void MovePlayer()
    {
        moveDirection = orientation.forward * move.ReadValue<Vector2>().y + orientation.right * move.ReadValue<Vector2>().x;
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);
        }
        
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier, ForceMode.Force);
            
            //fast falling
            if (rb.velocity.y < 0)
            {
                rb.AddForce(Vector3.down * fastFallForce, ForceMode.Force);
            }
        }


        //Slope handling
        if (OnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * slopeMultiplier, ForceMode.Force);

            if (rb.velocity.y < 0)
            {
                rb.AddForce(Vector3.down * 40f, ForceMode.Force);
            }
        }

        SpeedControl();
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > maxSpeed)
        {
            
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);

            /*if (rb.velocity.y < 0)
            {
                Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                Vector3 limitedVel = flatVel.normalized * maxSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            } 
            else
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }*/


        }
    }


    //Jumping group
    private void Jump(InputAction.CallbackContext context)
    {
        jumpbufferCounter = jumpBufferTime;

        if (Time.time - lastGroundedTime <= jumpBufferTime && jumpbufferCounter > 0)
        {
            //reset y velocity for consistent jumps, this might need to be removed for momentum systems though
            //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.y);

            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            jumpbufferCounter = 0f;
        }        
    }

    private void Crouch(InputAction.CallbackContext context)
    {
        transform.localScale = new Vector3(transform.localScale.x, crouchYscale, transform.localScale.z);
        rb.position = new Vector3(rb.position.x, rb.position.y - 0.5f, rb.position.z);
        moveSpeed *= crouchModifier;
        isCrouching = true;


    }

    private void StopCrouch(InputAction.CallbackContext context)
    {
        //Bug is introduced putting elongating if a ceiling exists
        transform.localScale = new Vector3(transform.localScale.x, startYscale, transform.localScale.z);
        moveSpeed /= crouchModifier;
        isCrouching = false;
    }

    private void StartSlide(InputAction.CallbackContext context)
    {
        if ((rb.velocity.magnitude > minSlideSpeed || OnSlope()) && grounded)
        {
            isSliding = true;
        }
    }

    private void Slide()
    {
        moveDirection = orientation.forward * move.ReadValue<Vector2>().y + orientation.right * move.ReadValue<Vector2>().x;
        rb.AddForce(moveDirection.normalized * slideForce, ForceMode.Force);
    }

    private void StopSlide(InputAction.CallbackContext context)
    {
        if (isSliding)
        isSliding = false;
    }

    //Wallrunning things

    public void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, isWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, isWall);
    }


    private void StartWallRun(InputAction.CallbackContext context)
    {
        if (wallLeft || wallRight)
        {
            isWallRunning = true;
        }
        
    }

    private void WallRun()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        //add some forward force
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
    }

    private void StopWallRun(InputAction.CallbackContext context)
    {
        isWallRunning = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
