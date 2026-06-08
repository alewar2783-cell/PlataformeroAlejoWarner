using UnityEngine;

public class CoinCompass : MonoBehaviour
{
    [Header("Targeting Settings")]
    [SerializeField, Tooltip("How smoothly the arrow rotates toward the target")]
    private float rotationSpeed = 10f;
    
    [SerializeField, Tooltip("Hide arrow if closest coin is closer than this distance (prevents erratic spinning)")]
    private float hideDistanceThreshold = 2.0f;

    [Header("Visuals")]
    [SerializeField, Tooltip("The visual mesh or child object to hide/show")]
    private GameObject arrowVisuals;

    [SerializeField, Tooltip("Offset applied if your 3D model doesn't point perfectly forward along the local Z axis")]
    private Vector3 rotationOffset = Vector3.zero;

    private CollectibleCoin[] allCoins;
    private float searchTimer = 0f;
    private float searchInterval = 0.25f; // Find objects 4 times a second to save performance

    private void Start()
    {
        // Initial search
        RefreshCoinList();
    }

    private void Update()
    {
        // Performance Optimization: Using FindObjectsOfType every single frame is very heavy.
        // Instead, we refresh our array on a slight interval.
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            RefreshCoinList();
            searchTimer = searchInterval;
        }

        CollectibleCoin closestCoin = GetClosestCoin();

        if (closestCoin == null)
        {
            // Win / Empty State: No coins left
            if (arrowVisuals != null && arrowVisuals.activeSelf)
                arrowVisuals.SetActive(false);
            return;
        }

        float distanceToCoin = Vector3.Distance(transform.position, closestCoin.transform.position);

        if (distanceToCoin < hideDistanceThreshold)
        {
            // Player is practically inside or directly beneath the coin
            // Hide the arrow to prevent erratic 360-degree spinning
            if (arrowVisuals != null && arrowVisuals.activeSelf)
                arrowVisuals.SetActive(false);
        }
        else
        {
            // Show the arrow
            if (arrowVisuals != null && !arrowVisuals.activeSelf)
                arrowVisuals.SetActive(true);

            // Calculate direction to the coin
            Vector3 directionToTarget = closestCoin.transform.position - transform.position;

            if (directionToTarget != Vector3.zero)
            {
                // Create a rotation looking at the coin
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                
                // Apply rotation offset in case the 3D model is modeled facing sideways/upwards
                targetRotation *= Quaternion.Euler(rotationOffset);

                // Smoothly Slerp toward the target rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void RefreshCoinList()
    {
        allCoins = FindObjectsOfType<CollectibleCoin>();
    }

    private CollectibleCoin GetClosestCoin()
    {
        if (allCoins == null || allCoins.Length == 0) return null;

        CollectibleCoin closest = null;
        float minDistance = float.MaxValue;

        foreach (CollectibleCoin coin in allCoins)
        {
            // In case a coin was destroyed between our search intervals
            if (coin == null) continue; 

            float dist = Vector3.Distance(transform.position, coin.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = coin;
            }
        }

        return closest;
    }
}
