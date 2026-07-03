using System.Collections;
using UnityEngine;

public class GridMoverOrc : MonoBehaviour
{
    public float moveTime = 0.25f;
    public LayerMask obstacleMask;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] ownColliders;

    private bool isMoving;
    private bool blocked;
    private bool isDead;

    public Direction facing = Direction.Down;

    [Header("Referencia al control de arma")]
    [SerializeField] private WeaponVisibility weaponVisibility;

    [Header("Muerte")]
    public float destroyDelay = 1.5f;       // ← NUEVO: segundos tras la animación de muerte

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ownColliders = GetComponentsInChildren<Collider2D>();

        if (animator == null)
            Debug.LogError($"[{name}] No se encontró ningún Animator en este objeto ni en sus hijos.");

        if (spriteRenderer == null)
            Debug.LogError($"[{name}] No se encontró ningún SpriteRenderer en este objeto ni en sus hijos.");
    }

    void Start()
    {
        ApplyFlip(facing);

        if (animator != null)
            animator.Play("OrcNormal_Idle");

        weaponVisibility?.ShowWeapon();
    }

    public bool IsMoving => isMoving;
    public bool IsDead => isDead;

    public bool TryMove(Direction dir, bool isChasing = false)
    {
        if (isMoving || isDead)
            return false;

        facing = dir;
        ApplyFlip(dir);

        Vector2 offset = DirToVector(dir);
        Vector3 targetPos = transform.position + (Vector3)offset;

        Collider2D obstacle = GetRealObstacle(targetPos);

        if (obstacle != null)
        {
            Debug.Log($"{name} no puede moverse a {dir}. Bloqueado por: {obstacle.name}");

            if (animator != null)
                animator.Play("OrcNormal_Idle", 0, 0f);

            return false;
        }

        StartCoroutine(MoveRoutine(targetPos, isChasing));
        return true;
    }

    Collider2D GetRealObstacle(Vector3 targetPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, 0.2f, obstacleMask);

        foreach (Collider2D hit in hits)
        {
            bool isOwn = false;

            foreach (Collider2D own in ownColliders)
            {
                if (hit == own) { isOwn = true; break; }
            }

            if (!isOwn)
                return hit;
        }

        return null;
    }

    IEnumerator MoveRoutine(Vector3 target, bool isChasing)
    {
        isMoving = true;
        blocked = false;

        string moveAnim = isChasing ? "OrcNormal_Run" : "OrcNormal_Walk";

        if (animator != null)
            animator.Play(moveAnim, 0, 0f);

        if (isChasing) weaponVisibility?.HideWeapon();
        else weaponVisibility?.ShowWeapon();

        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            if (blocked)
            {
                transform.position = start;
                isMoving = false;

                if (animator != null)
                    animator.Play("OrcNormal_Idle", 0, 0f);

                weaponVisibility?.ShowWeapon();
                yield break;
            }

            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(moveAnim))
                    animator.Play(moveAnim, 0, 0f);
            }

            t += Time.deltaTime / moveTime;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        isMoving = false;

        if (animator != null)
            animator.Play("OrcNormal_Idle", 0, 0f);

        if (isChasing) weaponVisibility?.HideWeapon();
        else weaponVisibility?.ShowWeapon();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleMask) != 0)
        {
            blocked = true;
            Debug.Log($"{name} chocó con: {collision.gameObject.name}");
        }
    }

    public void PlayAttack(Direction dir, System.Action onFinished = null)
    {
        if (isMoving || isDead)
            return;

        StartCoroutine(AttackRoutine(dir, onFinished));
    }

    IEnumerator AttackRoutine(Direction dir, System.Action onFinished)
    {
        isMoving = true;
        ApplyFlip(dir);

        weaponVisibility?.ShowWeapon();

        if (animator != null)
            animator.Play("OrcNormal_Atack", 0, 0f);

        yield return null;

        float clipLength = animator != null
            ? animator.GetCurrentAnimatorStateInfo(0).length
            : 0.5f;

        yield return new WaitForSeconds(clipLength);

        isMoving = false;

        if (animator != null)
            animator.Play("OrcNormal_Idle", 0, 0f);

        onFinished?.Invoke();
    }

    public void PlayDeath()
    {
        if (isDead)
            return;

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        isDead = true;
        isMoving = true;

        // ── NUEVO: deshabilitar todos los colliders inmediatamente ──
        foreach (Collider2D col in ownColliders)
            col.enabled = false;

        // ── NUEVO: detener físicas si tiene Rigidbody2D ──
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;

        ApplyFlip(facing);

        if (animator != null)
            animator.Play("OrcNormal_Death", 0, 0f);

        yield return null;

        float clipLength = animator != null
            ? animator.GetCurrentAnimatorStateInfo(0).length
            : 0.5f;

        yield return new WaitForSeconds(clipLength);

        weaponVisibility?.HideHandsOnDeath();

        // ── NUEVO: esperar el delay extra y destruir el objeto ──
        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }

    void ApplyFlip(Direction dir)
    {
        if (spriteRenderer == null)
            return;

        if (dir == Direction.Left) spriteRenderer.flipX = true;
        else if (dir == Direction.Right) spriteRenderer.flipX = false;
    }

    Vector2 DirToVector(Direction dir) => dir switch
    {
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        _ => Vector2.zero
    };

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (Application.isPlaying)
            Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}