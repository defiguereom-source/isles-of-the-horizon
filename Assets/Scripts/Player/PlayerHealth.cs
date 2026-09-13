using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    
    private static int savedHP = -1;

    public int maxHearts = 3;
    public int pointsPerHeart = 20;
    public int maxHP => maxHearts * pointsPerHeart;
    public int currentHP;

    public event Action OnHealthChanged;

    void Awake()
    {
        Instance = this;

        if (savedHP < 0)
            currentHP = maxHP;       
        else
            currentHP = savedHP;    
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        savedHP = currentHP;
        OnHealthChanged?.Invoke();

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        savedHP = currentHP;
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