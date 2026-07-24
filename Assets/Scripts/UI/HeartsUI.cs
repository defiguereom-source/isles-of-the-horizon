using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HeartsUI : MonoBehaviour
{
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;

    void OnEnable()
    {
        StartCoroutine(WaitForPlayerHealth());
    }

    IEnumerator WaitForPlayerHealth()
    {
        while (PlayerHealth.Instance == null)
            yield return null;

        PlayerHealth.Instance.OnHealthChanged += UpdateHearts;
        UpdateHearts();
    }

    void OnDisable()
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged -= UpdateHearts;
    }

    void UpdateHearts()
    {
        int hp = PlayerHealth.Instance.currentHP;
        int perHeart = PlayerHealth.Instance.pointsPerHeart;

        for (int i = 0; i < hearts.Length; i++)
        {
            int heartMinHP = i * perHeart;
            int hpInThisHeart = hp - heartMinHP;

            if (hpInThisHeart >= perHeart)
                hearts[i].sprite = fullHeart;
            else if (hpInThisHeart > 0)
                hearts[i].sprite = halfHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }
}