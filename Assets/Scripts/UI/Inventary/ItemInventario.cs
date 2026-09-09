using UnityEngine;
using UnityEngine.EventSystems;

public class ItemInventario : MonoBehaviour, IPointerClickHandler
{
    private bool esConsumible;
    private int curacion;
    private Equipo equipo;

    public void Configurar(Tienda datos, Equipo equipoRef)
    {
        esConsumible = datos.esConsumible;
        curacion = datos.curacion;
        equipo = equipoRef;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!esConsumible) return;
        if (PlayerHealth.Instance == null) return;

        PlayerHealth.Instance.Heal(curacion);

        if (equipo != null)
            equipo.RemoverObjeto();

        Destroy(gameObject);
    }
}