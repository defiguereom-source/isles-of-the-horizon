using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Equipo : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textDinero;
    [SerializeField] GameObject objetoDeEquipo;

    void Start()
    {
        ActualizarUI();
    }

    public void IncluirEquipo(int dinero, Tienda datosObject)
    {
        if (InventoryManager.Instance.AgregarItem(datosObject, dinero))
        {
            InstanciarItem(datosObject);
            textDinero.text = InventoryManager.Instance.dineroTotal.ToString();
        }
    }

    public void RemoverObjeto(Tienda datosObject)
    {
        InventoryManager.Instance.RemoverItem(datosObject);
    }

    private void InstanciarItem(Tienda datosObject)
    {
        GameObject equipoInstanciado = Instantiate(
            objetoDeEquipo,
            Vector2.zero,
            Quaternion.identity,
            GameObject.FindGameObjectWithTag("Tienda").transform
        );

        Image imagen = equipoInstanciado.GetComponent<Image>();
        imagen.sprite = datosObject.imagenObject;

        ItemInventario item = equipoInstanciado.GetComponent<ItemInventario>();
        if (item == null)
            item = equipoInstanciado.AddComponent<ItemInventario>();

        item.Configurar(datosObject, this);
    }

    private void ActualizarUI()
    {
        textDinero.text = InventoryManager.Instance.dineroTotal.ToString();

        // reconstruye los iconos que ya tenías comprados
        foreach (Tienda item in InventoryManager.Instance.itemsInventario)
        {
            InstanciarItem(item);
        }
    }
}