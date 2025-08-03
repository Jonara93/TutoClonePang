using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro.EditorUtilities;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupCollisionLayers();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    private const string BALL_LAYER_NAME = "Balls";
    private const int BALL_LAYER = 8;
    private const int MAX_LEVEL = 4;

    [Header("Niveau")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float levelTime = 120f;
    [SerializeField] private int initialBallCount = 1;

    [Header("Joueur")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int lives = 3;

    [Header("Balles")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private List<Color> ballColors = new();

    //ETAT DU JEU
    private bool isGameRunning = false;
    private int score = 0;
    private float remainingTime;
    private int remainingLives;

    //event du jeu
    public delegate void GameEvent();
    public event GameEvent OnGameStrat;
    public event GameEvent OnGameOver;
    public event GameEvent OnLevelComplete;
    public event GameEvent OnScoreChange;
    public event GameEvent OnLifeLost;

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (isGameRunning)
        {
            UpdateTimer();
            CheckLevelCompletion();
        }
    }


    private void InitializeGame()
    {
        score = 0;
        remainingLives = lives;
        remainingTime = levelTime;
        isGameRunning = true;

        SpawnPlayer();
        SpawnInitialBalls();

        OnGameStrat?.Invoke();
    }

    private void UpdateTimer()
    {
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
        {
            GameOver();
        }
    }

    private void CheckLevelCompletion()
    {
        Ball[] balls = FindObjectsOfType<Ball>();

        if (balls.Length == 0)
        {
            LevelComplete();
        }
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = new Vector3(0, -4f, 0);
        Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
    }

    private void SpawnInitialBalls()
    {
        for (int i = 0; i < initialBallCount; i++)
        {
            SpawnBall(3, new Vector3(-3f + i * 6f, 2f, 0));
        }
    }

    #region Gestion balls
    public GameObject SpawnBall(int size, Vector3 position)
    {
        GameObject ballObject = Instantiate(ballPrefab, position, Quaternion.identity);
        Ball ball = ballObject.GetComponent<Ball>();

        if (ball != null)
        {
            ball.Initialize(size, GetRandomBallColor());
        }
        return ballObject;
    }

    private Color GetRandomBallColor()
    {
        if (ballColors.Count > 0)
        {
            int randomIndex = Random.Range(0, ballColors.Count);
            return ballColors[randomIndex];
        }
        return Color.white;
    }
    #endregion

    #region GEstion score et joueur
    public void AddScore(int points)
    {
        score += points;
        OnScoreChange?.Invoke();
    }

    public void PlayerHit()
    {
        remainingLives--;
        OnLifeLost?.Invoke();

        if (remainingLives <= 0)
        {
            GameOver();
        }
        else
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                player.Reset();
            }
        }
    }
    #endregion
    #region gestion niveau
    private void LevelComplete()
    {
        isGameRunning = false;
        OnLevelComplete?.Invoke();

        currentLevel++;

        if (currentLevel > MAX_LEVEL)
        {
            currentLevel = 1;
        }
        Invoke(nameof(LoadNextLevel), 3f);
    }
    private void LoadNextLevel()
    {
        CleanupCurrentLevel();

        // UIManager uiManager = FindObjectOfType<UIManager>();
        // if (uiManager != null)
        // {
        //     uiManager.HideLevelCompletePanel();
        // }

        CenterPlayer();

        remainingTime = levelTime;

        RefreshBackground();

        SpawnBallsForLevel();

        isGameRunning = true;

        OnGameStrat?.Invoke();
    }

    private void CleanupCurrentLevel()
    {
        Ball[] balls = FindObjectsOfType<Ball>();
        foreach (Ball ball in balls)
        {
            Destroy(ball.gameObject);
        }

        Harpoon[] harpoons = FindObjectsOfType<Harpoon>();
        foreach (Harpoon harpoon in harpoons)
        {
            Destroy(harpoon.gameObject);
        }
    }

    private void CenterPlayer()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.Reset();
        }
    }

    private void RefreshBackground()
    {
        // LevelGenerator levelGenerator = FindObjectOfType<LevelGenerator>();
        //   if (levelGenerator != null)
        // {
        //     levelGenerator.UpdateBackground();
        // }
    }

    private void SpawnBallsForLevel()
    {
        int ballsToSpawn = currentLevel;

        if (ballsToSpawn < 1) ballsToSpawn = 1;

        float spacing = 6f / ballsToSpawn;
        float startX = -3f + spacing / 2;
        for (int i = 0; i < ballsToSpawn; i++)
        {
            Vector3 position = new Vector3(startX + i * spacing, 2f, 0);
            SpawnBall(3, position);
        }
    }

    private void GameOver()
    {
        isGameRunning = false;
        OnGameOver?.Invoke();

        Invoke(nameof(RestartGame), 3f);
    } private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    #endregion

}
