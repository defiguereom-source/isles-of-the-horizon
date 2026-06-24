using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(GridMoverFlam))]
public class EnemyAI : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    [Header("Tiempos de comportamiento normal")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;
    [Header("Detección de persecución")]
    public float chaseDistance = 5f;   // si el jugador está más cerca que esto, persigue
    public float catchDistance = 0.6f; // distancia para considerar que lo "alcanzó"
    public int chaseSteps = 5;         // pasos máximos persiguiendo de corrido
    public float chaseStepDelay = 0.05f;
    private GridMoverFlam mover;   
    private Coroutine behaviourCoroutine;

    void Start()
    {
        mover = GetComponent<GridMoverFlam>();
        behaviourCoroutine = StartCoroutine(BehaviourRoutine());
    }

    IEnumerator BehaviourRoutine()
    {
        while (true)
        {
            float distToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            // La persecución tiene prioridad sobre todo lo demás
            if (distToPlayer < chaseDistance)
            {
                yield return StartCoroutine(ChaseRoutine());
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

    IEnumerator ChaseRoutine()
    {
        for (int i = 0; i < chaseSteps; i++)
        {
            if (player == null) break;
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < catchDistance) break; // ya lo alcanzó, corta antes
            if (dist > chaseDistance) break; // el jugador se escapó del rango, deja de perseguir

            bool moved = false;
            foreach (Direction dir in GetChaseDirectionsOrdered())
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
                yield return new WaitForSeconds(chaseStepDelay);
            }
            else
            {
                // Bloqueado: ninguna de las 4 direcciones sirve, esperar antes de reintentar
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    // Devuelve las direcciones ordenadas de mejor a peor para acercarse al jugador
    List<Direction> GetChaseDirectionsOrdered()
    {
        Vector2 toward = (Vector2)player.position - (Vector2)transform.position;
        Direction primary, secondary;
        if (Mathf.Abs(toward.x) > Mathf.Abs(toward.y))
        {
            primary = toward.x > 0 ? Direction.Right : Direction.Left;
            secondary = toward.y > 0 ? Direction.Up : Direction.Down;
        }
        else
        {
            primary = toward.y > 0 ? Direction.Up : Direction.Down;
            secondary = toward.x > 0 ? Direction.Right : Direction.Left;
        }
        return new List<Direction>
        {
            primary,             // 1ra opción: eje dominante acercándose
            secondary,           // 2da opción: eje perpendicular acercándose
            Opposite(secondary), // 3ra opción: perpendicular alejándose (mejor que nada)
            Opposite(primary)    // 4ta opción: último recurso, directo lejos del jugador
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}