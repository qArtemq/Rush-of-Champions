using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityStandardAssets.Utility;
using UnityEngine.SceneManagement;

#pragma warning disable 649
namespace UnityStandardAssets.Vehicles.Car
{
    internal enum CarDriveType
    {
        FrontWheelDrive,
        RearWheelDrive,
        FourWheelDrive
    }

    internal enum SpeedType
    {
        MPH,
        KPH
    }

    public class CarController : MonoBehaviour
    {
        [SerializeField] private CarDriveType m_CarDriveType = CarDriveType.FourWheelDrive;
        [SerializeField] private WheelCollider[] m_WheelColliders = new WheelCollider[4];
        [SerializeField] private GameObject[] m_WheelMeshes = new GameObject[4];
        [SerializeField] private WheelEffects[] m_WheelEffects = new WheelEffects[4];
        [SerializeField] private Vector3 m_CentreOfMassOffset;
        [SerializeField] private float m_MaximumSteerAngle;
        [Range(0, 1)][SerializeField] private float m_SteerHelper; // 0 is raw physics , 1 the car will grip in the direction it is facing
        [Range(0, 1)][SerializeField] private float m_TractionControl; // 0 is no traction control, 1 is full interference
        [SerializeField] private float m_FullTorqueOverAllWheels;
        [SerializeField] private float m_ReverseTorque;
        [SerializeField] private float m_MaxHandbrakeTorque;
        [SerializeField] private float m_Downforce = 100f;
        [SerializeField] private SpeedType m_SpeedType;
        [SerializeField] private float m_Topspeed = 200;
        [SerializeField] private static int NoOfGears = 5;
        [SerializeField] private float m_RevRangeBoundary = 1f;
        [SerializeField] private float m_SlipLimit;
        [SerializeField] private float m_BrakeTorque;

        private Quaternion[] m_WheelMeshLocalRotations;
        private Vector3 m_Prevpos, m_Pos;
        private float m_SteerAngle;
        private int m_GearNum;
        private float m_GearFactor;
        private float m_OldRotation;
        private float m_CurrentTorque;
        private Rigidbody m_Rigidbody;
        private const float k_ReversingThreshold = 0.01f;

        public bool Skidding { get; private set; }
        public float BrakeInput { get; private set; }
        public float CurrentSteerAngle { get { return m_SteerAngle; } }
        public float CurrentSpeed
        {
            get
            {
                float speed = m_Rigidbody.velocity.magnitude * 2.23693629f; // Конвертируем в MPH
                                                                            // Проверяем угол между направлением движения и направлением вперёд автомобиля
                if (Vector3.Dot(transform.forward, m_Rigidbody.velocity) < 0)
                {
                    speed = -speed; // Устанавливаем отрицательное значение скорости при движении назад
                }
                return speed;
            }
        }
        public float MaxSpeed { get { return m_Topspeed; } }
        public float Revs { get; private set; }
        public float AccelInput { get; private set; }


        private RaceManager raceManager;
        public bool isPlayerCar; // Устанавливаем этот флаг только для машины игрока

        [Header("Speedometer")]
        [SerializeField] public GameObject speedometer;
        [SerializeField] private RectTransform speedometerNeedle;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI gearText; // Текст для отображения передачи
        [SerializeField] private float maxNeedleAngle = -260f; // Угол для нулевой скорости
        [SerializeField] private float minNeedleAngle = -10f;  // Угол для максимальной скорости
        [SerializeField] private float midNeedleAngle = -135f; // Позиция стрелки для переключения
        [SerializeField] private float needleReturnSpeed = 2f; // Скорость плавного возвращения
        private float targetNeedleRotation;
        private bool isReversing = false;
        private bool speedometerUpdated = false;

        [Header("Nitro")]
        [SerializeField] private float maxNitro = 100f; // Максимальный уровень нитро
        [SerializeField] private float nitroDepletionRate = 20f; // Скорость расхода нитро при ускорении
        [SerializeField] private float nitroRegenerationRate = 10f; // Скорость восстановления нитро
        [SerializeField] private float nitroBoostAmount = 50f; // Устанавливаем силу ускорения от нитро
        [SerializeField] private Image nitroIndicator; // UI элемент для индикатора нитро
        private float currentNitro; // Полный индикатор нитро в начале
        [SerializeField] private ParticleSystem[] nitro;
        [SerializeField] private AudioSource nitroSound;

        [Header("Lights")]
        [SerializeField] private GameObject[] backLights; // Объекты для включения при движении назад или остановке
        [SerializeField] private GameObject[] frontLights; // Объекты, которые включаются и отключаются по нажатию L
        private bool isLActive = false; // Флаг для состояния объектов, управляемых клавишей L

        [Header("Exhaust")]
        [SerializeField] private AudioSource idleRevSound; // Звук для состояния, когда машина "хочет ехать"
        [SerializeField] private AudioSource turbo_CL; // звук турбины
        [SerializeField] private ParticleSystem[] turboEffect; // еффект турбины
        [SerializeField] private GameObject light_B;
        bool isTryingToMove = false;
        bool isTurbo = true;


        [Header("Collision Sound")]
        [SerializeField] private AudioSource collisionSound; // Звук при столкновении
        private MeshCollider meshCollider;

        // Объявим флаг, чтобы убедиться, что код выполнится только один раз
        private bool hasFinishedSetup = false;

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (raceManager == null)
            {
                raceManager = FindObjectOfType<RaceManager>();
            }
        }
        // Use this for initialization
        private void Start()
        {
            meshCollider = GetComponentInChildren<MeshCollider>();
            currentNitro = maxNitro;
            m_WheelMeshLocalRotations = new Quaternion[4];
            for (int i = 0; i < 4; i++)
            {
                m_WheelMeshLocalRotations[i] = m_WheelMeshes[i].transform.localRotation;
            }
            m_WheelColliders[0].attachedRigidbody.centerOfMass = m_CentreOfMassOffset;

            m_MaxHandbrakeTorque = float.MaxValue;

            m_CurrentTorque = m_FullTorqueOverAllWheels - (m_TractionControl * m_FullTorqueOverAllWheels);

            targetNeedleRotation = minNeedleAngle;
            if (isPlayerCar)
            {
                idleRevSound.loop = true; // Установим звук в режиме "зациклено"}
            }
            // Проверяем, активна ли сцена "SpaceRace" при запуске
            if (SceneManager.GetActiveScene().name == "BrazilRace" && !isPlayerCar)
            {
                m_Topspeed = 140;
            }
            if (SceneManager.GetActiveScene().name == "SpaceRace" && !isPlayerCar)
            {
                m_Topspeed = 150;
            }
            if (SceneManager.GetActiveScene().name == "ParisRace" && !isPlayerCar)
            {
                m_Topspeed = 150;
            }
            if (SceneManager.GetActiveScene().name == "DriftRace" && !isPlayerCar)
            {
                m_Topspeed = 100;
            }
            if (SceneManager.GetActiveScene().name == "SpaceRace")
            {
                isLActive = true; // Включаем фонари по умолчанию
                SetObjectsActive(frontLights, isLActive);
            }
            else
            {
                isLActive = false;
                SetObjectsActive(frontLights, isLActive);
            }
            if (!isPlayerCar)
            {
                StartCoroutine(AutoActivateNitroEffectsForAI());
            }
        }
        void Update()
        {
            // Логика управления задними фонарями
            bool shouldActivateBackLights = false;

            // Проверка состояния движения назад или остановки
            if (CurrentSpeed < 0 || Input.GetKey(KeyCode.Space)) // Движение назад
            {
                shouldActivateBackLights = true;
            }
            if (isPlayerCar)
            {

                if (!speedometerUpdated)
                {
                    if (!raceManager.finishAnimation)
                    {
                        speedometer.SetActive(false);
                    }
                    else
                    {
                        speedometer.SetActive(true);
                        speedometerUpdated = true;
                    }
                }
                if (raceManager.playerFinished && !hasFinishedSetup)
                {
                    // Остановка нитро-эффектов
                    StopNitroEffects();

                    Destroy(gameObject.GetComponent<CarUserControl>());
                    gameObject.GetComponent<WaypointProgressTracker>().enabled = true;
                    gameObject.GetComponent<CarAIControl>().enabled = true;
                    speedometer.SetActive(false);
                    nitroSound.maxDistance = 50;
                    idleRevSound.maxDistance = 50;
                    turbo_CL.maxDistance = 50;
                    collisionSound.maxDistance = 50;
                    if (SceneManager.GetActiveScene().name == "DriftRace" && isPlayerCar)
                    {
                        m_Topspeed = 100;
                    }
                    isPlayerCar = false;

                    hasFinishedSetup = true;
                }
                // Проверка состояния остановки и попытки движения
                if (Input.GetKey(KeyCode.W))
                {
                    if (Mathf.Abs(CurrentSpeed) < 0.1f) // Проверка, что скорость близка к нулю
                    {
                        if (!isTryingToMove)
                        {
                            isTurbo = true;
                            idleRevSound.Play();
                            StartCoroutine(TurbinaSound(1f));
                            isTryingToMove = true;

                        }
                    }
                    else
                    {
                        if (isTryingToMove)
                        {
                            idleRevSound.Stop();
                            isTryingToMove = false;
                            isTurbo = false;
                        }
                    }
                }
                else
                {
                    if (isTryingToMove)
                    {
                        idleRevSound.Stop();
                        isTryingToMove = false;
                        isTurbo = false;
                    }
                }

                // Логика для включения и отключения объектов при нажатии клавиши L
                if (Input.GetKeyDown(KeyCode.L))
                {
                    isLActive = !isLActive; // Переключаем состояние
                    SetObjectsActive(frontLights, isLActive);
                }
                if (Input.GetKeyDown(KeyCode.R) && raceManager.raceStarted) // Проверяем, что это машина игрока
                {
                    raceManager.ResetCarPosition(gameObject);
                }

                if (Input.GetKey(KeyCode.Q) && currentNitro > 0 && raceManager.raceStarted)
                {
                    ActivateNitro();
                    shouldActivateBackLights = true; // Оставляем фонари включенными
                }
                else
                {
                    RegenerateNitro();
                }

                UpdateSpeedometer();
            }
            // Устанавливаем состояние задних фонарей на основе собранных условий
            SetObjectsActive(backLights, shouldActivateBackLights);
        }
        private void StartNitroEffects()
        {
            if (nitro.Length > 0)
            {
                foreach (var effect in nitro)
                {
                    effect.Play();
                }
            }
        }
        private void StopNitroEffects()
        {
            foreach (var effect in nitro)
            {
                effect.Stop();
            }
            nitroSound.Stop();
        }
        private IEnumerator AutoActivateNitroEffectsForAI()
        {
            while (true)
            {
                // Задаем случайное время ожидания перед активацией эффектов нитро
                float waitTime = UnityEngine.Random.Range(3f, 10f); // Ожидание между активациями от 3 до 10 секунд
                yield return new WaitForSeconds(waitTime);

                // Определяем случайную длительность для активации эффектов нитро
                float nitroEffectDuration = UnityEngine.Random.Range(1f, 5f); // Длительность эффектов от 1 до 5 секунд
                float startTime = Time.time;

                // Включаем эффекты частиц и звук нитро
                if (nitro != null && nitro.Length > 0)
                {
                    foreach (var effect in nitro) effect.Play();
                }
                nitroSound.Play();

                // Ждем указанное время, оставляя эффекты включенными
                while (Time.time - startTime < nitroEffectDuration)
                {
                    yield return null; // Ждем один кадр
                }

                // Останавливаем эффекты частиц и звук нитро
                if (nitro != null && nitro.Length > 0)
                {
                    foreach (var effect in nitro) effect.Stop();
                }
                nitroSound.Stop();
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            // Проверка силы удара, чтобы избежать эффекта на легкие касания
            if (collision.relativeVelocity.magnitude > 2f) // Порог силы удара
            {
                collisionSound.PlayOneShot(collisionSound.clip);
            }
        }

        IEnumerator TurbinaSound(float time)
        {
            while (isTurbo)
            {
                yield return new WaitForSeconds(time);
                Turbina();
            }
        }

        private void Turbina()
        {
            if (isPlayerCar)
            {
                // Включаем свет и эффект турбины
                light_B.SetActive(true);

                // Проигрываем звук турбины
                turbo_CL.PlayOneShot(turbo_CL.clip, 3f);

                // Включаем эффекты частиц
                foreach (var effect in turboEffect)
                {
                    effect.Play();
                }
                StartCoroutine(DeactivateLightB(0.01f));
            }
        }
        private IEnumerator DeactivateLightB(float delay)
        {
            yield return new WaitForSeconds(delay);
            light_B.SetActive(false);
        }

        private void SetObjectsActive(GameObject[] objects, bool isActive)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    obj.SetActive(isActive);
                }
            }
        }
        private void ActivateNitro()
        {
            currentNitro -= nitroDepletionRate * Time.deltaTime;
            // Увеличение силы ускорения, добавив к текущей скорости
            float nitroSpeedBoost = Mathf.Clamp(nitroBoostAmount, 0, maxNitro);
            m_Rigidbody.AddForce(transform.forward * nitroSpeedBoost, ForceMode.Acceleration);

            // Обновление индикатора нитро
            nitroIndicator.fillAmount = currentNitro / maxNitro;

            // Активируем эффекты нитро и ускорения, если они не активны
            if (!nitro[0].isPlaying)
            {
                foreach (var effect in nitro) effect.Play();
                nitroSound.Play();
            }

            // Прерываем восстановление нитро, если оно идет
            if (currentNitro < 0) currentNitro = 0;
        }
        private void RegenerateNitro()
        {
            if (currentNitro < maxNitro)
            {
                currentNitro += nitroRegenerationRate * Time.deltaTime;
                nitroIndicator.fillAmount = currentNitro / maxNitro;
            }

            // Останавливаем эффекты нитро и ускорения, если они активны
            StopNitroEffects();
        }
        private float GetGearSpeedFraction()
        {
            // Вычисляем максимальную скорость для текущей передачи
            float maxGearSpeed = MaxSpeed / NoOfGears * (m_GearNum + 1);
            float minGearSpeed = MaxSpeed / NoOfGears * m_GearNum;

            // Нормализуем текущую скорость относительно диапазона текущей передачи
            return Mathf.InverseLerp(minGearSpeed, maxGearSpeed, CurrentSpeed);
        }

        private void UpdateSpeedometer()
        {
            // Используем отношение скорости в пределах текущей передачи
            float gearSpeedFraction = GetGearSpeedFraction();

            if (isTryingToMove)
            {
                float jitter = UnityEngine.Random.Range(-220f, -250f);
                float idleNeedleRotation = maxNeedleAngle + jitter;
                speedometerNeedle.localEulerAngles = new Vector3(0, 0, idleNeedleRotation);
            }
            else
            {

                // Если автомобиль начинает двигаться назад, то постепенно переключаем передачи до "R"
                if (Input.GetKey(KeyCode.S) && CurrentSpeed < 1)
                {
                    isReversing = true;
                    if (m_GearNum > 0)
                    {
                        m_GearNum--; // Переключаем на более низкую передачу
                    }
                    else
                    {
                        gearText.text = "r"; // Когда доходим до "0", показываем "R"
                    }
                }
                else if (CurrentSpeed > 1)
                {
                    isReversing = false;
                }

                // Обновление текстового отображения передачи
                if (!isReversing)
                {
                    gearText.text = (m_GearNum + 1).ToString(); // Обновление текущей передачи при движении вперёд
                }

                if (m_GearNum == 0)
                {
                    targetNeedleRotation = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, gearSpeedFraction);
                }
                else
                {
                    targetNeedleRotation = Mathf.Lerp(midNeedleAngle, maxNeedleAngle, gearSpeedFraction);
                }

                // Плавное обновление положения стрелки
                float currentNeedleRotation = speedometerNeedle.localEulerAngles.z;
                float smoothNeedleRotation = Mathf.LerpAngle(currentNeedleRotation, targetNeedleRotation, Time.deltaTime * needleReturnSpeed);
                speedometerNeedle.localEulerAngles = new Vector3(0, 0, smoothNeedleRotation);

                // Обновляем текст скорости с форматированием трехзначного числа
                speedText.text = Mathf.Abs(Mathf.RoundToInt(CurrentSpeed)).ToString("D3"); // "D3" добавляет ведущие нули
            }

        }

        public AudioSource GearSound;

        private void GearChanging()
        {
            if (isReversing) return; // Пропускаем переключение передач при движении назад

            float f = Mathf.Abs(CurrentSpeed / MaxSpeed);
            float upgearlimit = (1 / (float)NoOfGears) * (m_GearNum + 1);
            float downgearlimit = (1 / (float)NoOfGears) * m_GearNum;

            if (m_GearNum > 0 && f < downgearlimit)
            {
                m_GearNum--;
                if (UnityEngine.Random.value > 0.5f)
                {
                    Turbina();
                }
                GearSound.Play();
            }

            if (f > upgearlimit && (m_GearNum < (NoOfGears - 1)))
            {
                m_GearNum++;
                if (UnityEngine.Random.value > 0.5f)
                {
                    Turbina();
                }
                GearSound.Play();
            }
        }


        // simple function to add a curved bias towards 1 for a value in the 0-1 range
        private static float CurveFactor(float factor)
        {
            return 1 - (1 - factor) * (1 - factor);
        }


        // unclamped version of Lerp, to allow value to exceed the from-to range
        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }


        private void CalculateGearFactor()
        {
            float f = (1 / (float)NoOfGears);
            // Коэффициент передачи - это нормализованное представление текущей скорости в диапазоне скоростей текущей передачи.
            // Мы сглаживаем в направлении «целевого» коэффициента передачи, чтобы обороты не скакали вверх или вниз при переключении передачи.
            var targetGearFactor = Mathf.InverseLerp(f * m_GearNum, f * (m_GearNum + 1), Mathf.Abs(CurrentSpeed / MaxSpeed));
            m_GearFactor = Mathf.Lerp(m_GearFactor, targetGearFactor, Time.deltaTime * 5f);
        }


        private void CalculateRevs()
        {
            // calculate engine revs (for display / sound)
            // (this is done in retrospect - revs are not used in force/power calculations)
            CalculateGearFactor();
            var gearNumFactor = m_GearNum / (float)NoOfGears;
            var revsRangeMin = ULerp(0f, m_RevRangeBoundary, CurveFactor(gearNumFactor));
            var revsRangeMax = ULerp(m_RevRangeBoundary, 1f, gearNumFactor);
            Revs = ULerp(revsRangeMin, revsRangeMax, m_GearFactor);
        }


        public void Move(float steering, float accel, float footbrake, float handbrake)
        {
            if (!raceManager.raceStarted)
            {
                accel = 0; // Останавливаем движение вперёд и назад
            }

            for (int i = 0; i < 4; i++)
            {
                Quaternion quat;
                Vector3 position;
                m_WheelColliders[i].GetWorldPose(out position, out quat);
                m_WheelMeshes[i].transform.position = position;
                m_WheelMeshes[i].transform.rotation = quat;
            }

            //зажим входных значений
            steering = Mathf.Clamp(steering, -1, 1);
            AccelInput = accel = Mathf.Clamp(accel, 0, 1);
            BrakeInput = footbrake = -1 * Mathf.Clamp(footbrake, -1, 0);
            handbrake = Mathf.Clamp(handbrake, 0, 1);

            //Установите рулевое управление на передние колеса.
            //Предполагаем, что колеса 0 и 1 - это передние колеса.
            m_SteerAngle = steering * m_MaximumSteerAngle;
            m_WheelColliders[0].steerAngle = m_SteerAngle;
            m_WheelColliders[1].steerAngle = m_SteerAngle;

            SteerHelper();

            if (raceManager.raceStarted)
            {
                ApplyDrive(accel, footbrake);
            }

            CapSpeed();

            //Установите ручной тормоз.
            //Предполагаем, что колеса 2 и 3 - это задние колеса.
            if (handbrake >= 0f)
            {
                var hbTorque = handbrake * m_MaxHandbrakeTorque;
                m_WheelColliders[2].brakeTorque = hbTorque;
                m_WheelColliders[3].brakeTorque = hbTorque;
            }

            if (handbrake <= 0f)
            {
                // Убираем тормозной момент с задних колес при отпускании пробела
                m_WheelColliders[2].brakeTorque = 0;
                m_WheelColliders[3].brakeTorque = 0;
            }


            CalculateRevs();
            GearChanging();

            AddDownForce();
            CheckForWheelSpin();
            TractionControl();
        }


        private void CapSpeed()
        {
            float speed = m_Rigidbody.velocity.magnitude;
            switch (m_SpeedType)
            {
                case SpeedType.MPH:

                    speed *= 2.23693629f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity = (m_Topspeed / 2.23693629f) * m_Rigidbody.velocity.normalized;
                    break;

                case SpeedType.KPH:
                    speed *= 3.6f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity = (m_Topspeed / 3.6f) * m_Rigidbody.velocity.normalized;
                    break;
            }
        }


        private void ApplyDrive(float accel, float footbrake)
        {

            float thrustTorque;
            switch (m_CarDriveType)
            {
                case CarDriveType.FourWheelDrive:
                    thrustTorque = accel * (m_CurrentTorque / 4f);
                    for (int i = 0; i < 4; i++)
                    {
                        m_WheelColliders[i].motorTorque = thrustTorque;
                    }
                    break;

                case CarDriveType.FrontWheelDrive:
                    thrustTorque = accel * (m_CurrentTorque / 2f);
                    m_WheelColliders[0].motorTorque = m_WheelColliders[1].motorTorque = thrustTorque;
                    break;

                case CarDriveType.RearWheelDrive:
                    thrustTorque = accel * (m_CurrentTorque / 2f);
                    m_WheelColliders[2].motorTorque = m_WheelColliders[3].motorTorque = thrustTorque;
                    break;

            }

            for (int i = 0; i < 4; i++)
            {
                if (CurrentSpeed > 5 && Vector3.Angle(transform.forward, m_Rigidbody.velocity) < 50f)
                {
                    m_WheelColliders[i].brakeTorque = m_BrakeTorque * footbrake;
                }
                else if (footbrake > 0)
                {
                    m_WheelColliders[i].brakeTorque = 0f;
                    m_WheelColliders[i].motorTorque = -m_ReverseTorque * footbrake;
                }
            }
        }


        private void SteerHelper()
        {
            for (int i = 0; i < 4; i++)
            {
                WheelHit wheelhit;
                m_WheelColliders[i].GetGroundHit(out wheelhit);
                if (wheelhit.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }

            // this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
            if (Mathf.Abs(m_OldRotation - transform.eulerAngles.y) < 10f)
            {
                var turnadjust = (transform.eulerAngles.y - m_OldRotation) * m_SteerHelper;
                Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
                m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
            }
            m_OldRotation = transform.eulerAngles.y;
        }


        // это используется для увеличения сцепления со скоростью
        private void AddDownForce()
        {
            m_WheelColliders[0].attachedRigidbody.AddForce(-transform.up * m_Downforce *
                                                         m_WheelColliders[0].attachedRigidbody.velocity.magnitude);
        }


        // checks if the wheels are spinning and is so does three things
        // 1) emits particles
        // 2) plays tiure skidding sounds
        // 3) leaves skidmarks on the ground
        // these effects are controlled through the WheelEffects class
        private void CheckForWheelSpin()
        {
            // loop through all wheels
            for (int i = 0; i < 4; i++)
            {
                WheelHit wheelHit;
                m_WheelColliders[i].GetGroundHit(out wheelHit);

                // is the tire slipping above the given threshhold
                if (Mathf.Abs(wheelHit.forwardSlip) >= m_SlipLimit || Mathf.Abs(wheelHit.sidewaysSlip) >= m_SlipLimit)
                {
                    m_WheelEffects[i].EmitTyreSmoke();

                    // avoiding all four tires screeching at the same time
                    // if they do it can lead to some strange audio artefacts
                    if (!AnySkidSoundPlaying())
                    {
                        m_WheelEffects[i].PlayAudio();
                    }
                    continue;
                }

                // if it wasnt slipping stop all the audio
                if (m_WheelEffects[i].PlayingAudio)
                {
                    m_WheelEffects[i].StopAudio();
                }
                // end the trail generation
                m_WheelEffects[i].EndSkidTrail();
            }
        }

        // грубый трекшн-контроль, который снижает мощность на колесах, если они слишком сильно вращаются.
        private void TractionControl()
        {
            WheelHit wheelHit;
            switch (m_CarDriveType)
            {
                case CarDriveType.FourWheelDrive:
                    // loop through all wheels
                    for (int i = 0; i < 4; i++)
                    {
                        m_WheelColliders[i].GetGroundHit(out wheelHit);

                        AdjustTorque(wheelHit.forwardSlip);
                    }
                    break;

                case CarDriveType.RearWheelDrive:
                    m_WheelColliders[2].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[3].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;

                case CarDriveType.FrontWheelDrive:
                    m_WheelColliders[0].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[1].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;
            }
        }


        private void AdjustTorque(float forwardSlip)
        {
            if (forwardSlip >= m_SlipLimit && m_CurrentTorque >= 0)
            {
                m_CurrentTorque -= 10 * m_TractionControl;
            }
            else
            {
                m_CurrentTorque += 10 * m_TractionControl;
                if (m_CurrentTorque > m_FullTorqueOverAllWheels)
                {
                    m_CurrentTorque = m_FullTorqueOverAllWheels;
                }
            }
        }


        private bool AnySkidSoundPlaying()
        {
            for (int i = 0; i < 4; i++)
            {
                if (m_WheelEffects[i].PlayingAudio)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
