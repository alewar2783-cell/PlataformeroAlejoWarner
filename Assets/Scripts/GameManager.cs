using UnityEngine;
using TMPro; // TextMeshPro namespace for modern UI

public class GameManager : MonoBehaviour
{
    // Singleton pattern so other scripts (like the coin) can easily access the manager
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField, Tooltip("Number of coins required to win the game")]
    private int coinsToWin = 5;

    [Header("UI References")]
    [SerializeField, Tooltip("Reference to the TextMeshPro UI element displaying the score")]
    private TextMeshProUGUI scoreText;

    [SerializeField, Tooltip("Reference to the GameObject containing the 'YOU WIN' text/UI")]
    private GameObject winScreenUI;

    private int currentScore = 0;

    private void Awake()
    {
        // Simple Singleton implementation
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple GameManagers found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ensure win screen is disabled at the start of the game
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(false);
        }

        UpdateScoreUI();
    }

    // Method called by coins when they are collected
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();

        // Check for win condition
        if (currentScore >= coinsToWin)
        {
            WinGame();
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Coins: {currentScore} / {coinsToWin}";
        }
    }

    private void WinGame()
    {
        // Activate the win UI
        if (winScreenUI != null)
        {
            winScreenUI.SetActive(true);
        }

        Debug.Log("YOU WIN!");
        // Optional: Freeze the game by setting Time.timeScale = 0f; if desired
    }
}
