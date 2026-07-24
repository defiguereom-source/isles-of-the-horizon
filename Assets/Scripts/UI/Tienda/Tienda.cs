using UnityEngine;

[CreateAssetMenu(fileName = "Objeto", menuName = "Objeto Tienda")]
public class Tienda : ScriptableObject
{
    public Sprite imagenObject;
    public string textObject;
    public int PrecioObject;
}