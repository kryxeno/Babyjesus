using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    private PlayerInputActions playerInputactions;
    private InputAction look;

    //Initialise variables
    public float sensX;
    public float sensY;

    public Transform orientation;
    public Transform player;
    public Transform visor;

    float mouseX;
    float mouseY;
    float xRotation;
    float yRotation;


    private void Awake()
    {
        playerInputactions = new PlayerInputActions();
    }


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        look = playerInputactions.Player.Look;
        look.Enable();
    }

    private void OnDisable()
    {
        look.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        mouseX = look.ReadValue<Vector2>().x * Time.deltaTime * sensX;
        mouseY = look.ReadValue<Vector2>().y * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Rotate camera and orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
        player.rotation = Quaternion.Euler(0, yRotation, 0);

    }
}
