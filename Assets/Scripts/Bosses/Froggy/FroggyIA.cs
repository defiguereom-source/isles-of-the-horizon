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

    [Header("Invocación de ranas (solo Fase 2)")]
    [Tooltip("Prefab de la rana chica a invocar (el que tiene FrogIA + GridMoverFrog).")]
    public GameObject frogPrefab;
    [Tooltip("Cuántas ranas invoca en cada ataque de fase 2.")]
    public int frogsPerSummon = 1;
    [Tooltip("Radio alrededor de Froggy en el que aparecen las ranas invocadas.")]
    public float summonRadius = 3f;
    [Tooltip("Capas que se consideran obstáculo para no invocar ranas dentro de paredes. Dejalo vacío (Nothing) si no querés chequeo.")]
    public LayerMask summonObstacleMask;
    [Tooltip("Cuántos intentos hace como máximo para encontrar un punto libre por cada rana antes de rendirse.")]
    public int summonPlacementAttempts = 10;

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

        // --- Invocación de ranas: en CADA ataque, pero solo si ya está en fase 2 ---
        if (!IsPhaseOne && currentHealth > 0)
            SummonFrogs();
    }

    private void SummonFrogs()
    {
        if (frogPrefab == null)
        {
            Debug.LogWarning("FroggyIA: frogPrefab no está asignado, no se pueden invocar ranas.");
            return;
        }

        for (int i = 0; i < frogsPerSummon; i++)
        {
            Vector3 spawnPos = GetFreeSummonPosition();
            Instantiate(frogPrefab, spawnPos, Quaternion.identity);
        }
    }

    // Busca un punto libre alrededor de Froggy (dentro de summonRadius) que no
    // caiga sobre un obstáculo. Si no encuentra ninguno tras varios intentos,
    // usa el último punto probado igual (mejor invocar algo mal ubicado que nada).
    private Vector3 GetFreeSummonPosition()
    {
        Vector3 candidate = transform.position;

        for (int attempt = 0; attempt < summonPlacementAttempts; attempt++)
        {
            Vector2 randomOffset = Random.insideUnitCircle.normalized * summonRadius;
            candidate = transform.position + (Vector3)randomOffset;

            if (summonObstacleMask.value == 0)
                break; // no se configuró máscara de obstáculos, no hay chequeo que hacer

            bool blocked = Physics2D.OverlapCircle(candidate, 0.3f, summonObstacleMask);
            if (!blocked)
                break;
        }

        return candidate;
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, summonRadius);
    }
}