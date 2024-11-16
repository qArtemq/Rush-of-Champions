using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMapEntity
{
    public bool showDetails = false;
    public Sprite icon;
    public bool rotateWithObject = true;
    public Vector3 upAxis;
    public float rotation;
    public Vector2 size;
    public bool clampInBorder;
    public float clampDist;
    public List<GameObject> mapObjects;
    // Добавляем новое поле для приоритета
    public int priority = 0;
}

public class MiniMapComponent : MonoBehaviour
{
    [Tooltip("Установите иконку этого игрового объекта")]
    public Sprite icon;
    [Tooltip("Установите размер значка")]
    public Vector2 size = new Vector2(20, 20);
    [Tooltip("Установите true, если иконка вращается вместе с игровым объектом")]
    public bool rotateWithObject = false;
    [Tooltip("Настройте оси вращения в соответствии с вашим игровым объектом. Значения каждой оси могут быть равны -1,0 или 1")]
    public Vector3 upAxis = new Vector3(0, 1, 0);
    [Tooltip("Настройка начального поворота значка")]
    public float initialIconRotation;
    [Tooltip("Если true, то значки будут зажаты в границах")]
    public bool clampIconInBorder = true;
    [Tooltip("Установите расстояние до цели, после которого значок не будет отображаться. Если установить значение 0, значок будет отображаться всегда.")]
    public float clampDistance = 100;

    // Добавляем новое поле для приоритета
    public int priority = 0;

    MiniMapController miniMapController;
    MiniMapEntity mme;
    MapObject mmo;
    RaceManager raceManager;

    // Флаг для отслеживания, зарегистрирован ли объект на миникарте
    bool isRegistered = false;
    void Start()
    {
        raceManager = FindObjectOfType<RaceManager>();
    }
    void Update()
    {
        if (raceManager.finishAnimation && !isRegistered)
        {
            miniMapController = GameObject.Find("CanvasMiniMap").GetComponent<MiniMapController>();

            mme = new MiniMapEntity();
            mme.icon = icon;
            mme.rotation = initialIconRotation;
            mme.size = size;
            mme.upAxis = upAxis;
            mme.rotateWithObject = rotateWithObject;
            mme.clampInBorder = clampIconInBorder;
            mme.clampDist = clampDistance;
            mme.priority = priority; // Присваиваем приоритет

            mmo = miniMapController.RegisterMapObject(this.gameObject, mme);

            isRegistered = true;
        }
    }

    void OnDisable()
    {
        if (miniMapController != null)
        {
            miniMapController.UnregisterMapObject(mmo, this.gameObject);
        }
    }

    void OnDestroy()
    {
        if (miniMapController != null)
        {
            miniMapController.UnregisterMapObject(mmo, this.gameObject);
        }
    }

}
