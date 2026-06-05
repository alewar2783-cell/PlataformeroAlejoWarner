using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Settings")]
    public float mouseSensitivity = 10f; // Adjusted for Input System typical values
    
    [Header("References")]
    public Transform orientation;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        if (lookAction != null) lookAction.action.Enable();
    }

    private void OnDisable()
    {
        if (lookAction != null) lookAction.action.Disable();
    }

    void Update()
    {
        if (lookAction == null) return;

        // Read Vector2 from new Input System
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        
        // Apply sensitivity and time factor 
        // Note: With the new Input System, scaling by deltaTime is sometimes omitted 
        // depending on if the action is set to Delta (like a mouse) or Value (like a gamepad stick). 
        // We multiply by sensitivity.
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        
        // Clamp up/down rotation so camera doesn't flip
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Rotate the camera up and down
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        
        // Rotate the orientation transform left and right
        if (orientation != null)
        {
            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }
}
