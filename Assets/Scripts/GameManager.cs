using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int totalCubes = 0;
    public float timeLimit = 120f; // 2 Minutes
    public float maxFallTime = 5f; // Time before Game Over if falling freely

    [Header("UI")]
    public Text timerText;
    public Text statusText; // "You Win" or "Game Over"

    private int _cubesCollected = 0;
    private float _currentTimer;
    private float _fallTimer = 0f;
    private bool _gameOver = false;

    void Awake()
    {
        Instance = this;
        _currentTimer = timeLimit;
        // Auto count cubes if not set
        if (totalCubes == 0) totalCubes = GameObject.FindGameObjectsWithTag("Pickup").Length;
    }

    void Update()
    {
        if (_gameOver) return;

        // Timer Logic
        _currentTimer -= Time.deltaTime;
        if(timerText) timerText.text = $"Time: {Mathf.CeilToInt(_currentTimer)}s";

        if (_currentTimer <= 0)
        {
            EndGame(false, "Time's Up!");
        }
    }

    public void CollectCube()
    {
        _cubesCollected++;
        if (_cubesCollected >= totalCubes)
        {
            EndGame(true, "All Cubes Collected!");
        }
    }

    public void UpdateFallState(bool isGrounded)
    {
        if (_gameOver) return;

        if (!isGrounded)
        {
            _fallTimer += Time.deltaTime;
            if (_fallTimer > maxFallTime)
            {
                EndGame(false, "Lost in Space!");
            }
        }
        else
        {
            _fallTimer = 0f;
        }
    }

    void EndGame(bool win, string message)
    {
        _gameOver = true;
        if(statusText) 
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }
        Debug.Log(win ? "WIN" : "LOSE");
        // Restart logic or Menu here
    }
}