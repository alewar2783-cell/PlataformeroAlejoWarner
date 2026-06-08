using UnityEngine;

public class KineticVFXManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem speedLinesParticles;

    [Header("Burst Settings")]
    [SerializeField, Tooltip("Number of particles to emit on dash")]
    private int dashBurstCount = 50;
    [SerializeField, Tooltip("Number of particles to emit on jump")]
    private int jumpBurstCount = 20;

    [Header("Wall Run Settings")]
    [SerializeField, Tooltip("Emission rate during wall run")]
    private float wallRunEmissionRate = 60f;

    [Header("Dynamic Rotation")]
    [SerializeField, Tooltip("If true, rotates particles to face backward (-direction)")]
    private bool faceBackward = true;

    private ParticleSystem.EmissionModule emissionModule;

    public int DashBurstCount => dashBurstCount;
    public int JumpBurstCount => jumpBurstCount;

    private void Start()
    {
        if (speedLinesParticles != null)
        {
            emissionModule = speedLinesParticles.emission;
            // Disable continuous emission initially
            emissionModule.rateOverTime = 0f;
        }
    }

    public void PlayBurst(Vector3 direction, int count)
    {
        if (speedLinesParticles != null && direction != Vector3.zero)
        {
            Vector3 finalDirection = faceBackward ? -direction : direction;
            speedLinesParticles.transform.rotation = Quaternion.LookRotation(finalDirection);
            speedLinesParticles.Emit(count);
        }
    }

    public void StartWallRunVFX(Vector3 direction)
    {
        if (speedLinesParticles != null && direction != Vector3.zero)
        {
            Vector3 finalDirection = faceBackward ? -direction : direction;
            speedLinesParticles.transform.rotation = Quaternion.LookRotation(finalDirection);
            emissionModule.rateOverTime = wallRunEmissionRate;
        }
    }

    public void StopWallRunVFX()
    {
        if (speedLinesParticles != null)
        {
            emissionModule.rateOverTime = 0f;
        }
    }
}
