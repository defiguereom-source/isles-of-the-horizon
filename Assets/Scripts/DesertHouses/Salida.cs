using UnityEngine;

public class SalidaInterior : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject exteriorGeneral;
    public GameObject interiorEspecifico;

    [Header("Posiciones")]
    public Transform puntoSalida;

    private Transform playerRef;
    private bool enCooldown = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !enCooldown)
        {
            playerRef = other.transform;
            Salir();
        }
    }

    void Salir()
    {
        enCooldown = true;
        playerRef.position = puntoSalida.position;
        interiorEspecifico.SetActive(false);        
        exteriorGeneral.SetActive(true);          
        Invoke("ResetCooldown", 0.5f);
    }

    void ResetCooldown() => enCooldown = false;
}