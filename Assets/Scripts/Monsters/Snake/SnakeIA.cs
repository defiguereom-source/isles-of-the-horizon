using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMoverSnake))]
public class SnakeIA : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Vida")]
    public int maxHealth = 30;
    public float deathDelay = 0.15f; // tiempo antes de destruir el objeto (para animación/efecto si querés)
    private int currentHealth;
    private bool isDead;

    [Header("Tiempos de comportamiento normal")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Detección de persecución")]
    public float chaseDistance = 5f;
    [Tooltip("Debe cubrir la distancia de una casilla adyacente (normalmente ~1), ya que el player bloquea el movimiento y la serpiente nunca puede acercarse más que eso.")]
    public float catchDistance = 1.1f;
    public int chaseSteps = 5;
    public float chaseStepDelay = 0.05f;

    [Header("Velocidad al perseguir")]
    [Tooltip("Multiplica el moveTime del GridMover durante la persecución. Menor a 1 = más rápida.")]
    public float chaseMoveTimeMultiplier = 0.7f;

    [Header("Daño por contacto")]
    public int contactDamage = 10;
    public float damageCooldown = 1f;

    [Header("Reacción al recibir golpe")]
    public float damagedPauseTime = 0.2f; // pausa la IA justo después del golpe

    private GridMoverSnake mover;
    private float lastDamageTime = -999f;
    private bool isDamaged;
    private Coroutine behaviourCoroutine;
    private Collider2D[] ownColliders;

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

        ownColliders = GetComponentsInChildren<Collider2D>();
    }

    void Start()
    {
        mover = GetComponent<GridMoverSnake>();
        currentHealth = maxHealth;
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
        // Se mantiene solo como intento adicional por distancia (respaldo).
        // La forma principal y confiable de detectar contacto es OnTriggerEnter2D/Stay2D más abajo.
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > catchDistance) return;

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph == null) ph = player.GetComponentInParent<PlayerHealth>();
        if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();

        DealDamageTo(ph);
    }

    // --- Detección real de contacto por colisión (igual de confiable que AttackHitbox) ---
    // IMPORTANTE: agregá un Collider2D (ej. CircleCollider2D, radio ~0.55) directamente
    // en este mismo GameObject (el que tiene SnakeIA), marcado como "Is Trigger".
    // No hace falta Rigidbody2D en la serpiente: el Player ya tiene uno y con eso alcanza
    // para que Unity dispare los eventos de trigger.
    void OnTriggerEnter2D(Collider2D other) => TryContactDamageFromCollider(other);
    void OnTriggerStay2D(Collider2D other) => TryContactDamageFromCollider(other);

    void TryContactDamageFromCollider(Collider2D other)
    {
        if (isDead) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null) ph = other.GetComponentInChildren<PlayerHealth>();

        DealDamageTo(ph);
    }

    void DealDamageTo(PlayerHealth ph)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        if (ph != null)
        {
            ph.TakeDamage(contactDamage);
            lastDamageTime = Time.time;
        }
        else
        {
            Debug.LogWarning("SnakeIA: El Player no tiene un componente PlayerHealth.");
        }
    }

    // --- Llamado desde el script de ataque del jugador (AttackHitbox) ---
    public void ReceiveDamage(int amount)
    {
        ReceiveDamage(amount, player);
    }

    public void ReceiveDamage(int amount, Transform source)
    {
        if (isDead) return;

        currentHealth -= amount;

        Direction knockDir = GetKnockbackDirection(source);
        mover.Knockback(knockDir);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (!isDamaged)
            StartCoroutine(DamagedFlow());
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Detiene todo comportamiento y evita más daño/colisiones
        StopAllCoroutines();
        foreach (var col in ownColliders)
            col.enabled = false;

        // Si querés reproducir una animación de muerte antes de destruir,
        // este es el lugar para hacerlo (ej: animator.Play("Snake_Die")).

        Destroy(gameObject, deathDelay);
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
        float originalMoveTime = mover.moveTime;
        mover.moveTime = originalMoveTime * chaseMoveTimeMultiplier;

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

        mover.moveTime = originalMoveTime;
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