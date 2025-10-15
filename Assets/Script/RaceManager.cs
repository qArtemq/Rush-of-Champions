using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;
using UnityStandardAssets.Vehicles.Car;
using System.Runtime.CompilerServices;

public class RaceManager : MonoBehaviour
{
    [Header("Main Setting")]
    public Color[] botColors;
    private Color[] playerColor;
    public GameObject playerCarPrefab;
    public GameObject[] botPrefabs;
    public GameObject[] playerPrefabs;
    public GameObject[] spawnPointsArray;
    public Transform[] spawnPoints;
    public int totalLaps = 3;
    public Transform[] checkpoints;
    List<string> botNames = new List<string> { "Brianna", "Lily", "Ivy", "Nana", "Belle", "Elma", "Lulu", "Perseus", "Alex", "Taylor", "Jordan", "Morgan", "Henry" };
    public GameObject Minimap;
    private GameObject lapDisplayObj;
    public GameObject[] EndGame;
    public GameObject EndDisplay;
    public GameObject startGame;

    [Header("Animation Setting")]
    public Camera animationCamera;
    public AnimationClip cameraAnimationClip;
    public bool startAnimation = false;
    public bool finishAnimation = false;
    private Animation animationComponent;

    [Header("Text")]
    public Text lapDisplay;
    public Text leaderboardDisplay;
    public Text countdownDisplay;
    public Text EndDisplayText;
    public Text timeGame;
    public Text progressGame;

    [Header("UI")]
    public GameObject lapDisplayPrefab;
    public Transform uiCanvas;

    public AudioSource audioSourceStart;

    public Dictionary<GameObject, int> carLaps = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, int> carCheckpointIndex = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, Text> carLapDisplays = new Dictionary<GameObject, Text>();
    private Dictionary<GameObject, float> carProgress = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, string> carNames = new Dictionary<GameObject, string>();
    private Dictionary<GameObject, float> carFinishTimes = new Dictionary<GameObject, float>();
    public bool playerFinished = false;
    float raceStartTime;
    public bool raceStarted = false;

    private int totalEarnings;

    void Start()
    {
        totalLaps = PlayerPrefs.GetInt("SelectedLaps");
        int selectedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex");

        totalEarnings = PlayerPrefs.GetInt("SelectedMoney");

        float playerCarColorR = PlayerPrefs.GetFloat("PlayerCarColorR");
        float playerCarColorG = PlayerPrefs.GetFloat("PlayerCarColorG");
        float playerCarColorB = PlayerPrefs.GetFloat("PlayerCarColorB");
        Color playerColor = new Color(playerCarColorR, playerCarColorG, playerCarColorB);

        int selectedBots = PlayerPrefs.GetInt("SelectedBots");
        int totalParticipants = selectedBots + 1;
        if (totalParticipants > 1)
        {
            totalParticipants = selectedBots;
        }

        for (int i = 0; i < spawnPointsArray.Length; i++)
        {
            spawnPointsArray[i].SetActive(i < totalParticipants);
        }

        playerCarPrefab = playerPrefabs[selectedCarIndex];

        animationCamera.enabled = true;

        animationComponent = animationCamera.gameObject.AddComponent<Animation>();
        animationComponent.clip = cameraAnimationClip;

        finishAnimation = false;
        animationComponent.Play();

        StartCoroutine(PlayCameraAnimation());

        Transform lapsFolder = uiCanvas.Find("Laps");

        for (int i = 0; i < totalParticipants; i++)
        {
            GameObject car = null;

            if (i == 0)
            {
                car = Instantiate(playerCarPrefab, spawnPoints[0].position, spawnPoints[0].rotation);
                SetCarColor(car, playerColor);
            }
            else if (selectedBots > 0)
            {
                int botIndex = (i - 1) % botPrefabs.Length;
                car = Instantiate(botPrefabs[botIndex], spawnPoints[i].position, spawnPoints[i].rotation);
                Color randomColor = botColors[Random.Range(0, botColors.Length)];
                SetCarColor(car, randomColor);
            }

            if (car != null && !carLaps.ContainsKey(car))
            {
                timeGame.text = "0:00.0";
                progressGame.text = "0%";
                carLaps.Add(car, 0);
                carCheckpointIndex.Add(car, 0);

                CarController controller = car.GetComponent<CarController>();
                if (controller != null && controller.isPlayerCar)
                {
                    lapDisplayObj = Instantiate(lapDisplayPrefab, lapsFolder);
                    lapDisplayObj.transform.Find("Mesto");
                    Text lapDisplayText = lapDisplayObj.GetComponent<Text>();
                    lapDisplayText.text = "Lap: 0/" + totalLaps;
                    carLapDisplays[car] = lapDisplayText;
                }
                if (car.CompareTag("Player"))
                {
                    string playerName = PlayerPrefs.GetString("PlayerName");
                    carNames.Add(car, playerName);
                }
                else
                {
                    string botName = botNames[Random.Range(0, botNames.Count)];
                    carNames.Add(car, botName);
                    botNames.Remove(botName);
                }
            }
        }
    }

    void Update()
    {
        if (raceStarted && !playerFinished)
        {
            float elapsedTime = Time.time - raceStartTime;

            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            int milliseconds = Mathf.FloorToInt((elapsedTime * 10) % 10);

            timeGame.text = $"{minutes}:{seconds:D2}.{milliseconds:D1}";

            ProgressGame();
        }

        foreach (var car in carLaps.Keys)
        {
            CalculateProgress(car);
        }

        ShowLeaderboard();
    }

    void SetCarColor(GameObject car, Color color)
    {
        Renderer renderer = car.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
    void SetUIVisibility(bool isVisible)
    {
        Minimap.SetActive(isVisible);
        foreach (GameObject uiElement in EndGame)
        {
            uiElement.SetActive(isVisible);
        }
    }
    IEnumerator PlayCameraAnimation()
    {
        SetUIVisibility(false);
        yield return new WaitForSeconds(cameraAnimationClip.length);

        SetUIVisibility(true);
        animationCamera.enabled = false;
        startAnimation = true;
        finishAnimation = true;

        StartCoroutine(StartCountdown(0.7f));
    }
    void ProgressGame()
    {
        GameObject playerCar = GameObject.FindWithTag("Player");

        int currentLap = carLaps[playerCar];
        int checkpointIndex = carCheckpointIndex[playerCar];

        float lapProgress = (float)currentLap / totalLaps;

        float checkpointProgress = (float)checkpointIndex / checkpoints.Length / totalLaps;

        Transform currentCheckpoint = checkpoints[checkpointIndex];
        Transform nextCheckpoint = checkpoints[(checkpointIndex + 1) % checkpoints.Length];
        float distanceToNextCheckpoint = Vector3.Distance(playerCar.transform.position, nextCheckpoint.position);
        float totalDistanceBetweenCheckpoints = Vector3.Distance(currentCheckpoint.position, nextCheckpoint.position);
        float checkpointDistanceProgress = (1 - Mathf.Clamp01(distanceToNextCheckpoint / totalDistanceBetweenCheckpoints)) / checkpoints.Length / totalLaps;

        float totalProgress = (lapProgress + checkpointProgress + checkpointDistanceProgress) * 100;

        progressGame.text = $"{Mathf.Clamp(Mathf.FloorToInt(totalProgress), 0, 100)}%";
    }

    void OnCarFinish(GameObject car)
    {
        if (!carFinishTimes.ContainsKey(car))
        {
            carFinishTimes[car] = Time.time - raceStartTime;

            UpdateLeaderboard(car, carFinishTimes[car]);
            if (car.CompareTag("Player"))
            {
                playerFinished = true;
                DisplayLeaderboard();
            }
            else if (playerFinished)
            {
                DisplayLeaderboard();
            }
        }
    }
    void DisableForEndGame()
    {
        Minimap.SetActive(false);
        foreach (GameObject whenEnd in EndGame)
        {
            whenEnd.SetActive(false);
        }
    }

    void UpdateLeaderboard(GameObject car, float finishTime)
    {
        int minutes = Mathf.FloorToInt(finishTime / 60);
        int seconds = Mathf.FloorToInt(finishTime % 60);
        int milliseconds = Mathf.FloorToInt((finishTime * 10) % 10);

        EndDisplayText.text += $"{carNames[car]} - {minutes}:{seconds:D2}.{milliseconds:D1} \n";
    }

    void DisplayLeaderboard()
    {
        EndDisplay.SetActive(true);
        DisableForEndGame();

        int currentCoins = PlayerPrefs.GetInt("MakeMoneyFinish");

        int makemoney = currentCoins + totalEarnings;

        PlayerPrefs.SetInt("MakeMoneyFinish", makemoney);
    }
    public void ResetCarPosition(GameObject car)
    {
        if (carCheckpointIndex.ContainsKey(car))
        {
            int lastCheckpointIndex = Mathf.Max(0, carCheckpointIndex[car] - 1);
            Transform lastCheckpoint = checkpoints[lastCheckpointIndex];
            car.transform.position = lastCheckpoint.position;
            car.transform.rotation = lastCheckpoint.rotation;

            Rigidbody rb = car.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
    public void OnCheckpointReached(GameObject car, int checkpointIndex)
    {
        if (!raceStarted) return;

        if (carCheckpointIndex.ContainsKey(car) && carCheckpointIndex[car] == checkpointIndex)
        {
            carCheckpointIndex[car]++;

            if (carCheckpointIndex[car] >= checkpoints.Length)
            {
                carLaps[car]++;
                if (carCheckpointIndex[car] >= checkpoints.Length)
                {
                    carCheckpointIndex[car] = 0;
                }

                if (carLapDisplays.ContainsKey(car))
                {
                    carLapDisplays[car].text = $"Lap: {carLaps[car]}/{totalLaps}";
                }

                if (carLaps[car] >= totalLaps)
                {
                    OnCarFinish(car);
                }
            }
        }
    }
    void ShowLeaderboard()
    {
        List<GameObject> sortedCars = new List<GameObject>(carProgress.Keys);
        sortedCars.Sort((car1, car2) => carProgress[car2].CompareTo(carProgress[car1]));

        leaderboardDisplay.text = "Leaderboard:\n";
        for (int i = 0; i < sortedCars.Count; i++)
        {
            GameObject car = sortedCars[i];
            string carName = carNames[car];
            int carLapsCompleted = carLaps[car];

            leaderboardDisplay.text += $"{i + 1}. {carName}\n";
        }
    }
    void CalculateProgress(GameObject car)
    {
        int checkpointIndex = carCheckpointIndex[car];
        Transform currentCheckpoint = checkpoints[checkpointIndex];
        Transform nextCheckpoint = checkpoints[(checkpointIndex + 1) % checkpoints.Length];

        float distanceToNextCheckpoint = Vector3.Distance(car.transform.position, nextCheckpoint.position);

        carProgress[car] = carLaps[car] * checkpoints.Length * 1000 + checkpointIndex * 100 - distanceToNextCheckpoint;
    }

    public void StartRace()
    {
        raceStarted = true;

        startGame.SetActive(false);

        raceStartTime = Time.time;
    }

    IEnumerator StartCountdown(float time)
    {
        audioSourceStart.PlayOneShot(audioSourceStart.clip, 5f);
        countdownDisplay.text = "3";
        yield return new WaitForSeconds(time);
        countdownDisplay.text = "2";
        yield return new WaitForSeconds(time);
        countdownDisplay.text = "1";
        yield return new WaitForSeconds(time);
        countdownDisplay.text = "Go!";
        yield return new WaitForSeconds(time);
        countdownDisplay.text = "";
        StartRace();
    }
}
