using UnityEngine;
using UnityEngine.InputSystem;

public class InventarioUI : MonoBehaviour
{
    [SerializeField] private GameObject panelInventario; 

    private void Start()
    {
        panelInventario.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            panelInventario.SetActive(!panelInventario.activeSelf);
        }
    }
}