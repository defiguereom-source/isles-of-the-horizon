using UnityEngine;
using System.Collections.Generic;

public class AttackHitbox : MonoBehaviour
{
    public int damage = 10;

    private Collider2D col;
    private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.enabled = false; // empieza desactivada
    }

    public void Enable()
    {
        alreadyHit.Clear(); // permite golpear de nuevo en el próximo ataque
        col.enabled = true;
    }

    public void Disable() => col.enabled = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (alreadyHit.Contains(other))
            return;

        if (other.CompareTag("Enemy"))
        {
            OrcIA orc = other.GetComponentInParent<OrcIA>();

            if (orc != null)
            {
                alreadyHit.Add(other);
                orc.ReceiveDamage(damage);
            }
        }
    }
}