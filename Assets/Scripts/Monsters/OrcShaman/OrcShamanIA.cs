using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMoverOrcShaman))]
public class OrcShamanIA : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Tiempos de comportamiento normal")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Detección")]
    public float detectionDistance = 6f;   // a partir de aquí "nota" al jugador

    [Header("Rango de hechizo")]
    public float minCastDistance = 2.5f;   // si el jugador está MÁS cerca que esto, retrocede
    public float maxCastDistance = 4.5f;   // si está MÁS lejos que esto, se acerca
    // Entre minCastDistance y maxCastDistance = "zona cómoda", se queda y lanza hechizos

    [Header("Movimiento")]
    public int repositionSteps = 1;        // pasos por reposicionamiento (acercar/alejar)
    public float stepDelay = 0.05f;

    [Header("Ataque")]
    public float attackCooldown = 2f;

    [Header("Vida")]
    public int maxHealth = 2;
    private int currentHealth;

    private GridMoverOrcShaman mover;
    private Coroutine behaviourCoroutine;
    private float lastAttackTime = -999f;

    void Start()
    {
        mover = GetComponent<GridMoverOrcShaman>();
        currentHealth = maxHealth;
        behaviourCoroutine = StartCoroutine(BehaviourRoutine());
    }

    public void TakeDamage(int amount)
    {
        if (mover.IsDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (behaviourCoroutine != null) StopCoroutine(behaviourCoroutine);
        mover.PlayDeath();
        Destroy(gameObject, 2f);
    }

    IEnumerator BehaviourRoutine()
    {
        while (true)
        {
            if (mover.IsDead) yield break;

            float dist = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            // Jugador fuera de rango de detección -> idle/wander
            if (dist > detectionDistance)
            {
                float wait = Random.Range(minWaitTime, maxWaitTime);
                yield return new WaitForSeconds(wait);

                if (mover.IsMoving || mover.IsDead) continue;

                if (Random.value < 0.5f)
                {
                    Direction randomDir = (Direction)Random.Range(0, 4);
                    mover.TryMove(randomDir);
                }
                continue;
            }

            // Jugador demasiado cerca -> retroceder
            if (dist < minCastDistance)
            {
                yield return StartCoroutine(RepositionRoutine(awayFromPlayer: true));
                continue;
            }

            // Jugador demasiado lejos -> acercarse un poco
            if (dist > maxCastDistance)
            {
                yield return StartCoroutine(RepositionRoutine(awayFromPlayer: false));
                continue;
            }

            // Está en la "zona cómoda" -> lanza hechizo
            FaceTowardsPlayer();
            if (Time.time - lastAttackTime >= attackCooldown && !mover.IsMoving)
            {
                lastAttackTime = Time.time;
                bool attackDone = false;
                mover.PlayAttack(() => attackDone = true);
                while (!attackDone) yield return null;
            }
            else
            {
                yield return null;
            }
        }
    }

    IEnumerator RepositionRoutine(bool awayFromPlayer)
    {
        for (int i = 0; i < repositionSteps; i++)
        {
            if (player == null || mover.IsDead) break;

            float dist = Vector2.Distance(transform.position, player.position);

            // Revalida condición en cada paso (puede que ya esté en zona cómoda)
            if (awayFromPlayer && dist >= minCastDistance) break;
            if (!awayFromPlayer && dist <= maxCastDistance) break;

            bool moved = false;
            foreach (Direction dir in GetDirectionsOrdered(awayFromPlayer))
            {
                if (mover.TryMove(dir))
                {
                    moved = true;
                    break;
                }
            }

            if (moved)
            {
                while (mover.IsMoving) yield return null;
                yield return new WaitForSeconds(stepDelay);
            }
            else
            {
                // Bloqueado, no insistas más este frame
                yield return new WaitForSeconds(0.2f);
                break;
            }
        }
    }

    void FaceTowardsPlayer()
    {
        if (player == null) return;
        Vector2 toward = (Vector2)player.position - (Vector2)transform.position;

        Direction dir;
        if (Mathf.Abs(toward.x) > Mathf.Abs(toward.y))
            dir = toward.x > 0 ? Direction.Right : Direction.Left;
        else
            dir = toward.y > 0 ? Direction.Up : Direction.Down;

        mover.facing = dir;

        if (dir == Direction.Left)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (dir == Direction.Right)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    // Si awayFromPlayer=true, devuelve direcciones priorizando ALEJARSE del jugador.
    // Si es false, prioriza ACERCARSE.
    List<Direction> GetDirectionsOrdered(bool awayFromPlayer)
    {
        Vector2 toward = (Vector2)player.position - (Vector2)transform.position;
        if (awayFromPlayer) toward = -toward; // invertimos para alejarnos

        Direction primary, secondary;
        if (Mathf.Abs(toward.x) > Mathf.Abs(toward.y))
        {
            primary = toward.x > 0 ? Direction.Right : Direction.Left;
            secondary = toward.y > 0 ? Direction.Up : Direction.Down;
        }
        else
        {
            primary = toward.y > 0 ? Direction.Up : Direction.Down;
            secondary = toward.x > 0 ? Direction.Right : Direction.Left;
        }

        // Al alejarse, también miramos hacia el jugador para mantener orientación de cara
        if (awayFromPlayer) FaceTowardsPlayer();

        return new List<Direction>
        {
            primary,
            secondary,
            Opposite(secondary),
            Opposite(primary)
        };
    }

    Direction Opposite(Direction d) => d switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => d
    };

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxCastDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minCastDistance);
    }
}