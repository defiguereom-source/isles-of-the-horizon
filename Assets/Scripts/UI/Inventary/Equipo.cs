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

    public void IncluirEquipo(int dinero, Image imagenEquipo)
    {
        if (dinero <= dineroTotal && numeroMaximoObjetos <= 4)
        {
            dineroTotal -= dinero;
            numeroMaximoObjetos++;
            GameObject equipo = GameObject.Instantiate(objetoDeEquipo, Vector2.zero, Quaternion.identity, GameObject.FindGameObjectWithTag("Tienda").transform);
            Image imagen = equipo.GetComponent<Image>();
            imagen.sprite = imagenEquipo.sprite;
            textDinero.text = dineroTotal.ToString();

        }
    }
}