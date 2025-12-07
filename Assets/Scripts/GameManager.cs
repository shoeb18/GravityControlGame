using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int totalCubes = 0;
    public float timeLimit = 120f; // Default 2 Minutes Timer
    public float maxFallTime = 5f; // Time before Game Over if falling freely

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText; // used for endscreen msg
    public GameObject endScreen;

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

    void Start()
    {
        endScreen.SetActive(false);
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

    // score
    public void CollectCube()
    {
        _cubesCollected++;
        if (_cubesCollected >= totalCubes)
        {
            EndGame(true, "All Cubes Collected!");
        }
    }

    // player fall
    public void UpdateFallState(bool isGrounded)
    {
        if (_gameOver) return;

        if (!isGrounded)
        {
            _fallTimer += Time.deltaTime;
            if (_fallTimer > maxFallTime)
            {
                EndGame(false, "Lost in Space! Game Over");
            }
        }
        else
        {
            _fallTimer = 0f;
        }
    }

    // endscreen
    void EndGame(bool win, string message)
    {
        _gameOver = true;
        endScreen.SetActive(true);
        // FindAnyObjectByType<PlayerController>().enabled = false;
        FindAnyObjectByType<CameraThirdPerson>().enabled = false;

        Cursor.lockState = CursorLockMode.None; // unlocking cursor
        Cursor.visible = true;
        
        if(statusText) 
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }
        Debug.Log(win ? "WIN" : "LOSE");
    }

    // ui methods
    public void RestartGame()
    {
        Debug.Log("Restart Game");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}