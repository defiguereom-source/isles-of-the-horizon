using UnityEngine;

public class EntradaInterior : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject exteriorGeneral;
    public GameObject interiorEspecifico;

    [Header("Posiciones")]
    public Transform puntoEntrada;

    private Transform playerRef;
    private bool enCooldown = false; // ← ya NO es static

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !enCooldown)
        {
            playerRef = other.transform;
            Entrar();
        }
    }

    void Entrar()
    {
        enCooldown = true;
        exteriorGeneral.SetActive(false);
        interiorEspecifico.SetActive(true);
        playerRef.position = puntoEntrada.position;
        Invoke("ResetCooldown", 1f); // 1f para entrar
    }

    void ResetCooldown() => enCooldown = false;
}