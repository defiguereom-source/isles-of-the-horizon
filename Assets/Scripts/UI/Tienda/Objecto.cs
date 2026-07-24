using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class Objecto : MonoBehaviour
{
    [SerializeField] Image imagenObject;
    [SerializeField] TextMeshProUGUI textObject;
    [SerializeField] TextMeshProUGUI precioObject;
    [SerializeField] Button botonComprar;

    private int precio;
    private Equipo equipo;

    private void Awake()
    {
        if (botonComprar != null)
        {
            botonComprar.onClick.AddListener(ComprarObjecto);
        }
    }

    public void SetEquipo(Equipo equipoRef)
    {
        equipo = equipoRef;
    }

    public void CrearObject(Tienda datosObject)
    {
        precio = datosObject.PrecioObject;
        imagenObject.sprite = datosObject.imagenObject;
        textObject.text = datosObject.textObject;
        precioObject.text = datosObject.PrecioObject.ToString();
    }

    public void ComprarObjecto()
    {
        if (equipo == null)
        {
            Debug.LogWarning("Equipo no asignado en Objecto.");
            return;
        }
        equipo.IncluirEquipo(precio, imagenObject);
    }
}