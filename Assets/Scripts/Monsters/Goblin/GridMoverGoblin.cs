using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class GridMoverGoblin : MonoBehaviour
{
    public float moveTime = 0.25f;
    public LayerMask obstacleMask;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isMoving;
    private bool blocked;
    private Coroutine moveCoroutine;

    public Direction facing = Direction.Down;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        animator.Play(GetIdleAnim(facing));
    }

    public bool IsMoving => isMoving;

    public bool TryMove(Direction dir)
    {
        if (isMoving) return false;

        facing = dir;
        Vector2 offset = DirToVector(dir);
        Vector3 targetPos = transform.position + (Vector3)offset;

        if (Physics2D.OverlapCircle(targetPos, 0.2f, obstacleMask))
        {
            // No se puede mover, pero igual queda mirando hacia esa dirección
            animator.Play(GetIdleAnim(dir), 0, 0f);
            return false;
        }

        moveCoroutine = StartCoroutine(MoveRoutine(targetPos, dir));
        return true;
    }

    IEnumerator MoveRoutine(Vector3 target, Direction dir)
    {
        isMoving = true;
        blocked = false;
        string walkAnim = GetWalkAnim(dir);
        animator.Play(walkAnim, 0, 0f);

        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            if (blocked)
            {
                transform.position = start;
                isMoving = false;
                animator.Play(GetIdleAnim(dir), 0, 0f);
                yield break;
            }

            // Si el Animator se salió solo del estado de caminar (por una transición
            // automática o porque el clip no está en loop), lo forzamos a volver.
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(walkAnim))
            {
                animator.Play(walkAnim, 0, 0f);
            }

            t += Time.deltaTime / moveTime;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        isMoving = false;
        animator.Play(GetIdleAnim(dir), 0, 0f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleMask) != 0)
        {
            blocked = true;
        }
    }

    // --- Ataque ---
    // El goblin solo tiene animación de ataque hacia la derecha (Goblin_atack1).
    public void PlayAttack(Direction dir, System.Action onFinished = null)
    {
        if (isMoving) return;
        StartCoroutine(AttackRoutine(dir, onFinished));
    }

    IEnumerator AttackRoutine(Direction dir, System.Action onFinished)
    {
        isMoving = true; // bloquea movimiento mientras ataca

        // Flip horizontal: si ataca a la izquierda, espejamos el sprite de ataque (que es a la derecha)
        if (spriteRenderer != null)
            spriteRenderer.flipX = (dir == Direction.Left);

        animator.Play("Goblin_atack1", 0, 0f);

        // Espera a que termine el clip de ataque
        yield return null; // deja que el Animator cargue el state
        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);

        if (spriteRenderer != null)
            spriteRenderer.flipX = false;

        isMoving = false;
        animator.Play(GetIdleAnim(dir), 0, 0f);

        onFinished?.Invoke();
    }

    // --- Daño ---
    public void PlayDamage(System.Action onFinished = null)
    {
        StartCoroutine(DamageRoutine(onFinished));
    }

    IEnumerator DamageRoutine(System.Action onFinished)
    {
        bool wasMoving = isMoving;
        isMoving = true; // bloquea movimiento mientras recibe daño

        animator.Play("Goblin_Daño", 0, 0f);

        yield return null;
        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);

        isMoving = wasMoving;
        animator.Play(GetIdleAnim(facing), 0, 0f);

        onFinished?.Invoke();
    }

    Vector2 DirToVector(Direction dir) => dir switch
    {
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        _ => Vector2.zero
    };

    // --- Mapeo de animaciones según dirección ---
    // Idle (quieto)
    string GetIdleAnim(Direction dir) => dir switch
    {
        Direction.Down => "Goblin_1",  // mirando de frente
        Direction.Right => "Goblin_2", // mirando a la derecha
        Direction.Left => "Goblin_3",  // mirando a la izquierda
        Direction.Up => "Goblin_4",    // mirando hacia atrás
        _ => "Goblin_1"
    };

    // Caminando
    string GetWalkAnim(Direction dir) => dir switch
    {
        Direction.Down => "Goblin_5",  // caminando hacia el frente
        Direction.Right => "Goblin_6", // caminando a la derecha
        Direction.Left => "Goblin_7",  // caminando a la izquierda
        Direction.Up => "Goblin_8",    // caminando hacia atrás
        _ => "Goblin_5"
    };
}