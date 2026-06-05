using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceStaminaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Reference to the player to read stamina")]
    private KineticPlayerController player;
    [SerializeField, Tooltip("The primary Image component that fills up")]
    private Image mainFillImage;
    [SerializeField, Tooltip("The secondary 'Ghost' Image that trails behind")]
    private Image ghostFillImage;
    [SerializeField, Tooltip("The Camera this UI should face. Defaults to Main Camera if null.")]
    private Camera targetCamera;

    [Header("Main Bar Settings")]
    [SerializeField, Tooltip("How smoothly the main bar updates")]
    private float mainFillSpeed = 12f;
    [SerializeField, Tooltip("Color gradient based on stamina percentage (0 to 1). E.g., Red at 0%, Green at 100%.")]
    private Gradient staminaGradient;

    [Header("Ghost Bar Settings")]
    [SerializeField, Tooltip("How long to wait before the ghost bar starts shrinking")]
    private float ghostShrinkDelay = 0.4f;
    [SerializeField, Tooltip("How smoothly the ghost bar catches up to the main bar")]
    private float ghostShrinkSpeed = 3f;

    [Header("Billboard Settings")]
    [SerializeField, Tooltip("Lock the X rotation so the bar doesn't tilt up/down")]
    private bool lockXRotation = true;
    [SerializeField, Tooltip("Lock the Z rotation so the bar doesn't roll")]
    private bool lockZRotation = true;

    private float ghostTimer;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        
        // Snap bars to starting values
        if (player != null)
        {
            float initialFill = player.CurrentStamina / player.MaxStamina;
            if (mainFillImage != null) mainFillImage.fillAmount = initialFill;
            if (ghostFillImage != null) ghostFillImage.fillAmount = initialFill;
        }
    }

    private void Update()
    {
        if (player == null || mainFillImage == null || ghostFillImage == null) return;

        float targetFill = player.CurrentStamina / player.MaxStamina;

        // 1. Smoothly Lerp the Main Bar
        mainFillImage.fillAmount = Mathf.Lerp(mainFillImage.fillAmount, targetFill, Time.deltaTime * mainFillSpeed);
        
        // 2. Evaluate Color Gradient
        // Evaluates from 0.0 (left/empty) to 1.0 (right/full)
        if (staminaGradient != null)
        {
            mainFillImage.color = staminaGradient.Evaluate(mainFillImage.fillAmount);
        }

        // 3. Ghost Bar Logic
        if (ghostFillImage.fillAmount > mainFillImage.fillAmount)
        {
            // The main bar is lower (stamina consumed). Start the delay timer.
            ghostTimer += Time.deltaTime;
            
            if (ghostTimer > ghostShrinkDelay)
            {
                // Delay finished. Smoothly shrink ghost bar to catch up.
                ghostFillImage.fillAmount = Mathf.Lerp(ghostFillImage.fillAmount, mainFillImage.fillAmount, Time.deltaTime * ghostShrinkSpeed);
            }
        }
        else
        {
            // Stamina is recharging. Snap ghost bar perfectly to main bar so it doesn't trail backwards.
            ghostFillImage.fillAmount = mainFillImage.fillAmount;
            ghostTimer = 0f; // Reset delay timer
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // Billboard logic: rotate to face the camera perfectly
        transform.LookAt(transform.position + targetCamera.transform.rotation * Vector3.forward,
                         targetCamera.transform.rotation * Vector3.up);

        // Optional rotation locks to keep it standing upright
        Vector3 eulerAngles = transform.eulerAngles;
        if (lockXRotation) eulerAngles.x = 0f;
        if (lockZRotation) eulerAngles.z = 0f;
        transform.eulerAngles = eulerAngles;
    }
}
