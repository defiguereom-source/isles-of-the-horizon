using UnityEngine;
using UnityEngine.InputSystem;

public class NPCTienda : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject canvasTienda;
    [SerializeField] private GameObject canvasInventario;

    [Header("Detección de jugador")]
    private bool jugadorCerca = false;
    private bool tiendaAbierta = false;

    private void Start()
    {
        if (canvasTienda != null)
            canvasTienda.SetActive(false);

        if (canvasInventario != null)
            canvasInventario.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (jugadorCerca && !tiendaAbierta && Keyboard.current.fKey.wasPressedThisFrame)
        {
            AbrirTienda();
        }

        if (tiendaAbierta && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CerrarTienda();
        }
    }

    private void AbrirTienda()
    {
        if (canvasTienda != null)
            canvasTienda.SetActive(true);

        if (canvasInventario != null)
            canvasInventario.SetActive(true);

        tiendaAbierta = true;
    }

    public void CerrarTienda()
    {
        if (canvasTienda != null)
            canvasTienda.SetActive(false);

        if (canvasInventario != null)
            canvasInventario.SetActive(false);

        tiendaAbierta = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;

            if (tiendaAbierta)
            {
                CerrarTienda();
            }
        }
    }
}