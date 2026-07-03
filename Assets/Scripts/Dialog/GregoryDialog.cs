using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class GregoryDialog : MonoBehaviour
{
    [SerializeField] private GameObject dialogMark;
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text dialogText;

    [SerializeField, TextArea(4, 6)] private string[] dialogLines;

    private bool isPlayerInRange;
    private bool isDialogActive;
    private int currentLine;

    void Start()
    {
        dialogMark.SetActive(false);
        dialogPanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return; 

        bool pressedKey = Keyboard.current.fKey.wasPressedThisFrame;

        if (isPlayerInRange && !isDialogActive && pressedKey)
        {
            StartDialog();
        }
        else if (isDialogActive && pressedKey)
        {
            NextLine();
        }
    }

    private void StartDialog()
    {
        if (dialogLines.Length == 0) return;

        isDialogActive = true;
        currentLine = 0;

        dialogMark.SetActive(false);
        dialogPanel.SetActive(true);
        dialogText.text = dialogLines[currentLine];
    }

    private void NextLine()
    {
        currentLine++;

        if (currentLine < dialogLines.Length)
        {
            dialogText.text = dialogLines[currentLine];
        }
        else
        {
            EndDialog();
        }
    }

    private void EndDialog()
    {
        isDialogActive = false;
        dialogPanel.SetActive(false);

        if (isPlayerInRange)
        {
            dialogMark.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInRange = true;

            if (!isDialogActive)
            {
                dialogMark.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerInRange = false;
            dialogMark.SetActive(false);

            if (isDialogActive)
            {
                EndDialog();
            }
        }
    }
}