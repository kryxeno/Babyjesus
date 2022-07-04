using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wallrunning : MonoBehaviour
{
    [Header("Wallrunning")]
    public LayerMask isWall;
    public LayerMask isGround;
    public float wallRunForce;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    public bool wallLeft;
    public bool wallRight;

    [Header("References")]
    public Transform orientation;
    private PlayerController pm;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerController>();
    }

    private void Update()
    {
        //Could perhaps only check while trying to start a walljump, we don't always need to know (performance and stuff)
        CheckForWall();
    }

    public void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, isWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, isWall);
    }

    private bool AboveGround()
    {
        // function is not used now, but this check might be useful in the future with the state handling.
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, isGround);
    }

    public void WallRunningMovement()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        //add some forward force
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
    }

}
