using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class GridMover : MonoBehaviour
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
        animator.Play("Chiken");
    }
    public bool IsMoving => isMoving;
    public bool TryMove(Direction dir)
    {
        if (isMoving) return false;
        facing = dir;
        if (dir == Direction.Left)
            spriteRenderer.flipX = true;
        else if (dir == Direction.Right)
            spriteRenderer.flipX = false;
        Vector2 offset = DirToVector(dir);
        Vector3 targetPos = transform.position + (Vector3)offset;
        if (Physics2D.OverlapCircle(targetPos, 0.2f, obstacleMask))
            return false;
        moveCoroutine = StartCoroutine(MoveRoutine(targetPos));
        return true;
    }
    IEnumerator MoveRoutine(Vector3 target)
    {
        isMoving = true;
        blocked = false;
        animator.Play("Chiken", 0, 0f);
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            if (blocked)
            {
                // Choque real detectado a mitad de camino: cancelar y volver
                transform.position = start;
                isMoving = false;
                animator.Play("Chicken_Chill", 0, 0f);
                yield break;
            }
            t += Time.deltaTime / moveTime;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.position = target;
        isMoving = false;
        animator.Play("Chicken_Chill", 0, 0f);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Solo nos importa si el otro objeto está en la capa de obstáculos
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
}