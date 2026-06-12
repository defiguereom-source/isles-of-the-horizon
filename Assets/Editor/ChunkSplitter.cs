using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class ChunkSplitter : EditorWindow
{
    public GameObject mundoRoot;   // tu GameObject "Mundo"
    public int chunkSize = 16;

    [MenuItem("Tools/Chunk Splitter")]
    public static void ShowWindow()
    {
        GetWindow<ChunkSplitter>("Chunk Splitter");
    }

    void OnGUI()
    {
        GUILayout.Label("Dividir Tilemap en Chunks", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        mundoRoot = (GameObject)EditorGUILayout.ObjectField(
            "Mundo (root)", mundoRoot, typeof(GameObject), true);

        chunkSize = EditorGUILayout.IntField("Chunk Size (tiles)", chunkSize);

        EditorGUILayout.HelpBox(
            "Busca 'Map' y 'Camp' dentro de Mundo y los divide en chunks.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Dividir en Chunks", GUILayout.Height(40)))
        {
            if (mundoRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Asigna el GameObject 'Mundo'.", "OK");
                return;
            }
            SplitAll();
        }
    }

    void SplitAll()
    {
        // Buscar Map y Camp dentro de Mundo
        Transform mapTransform = mundoRoot.transform.Find("Map");
        Transform campTransform = mundoRoot.transform.Find("Camp");

        if (mapTransform == null && campTransform == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontraron 'Map' ni 'Camp' dentro de Mundo.", "OK");
            return;
        }

        if (mapTransform != null) SplitTilemap(mapTransform.gameObject, "Map");
        if (campTransform != null) SplitTilemap(campTransform.gameObject, "Camp");

        // Agregar ChunkManager a Mundo si no lo tiene
        if (mundoRoot.GetComponent<ChunkManager>() == null)
        {
            var cm = mundoRoot.AddComponent<ChunkManager>();
            cm.chunkSize = chunkSize;
            cm.loadRadius = 1;
            cm.unloadRadius = 2;

            if (mapTransform != null) cm.mapParent = mapTransform.gameObject;
            if (campTransform != null) cm.campParent = campTransform.gameObject;

            Debug.Log("ChunkManager agregado a Mundo. Asigna el Player en el Inspector.");
        }

        EditorUtility.DisplayDialog("Listo",
            "Chunks creados correctamente.\nAsigna el Player en ChunkManager.", "OK");

        Debug.Log($"[ChunkSplitter] Dividido en chunks de {chunkSize}x{chunkSize}.");
    }

    void SplitTilemap(GameObject parent, string parentName)
    {
        Tilemap originalTilemap = parent.GetComponent<Tilemap>();
        if (originalTilemap == null)
        {
            Debug.LogWarning($"[ChunkSplitter] '{parentName}' no tiene Tilemap. Saltando.");
            return;
        }

        originalTilemap.CompressBounds();
        BoundsInt bounds = originalTilemap.cellBounds;

        // Calcular rango de chunks
        int startCX = Mathf.FloorToInt((float)bounds.xMin / chunkSize);
        int startCY = Mathf.FloorToInt((float)bounds.yMin / chunkSize);
        int endCX = Mathf.FloorToInt((float)(bounds.xMax - 1) / chunkSize);
        int endCY = Mathf.FloorToInt((float)(bounds.yMax - 1) / chunkSize);

        int total = (endCX - startCX + 1) * (endCY - startCY + 1);
        int done = 0;

        for (int cx = startCX; cx <= endCX; cx++)
        {
            for (int cy = startCY; cy <= endCY; cy++)
            {
                done++;
                EditorUtility.DisplayProgressBar(
                    "Dividiendo chunks...",
                    $"{parentName} → Chunk_{cx}_{cy} ({done}/{total})",
                    (float)done / total);

                // Crear GameObject hijo
                GameObject chunkGO = new GameObject($"Chunk_{cx}_{cy}");
                chunkGO.transform.SetParent(parent.transform, false);

                // Agregar Tilemap y Renderer
                Tilemap chunkTilemap = chunkGO.AddComponent<Tilemap>();
                TilemapRenderer chunkRend = chunkGO.AddComponent<TilemapRenderer>();

                // Copiar sorting order del original
                TilemapRenderer origRend = parent.GetComponent<TilemapRenderer>();
                if (origRend != null)
                {
                    chunkRend.sortingLayerID = origRend.sortingLayerID;
                    chunkRend.sortingOrder = origRend.sortingOrder;
                }

                // Rango de tiles de este chunk
                int x0 = cx * chunkSize;
                int y0 = cy * chunkSize;

                for (int x = x0; x < x0 + chunkSize; x++)
                {
                    for (int y = y0; y < y0 + chunkSize; y++)
                    {
                        Vector3Int tilePos = new Vector3Int(x, y, 0);
                        TileBase tile = originalTilemap.GetTile(tilePos);
                        if (tile == null) continue;

                        chunkTilemap.SetTile(tilePos, tile);
                        // Copiar flags y color
                        chunkTilemap.SetTileFlags(tilePos,
                            originalTilemap.GetTileFlags(tilePos));
                        chunkTilemap.SetColor(tilePos,
                            originalTilemap.GetColor(tilePos));
                    }
                }

                // Si el chunk quedó vacío, eliminarlo
                chunkTilemap.CompressBounds();
                if (chunkTilemap.cellBounds.size == Vector3Int.zero)
                {
                    DestroyImmediate(chunkGO);
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(chunkGO, "Create Chunk");
            }
        }

        EditorUtility.ClearProgressBar();

        // Desactivar el Tilemap original (queda como referencia)
        originalTilemap.enabled = false;
        parent.GetComponent<TilemapRenderer>().enabled = false;

        Debug.Log($"[ChunkSplitter] '{parentName}' dividido correctamente.");
    }
}
