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
    public float attackCooldown = 1f;   // pausa entre cada golpe del combo
    public int attackDamage = 10;
    public int comboHits = 3;           // cuántos golpes por combo
    public float comboRestTime = 2f;    // pausa después de completar el combo

    [Header("Vida")]
    public int maxHealth = 30;

    private GridMoverOrc mover;
    private int currentHealth;
    private float lastComboEndTime = -999f;
    private bool isAttacking;
    private bool isDamaged;
    private bool isDead;

    void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            else
                Debug.LogWarning("OrcIA: No se encontró ningún objeto con el Tag Player.");
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
            if (isDead) yield break;

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
                // Esperar a que termine el descanso del combo
                if (Time.time - lastComboEndTime >= comboRestTime)
                    yield return StartCoroutine(DoCombo());
                else
                    yield return null;

                continue;
            }

            if (distToPlayer < chaseDistance)
            {
                yield return StartCoroutine(ChaseRoutine());
                continue;
            }

            float wait = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(wait);

            if (mover.IsMoving) continue;

            if (Random.value < 0.5f)
            {
                Direction randomDir = (Direction)Random.Range(0, 4);
                mover.TryMove(randomDir);
            }
        }
    }

    IEnumerator DoCombo()
    {
        isAttacking = true;

        for (int i = 0; i < comboHits; i++)
        {
            if (isDead) break;

            // Verificar que el jugador sigue en rango antes de cada golpe
            if (player == null) break;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist > catchDistance) break;

            yield return StartCoroutine(DoAttack());

            // Pausa entre golpes (excepto después del último)
            if (i < comboHits - 1)
                yield return new WaitForSeconds(attackCooldown);
        }

        lastComboEndTime = Time.time;
        isAttacking = false;
    }

    IEnumerator DoAttack()
    {
        Direction dirToPlayer = GetDirectionTowardsPlayer();
        bool finished = false;

        mover.PlayAttack(dirToPlayer, () => finished = true);

        while (!finished) yield return null;

        if (player != null)
        {
            float distCheck = Vector2.Distance(transform.position, player.position);
            if (distCheck <= catchDistance)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                    ph.TakeDamage(attackDamage);
                else
                    Debug.LogWarning("OrcIA: El Player no tiene un componente PlayerHealth.");
            }
        }
    }

    public void ReceiveDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
            Die();
        else if (!isDamaged)
            StartCoroutine(DamageFlow());
    }

    IEnumerator DamageFlow()
    {
        isDamaged = true;
        yield return new WaitForSeconds(0.3f);
        isDamaged = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        mover.PlayDeath();
    }

    IEnumerator ChaseRoutine()
    {
        for (int i = 0; i < chaseSteps; i++)
        {
            if (player == null || isDead) break;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= catchDistance || dist > chaseDistance) break;

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
                while (mover.IsMoving) yield return null;
                yield return new WaitForSeconds(chaseStepDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    Direction GetDirectionTowardsPlayer()
    {
        if (player == null) return mover.facing;
        Vector2 toward = (Vector2)player.position - (Vector2)transform.position;
        if (Mathf.Abs(toward.x) > Mathf.Abs(toward.y))
            return toward.x > 0 ? Direction.Right : Direction.Left;
        return toward.y > 0 ? Direction.Up : Direction.Down;
    }

    List<Direction> GetChaseDirectionsOrdered()
    {
        Vector2 toward = (Vector2)player.position - (Vector2)transform.position;
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

        return new List<Direction> { primary, secondary, Opposite(secondary), Opposite(primary) };
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