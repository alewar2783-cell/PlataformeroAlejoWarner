using UnityEngine;

public class KineticVFXManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KineticPlayerController playerController;
    [SerializeField] private ParticleSystem speedLinesParticles;

    [Header("Continuous Speed Settings")]
    [SerializeField, Tooltip("Speed threshold above which speed lines start appearing")]
    private float speedThreshold = 8f;
    [SerializeField, Tooltip("Maximum emission rate when at max speed")]
    private float maxEmissionRate = 60f;

    [Header("Burst Settings")]
    [SerializeField, Tooltip("Number of particles to emit on dash")]
    private int dashBurstCount = 50;
    [SerializeField, Tooltip("Number of particles to emit on jump")]
    private int jumpBurstCount = 20;

    private ParticleSystem.EmissionModule emissionModule;

    private void Start()
    {
        if (speedLinesParticles != null)
        {
            emissionModule = speedLinesParticles.emission;
            // Disable continuous emission initially, as we'll drive it via script
            emissionModule.rateOverTime = 0f;
        }
    }

    private void Update()
    {
        if (playerController == null || speedLinesParticles == null) return;

        // 1. Manage continuous speed lines based on current kinetic momentum
        float currentSpeed = playerController.CurrentSpeed;

        if (currentSpeed > speedThreshold)
        {
            // Calculate a 0-1 ratio of how far past the threshold we are, capped slightly above RunSpeed
            float maxExpectedSpeed = playerController.RunSpeed + 5f; 
            float speedRatio = Mathf.Clamp01((currentSpeed - speedThreshold) / (maxExpectedSpeed - speedThreshold));
            
            // Smoothly lerp emission rate so wind lines fade in as you accelerate
            emissionModule.rateOverTime = Mathf.Lerp(0f, maxEmissionRate, speedRatio);
        }
        else
        {
            // Stop emitting when moving too slowly
            emissionModule.rateOverTime = 0f;
        }
    }

    // --- Public Burst Methods for KineticPlayerController to call ---

    public void PlayDashBurst()
    {
        if (speedLinesParticles != null)
        {
            speedLinesParticles.Emit(dashBurstCount);
        }
    }

    public void PlayJumpBurst()
    {
        if (speedLinesParticles != null)
        {
            speedLinesParticles.Emit(jumpBurstCount);
        }
    }
}
