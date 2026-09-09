using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Equipo : MonoBehaviour
{
    [SerializeField] private int dineroTotal = 0;
    [SerializeField] TextMeshProUGUI textDinero;
    [SerializeField] GameObject objetoDeEquipo;

    private int numeroMaximoObjetos = 0;

    void Start()
    {
        textDinero.text = dineroTotal.ToString();
    }

    public void IncluirEquipo(int dinero, Tienda datosObject)
    {
        if (dinero <= dineroTotal && numeroMaximoObjetos <= 4)
        {
            dineroTotal -= dinero;
            numeroMaximoObjetos++;

            GameObject equipoInstanciado = GameObject.Instantiate(
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

            textDinero.text = dineroTotal.ToString();
        }
    }

    public void RemoverObjeto()
    {
        numeroMaximoObjetos = Mathf.Max(0, numeroMaximoObjetos - 1);
    }
}