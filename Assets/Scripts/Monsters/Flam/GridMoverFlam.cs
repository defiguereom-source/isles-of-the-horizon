using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class GridMoverFlam : MonoBehaviour
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
            return false;
        moveCoroutine = StartCoroutine(MoveRoutine(targetPos, dir));
        return true;
    }

    IEnumerator MoveRoutine(Vector3 target, Direction dir)
    {
        isMoving = true;
        blocked = false;
        animator.Play(GetWalkAnim(dir), 0, 0f);
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

    Vector2 DirToVector(Direction dir) => dir switch
    {
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        _ => Vector2.zero
    };

    // --- Mapeo de animaciones según dirección ---
    string GetWalkAnim(Direction dir) => dir switch
    {
        Direction.Down => "Flam_1",
        Direction.Up => "Flam_2",
        Direction.Left => "Flam_3",
        Direction.Right => "Flam_4",
        _ => "Flam_1"
    };

    // Si tienes animaciones de "quieto" distintas, cámbialas aquí.
    // Por ahora usa el mismo clip de caminar como pose final (puedes reemplazar
    // por "Flam_1_Idle" etc. si las tienes).
    string GetIdleAnim(Direction dir) => GetWalkAnim(dir);
}