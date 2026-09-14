using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    [Header("UI (arrastra los objetos del Canvas aquí)")]
    [SerializeField] private Slider barraProgreso;      // opcional, puede quedar null
    [SerializeField] private TMP_Text textoProgreso;    // objeto "Cargando" con TextMeshPro - UGUI

    [Header("Config")]
    [SerializeField] private float tiempoMinimoCarga = 1.5f; // evita parpadeos en cargas muy rápidas

    private void Start()
    {
        if (string.IsNullOrEmpty(CargarNivel.siguienteScene))
        {
            Debug.LogWarning("No se definió ninguna escena destino. Volviendo al menú.");
            return;
        }

        StartCoroutine(CargarEscenaAsync(CargarNivel.siguienteScene));
    }

    private IEnumerator CargarEscenaAsync(string nombreEscena)
    {
        float tiempoInicio = Time.time;

        AsyncOperation operacion = SceneManager.LoadSceneAsync(nombreEscena);
        operacion.allowSceneActivation = false; // esperamos nosotros antes de activar

        while (!operacion.isDone)
        {
            // operacion.progress va de 0 a 0.9 mientras carga, y salta a 1 al activar
            float progresoReal = Mathf.Clamp01(operacion.progress / 0.9f);

            if (barraProgreso != null)
                barraProgreso.value = progresoReal;

            if (textoProgreso != null)
                textoProgreso.text = $"Cargando... {Mathf.RoundToInt(progresoReal * 100f)}%";

            bool tiempoMinimoCumplido = Time.time - tiempoInicio >= tiempoMinimoCarga;

            if (operacion.progress >= 0.9f && tiempoMinimoCumplido)
            {
                operacion.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}