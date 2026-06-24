using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class MonsterType
    {
        public string name;        // Ej: "Flam", "Lizard", "Dragon Yellow"
        public GameObject prefab;
        public int minAmount = 2;
        public int maxAmount = 5;  // El Random.Range con int es exclusivo arriba, así que para incluir 5 usamos 6 al llamarlo
    }

    [Header("Tipos de Monstruos")]
    public MonsterType[] monsterTypes;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    void Start()
    {
        foreach (MonsterType type in monsterTypes)
        {
            if (type.prefab == null)
            {
                Debug.LogWarning($"El tipo de monstruo '{type.name}' no tiene prefab asignado.");
                continue;
            }

            int amount = Random.Range(type.minAmount, type.maxAmount + 1);
            SpawnMonsters(type.prefab, amount);
        }
    }

    void SpawnMonsters(GameObject prefab, int amount)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No hay spawn points asignados.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(prefab, point.position, Quaternion.identity);
        }
    }
}