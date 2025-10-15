using UnityEngine;

public class CarCam : MonoBehaviour
{
    Transform rootNode;
    Transform car;
    Rigidbody carPhysics;

    public float rotationThreshold = 1f;

    public float cameraStickiness = 10.0f;

    public float cameraRotationSpeed = 5.0f;

    public Camera[] cameras;

    public GameObject Endcamera;

    private int currentCamIndex = 0;

    private bool isLastCameraStatic = false;

    RaceManager raceManager;

    void Awake()
    {
        rootNode = GetComponent<Transform>();
        car = rootNode.parent.GetComponent<Transform>();
        carPhysics = car.GetComponent<Rigidbody>();
        raceManager = FindObjectOfType<RaceManager>();
        SetActiveCamera(currentCamIndex);
    }

    void Start()
    {
        rootNode.parent = null;
    }
    void FixedUpdate()
    {
        if (raceManager.startAnimation && !raceManager.finishAnimation)
        {
            ToggleCameras(false);
        }
        else if (!raceManager.startAnimation && raceManager.finishAnimation)
        {
            ToggleCameras(true);
        }

        Quaternion look;

        if (raceManager.playerFinished)
        {
            ToggleCameras(false);

            Endcamera.SetActive(true);

            rootNode.position = car.position;

            if (carPhysics.linearVelocity.magnitude < rotationThreshold)
                look = Quaternion.LookRotation(car.forward);
            else
                look = Quaternion.LookRotation(carPhysics.linearVelocity.normalized);

            look = Quaternion.Slerp(rootNode.rotation, look, cameraRotationSpeed * Time.fixedDeltaTime);
            rootNode.rotation = look;

            return;
        }
        else
        {
            Endcamera.SetActive(false);
            rootNode.position = Vector3.Lerp(rootNode.position, car.position, cameraStickiness * Time.fixedDeltaTime);
            if (carPhysics.linearVelocity.magnitude < rotationThreshold)
                look = Quaternion.LookRotation(car.forward);
            else
                look = Quaternion.LookRotation(carPhysics.linearVelocity.normalized);

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
        if (Input.GetKeyDown(KeyCode.V))
        {
            SwitchCamera();
        }
    }
    void SetActiveCamera(int camIndex)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == camIndex);
        }

        isLastCameraStatic = (camIndex == cameras.Length - 1);
    }
    void SwitchCamera()
    {
        currentCamIndex = (currentCamIndex + 1) % cameras.Length;
        SetActiveCamera(currentCamIndex);
    }
}