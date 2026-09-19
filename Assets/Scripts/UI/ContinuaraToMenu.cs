using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ContinuaraToMenu : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private float duration = 4f;
    [SerializeField] private bool allowSkipWithKey = true;

    private bool isLoading;

    void Start()
    {
        Time.timeScale = 1f;
        StartCoroutine(GoToMenuAfterDelay());
    }

    void Update()
    {
        if (!allowSkipWithKey || isLoading) return;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            LoadMenu();
    }

    private IEnumerator GoToMenuAfterDelay()
    {
        yield return new WaitForSeconds(duration);
        LoadMenu();
    }

    private void LoadMenu()
    {
        if (isLoading) return;
        isLoading = true;
        SceneManager.LoadScene(menuSceneName);
    }
}