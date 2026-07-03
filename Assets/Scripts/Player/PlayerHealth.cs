using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    public int maxHearts = 3;
    public int pointsPerHeart = 20;
    public int maxHP => maxHearts * pointsPerHeart;     
    public int currentHP;

    public event Action OnHealthChanged;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        currentHP = maxHP; // empieza con 60
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke();

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke();
    }

    void Die()
    {
        Debug.Log("Game Over");

        Player player = GetComponent<Player>();
        if (player != null)
            player.Die();
    }
}