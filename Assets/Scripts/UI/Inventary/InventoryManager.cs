using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public int dineroTotal = 0;
    public List<Tienda> itemsInventario = new List<Tienda>();
    public int maxObjetos = 4;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool AgregarItem(Tienda item, int precio)
    {
        if (precio <= dineroTotal && itemsInventario.Count < maxObjetos)
        {
            dineroTotal -= precio;
            itemsInventario.Add(item);
            return true;
        }
        return false;
    }

    public void RemoverItem(Tienda item)
    {
        itemsInventario.Remove(item);
    }
}