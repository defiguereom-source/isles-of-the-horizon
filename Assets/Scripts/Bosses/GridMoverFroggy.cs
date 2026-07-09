using System;
using System.Collections;
using UnityEngine;

public class GridMoverFroggy : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] ownColliders;

    private bool isMoving;
    private bool isDead;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ownColliders = GetComponentsInChildren<Collider2D>();

        if (animator == null)
            Debug.LogError($"[{name}] No se encontro ningun Animator en este objeto ni en sus hijos.");

        if (spriteRenderer == null)
            Debug.LogError($"[{name}] No se encontro ningun SpriteRenderer en este objeto ni en sus hijos.");
    }

    public bool IsMoving => isMoving;
    public bool IsDead => isDead;

    public void FaceTarget(Transform target)
    {
        if (target == null || spriteRenderer == null) return;
        // Invertido: antes miraba para el lado contrario del player
        spriteRenderer.flipX = target.position.x > transform.position.x;
    }

    // Movimiento libre hacia el player mientras dura Froggy_Jump (fase de movimiento)
    public IEnumerator MoveTowardsRoutine(Transform target, float speed, float duration, float animSpeed)
    {
        if (isDead) yield break;

        isMoving = true;
        FaceTarget(target);

        if (animator != null)
        {
            animator.speed = animSpeed;
            animator.Play("Froggy_Jump", 0, 0f);
        }

        Vector3 targetPos = target != null ? target.position : transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (isDead) break;

            elapsed += Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        if (animator != null) animator.speed = 1f;
        isMoving = false;
    }

    // Telegraph antes del ataque (Froggy_Change)
    public IEnumerator PlayChangeRoutine(float duration, float animSpeed, Transform faceTarget)
    {
        if (isDead) yield break;

        FaceTarget(faceTarget);

        if (animator != null)
        {
            animator.speed = animSpeed;
            animator.Play("Froggy_Change", 0, 0f);
        }

        yield return new WaitForSeconds(duration);

        if (animator != null) animator.speed = 1f;
    }

    // Fase de ataque (Froggy_Atack). onHitCheck se llama a mitad de la animacion.
    public IEnumerator PlayAttackRoutine(float duration, float animSpeed, Transform faceTarget, Action onHitCheck)
    {
        if (isDead) yield break;

        FaceTarget(faceTarget);

        if (animator != null)
        {
            animator.speed = animSpeed;
            animator.Play("Froggy_Atack", 0, 0f);
        }

        yield return new WaitForSeconds(duration * 0.5f);
        onHitCheck?.Invoke();

        yield return new WaitForSeconds(duration * 0.5f);

        if (animator != null) animator.speed = 1f;
    }

    // Descanso (Froggy_Idle), ventana en la que el player puede pegarle
    public void PlayIdle()
    {
        if (isDead) return;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play("Froggy_Idle", 0, 0f);
        }
    }

    // Reaccion al recibir un golpe del player
    public void PlayDamage()
    {
        if (isDead) return;

        if (animator != null)
            animator.Play("Froggy_Damage", 0, 0f);
    }

    public void PlayDeath(float destroyDelay, Action onFinished = null)
    {
        if (isDead) return;

        StartCoroutine(DeathRoutine(destroyDelay, onFinished));
    }

    private IEnumerator DeathRoutine(float destroyDelay, Action onFinished)
    {
        isDead = true;
        isMoving = true;

        foreach (Collider2D col in ownColliders)
            col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;

        // No hay un clip de muerte en la lista que diste (Idle/Jump/Change/Atack/Damage).
        // Dejo Froggy_Damage como pose final. Si agregas un clip "Froggy_Death",
        // reemplaza la linea de abajo por: animator.Play("Froggy_Death", 0, 0f);
        if (animator != null)
            animator.Play("Froggy_Damage", 0, 0f);

        yield return new WaitForSeconds(destroyDelay);

        onFinished?.Invoke();
        gameObject.SetActive(false);
    }
}