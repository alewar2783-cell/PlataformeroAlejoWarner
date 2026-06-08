using UnityEngine;

public class CollectibleCoin : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField, Tooltip("Value of this coin when collected")]
    private int coinValue = 1;

    [Header("Visuals & Animation")]
    [SerializeField, Tooltip("The visual mesh to animate. If empty, uses this object's Transform.")]
    private Transform visualTransform;

    [SerializeField, Tooltip("Rotation speed of the coin in degrees per second")]
    private Vector3 rotationSpeed = new Vector3(0f, 100f, 0f);

    [SerializeField, Tooltip("How fast the squash & stretch animation plays")]
    private float stretchSpeed = 5f;

    [SerializeField, Tooltip("How intensely the coin squashes and stretches")]
    private float stretchAmount = 0.2f;

    private Vector3 baseScale;

    private void Start()
    {
        // If no visual transform is assigned, default to this GameObject
        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        // Store the original scale so we animate relative to it
        baseScale = visualTransform.localScale;
    }

    private void Update()
    {
        // 1. Continuous Rotation
        visualTransform.Rotate(rotationSpeed * Time.deltaTime, Space.World);

        // 2. Procedural Squash & Stretch (Volume Preservation)
        // Calculate a sine wave that goes from -1 to 1 based on time
        float sinWave = Mathf.Sin(Time.time * stretchSpeed);
        
        // Stretch the Y axis based on the sine wave
        float scaleY = baseScale.y + (sinWave * stretchAmount);
        
        // To preserve volume (mass), when Y stretches UP, X and Z must squash DOWN.
        // We use -sinWave to invert the effect for X and Z axes.
        float scaleXZOffset = (-sinWave * stretchAmount) * 0.5f; 
        
        float scaleX = baseScale.x + scaleXZOffset;
        float scaleZ = baseScale.z + scaleXZOffset;

        visualTransform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the "Player" tag
        if (other.CompareTag("Player"))
        {
            // Tell the GameManager to add points
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(coinValue);
            }
            else
            {
                Debug.LogWarning("Coin collected but GameManager.Instance is missing!");
            }

            // Destroy the coin object immediately
            Destroy(gameObject);
        }
    }
}
