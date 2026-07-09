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

        if (!other.CompareTag("Enemy"))
            return;

        // Intenta con cada tipo de enemigo que exista en el juego.
        // Si agregás enemigos nuevos con su propio script, sumá una línea igual a estas.
        OrcIA orc = other.GetComponentInParent<OrcIA>();
        if (orc != null)
        {
            alreadyHit.Add(other);
            orc.ReceiveDamage(damage);
            return;
        }

        SnakeIA snake = other.GetComponentInParent<SnakeIA>();
        if (snake != null)
        {
            alreadyHit.Add(other);
            snake.ReceiveDamage(damage);
            return;
        }

        FroggyIA froggy = other.GetComponentInParent<FroggyIA>();
        if (froggy != null)
        {
            alreadyHit.Add(other);
            froggy.ReceiveDamage(damage);
            return;
        }

        FrogIA frog = other.GetComponentInParent<FrogIA>();
        if (frog != null)
        {
            alreadyHit.Add(other);
            frog.ReceiveDamage(damage);
            return;
        }

        SlimeIA slime = other.GetComponentInParent<SlimeIA>();
        if (slime != null)
        {
            alreadyHit.Add(other);
            slime.ReceiveDamage(damage);
            return;
        }
    }
}