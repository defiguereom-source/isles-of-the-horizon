using System.Collections;
using UnityEngine;

public class GridMoverSnake : MonoBehaviour
{
    public float moveTime = 0.25f;
    public LayerMask obstacleMask;

    [Header("Knockback")]
    public float knockbackTime = 0.15f;
    [Tooltip("Altura del saltito visual durante el knockback.")]
    public float knockbackHopHeight = 0.15f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] ownColliders;

    private bool isMoving;
    private bool blocked;
    private Coroutine currentRoutine;

    public Direction facing = Direction.Down;

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
        if (animator != null)
            animator.Play(GetAnim(facing));
    }

    public bool IsMoving => isMoving;

    public bool TryMove(Direction dir)
    {
        if (isMoving) return false;

        facing = dir;
        Vector2 offset = DirToVector(dir);
        Vector3 targetPos = transform.position + (Vector3)offset;

        Collider2D obstacle = GetRealObstacle(targetPos);

        if (obstacle != null)
        {
            if (animator != null)
                animator.Play(GetAnim(dir), 0, 0f);

            return false;
        }

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(MoveRoutine(targetPos, dir));
        return true;
    }

    // --- Knockback al recibir un golpe, con saltito visual ---
    public void Knockback(Direction dir)
    {
        Vector2 offset = DirToVector(dir);
        Vector3 targetPos = transform.position + (Vector3)offset;

        // Si hay obstáculo detrás, no se mueve pero igual reproduce la reacción
        Collider2D obstacle = GetRealObstacle(targetPos);
        Vector3 finalTarget = obstacle != null ? transform.position : targetPos;

        facing = dir;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(KnockbackRoutine(finalTarget, dir));
    }

    IEnumerator KnockbackRoutine(Vector3 target, Direction dir)
    {
        isMoving = true;
        blocked = false;

        if (animator != null)
            animator.Play(GetAnim(dir), 0, 0f);

        Vector3 start = transform.position;
        float t = 0f;
        Vector3 spriteBasePos = spriteRenderer != null ? spriteRenderer.transform.localPosition : Vector3.zero;

        while (t < 1f)
        {
            if (blocked)
            {
                transform.position = start;
                break;
            }

            t += Time.deltaTime / knockbackTime;
            float clampedT = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(start, target, clampedT);

            // Saltito: arco con seno, sube y vuelve a bajar durante el movimiento
            if (spriteRenderer != null)
            {
                float hop = Mathf.Sin(clampedT * Mathf.PI) * knockbackHopHeight;
                spriteRenderer.transform.localPosition = spriteBasePos + new Vector3(0f, hop, 0f);
            }

            yield return null;
        }

        if (t >= 1f)
            transform.position = target;

        // Asegura que el sprite vuelva a su posición base al terminar el salto
        if (spriteRenderer != null)
            spriteRenderer.transform.localPosition = spriteBasePos;

        isMoving = false;

        if (animator != null)
            animator.Play(GetAnim(facing), 0, 0f);
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

    IEnumerator MoveRoutine(Vector3 target, Direction dir)
    {
        isMoving = true;
        blocked = false;

        if (animator != null)
            animator.Play(GetAnim(dir), 0, 0f);

        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            if (blocked)
            {
                transform.position = start;
                isMoving = false;

                if (animator != null)
                    animator.Play(GetAnim(dir), 0, 0f);

                yield break;
            }

            t += Time.deltaTime / moveTime;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        isMoving = false;

        if (animator != null)
            animator.Play(GetAnim(dir), 0, 0f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleMask) != 0)
        {
            blocked = true;
        }
    }

    Vector2 DirToVector(Direction dir) => dir switch
    {
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        _ => Vector2.zero
    };

    string GetAnim(Direction dir) => dir switch
    {
        Direction.Down => "Snake_1",
        Direction.Up => "Snake_2",
        Direction.Left => "Snake_3",
        Direction.Right => "Snake_4",
        _ => "Snake_1"
    };

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (Application.isPlaying)
            Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}