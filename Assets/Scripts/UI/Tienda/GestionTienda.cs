using UnityEngine;

public class GestionTienda : MonoBehaviour
{
    [SerializeField] GameObject prefabObjectTienda;
    [SerializeField] int numeroMaximoObject;
    [SerializeField] Tienda[] listaTienda;
    [SerializeField] Transform contenedorTienda;
    [SerializeField] Equipo equipo; 

    private Objecto objeto;

    private void Start()
    {
        for (int i = 0; i < numeroMaximoObject; i++)
        {
            GameObject tienda = Instantiate(prefabObjectTienda, contenedorTienda);
            int indice = Random.Range(0, listaTienda.Length);
            objeto = tienda.GetComponent<Objecto>();
            objeto.CrearObject(listaTienda[indice]);
            objeto.SetEquipo(equipo); 
        }
    }
}