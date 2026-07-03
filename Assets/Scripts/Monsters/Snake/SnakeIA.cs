using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMoverSnake))]
public class SnakeIA : MonoBehaviour
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

    [Header("Daño por contacto")]
    public int contactDamage = 10;
    public float damageCooldown = 1f;

    [Header("Reacción al recibir golpe (inmortal)")]
    public float damagedPauseTime = 0.2f; // pausa la IA justo después del golpe

    private GridMoverSnake mover;
    private float lastDamageTime = -999f;
    private bool isDamaged;
    private Coroutine behaviourCoroutine;

    void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            else
                Debug.LogWarning("SnakeIA: No se encontró ningún objeto con el Tag Player.");
        }
    }

    void Start()
    {
        mover = GetComponent<GridMoverSnake>();
        behaviourCoroutine = StartCoroutine(BehaviourRoutine());
    }

    IEnumerator BehaviourRoutine()
    {
        while (true)
        {
            if (isDamaged)
            {
                yield return null;
                continue;
            }

            float distToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            if (distToPlayer <= catchDistance)
            {
                TryDealContactDamage();
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

    void TryDealContactDamage()
    {
        if (player == null) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > catchDistance) return;

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(contactDamage);
        else
            Debug.LogWarning("SnakeIA: El Player no tiene un componente PlayerHealth.");

        lastDamageTime = Time.time;
    }

    // --- NUEVO: llamado desde el script de ataque del jugador ---
    // Ignora el daño real (la serpiente es inmortal) pero reacciona con knockback.
    public void ReceiveDamage(int amount)
    {
        ReceiveDamage(amount, player);
    }

    public void ReceiveDamage(int amount, Transform source)
    {
        Direction knockDir = GetKnockbackDirection(source);
        mover.Knockback(knockDir);

        if (!isDamaged)
            StartCoroutine(DamagedFlow());
    }

    IEnumerator DamagedFlow()
    {
        isDamaged = true;
        yield return new WaitForSeconds(damagedPauseTime);
        isDamaged = false;
    }

    Direction GetKnockbackDirection(Transform source)
    {
        Transform reference = source != null ? source : player;
        if (reference == null) return Opposite(mover.facing);

        Vector2 away = (Vector2)transform.position - (Vector2)reference.position;

        if (Mathf.Abs(away.x) > Mathf.Abs(away.y))
            return away.x > 0 ? Direction.Right : Direction.Left;
        return away.y > 0 ? Direction.Up : Direction.Down;
    }

    IEnumerator ChaseRoutine()
    {
        for (int i = 0; i < chaseSteps; i++)
        {
            if (player == null) break;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= catchDistance) break;
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}