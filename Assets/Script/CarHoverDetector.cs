using UnityEngine;

public class CarHoverDetector : MonoBehaviour
{
    public Menu menuScript;

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
