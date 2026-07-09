using System.Collections;
using UnityEngine;

public class GridMoverSlime : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("Tamaño real de celda de tu Grid/Tilemap. Revisá el componente Grid y poné el mismo valor acá.")]
    public float cellSize = 1f;

    [Header("Movimiento (velocidad, no duración)")]
    [Tooltip("Unidades por segundo al caminar normal.")]
    public float moveSpeed = 2f;
    [Tooltip("Multiplica moveSpeed cuando running=true (persecución). El slime usa la misma animación Slime_Walk, solo cambia la velocidad.")]
    public float runSpeedMultiplier = 1.5f;

    [Header("Colisiones")]
    public LayerMask obstacleMask;
    [Tooltip("Radio del chequeo de colisión al moverse. Debe ser un poco menor que la mitad del cellSize.")]
    public float collisionRadius = 0.2f;

    [Header("Nombres de animación")]
    public string animIdle = "Slime_Idle";
    public string animWalk = "Slime_Walk";
    public string animSit = "Slime_Sit";
    public string animAttack = "Slime_Atack1";
    public string animDie = "Slime_Dead";

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] ownColliders;

    private bool isMoving;
    private Coroutine currentRoutine;

    public Direction facing = Direction.Down;
    public bool IsMoving => isMoving;

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
        PlayIdle();
    }

    public bool TryMove(Direction dir, bool running = false)
    {
        if (isMoving)
            return false;

        facing = dir;
        UpdateFacingVisual(dir);

        Vector2 offset = DirToVector(dir) * cellSize;
        Vector3 targetPos = transform.position + (Vector3)offset;

        Collider2D obstacle = GetRealObstacle(transform.position, targetPos);

        if (obstacle != null)
            return false;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(MoveRoutine(targetPos, running));
        return true;
    }

    // --- Movimiento por velocidad constante (mismo patrón que GridMoverFrog) ---
    // El slime solo tiene una animación de desplazamiento (Slime_Walk); running solo afecta la velocidad.
    IEnumerator MoveRoutine(Vector3 target, bool running)
    {
        isMoving = true;

        if (animator != null)
            animator.Play(animWalk, 0, 0f);

        float speed = moveSpeed * (running ? runSpeedMultiplier : 1f);

        while ((target - transform.position).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;

        isMoving = false;
        PlayIdle();
    }

    // --- Reacción al recibir daño (rebote) ---
    public void Knockback(Direction dir)
    {
        Vector2 offset = DirToVector(dir) * cellSize;
        Vector3 targetPos = transform.position + (Vector3)offset;

        Collider2D obstacle = GetRealObstacle(transform.position, targetPos);
        Vector3 finalTarget = obstacle != null ? transform.position : targetPos;

        facing = dir;
        UpdateFacingVisual(dir);

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(KnockbackRoutine(finalTarget));
    }

    IEnumerator KnockbackRoutine(Vector3 target)
    {
        isMoving = true;

        if (animator != null)
            animator.Play(animWalk, 0, 0f);

        float speed = moveSpeed * runSpeedMultiplier; // el rebote se siente mejor rápido

        while ((target - transform.position).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = target;

        isMoving = false;
        PlayIdle();
    }

    // --- Acciones cosméticas / de combate, llamadas desde SlimeIA ---
    public void PlayIdle()
    {
        if (animator != null) animator.Play(animIdle, 0, 0f);
    }

    public void PlaySit()
    {
        if (animator != null) animator.Play(animSit, 0, 0f);
    }

    public void PlayAttack()
    {
        if (animator != null) animator.Play(animAttack, 0, 0f);
    }

    public void PlayDie()
    {
        if (animator != null) animator.Play(animDie, 0, 0f);
    }

    /// <summary>
    /// Devuelve la duración real (en segundos) de un clip de animación buscándolo
    /// por nombre dentro del Animator Controller. Sirve para esperar exactamente
    /// lo que dura Slime_Dead sin depender de un valor fijo en el Inspector.
    /// Devuelve 0 si no lo encuentra.
    /// </summary>
    public float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == clipName)
                return clip.length;
        }

        return 0f;
    }

    // --- Chequeo de obstáculo a lo largo de TODO el trayecto (no solo el punto final) ---
    Collider2D GetRealObstacle(Vector3 fromPos, Vector3 targetPos)
    {
        Vector2 start = fromPos;
        Vector2 end = targetPos;
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);

        if (dist <= 0.0001f)
        {
            Collider2D[] hitsAtPoint = Physics2D.OverlapCircleAll(end, collisionRadius, obstacleMask);
            foreach (var hit in hitsAtPoint)
            {
                if (hit.CompareTag("Player")) continue;
                if (IsOwnCollider(hit)) continue;
                return hit;
            }
            return null;
        }

        RaycastHit2D[] hits = Physics2D.CircleCastAll(start, collisionRadius, dir, dist, obstacleMask);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.CompareTag("Player")) continue;
            if (IsOwnCollider(hit.collider)) continue;

            return hit.collider;
        }

        return null;
    }

    bool IsOwnCollider(Collider2D col)
    {
        foreach (Collider2D own in ownColliders)
        {
            if (col == own) return true;
        }
        return false;
    }

    void UpdateFacingVisual(Direction dir)
    {
        if (spriteRenderer == null) return;

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
        Gizmos.color = Color.yellow;
        if (Application.isPlaying)
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}