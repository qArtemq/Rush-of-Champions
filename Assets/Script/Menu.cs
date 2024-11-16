using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [Header("Screen")]
    public CanvasGroup splashScreen;
    public CanvasGroup helloScreen;
    public GameObject playScreen;
    public GameObject choiceQuitGame;
    public GameObject openSettings;
    public GameObject mainMenu;
    public GameObject garageMenu;
    public InputField nameInputField; // Поле ввода имени
    public CanvasGroup nameInput; // Поле ввода имени
    public TextMeshProUGUI nameText; // Текст для отображения имени
    public TextMeshProUGUI errorText; // Поле для вывода сообщения об ошибке

    [Header("Players")]
    public int minBots = 0;
    public int maxBots = 4;
    private int selectedBots = 0;
    public TextMeshProUGUI botText;

    [Header("Laps")]
    public int minLaps = 1;
    public int maxLaps = 3;
    private int selectedLaps = 1;
    public TextMeshProUGUI lapText;

    [Header("Maps")]
    public Sprite[] mapImages; // Массив изображений карты в виде спрайтов
    private int selectedMapIndex = 0; // Индекс выбранной карты
    public Image mapPreview; // Текущее изображение карты
    public Image mapRight;   // Изображение справа от текущей карты
    public Image mapLeft;    // Изображение слева от текущей карты
    public TextMeshProUGUI makeMoneyText;
    public int[] mapPrices;
    int totalReward;

    [Header("Camera")]
    public Camera mainCamera;
    public Transform camPosA;
    public Transform camPosB;
    public float cameraMoveDuration = 1f; // Длительность движения камеры

    [Header("Car")]
    public GameObject[] cars; // Массив префабов машин
    private GameObject currentCar; // Текущая отображаемая машина
    private int selectedCarIndex = 0;
    public Transform carDisplayPosition; // Позиция для отображения машины в гараже

    [Header("Color")]
    public List<Color> availableColors; // Список доступных цветов
    private Renderer carRenderer; // Рендерер текущей машины
    private Material carMaterialInstance; // Индивидуальный материал для текущей машины
    private Color selectedColor;

    [Header("Bool")]
    public bool inSettings = false;
    public bool isStartScreen = true;
    private bool inGarage = false;
    private bool inPlayGame = false;
    private bool isHoveringCar = false; // Отслеживаем, находится ли мышь над автомобилем

    [Header("Coin")]
    public GameObject screenMoney;
    public int coins = 0; // Количество монет
    public Text coinText; // UI элемент для отображения монет
    public Text selectButtonText; // Текст на кнопке «Выбрать»

    [Header("Shop")]
    public int[] carPrices; // Цены для каждого автомобиля, первые три установлены в 0
    public CanvasGroup insufficientFundsMessage; // Ссылка на текст сообщения
    private bool[] carPurchased;

    void Start()
    {
        // Инициализация количества машин в массиве carPurchased
        carPurchased = new bool[cars.Length];
        for (int i = 0; i < cars.Length; i++)
        {
            carPurchased[i] = PlayerPrefs.GetInt($"CarPurchased_{i}", 0) == 1;
        }
        coins = PlayerPrefs.GetInt("MakeMoneyFinish");
        UpdateCoinDisplay();
        UpdateSelectButton();
        insufficientFundsMessage.alpha = 0;
        choiceQuitGame.SetActive(false);
        openSettings.SetActive(false);
        mainCamera.gameObject.SetActive(true);
        playScreen.SetActive(false);
        mainCamera.fieldOfView = 50;
        mainMenu.SetActive(false);
        screenMoney.SetActive(false);
        garageMenu.SetActive(false);
        splashScreen.alpha = 1.0f; // Устанавливаем полную видимость
        splashScreen.gameObject.SetActive(true);
        helloScreen.gameObject.SetActive(true);
        helloScreen.alpha = 1.0f;
        nameInput.alpha = 1.0f;
        selectedCarIndex = PlayerPrefs.HasKey("SelectedCarIndex") ? PlayerPrefs.GetInt("SelectedCarIndex") : Random.Range(0, cars.Length);
        selectedColor = PlayerPrefs.HasKey($"CarColor_{selectedCarIndex}_R") ? LoadCarColor(selectedCarIndex) : availableColors[Random.Range(1, availableColors.Count)];
        ShowSelectedCar();
        ApplySavedColor();
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            string savedName = PlayerPrefs.GetString("PlayerName");
            nameText.text = savedName;
            nameText.gameObject.SetActive(true);
            StartCoroutine(FadeOutScreen(nameInput, 0.5f));
            StartCoroutine(FadeOutScreen(helloScreen, 4f));
        }
        else
        {
            nameInput.alpha = 1.0f;
        }
    }


    void Update()
    {
        // Проверяем нажатие клавиши "A"
        if (Input.GetKeyDown(KeyCode.F) && isStartScreen)
        {
            mainMenu.SetActive(true);
            screenMoney.SetActive(true);
            StartCoroutine(FadeOutScreen(splashScreen, 0.5f)); // Начинаем плавное исчезновение
            isStartScreen = false;
        }
        if (Input.GetKeyDown(KeyCode.Return) && !isStartScreen)
        {
            if (choiceQuitGame.activeSelf)
            {
                YesQuitGame();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !isStartScreen)
        {
            if (choiceQuitGame.activeSelf && !inSettings && !inGarage && !inPlayGame)
            {
                CloseQuitGame();
            }
            else if (!choiceQuitGame.activeSelf && !inSettings && !inGarage && !inPlayGame)
            {
                OpenQuitGame();
            }
            if (garageMenu.activeSelf)
            {
                CloseGarageGame();
            }
            if (playScreen.activeSelf)
            {
                ClosePlayGame();
            }
        }
        if (isHoveringCar && Input.GetMouseButton(0))
        {
            float rotationSpeed = 1000f;
            currentCar.transform.Rotate(Vector3.up, -Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime);
        }
        if (playScreen.activeSelf && Input.GetMouseButtonDown(0))
        {
            CheckClickOutsidePlayScreen();
        }
    }
    private void CheckClickOutsidePlayScreen()
    {
        RectTransform playScreenRect = playScreen.GetComponent<RectTransform>();
        Vector2 mousePosition = Input.mousePosition;

        if (!RectTransformUtility.RectangleContainsScreenPoint(playScreenRect, mousePosition, Camera.main))
        {
            ClosePlayGame();
        }
    }
    public void StartGame()
    {
        // Проверка: выбрана ли машина
        if (!PlayerPrefs.HasKey("SelectedCarIndex"))
        {
            errorText.text = "Select a car";
            errorText.gameObject.SetActive(true);
            StartCoroutine(HideErrorText()); // Скрыть сообщение об ошибке через некоторое время
            return; // Прерываем запуск игры, если машина не выбрана
        }

        PlayerPrefs.SetInt("SelectedMapIndex", selectedMapIndex);
        PlayerPrefs.SetInt("SelectedLaps", selectedLaps);
        PlayerPrefs.SetInt("SelectedBots", selectedBots);
        PlayerPrefs.SetInt("SelectedMoney", totalReward);
        PlayerPrefs.Save();

        isStartScreen = true;
        switch (selectedMapIndex)
        {
            case 0:
                SceneManager.LoadScene("BrazilRace");
                break;
            case 1:
                SceneManager.LoadScene("DriftRace");
                break;
            case 2:
                SceneManager.LoadScene("ParisRace");
                break;
            case 3:
                SceneManager.LoadScene("SpaceRace");
                break;

        }
    }
    private IEnumerator HideErrorText()
    {
        yield return new WaitForSeconds(2f); // Ожидаем 2 секунды
        errorText.gameObject.SetActive(false); // Скрываем текст
    }
    public void IncreaseBots()
    {
        selectedBots = Mathf.Clamp(selectedBots + 1, minBots, maxBots);
        botText.text = selectedBots.ToString();
        UpdateSelectMaps();
    }

    public void DecreaseBots()
    {
        selectedBots = Mathf.Clamp(selectedBots - 1, minBots, maxBots);
        botText.text = selectedBots.ToString();
        UpdateSelectMaps();
    }
    public void IncreaseLap()
    {
        selectedLaps = Mathf.Clamp(selectedLaps + 1, minLaps, maxLaps);
        lapText.text = selectedLaps.ToString();
        UpdateSelectMaps();
    }

    public void DecreaseLap()
    {
        selectedLaps = Mathf.Clamp(selectedLaps - 1, minLaps, maxLaps);
        lapText.text = selectedLaps.ToString();
        UpdateSelectMaps();
    }
    public void NextMap()
    {
        selectedMapIndex = (selectedMapIndex + 1) % mapImages.Length;
        UpdateMapPreview();
        UpdateSelectMaps();
    }

    public void PreviousMap()
    {
        selectedMapIndex = (selectedMapIndex - 1 + mapImages.Length) % mapImages.Length;
        UpdateMapPreview();
        UpdateSelectMaps();
    }

    // Метод для обновления изображений карт
    public void UpdateMapPreview()
    {
        mapPreview.sprite = mapImages[selectedMapIndex];
        mapRight.sprite = mapImages[(selectedMapIndex + 1) % mapImages.Length];
        mapLeft.sprite = mapImages[(selectedMapIndex - 1 + mapImages.Length) % mapImages.Length];
    }
    public void PlayGame()
    {
        playScreen.SetActive(true);
        UpdateMapPreview();
        UpdateSelectMaps();
        inPlayGame = true;
    }
    public void ClosePlayGame()
    {
        playScreen.SetActive(false);
        inPlayGame = false;
    }
    private void UpdateSelectMaps()
    {
        int mapPrice = mapPrices[selectedMapIndex];

        // Базовая награда за 1 круг и 1 бота
        int baseReward = mapPrice;

        // Дополнительные деньги за каждый круг и каждого бота
        int lapBonus = 20; // Например, +50 за каждый круг
        int botBonus = 15; // Например, +100 за каждого бота

        // Общая награда с учетом выбранных кругов и ботов
        totalReward = baseReward;

        // Если количество кругов больше 1, добавляем бонус за каждый круг
        if (selectedLaps > 1)
        {
            totalReward += (selectedLaps - 1) * lapBonus;
        }

        // Если количество ботов больше 1, добавляем бонус за каждого бота
        if (selectedBots > 1)
        {
            totalReward += (selectedBots - 1) * botBonus;
        }

        // Обновляем текст награды на экране
        makeMoneyText.text = $"+ {totalReward}";
    }
    private void UpdateSelectButton()
    {
        int carPrice = carPrices[selectedCarIndex];
        if (carPurchased[selectedCarIndex] || carPrice == 0)
        {
            selectButtonText.text = "Select";
        }
        else
        {
            selectButtonText.text = $"{carPrice}";
        }
    }
    private void SaveCarColor(int carIndex, Color selectedColor)
    {
        PlayerPrefs.SetFloat("PlayerCarColorR", selectedColor.r);
        PlayerPrefs.SetFloat("PlayerCarColorG", selectedColor.g);
        PlayerPrefs.SetFloat("PlayerCarColorB", selectedColor.b);
        PlayerPrefs.Save();
    }

    private Color LoadCarColor(int carIndex)
    {
        float r = PlayerPrefs.GetFloat($"CarColor_{carIndex}_R", 1.0f); // Default to white if no value
        float g = PlayerPrefs.GetFloat($"CarColor_{carIndex}_G", 1.0f);
        float b = PlayerPrefs.GetFloat($"CarColor_{carIndex}_B", 1.0f);
        return new Color(r, g, b);
    }
    public void SelectedCar()
    {
        int carPrice = carPrices[selectedCarIndex];

        if (carPrice == 0 || coins >= carPrice || carPurchased[selectedCarIndex])
        {
            if (carPrice > 0 && !carPurchased[selectedCarIndex])
            {
                coins -= carPrice;
                UpdateCoinDisplay();
            }

            // Помечаем машину как купленную
            carPurchased[selectedCarIndex] = true;
            PlayerPrefs.SetInt($"CarPurchased_{selectedCarIndex}", 1);

            PlayerPrefs.SetInt("SelectedCarIndex", selectedCarIndex);
            SaveCarColor(selectedCarIndex, selectedColor);
            PlayerPrefs.Save();

            CloseGarageGame();
        }
        else
        {
            insufficientFundsMessage.alpha = 1;
            StartCoroutine(FadeOutScreen(insufficientFundsMessage, 2f));
        }
        UpdateSelectButton(); // Обновляем кнопку после покупки
    }
    private void ApplySavedColor()
    {
        selectedColor = LoadCarColor(selectedCarIndex);
        if (carMaterialInstance != null)
        {
            carMaterialInstance.color = selectedColor;
        }
    }
    public void ChangeCarColor(int colorIndex)
    {
        selectedColor = availableColors[colorIndex];
        if (carMaterialInstance != null)
        {
            carMaterialInstance.color = selectedColor;
            SaveCarColor(selectedCarIndex, selectedColor); // Сохраните цвет в формате RGB
        }
    }
    public void SetHoveringCar(bool isHovering)
    {
        isHoveringCar = isHovering;
    }
    public void NextCar()
    {
        selectedCarIndex = (selectedCarIndex + 1) % cars.Length;
        ShowSelectedCar();
    }

    public void PreviousCar()
    {
        selectedCarIndex = (selectedCarIndex - 1 + cars.Length) % cars.Length;
        ShowSelectedCar();
    }
    private void ShowSelectedCar()
    {
        if (currentCar != null)
            Destroy(currentCar);

        currentCar = Instantiate(cars[selectedCarIndex], carDisplayPosition.position, Quaternion.identity);
        currentCar.transform.SetParent(carDisplayPosition);

        // Устанавливаем ссылку на скрипт меню в CarHoverDetector
        CarHoverDetector hoverDetector = currentCar.GetComponent<CarHoverDetector>();
        if (hoverDetector != null)
        {
            hoverDetector.menuScript = this;
        }
        // Получаем рендерер машины
        carRenderer = currentCar.GetComponentInChildren<Renderer>();
        if (carRenderer != null)
        {
            // Создаем копию материала для индивидуального изменения цвета
            carMaterialInstance = new Material(carRenderer.material);
            carRenderer.material = carMaterialInstance;

            carMaterialInstance.color = selectedColor; // Применяем текущий цвет
        }
        UpdateSelectButton();
    }
    private IEnumerator FadeOutScreen(CanvasGroup screen, float fadeDuration)
    {
        float startAlpha = screen.alpha; // Текущий уровень альфа
        float time = 0; // Время для расчета прогресса

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            screen.alpha = Mathf.Lerp(startAlpha, 0, time / fadeDuration); // Плавное уменьшение альфа
            yield return null;
        }

        screen.alpha = 0; // Устанавливаем альфа на 0
        screen.blocksRaycasts = false;
    }
    private IEnumerator HideScreen(CanvasGroup screen, float fadeDuration, float wait)
    {
        yield return new WaitForSeconds(wait); // Ожидаем 2 секунды

        float startAlpha = screen.alpha; // Текущий уровень альфа
        float time = 0; // Время для расчета прогресса

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            screen.alpha = Mathf.Lerp(startAlpha, 0, time / fadeDuration); // Плавное уменьшение альфа
            yield return null;
        }

        screen.alpha = 0; // Устанавливаем альфа на 0
        screen.blocksRaycasts = false;
    }

    public void CloseSettings()
    {
        openSettings.SetActive(false);
        inSettings = false;
    }
    public void OpenSettings()
    {
        openSettings.SetActive(true);
        inSettings = true;
    }
    public void OpenGarageGame()
    {
        mainMenu.SetActive(false);
        StartCoroutine(MoveCamera(camPosA, camPosB, 50, 60));
        garageMenu.SetActive(true);
        inGarage = true;
        // Используйте сохраненный индекс или значение по умолчанию 0 при открытии гаража
        selectedCarIndex = PlayerPrefs.HasKey("SelectedCarIndex") ? PlayerPrefs.GetInt("SelectedCarIndex") : 0;
        ShowSelectedCar();
    }
    public void CloseGarageGame()
    {
        mainMenu.SetActive(true);
        StartCoroutine(MoveCamera(camPosB, camPosA, 60, 50));
        garageMenu.SetActive(false);
        inGarage = false;
    }
    private IEnumerator MoveCamera(Transform startPos, Transform endPos, float startFOV, float endFOV)
    {
        float time = 0f;

        while (time < cameraMoveDuration)
        {
            time += Time.deltaTime;
            mainCamera.transform.position = Vector3.Lerp(startPos.position, endPos.position, time / cameraMoveDuration);
            mainCamera.transform.rotation = Quaternion.Lerp(startPos.rotation, endPos.rotation, time / cameraMoveDuration);
            mainCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, time / cameraMoveDuration);
            yield return null;
        }

        mainCamera.transform.position = endPos.position;
        mainCamera.transform.rotation = endPos.rotation;
        mainCamera.fieldOfView = endFOV;
    }
    public void CloseQuitGame()
    {
        choiceQuitGame.SetActive(false);
    }
    public void OpenQuitGame()
    {
        choiceQuitGame.SetActive(true);
    }
    public void NoQuitGame()
    {
        choiceQuitGame.SetActive(false);
    }
    public void YesQuitGame()
    {
        Application.Quit();
    }
    public void OnNameEntered()
    {
        string playerName = nameInputField.text;

        // Save the name
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        nameText.text = playerName; // Display the entered name
        nameText.gameObject.SetActive(true);
        isStartScreen = false;
        StartCoroutine(FadeOutScreen(nameInput, 0.5f));
        StartCoroutine(HideScreen(helloScreen, 2f, 1f));
    }

    public void UpdateCoinDisplay()
    {

        coinText.text = $"{coins}";

        PlayerPrefs.SetInt("MakeMoneyFinish", coins); // Сохраняем обновленное количество монет
        PlayerPrefs.Save();
    }
}
