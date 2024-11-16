using UnityEngine;

public class CarHoverDetector : MonoBehaviour
{
    public Menu menuScript; // —сылка на скрипт главного меню

    void Start()
    {
        menuScript = FindObjectOfType<Menu>();
    }
    private void OnMouseEnter()
    {
        menuScript.SetHoveringCar(true);
    }

    private void OnMouseExit()
    {
        menuScript.SetHoveringCar(false);
    }
}
