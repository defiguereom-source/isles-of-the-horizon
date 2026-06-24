using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(GridMover))]
public class ChickenAI : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    [Header("Tiempos de comportamiento normal")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;
    [Header("Detección de huida")]
    public float fleeDistance = 3f;   // si el jugador está más cerca que esto, huye
    public float safeDistance = 4.5f; // a esta distancia se considera "a salvo"
    public int fleeSteps = 5;         // pasos máximos huyendo de corrido
    public float fleeStepDelay = 0.05f;
    private GridMover mover;
    private Coroutine behaviourCoroutine;
    void Start()
    {
        mover = GetComponent<GridMover>();
        behaviourCoroutine = StartCoroutine(BehaviourRoutine());
    }
    IEnumerator BehaviourRoutine()
    {
        while (true)
        {
            float distToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;
            // El miedo tiene prioridad sobre todo lo demás
            if (distToPlayer < fleeDistance)
            {
                yield return StartCoroutine(FleeRoutine());
                continue;
            }
            // Comportamiento normal: esperar un poco antes de decidir
            float wait = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(wait);
            if (mover.IsMoving) continue;
            // 50% camina, 50% se queda quieto
            if (Random.value < 0.5f)
            {
                Direction randomDir = (Direction)Random.Range(0, 4);
                mover.TryMove(randomDir);
            }
        }
    }
    IEnumerator FleeRoutine()
    {
        for (int i = 0; i < fleeSteps; i++)
        {
            if (player == null) break;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist > safeDistance) break; // ya está a salvo, corta antes

            bool moved = false;
            foreach (Direction dir in GetFleeDirectionsOrdered())
            {
                if (mover.TryMove(dir))
                {
                    moved = true;
                    break;
                }
            }

            if (moved)
            {
                while (mover.IsMoving) yield return null;
                yield return new WaitForSeconds(fleeStepDelay);
            }
            else
            {
                // Está acorralada: ninguna de las 4 direcciones sirve, esperar un poco más
                // antes de reintentar para no quedar "vibrando" contra la pared
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
    // Devuelve las direcciones ordenadas de mejor a peor para alejarse del jugador
    List<Direction> GetFleeDirectionsOrdered()
    {
        Vector2 away = (Vector2)transform.position - (Vector2)player.position;
        Direction primary, secondary;
        if (Mathf.Abs(away.x) > Mathf.Abs(away.y))
        {
            primary = away.x > 0 ? Direction.Right : Direction.Left;
            secondary = away.y > 0 ? Direction.Up : Direction.Down;
        }
        else
        {
            primary = away.y > 0 ? Direction.Up : Direction.Down;
            secondary = away.x > 0 ? Direction.Right : Direction.Left;
        }
        return new List<Direction>
        {
            primary,            // 1ra opción: eje dominante alejándose
            secondary,          // 2da opción: eje perpendicular alejándose
            Opposite(secondary),// 3ra opción: perpendicular acercándose (mejor que nada)
            Opposite(primary)   // 4ta opción: último recurso, directo hacia el jugador
        };
    }
    Direction Opposite(Direction d) => d switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => d
    };
    // Útil para debug: dibuja los radios en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
    }
}