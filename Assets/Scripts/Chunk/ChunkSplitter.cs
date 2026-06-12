using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public GameObject mapParent;
    public GameObject campParent;

    [Header("Configuración")]
    public int chunkSize = 16;
    public int loadRadius = 1;
    public int unloadRadius = 2;

    private struct ChunkPair
    {
        public GameObject map;
        public GameObject camp;
    }

    private Dictionary<Vector2Int, ChunkPair> chunks = new();
    private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, 0);

    void Start()
    {
        RegisterChunks(mapParent,  isCamp: false);
        RegisterChunks(campParent, isCamp: true);

        foreach (var kv in chunks)
        {
            kv.Value.map?.SetActive(false);
            kv.Value.camp?.SetActive(false);
        }

        UpdateChunks(GetChunkCoord(player.position));
    }

    void Update()
    {
        Vector2Int current = GetChunkCoord(player.position);
        if (current != lastPlayerChunk)
            UpdateChunks(current);
    }

    void RegisterChunks(GameObject parent, bool isCamp)
    {
        if (parent == null) return;
        foreach (Transform child in parent.transform)
        {
            string[] parts = child.name.Split('_');
            if (parts.Length == 3 &&
                int.TryParse(parts[1], out int cx) &&
                int.TryParse(parts[2], out int cy))
            {
                Vector2Int coord = new Vector2Int(cx, cy);
                ChunkPair pair = chunks.ContainsKey(coord) ? chunks[coord] : new ChunkPair();
                if (isCamp) pair.camp = child.gameObject;
                else        pair.map  = child.gameObject;
                chunks[coord] = pair;
            }
        }
    }

    Vector2Int GetChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.y / chunkSize)
        );
    }

    void UpdateChunks(Vector2Int center)
    {
        foreach (var kv in chunks)
        {
            int dist = Mathf.Max(
                Mathf.Abs(kv.Key.x - center.x),
                Mathf.Abs(kv.Key.y - center.y)
            );
            bool active = dist <= loadRadius;

            if (kv.Value.map  != null && kv.Value.map.activeSelf  != active)
                kv.Value.map.SetActive(active);
            if (kv.Value.camp != null && kv.Value.camp.activeSelf != active)
                kv.Value.camp.SetActive(active);
        }
        lastPlayerChunk = center;
    }
}
