using UnityEngine;

public class MenuSystem : MonoBehaviour
{
    [SerializeField] private string escenaJugar = "Pueblo";

    public void Jugar()
    {
        CargarNivel.NivelCarga(escenaJugar);
    }

    public void Salir()
    {
        Application.Quit();
    }
}