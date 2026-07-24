using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMoverOrcWarrior))]
public class OrcWarriorIA : MonoBehaviour
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
    public float attackRange = 0.9f;
    public float attackCooldown = 1f;
    public float attackDamage = 60;

    [Header("Vida")]
    public int maxHealth = 3;
    private int currentHealth;

    private GridMoverOrcWarrior mover;
    private Coroutine behaviourCoroutine;
    private float lastAttackTime = -999f;

    void Start()
    {
        mover = GetComponent<GridMoverOrcWarrior>();
        currentHealth = maxHealth;
        behaviourCoroutine = StartCoroutine(BehaviourRoutine());
    }

    public void TakeDamage(int amount)
    {
        if (mover.IsDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
            Die();
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

            float distToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            if (distToPlayer <= attackRange)
            {
                if (Time.time - lastAttackTime >= attackCooldown && !mover.IsMoving)
                {
                    FaceTowardsPlayer();
                    lastAttackTime = Time.time;
                    bool attackDone = false;

                    mover.PlayAttack(() =>
                    {
                        attackDone = true;

                        if (player != null)
                        {
                            float dist = Vector2.Distance(transform.position, player.position);
                            if (dist <= attackRange)
                            {
                                PlayerHealth health = player.GetComponent<PlayerHealth>();
                                if (health != null)
                                    health.TakeDamage((int)attackDamage);
                            }
                        }
                    });

                    while (!attackDone) yield return null;
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

            if (mover.IsMoving || mover.IsDead) continue;

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
            if (player == null || mover.IsDead) break;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange) break;
            if (dist > chaseDistance) break;

            bool moved = false;
            foreach (Direction dir in GetChaseDirectionsOrdered())
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
                yield return new WaitForSeconds(chaseStepDelay);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
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
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}