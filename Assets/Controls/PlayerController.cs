using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //Initialise all player action necessities
    private PlayerInputActions playerInputActions;
    public InputAction move;
    public InputAction jump;

    public Transform orientation;
    public Rigidbody rb;

    Vector3 moveDirection;

    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;

    public float jumpForce;
    public float airMultiplier;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask isGround;
    public bool grounded;


    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        move = playerInputActions.Player.Move;
        move.Enable();

        jump = playerInputActions.Player.Jump;
        jump.Enable();
        jump.performed += Jump;

    }

    private void OnDisable()
    {
        move.Disable();
        jump.Disable();
    }


    private void Update()
    {
        //Non-physics stuff here
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, isGround);

        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
        
    }


    private void FixedUpdate()
    {
        //Use physics based stuff here
        MovePlayer();
        
    }


    private void MovePlayer()
    {
        if (grounded)
        {
            moveDirection = orientation.forward * move.ReadValue<Vector2>().y + orientation.right * move.ReadValue<Vector2>().x;
            rb.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);
        }
        
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier, ForceMode.Force);
        }
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (grounded)
        {
            //reset y velocity for consistent jumps, this might need to be removed for momentum systems though
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.y);

            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
        
    }



}
