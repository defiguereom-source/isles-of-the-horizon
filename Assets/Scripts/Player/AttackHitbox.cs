using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.enabled = false; // empieza desactivada
    }

    public void Enable() => col.enabled = true;
    public void Disable() => col.enabled = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // other.GetComponent<Enemy>()?.TakeDamage(10); esto es en la actu de combate
        }
    }
}