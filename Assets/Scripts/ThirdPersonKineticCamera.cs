using UnityEngine;

// We use Component and Reflection here to strictly prevent Unity compiler errors 
// in case Cinemachine is not yet installed in your project.
public class ThirdPersonKineticCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KineticPlayerController player;
    [SerializeField, Tooltip("Drag your Cinemachine Virtual Camera here")]
    private Component virtualCamera; 

    [Header("FOV Dynamics")]
    [SerializeField] private float minFOV = 40f;
    [SerializeField] private float maxFOV = 70f;
    [SerializeField] private float dashFOV = 85f;
    [SerializeField] private float fovTransitionSpeed = 5f;

    private System.Reflection.PropertyInfo fovProperty;
    private object lensObject;
    private System.Reflection.FieldInfo lensField;

    private void Start()
    {
        if (virtualCamera != null)
        {
            // Safely fetch the m_Lens.FieldOfView using reflection so we don't need the Cinemachine namespace
            lensField = virtualCamera.GetType().GetField("m_Lens");
            if (lensField != null)
            {
                lensObject = lensField.GetValue(virtualCamera);
                if (lensObject != null)
                {
                    fovProperty = lensObject.GetType().GetProperty("FieldOfView");
                }
            }
        }
    }

    private void Update()
    {
        if (player == null || virtualCamera == null || fovProperty == null) return;

        float targetFOV = minFOV;
        
        if (player.CurrentState == KineticPlayerController.PlayerState.Dashing)
        {
            targetFOV = dashFOV;
        }
        else
        {
            // Calculate dynamic FOV based on kinetic speed
            float currentSpeed = player.CurrentSpeed;
            if (currentSpeed > player.WalkSpeed)
            {
                float speedRatio = Mathf.Clamp01((currentSpeed - player.WalkSpeed) / (player.RunSpeed - player.WalkSpeed));
                targetFOV = Mathf.Lerp(minFOV, maxFOV, speedRatio);
            }
        }

        // Apply new FOV back to the Cinemachine Virtual Camera using reflection
        lensObject = lensField.GetValue(virtualCamera);
        float currentFOV = (float)fovProperty.GetValue(lensObject);
        float newFOV = Mathf.Lerp(currentFOV, targetFOV, fovTransitionSpeed * Time.deltaTime);
        
        fovProperty.SetValue(lensObject, newFOV);
        lensField.SetValue(virtualCamera, lensObject);
    }
}
