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
    public Color[] botColors; // Массив для хранения цветов для ботов
    private Color[] playerColor; // Храните выбранный игроком цвет
    public GameObject playerCarPrefab; // Префаб игрока
    public GameObject[] botPrefabs; // Массив префабов ботов
    public GameObject[] playerPrefabs; // Массив префабов игрока
    public GameObject[] spawnPointsArray; // Массив точек спавнов
    public Transform[] spawnPoints; // Точки появления машин
    public int totalLaps = 3; // Количество кругов
    public Transform[] checkpoints; // Контрольные точки
    List<string> botNames = new List<string> { "Brianna", "Lily", "Ivy", "Nana", "Belle", "Elma", "Lulu", "Perseus", "Alex", "Taylor", "Jordan", "Morgan", "Henry" };
    public GameObject Minimap;
    private GameObject lapDisplayObj; // Поле для хранения ссылки на UI-объект
    public GameObject[] EndGame;
    public GameObject EndDisplay;
    public GameObject startGame;

    [Header("Animation Setting")]
    public Camera animationCamera;  // Камера для анимации подлета
    public AnimationClip cameraAnimationClip; // Клип анимации камеры
    public bool startAnimation = false;
    public bool finishAnimation = false;
    private Animation animationComponent;

    [Header("Text")]
    public Text lapDisplay; // Текст для отображения кругов
    public Text leaderboardDisplay; // Текст для отображения таблицы лидеров
    public Text countdownDisplay; // Текст для отображения отсчета
    public Text EndDisplayText; // Текст для отображения отсчета
    public Text timeGame;
    public Text progressGame;

    [Header("UI")]
    public GameObject lapDisplayPrefab; // Prefab for lap display UI
    public Transform uiCanvas; // Parent Canvas for UI elements

    public AudioSource audioSourceStart;

    public Dictionary<GameObject, int> carLaps = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, int> carCheckpointIndex = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, Text> carLapDisplays = new Dictionary<GameObject, Text>();
    private Dictionary<GameObject, float> carProgress = new Dictionary<GameObject, float>(); // Прогресс машины на трассе
    public Dictionary<GameObject, string> carNames = new Dictionary<GameObject, string>(); // Никнеймы машин
    private Dictionary<GameObject, float> carFinishTimes = new Dictionary<GameObject, float>(); // Хранит время окончания
    public bool playerFinished = false; // Проверяет, закончил ли игрок первым
    float raceStartTime;
    public bool raceStarted = false;

    private int totalEarnings;

    void Start()
    {
        // Проверяем, были ли сохранены значения кругов и ботов
        totalLaps = PlayerPrefs.GetInt("SelectedLaps");
        int selectedCarIndex = PlayerPrefs.GetInt("SelectedCarIndex");

        totalEarnings = PlayerPrefs.GetInt("SelectedMoney");

        // Загрузка выбранного цвета игрока
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

        // Активируем нужное количество точек спавна
        for (int i = 0; i < spawnPointsArray.Length; i++)
        {
            spawnPointsArray[i].SetActive(i < totalParticipants);
        }

        playerCarPrefab = playerPrefabs[selectedCarIndex];

        animationCamera.enabled = true;

        // Добавляем компонент Animation и присваиваем анимационный клип
        animationComponent = animationCamera.gameObject.AddComponent<Animation>();
        animationComponent.clip = cameraAnimationClip;

        finishAnimation = false;
        // Запускаем анимацию
        animationComponent.Play();

        // Ожидание завершения анимации перед началом отсчета
        StartCoroutine(PlayCameraAnimation());

        Transform lapsFolder = uiCanvas.Find("Laps"); // Находим папку Laps в uiCanvas

        for (int i = 0; i < totalParticipants; i++)
        {
            GameObject car = null;

            if (i == 0)
            {
                car = Instantiate(playerCarPrefab, spawnPoints[0].position, spawnPoints[0].rotation);
                SetCarColor(car, playerColor); // Применение выбранного цвета игрока
            }
            else if (selectedBots > 0)
            {
                int botIndex = (i - 1) % botPrefabs.Length; // Используем % для безопасного обращения к массиву
                car = Instantiate(botPrefabs[botIndex], spawnPoints[i].position, spawnPoints[i].rotation);
                Color randomColor = botColors[Random.Range(0, botColors.Length)]; // Выбор случайного цвета
                SetCarColor(car, randomColor); // Применение случайного цвета для бота
            }

            if (car != null && !carLaps.ContainsKey(car))
            {
                timeGame.text = "0:00.0";
                progressGame.text = "0%";
                carLaps.Add(car, 0);
                carCheckpointIndex.Add(car, 0); // Начинаем с первой контрольной точки

                // Проверяем, является ли машина игроком, чтобы создать UI для кругов
                CarController сontroller = car.GetComponent<CarController>();
                if (сontroller != null && сontroller.isPlayerCar)
                {
                    lapDisplayObj = Instantiate(lapDisplayPrefab, lapsFolder);
                    lapDisplayObj.transform.Find("Mesto");
                    Text lapDisplayText = lapDisplayObj.GetComponent<Text>();
                    lapDisplayText.text = "Lap: 0/" + totalLaps;
                    carLapDisplays[car] = lapDisplayText; // Привязываем UI элемент к машине игрока
                }
                // Назначаем никнеймы: игроку свой, ботам - случайные из списка
                if (car.CompareTag("Player"))
                {
                    string playerName = PlayerPrefs.GetString("PlayerName");
                    carNames.Add(car, playerName);
                }
                else
                {
                    string botName = botNames[Random.Range(0, botNames.Count)];
                    carNames.Add(car, botName);
                    botNames.Remove(botName); // Удаляем имя из списка, чтобы оно не повторялось
                }
            }
        }
    }

    void Update()
    {
        // Если гонка началась и игрок еще не финишировал
        if (raceStarted && !playerFinished)
        {
            // Рассчитываем прошедшее время
            float elapsedTime = Time.time - raceStartTime;

            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            int milliseconds = Mathf.FloorToInt((elapsedTime * 10) % 10);

            // Обновляем текст таймера
            timeGame.text = $"{minutes}:{seconds:D2}.{milliseconds:D1}";

            ProgressGame();
        }

        foreach (var car in carLaps.Keys)
        {
            CalculateProgress(car); // Обновляем прогресс для каждой машины
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
        // Скрываем интерфейсы перед началом анимации
        SetUIVisibility(false);
        // Ждем окончания анимации
        yield return new WaitForSeconds(cameraAnimationClip.length);

        // После завершения анимации переключаем камеры
        // Показываем интерфейсы после завершения анимации
        SetUIVisibility(true);
        animationCamera.enabled = false;
        startAnimation = true;
        finishAnimation = true;

        // Запуск отсчета
        StartCoroutine(StartCountdown(0.7f));
    }
    void ProgressGame()
    {
        GameObject playerCar = GameObject.FindWithTag("Player");

        int currentLap = carLaps[playerCar];
        int checkpointIndex = carCheckpointIndex[playerCar];

        // 1. Прогресс по кругам (в процентах)
        float lapProgress = (float)currentLap / totalLaps;

        // 2. Прогресс по чекпоинтам в текущем круге
        float checkpointProgress = (float)checkpointIndex / checkpoints.Length / totalLaps;

        // 3. Прогресс до следующего чекпоинта
        Transform currentCheckpoint = checkpoints[checkpointIndex];
        Transform nextCheckpoint = checkpoints[(checkpointIndex + 1) % checkpoints.Length];
        float distanceToNextCheckpoint = Vector3.Distance(playerCar.transform.position, nextCheckpoint.position);
        float totalDistanceBetweenCheckpoints = Vector3.Distance(currentCheckpoint.position, nextCheckpoint.position);
        float checkpointDistanceProgress = (1 - Mathf.Clamp01(distanceToNextCheckpoint / totalDistanceBetweenCheckpoints)) / checkpoints.Length / totalLaps;

        // Итоговый прогресс
        float totalProgress = (lapProgress + checkpointProgress + checkpointDistanceProgress) * 100;

        // Обновляем отображение прогресса
        progressGame.text = $"{Mathf.Clamp(Mathf.FloorToInt(totalProgress), 0, 100)}%";
    }

    void OnCarFinish(GameObject car)
    {
        if (!carFinishTimes.ContainsKey(car))
        {
            carFinishTimes[car] = Time.time - raceStartTime; // Храните время финиша, основанное на старте гонки

            // Обновляйте таблицу лидеров с каждым финишером
            UpdateLeaderboard(car, carFinishTimes[car]);
            // Если игрок закончил
            if (car.CompareTag("Player"))
            {
                playerFinished = true;
                DisplayLeaderboard();
            }
            else if (playerFinished) // Обновите таблицу лидеров, если игрок стал первым, а остальные закончили
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

    // Функция для обновления текста таблицы лидеров
    void UpdateLeaderboard(GameObject car, float finishTime)
    {
        int minutes = Mathf.FloorToInt(finishTime / 60);
        int seconds = Mathf.FloorToInt(finishTime % 60);
        int milliseconds = Mathf.FloorToInt((finishTime * 10) % 10);

        EndDisplayText.text += $"{carNames[car]} - {minutes}:{seconds:D2}.{milliseconds:D1} \n";
    }

    // Функция отображения таблицы лидеров, если игрок финиширует
    void DisplayLeaderboard()
    {
        EndDisplay.SetActive(true);
        DisableForEndGame();

        int currentCoins = PlayerPrefs.GetInt("MakeMoneyFinish"); // Получаем текущее сохраненное количество монет

        int makemoney = currentCoins + totalEarnings; // Добавляем заработанные монеты к текущему количеству монет

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
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
    public void OnCheckpointReached(GameObject car, int checkpointIndex)
    {
        if (!raceStarted) return;

        // Проверяем, что машина пересекла нужную контрольную точку
        if (carCheckpointIndex.ContainsKey(car) && carCheckpointIndex[car] == checkpointIndex)
        {
            carCheckpointIndex[car]++; // Переход к следующей контрольной точке

            // Если машина пересекла последний чекпоинт, засчитываем круг
            if (carCheckpointIndex[car] >= checkpoints.Length)
            {
                carLaps[car]++;
                if (carCheckpointIndex[car] >= checkpoints.Length)
                {
                    carCheckpointIndex[car] = 0;
                }

                // Обновляем отображение кругов
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
        // Сортируем машины по прогрессу
        List<GameObject> sortedCars = new List<GameObject>(carProgress.Keys);
        sortedCars.Sort((car1, car2) => carProgress[car2].CompareTo(carProgress[car1]));

        // Обновляем текст таблицы лидеров
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
        // Получаем текущий чекпоинт и следующий чекпоинт для машины
        int checkpointIndex = carCheckpointIndex[car];
        Transform currentCheckpoint = checkpoints[checkpointIndex];
        Transform nextCheckpoint = checkpoints[(checkpointIndex + 1) % checkpoints.Length];

        // Рассчитываем расстояние до следующего чекпоинта
        float distanceToNextCheckpoint = Vector3.Distance(car.transform.position, nextCheckpoint.position);

        // Общий прогресс = (количество кругов * большое значение) + (индекс чекпоинта * среднее значение) - (расстояние до следующего чекпоинта)
        // Чем больше значение у круга и чекпоинта, тем важнее прогресс на них в ранжировании
        carProgress[car] = carLaps[car] * checkpoints.Length * 1000 + checkpointIndex * 100 - distanceToNextCheckpoint;
    }

    public void StartRace()
    {
        raceStarted = true;

        startGame.SetActive(false);

        raceStartTime = Time.time;
    }

    // Коррутина для отсчета 3, 2, 1, Go!
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
        countdownDisplay.text = ""; // Очистка текста после "Go!"
        StartRace(); // Запуск гонки
    }
}
