using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceStaminaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Reference to the player to read stamina")]
    private KineticPlayerController player;
    [SerializeField, Tooltip("The Image component that fills up")]
    private Image fillImage;
    [SerializeField, Tooltip("The Camera this UI should face. Defaults to Main Camera if null.")]
    private Camera targetCamera;

    [Header("Settings")]
    [SerializeField, Tooltip("How smoothly the bar fills")]
    private float smoothSpeed = 15f;
    [SerializeField, Tooltip("Lock the X rotation so the bar doesn't tilt up/down")]
    private bool lockXRotation = true;
    [SerializeField, Tooltip("Lock the Z rotation so the bar doesn't roll")]
    private bool lockZRotation = true;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Update()
    {
        if (player == null || fillImage == null) return;

        // Smoothly interpolate the fill amount based on current stamina
        float targetFill = player.CurrentStamina / player.MaxStamina;
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
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
