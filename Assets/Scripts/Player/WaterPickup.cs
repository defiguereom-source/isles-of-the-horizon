using UnityEngine;

public class WaterPickup : MonoBehaviour
{
    [SerializeField] private int curacion = 10; 

    private void OnMouseDown()
    {
        if (PlayerHealth.Instance == null) return;

        PlayerHealth.Instance.Heal(curacion);
        Destroy(gameObject);
    }
}