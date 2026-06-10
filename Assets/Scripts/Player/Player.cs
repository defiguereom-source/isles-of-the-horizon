using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;

    [Header("Ataque")]
    public AttackHitbox attackHitbox;
    public float hitboxDuration = 0.15f; // segundos que la hitbox permanece activa

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    private Vector2 movement;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool facingRight = true;

    private enum Dir { Down, Up, Left, Right }
    private Dir lastDir = Dir.Down;

    private static readonly int HashIdleFront = Animator.StringToHash("player_0");
    private static readonly int HashIdleSide = Animator.StringToHash("player_1");
    private static readonly int HashIdleBack = Animator.StringToHash("player_2");
    private static readonly int HashWalkFront = Animator.StringToHash("player_3");    // caminar hacia el frente
    private static readonly int HashWalkBack = Animator.StringToHash("player_run_2"); // caminar hacia arriba
    private static readonly int HashRun = Animator.StringToHash("player_run");   // caminar hacia los lados
    private static readonly int HashDie = Animator.StringToHash("player_die");

    private const string StateSwordFront = "player_sword_1";
    private const string StateSwordSide = "player_sword_2";
    private const string StateSwordBack = "player_sword_3";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isDead || isAttacking)
            return;

        ReadInput();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (isDead || isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        bool running = Keyboard.current != null &&
                       Keyboard.current.leftShiftKey.isPressed;

        float speed = running ? runSpeed : walkSpeed;

        rb.linearVelocity = movement * speed;
    }

    void LateUpdate()
    {
        sr.flipX = !facingRight;
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
        {
            movement = Vector2.zero;
            return;
        }

        movement = new Vector2(
            (Keyboard.current.dKey.isPressed ? 1 : 0) -
            (Keyboard.current.aKey.isPressed ? 1 : 0),

            (Keyboard.current.wKey.isPressed ? 1 : 0) -
            (Keyboard.current.sKey.isPressed ? 1 : 0)
        ).normalized;

        float x = movement.x;
        float y = movement.y;

        if (x > 0.01f)
        {
            lastDir = Dir.Right;
            facingRight = true;
        }
        else if (x < -0.01f)
        {
            lastDir = Dir.Left;
            facingRight = false;
        }
        else if (y > 0.01f)
        {
            lastDir = Dir.Up;
        }
        else if (y < -0.01f)
        {
            lastDir = Dir.Down;
        }

        if (Keyboard.current.jKey.wasPressedThisFrame ||
            Keyboard.current.zKey.wasPressedThisFrame)
        {
            StartCoroutine(AttackRoutine());
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Die();
        }
    }

    private void UpdateAnimation()
    {
        if (movement == Vector2.zero)
        {
            switch (lastDir)
            {
                case Dir.Up:
                    Play(HashIdleBack);
                    break;

                case Dir.Left:
                case Dir.Right:
                    Play(HashIdleSide);
                    break;

                default:
                    Play(HashIdleFront);
                    break;
            }

            return;
        }

        // Movimiento: animación según dirección
        switch (lastDir)
        {
            case Dir.Down:
                Play(HashWalkFront); // player_3
                break;

            case Dir.Up:
                Play(HashWalkBack);  // player_run_2
                break;

            default:
                Play(HashRun);       // player_run (lados)
                break;
        }
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        if (isDead)
            yield break;

        isAttacking = true;

        movement = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        string swordState;

        switch (lastDir)
        {
            case Dir.Up:
                swordState = StateSwordBack;   // player_sword_3
                break;

            case Dir.Down:
                swordState = StateSwordFront;  // player_sword_1
                break;

            default:
                swordState = StateSwordSide;   // player_sword_2
                break;
        }

        // Posicionar hitbox según dirección
        if (attackHitbox != null)
        {
            switch (lastDir)
            {
                case Dir.Down: attackHitbox.transform.localPosition = new Vector2(0f, -0.5f); break;
                case Dir.Up: attackHitbox.transform.localPosition = new Vector2(0f, 0.5f); break;
                case Dir.Right: attackHitbox.transform.localPosition = new Vector2(0.5f, 0f); break;
                case Dir.Left: attackHitbox.transform.localPosition = new Vector2(-0.5f, 0f); break;
            }
        }

        anim.Play(swordState, 0, 0f);

        yield return null;
        yield return null;

        // Activar hitbox a la mitad de la animación
        attackHitbox?.Enable();
        yield return new WaitForSeconds(hitboxDuration);
        attackHitbox?.Disable();

        while (
            anim.GetCurrentAnimatorStateInfo(0).IsName(swordState) &&
            anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f
        )
        {
            yield return null;
        }

        isAttacking = false;
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isAttacking = false;

        movement = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        anim.Play("player_die", 0, 0f);
    }

    private void Play(int stateHash)
    {
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);

        if (state.shortNameHash != stateHash)
        {
            anim.Play(stateHash);
        }
    }
}   