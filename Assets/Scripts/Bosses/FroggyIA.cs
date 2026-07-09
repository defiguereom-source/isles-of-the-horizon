using System.Collections;
using UnityEngine;

[RequireComponent(typeof(GridMoverFroggy))]
public class FroggyIA : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Movimiento (fase Froggy_Jump)")]
    public float moveSpeed = 2f;
    public float jumpMoveDuration = 1f;

    [Header("Telegraph (fase Froggy_Change)")]
    public float changeTelegraphDuration = 1f;

    [Header("Ataque (fase Froggy_Atack)")]
    [Tooltip("Opcional. Si lo dejas vacio, el area de ataque se centra en el propio sapo (transform.position). Util porque el ataque es un grito/AOE alrededor de el.")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public LayerMask playerLayer;
    public int attackDamage = 15;
    public float atackDuration = 0.8f;

    [Header("Descanso (fase Froggy_Idle)")]
    public float restDuration = 3f;

    [Header("Tiempos generales")]
    public float timeBetweenActions = 0.5f;

    [Header("Vida")]
    public int maxHealth = 500;

    [Header("Fase 2 (mas rapido)")]
    public float phase2SpeedMultiplier = 1.6f;

    [Header("Muerte")]
    public float destroyDelay = 1.5f;

    private GridMoverFroggy mover;
    private int currentHealth;

    private enum BossState { Inactive, JumpMove, Telegraph, Attack, Resting, Dead }
    private BossState currentState = BossState.Inactive;

    private bool isVulnerable; // true solo durante el descanso (Froggy_Idle)
    private int hitsTakenThisRest;
    private int maxHitsThisRest;
    private bool combatStarted;
    private bool isDead;

    public bool IsVulnerable => isVulnerable;

    // Centro real del area de ataque: si no asignaste attackPoint, usa la posicion del propio sapo
    private Vector3 AttackCenter => attackPoint != null ? attackPoint.position : transform.position;

    void Awake()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            else
                Debug.LogWarning("FroggyIA: No se encontro ningun objeto con el Tag Player.");
        }
    }

    void Start()
    {
        mover = GetComponent<GridMoverFroggy>();
        currentHealth = maxHealth;
    }

    private bool IsPhaseOne => currentHealth > maxHealth / 2;

    // Llamar desde FroggyDialog cuando el dialogo termine de forma natural
    public void StartCombat()
    {
        if (combatStarted) return;
        combatStarted = true;
        currentHealth = maxHealth;
        StartCoroutine(CombatLoop());
    }

    private IEnumerator CombatLoop()
    {
        while (currentHealth > 0)
        {
            float speedMult = IsPhaseOne ? 1f : phase2SpeedMultiplier;

            yield return StartCoroutine(JumpMovePhase(speedMult));
            yield return new WaitForSeconds(timeBetweenActions / speedMult);

            yield return StartCoroutine(TelegraphPhase(speedMult));
            yield return new WaitForSeconds(timeBetweenActions / speedMult);

            yield return StartCoroutine(AttackPhase(speedMult));
            yield return new WaitForSeconds(timeBetweenActions / speedMult);

            if (currentHealth <= 0) break;

            yield return StartCoroutine(RestPhase());

            yield return new WaitForSeconds(timeBetweenActions);
        }

        Die();
    }

    private IEnumerator JumpMovePhase(float speedMult)
    {
        currentState = BossState.JumpMove;
        isVulnerable = false;

        float duration = jumpMoveDuration / speedMult;
        yield return StartCoroutine(mover.MoveTowardsRoutine(player, moveSpeed * speedMult, duration, speedMult));
    }

    private IEnumerator TelegraphPhase(float speedMult)
    {
        currentState = BossState.Telegraph;
        isVulnerable = false;

        float duration = changeTelegraphDuration / speedMult;
        yield return StartCoroutine(mover.PlayChangeRoutine(duration, speedMult, player));
    }

    private IEnumerator AttackPhase(float speedMult)
    {
        currentState = BossState.Attack;
        isVulnerable = false;

        float duration = atackDuration / speedMult;
        yield return StartCoroutine(mover.PlayAttackRoutine(duration, speedMult, player, CheckAttackHit));
    }

    private IEnumerator RestPhase()
    {
        currentState = BossState.Resting;
        isVulnerable = true;
        hitsTakenThisRest = 0;
        maxHitsThisRest = Random.Range(1, 6); // entre 1 y 5 golpes posibles

        mover.PlayIdle();

        float elapsed = 0f;
        while (elapsed < restDuration && hitsTakenThisRest < maxHitsThisRest && currentHealth > 0)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isVulnerable = false;
    }

    private void CheckAttackHit()
    {
        Collider2D hit = Physics2D.OverlapCircle(AttackCenter, attackRadius, playerLayer);
        if (hit != null)
        {
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(attackDamage);
            else
                Debug.LogWarning("FroggyIA: el objeto detectado no tiene PlayerHealth.");
        }
    }

    // Llamado desde AttackHitbox (mismo patron que OrcIA.ReceiveDamage)
    public void ReceiveDamage(int damage)
    {
        if (!isVulnerable || currentHealth <= 0 || isDead || currentState == BossState.Dead) return;

        hitsTakenThisRest++;
        currentHealth -= damage;
        mover.PlayDamage();

        if (currentHealth <= 0)
            currentHealth = 0;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = BossState.Dead;
        isVulnerable = false;

        mover.PlayDeath(destroyDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackCenter, attackRadius);
    }
}