using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class GridMoverOrcWarrior : MonoBehaviour
{
    public float moveTime = 0.25f;
    public LayerMask obstacleMask;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isMoving;
    private bool blocked;
    private bool isDead;
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
    public bool IsDead => isDead;

    public bool TryMove(Direction dir)
    {
        if (isMoving || isDead) return false;

        facing = dir;
        UpdateFlip(dir);

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
        animator.Play(GetRunAnim(), 0, 0f);

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

    public void PlayAttack(System.Action onComplete = null)
    {
        if (isDead) return;
        StartCoroutine(AttackRoutine(onComplete));
    }

    IEnumerator AttackRoutine(System.Action onComplete)
    {
        isMoving = true; // bloquea movimiento mientras ataca
        animator.Play(GetAttackAnim(), 0, 0f);

        // Espera la duración del clip actual
        yield return new WaitForSeconds(GetClipLength(GetAttackAnim()));

        isMoving = false;
        animator.Play(GetIdleAnim(facing), 0, 0f);
        onComplete?.Invoke();
    }

    public void PlayDeath()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        animator.Play(GetDeathAnim(), 0, 0f);
    }

    float GetClipLength(string animName)
    {
        RuntimeAnimatorController rac = animator.runtimeAnimatorController;
        foreach (var clip in rac.animationClips)
        {
            if (clip.name == animName)
                return clip.length;
        }
        return 0.4f; // fallback
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleMask) != 0)
        {
            blocked = true;
        }
    }

    void UpdateFlip(Direction dir)
    {
        if (dir == Direction.Left)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (dir == Direction.Right)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    Vector2 DirToVector(Direction dir) => dir switch
    {
        Direction.Up => Vector2.up,
        Direction.Down => Vector2.down,
        Direction.Left => Vector2.left,
        Direction.Right => Vector2.right,
        _ => Vector2.zero
    };

    // --- Mapeo de animaciones ---
    // OrcWarrior usa flip en X, así que Idle/Run/Attack/Death no dependen de la dirección,
    // solo se gira el sprite con UpdateFlip.
    string GetIdleAnim(Direction dir) => "OrcWarrior_Idle";
    string GetRunAnim() => "OrcWarrior_Run";
    string GetAttackAnim() => "OrcWarrior_Atack";
    string GetDeathAnim() => "OrcWarrior_Death";
}