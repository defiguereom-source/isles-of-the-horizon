using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMoverOrc))]
public class OrcIA : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Tiempos de comportamiento normal")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Detección de persecución")]
    public float chaseDistance = 5f;
    public float catchDistance = 0.6f;
    public int chaseSteps = 5;
    public float chaseStepDelay = 0.05f;

    [Header("Ataque")]
    public float attackCooldown = 1f;

    [Header("Vida")]
    public int maxHealth = 30;

    private GridMoverOrc mover;
    private int currentHealth;
    private float lastAttackTime = -999f;
    private bool isAttacking;
    private bool isDamaged;
    private bool isDead;

    void Awake()
    {
        // Si no fue asignado manualmente, busca al jugador por su Tag.
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogWarning("OrcIA: No se encontró ningún objeto con el Tag Player.");
            }
        }
    }

    void Start()
    {
        mover = GetComponent<GridMoverOrc>();
        currentHealth = maxHealth;
        StartCoroutine(BehaviourRoutine());
    }

    IEnumerator BehaviourRoutine()
    {
        while (true)
        {
            if (isDead)
            {
                yield break;
            }

            if (isAttacking || isDamaged)
            {
                yield return null;
                continue;
            }

            float distToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            if (distToPlayer <= catchDistance)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    yield return StartCoroutine(DoAttack());
                }
                else
                {
                    yield return null;
                }

                continue;
            }

            if (distToPlayer < chaseDistance)
            {
                yield return StartCoroutine(ChaseRoutine());
                continue;
            }

            float wait = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(wait);

            if (mover.IsMoving)
                continue;

            if (Random.value < 0.5f)
            {
                Direction randomDir = (Direction)Random.Range(0, 4);
                mover.TryMove(randomDir);
            }
        }
    }

    IEnumerator ChaseRoutine()
    {
        for (int i = 0; i < chaseSteps; i++)
        {
            if (player == null || isDead)
                break;

            float dist = Vector2.Distance(transform.position, player.position);

            if (dist <= catchDistance || dist > chaseDistance)
                break;

            bool moved = false;

            foreach (Direction dir in GetChaseDirectionsOrdered())
            {
                if (mover.TryMove(dir, isChasing: true))
                {
                    moved = true;
                    break;
                }
            }

            if (moved)
            {
                while (mover.IsMoving)
                    yield return null;

                yield return new WaitForSeconds(chaseStepDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        Direction dirToPlayer = GetDirectionTowardsPlayer();
        bool finished = false;

        mover.PlayAttack(dirToPlayer, () => finished = true);

        while (!finished)
            yield return null;

        isAttacking = false;
    }

    public void ReceiveDamage(int amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (!isDamaged)
        {
            StartCoroutine(DamageFlow());
        }
    }

    IEnumerator DamageFlow()
    {
        isDamaged = true;
        yield return new WaitForSeconds(0.3f);
        isDamaged = false;
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        StopAllCoroutines();
        mover.PlayDeath();
    }

    Direction GetDirectionTowardsPlayer()
    {
        if (player == null)
            return mover.facing;

        Vector2 toward = (Vector2)player.position - (Vector2)transform.position;

        if (Mathf.Abs(toward.x) > Mathf.Abs(toward.y))
            return toward.x > 0 ? Direction.Right : Direction.Left;

        return toward.y > 0 ? Direction.Up : Direction.Down;
    }

    List<Direction> GetChaseDirectionsOrdered()
    {
        Vector2 toward = (Vector2)player.position - (Vector2)transform.position;

        Direction primary;
        Direction secondary;

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
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}