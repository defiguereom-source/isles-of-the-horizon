using UnityEngine;
using UnityEngine.SceneManagement;

public static class CargarNivel
{
    // Nombre de la escena real a la que queremos ir DESPUÉS del loading        
    public static string siguienteScene;

    // Nombre fijo de tu escena de carga
    private const string LOADING_SCENE = "LoadingScene";

    // Llama a este método desde cualquier botón o trigger:
    // CargarNivel.NivelCarga("Nivel1");
    public static void NivelCarga(string nombre)
    {
        siguienteScene = nombre;
        SceneManager.LoadScene(LOADING_SCENE);
    }
}