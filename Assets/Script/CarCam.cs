using UnityEngine;

public class CarCam : MonoBehaviour
{
    Transform rootNode;
    Transform car;
    Rigidbody carPhysics;

    [Tooltip("Если скорость автомобиля ниже этого значения, то камера по умолчанию будет смотреть вперед")]
    public float rotationThreshold = 1f;

    [Tooltip("Насколько точно камера следует за положением автомобиля. Чем меньше значение, тем больше камера будет отставать")]
    public float cameraStickiness = 10.0f;

    [Tooltip("Насколько точно камера соответствует вектору скорости автомобиля. " +
        "Чем меньше значение, тем более плавными будут повороты камеры, но слишком большое значение приводит к тому, что вы не можете видеть, куда едете.")]
    public float cameraRotationSpeed = 5.0f;

    [Tooltip("Массив камер для переключения")]
    public Camera[] cameras; // Массив камер для переключения

    public GameObject Endcamera;

    private int currentCamIndex = 0; // Индекс текущей активной камеры

    private bool isLastCameraStatic = false; // Флаг, указывающий, что последняя камера статична

    RaceManager raceManager;

    void Awake()
    {
        rootNode = GetComponent<Transform>();
        car = rootNode.parent.GetComponent<Transform>();
        carPhysics = car.GetComponent<Rigidbody>();
        raceManager = FindObjectOfType<RaceManager>();
        // Убедитесь, что активна только одна камера в начале
        SetActiveCamera(currentCamIndex);
    }

    void Start()
    {
        rootNode.parent = null;
    }
    void FixedUpdate()
    {
        // Управление состоянием камер в зависимости от анимации
        if (raceManager.startAnimation && !raceManager.finishAnimation)
        {
            // Отключаем все камеры, если началась анимация
            ToggleCameras(false);
        }
        else if (!raceManager.startAnimation && raceManager.finishAnimation)
        {
            // Включаем камеры, если анимация завершена
            ToggleCameras(true);
        }

        Quaternion look;

        // Проверка, завершил ли игрок гонку
        if (raceManager.playerFinished)
        {
            ToggleCameras(false);

            Endcamera.SetActive(true);

            rootNode.position = car.position; // Привязать камеру прямо к позиции машины

            // Если машина не движется, по умолчанию смотрим вперед. Предотвращает срабатывание камеры, когда нулевая скорость помещается в Quaternion.LookRotation
            if (carPhysics.velocity.magnitude < rotationThreshold)
                look = Quaternion.LookRotation(car.forward);
            else
                look = Quaternion.LookRotation(carPhysics.velocity.normalized);

            // Поверните камеру в направлении вектора скорости.
            look = Quaternion.Slerp(rootNode.rotation, look, cameraRotationSpeed * Time.fixedDeltaTime);
            rootNode.rotation = look;

            return; // Выходим из метода, чтобы избежать дальнейших операций
        }
        else
        {
            Endcamera.SetActive(false);
            // Перемещает камеру в соответствии с положением автомобиля с учётом cameraStickiness
            rootNode.position = Vector3.Lerp(rootNode.position, car.position, cameraStickiness * Time.fixedDeltaTime);
            // Если машина не движется, по умолчанию смотрим вперед. Предотвращает срабатывание камеры, когда нулевая скорость помещается в Quaternion.LookRotation
            if (carPhysics.velocity.magnitude < rotationThreshold)
                look = Quaternion.LookRotation(car.forward);
            else
                look = Quaternion.LookRotation(carPhysics.velocity.normalized);

            // Поверните камеру в направлении вектора скорости.
            look = Quaternion.Slerp(rootNode.rotation, look, cameraRotationSpeed * Time.fixedDeltaTime);
            rootNode.rotation = look;
        }
    }
    private void ToggleCameras(bool state)
    {
        foreach (var camera in cameras)
        {
            camera.enabled = state;
        }
    }

    void Update()
    {
        // Переключение позиции камеры при нажатии клавиши V
        if (Input.GetKeyDown(KeyCode.V))
        {
            SwitchCamera();
        }
    }
    // Функция для активации камеры по индексу
    void SetActiveCamera(int camIndex)
    {
        // Деактивировать все камеры
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == camIndex); // Активна только камера с текущим индексом
        }

        // Если текущая камера последняя, выключаем cameraStickiness
        isLastCameraStatic = (camIndex == cameras.Length - 1);
    }
    // Функция для переключения камеры
    void SwitchCamera()
    {
        // Переключаем индекс на следующую камеру
        currentCamIndex = (currentCamIndex + 1) % cameras.Length;
        SetActiveCamera(currentCamIndex);
    }
}