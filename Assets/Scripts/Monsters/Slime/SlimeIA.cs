using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMoverSlime))]
public class SlimeIA : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Vida")]
    public int maxHealth = 15;
    [Tooltip("Duración de la animación Slime_Dead. Se espera ese tiempo antes de destruir el objeto.")]
    public float deathDelay = 2f;
    private int currentHealth;
    private bool isDead;

    [Header("Tiempos de comportamiento normal")]
    public float minWaitTime = 1.5f;
    public float maxWaitTime = 4f;
    [Tooltip("Probabilidad de sentarse (acción cosmética, sin moverse) en vez de caminar, mientras está en reposo.")]
    [Range(0f, 1f)] public float idleActionChance = 0.4f;

    [Header("Detección de persecución")]
    public float chaseDistance = 5f;
    [Tooltip("Debe cubrir la distancia de una casilla adyacente (normalmente ~1), ya que el player bloquea el movimiento y el slime nunca puede acercarse más que eso.")]
    public float catchDistance = 1.1f;
    public int chaseSteps = 6;
    public float chaseStepDelay = 0.05f;

    [Header("Daño por contacto")]
    public int contactDamage = 6;
    public float damageCooldown = 1f;
    [Tooltip("Tiempo que dura la animación Slime_Atack1 antes de poder volver a atacar/moverse.")]
    public float attackAnimTime = 0.4f;

    [Header("Reacción al recibir golpe")]
    public float damagedPauseTime = 0.2f;

    private GridMoverSlime mover;
    private float lastDamageTime = -999f;
    private bool isDamaged;
    private bool isAttacking;
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
                Debug.LogWarning("SlimeIA: No se encontró ningún objeto con el Tag Player.");
        }

        ownColliders = GetComponentsInChildren<Collider2D>();
    }

    void Start()
    {
        mover = GetComponent<GridMoverSlime>();
        currentHealth = maxHealth;
        behaviourCoroutine = StartCoroutine(BehaviourRoutine());
    }

    IEnumerator BehaviourRoutine()
    {
        while (true)
        {
            if (isDamaged || isAttacking)
            {
                yield return null;
                continue;
            }

            float distToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            // --- Ataque por contacto ---
            if (distToPlayer <= catchDistance)
            {
                yield return StartCoroutine(AttackRoutine());
                continue;
            }

            // --- Persecución caminando (más rápido) ---
            if (distToPlayer < chaseDistance)
            {
                yield return StartCoroutine(ChaseRoutine());
                continue;
            }

            // --- Comportamiento de reposo: esperar, sentarse o caminar en una dirección aleatoria ---
            float wait = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(wait);
            if (mover.IsMoving) continue;

            if (Random.value < idleActionChance)
            {
                // Acción puramente cosmética: se sienta, sin moverse.
                mover.PlaySit();
                yield return new WaitForSeconds(0.8f);
                mover.PlayIdle();
            }
            else if (Random.value < 0.6f)
            {
                Direction randomDir = (Direction)Random.Range(0, 4);
                mover.TryMove(randomDir, running: false); // camina (Slime_Walk)
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        mover.PlayAttack();

        TryDealContactDamage();

        yield return new WaitForSeconds(attackAnimTime);
        isAttacking = false;
        mover.PlayIdle();
    }

    void TryDealContactDamage()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > catchDistance) return;

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph == null) ph = player.GetComponentInParent<PlayerHealth>();
        if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();

        DealDamageTo(ph);
    }

    // --- Detección real de contacto por colisión ---
    // IMPORTANTE: agregá un Collider2D (ej. CircleCollider2D, radio ~0.5) directamente
    // en este mismo GameObject (el que tiene SlimeIA), marcado como "Is Trigger".
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
            Debug.LogWarning("SlimeIA: El Player no tiene un componente PlayerHealth.");
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
            isDead = true; // marcamos ya acá para bloquear todo lo demás
            if (behaviourCoroutine != null)
                StopCoroutine(behaviourCoroutine); // solo detiene el comportamiento normal
            StartCoroutine(Die());
            return;
        }

        if (!isDamaged)
            StartCoroutine(DamagedFlow());
    }

    IEnumerator Die()
    {
        foreach (var col in ownColliders)
            col.enabled = false;

        mover.PlayDie();

        // Espera la duración REAL del clip Slime_Dead (leída desde el Animator).
        // Así el objeto se destruye justo cuando termina de reproducirse por
        // primera vez, sin importar si "Loop Time" quedó tildado en el clip.
        // Si por algún motivo no se pudo encontrar el clip, usa deathDelay como respaldo.
        float clipLength = mover.GetClipLength(mover.animDie);
        yield return new WaitForSeconds(clipLength > 0f ? clipLength : deathDelay);

        Destroy(gameObject);
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
        // TryMove(dir, running: true) usa automáticamente runSpeedMultiplier dentro de GridMoverSlime,
        // reproduciendo siempre Slime_Walk pero a mayor velocidad.
        for (int i = 0; i < chaseSteps; i++)
        {
            if (player == null) break;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= catchDistance) break;
            if (dist > chaseDistance) break;

            bool moved = false;
            foreach (Direction dir in GetChaseDirectionsOrdered())
            {
                if (mover.TryMove(dir, running: true))
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