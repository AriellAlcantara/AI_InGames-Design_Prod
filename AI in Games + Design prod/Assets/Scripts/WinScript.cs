using UnityEngine;
using TMPro;

public class WinScript : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The WinPanel GameObject (child of Canvas). Hidden during gameplay.")]
    public GameObject winPanel;

    [Tooltip("The WinText TextMeshPro component inside the WinPanel.")]
    public TextMeshProUGUI winText;

    [Header("References")]
    [Tooltip("The TeamSpawner in the scene.")]
    public TeamSpawner teamSpawner;

    private bool gameOver = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Hide the win panel at the start of the game
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameOver) return;
        if (teamSpawner == null) return;

        // Don't check until both teams have started spawning
        bool redSpawnedOut = teamSpawner.IsTeamSpawnedOut(Team.Red);
        bool blueSpawnedOut = teamSpawner.IsTeamSpawnedOut(Team.Blue);

        // Only start checking for a winner once at least one team has used all their spawns
        if (!redSpawnedOut && !blueSpawnedOut) return;

        int redAlive = teamSpawner.GetRedAliveCount();
        int blueAlive = teamSpawner.GetBlueAliveCount();

        // Red team wiped out — Blue wins
        if (redSpawnedOut && redAlive <= 0)
        {
            EndGame(Team.Blue);
        }
        // Blue team wiped out — Red wins
        else if (blueSpawnedOut && blueAlive <= 0)
        {
            EndGame(Team.Red);
        }
    }

    private void EndGame(Team winner)
    {
        gameOver = true;

        // Show the win panel
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        // Set the win text
        if (winText != null)
        {
            winText.text = $"{winner} Wins!";
        }

        Debug.Log($"Game Over! {winner} Wins!");
    }
}
