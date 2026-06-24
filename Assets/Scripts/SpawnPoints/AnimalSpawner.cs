using UnityEngine;

public class AnimalSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject roosterPrefab;
    public GameObject henPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Cantidad")]
    public int roosters = 2;
    public int hens = 4;

    void Start()
    {
        SpawnAnimals(roosterPrefab, roosters);
        SpawnAnimals(henPrefab, hens);
    }

    void SpawnAnimals(GameObject prefab, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Transform point =
                spawnPoints[Random.Range(0, spawnPoints.Length)];

            Instantiate(prefab, point.position, Quaternion.identity);
        }
    }
}