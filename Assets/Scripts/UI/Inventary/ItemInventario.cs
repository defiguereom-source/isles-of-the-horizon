using UnityEngine;
using UnityEngine.EventSystems;

public class ItemInventario : MonoBehaviour, IPointerClickHandler
{
    private bool esConsumible;
    private int curacion;
    private Tienda datos;
    private Equipo equipo;

    public void Configurar(Tienda datosObject, Equipo equipoRef)
    {
        datos = datosObject;
        esConsumible = datosObject.esConsumible;
        curacion = datosObject.curacion;
        equipo = equipoRef;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!esConsumible) return;
        if (PlayerHealth.Instance == null) return;

        PlayerHealth.Instance.Heal(curacion);

        if (equipo != null)
            equipo.RemoverObjeto(datos);

        Destroy(gameObject);
    }
}