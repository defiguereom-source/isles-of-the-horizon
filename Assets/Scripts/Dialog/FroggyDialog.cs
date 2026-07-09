using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.InputSystem;

public class FroggyDialog : MonoBehaviour
{
    [SerializeField] private GameObject dialogMark;
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text dialogText;

    [SerializeField, TextArea(4, 6)] private string[] dialogLines;

    [Header("Combate")]
    [SerializeField] private FroggyIA froggyBoss; // arrastra el mismo objeto que tiene FroggyIA
    public UnityEvent onDialogComplete; // opcional, por si quieres enganchar otras cosas desde el inspector

    private bool isPlayerInRange;
    private bool isDialogActive;
    private int currentLine;
    private bool combatStarted; // true en cuanto arranca la pelea: desactiva toda interaccion

    void Start()
    {
        dialogMark.SetActive(false);
        dialogPanel.SetActive(false);
    }

    void Update()
    {
        if (combatStarted) return; // ya empezo la pelea, no se puede volver a hablar con el sapo

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
            EndDialog(completedNaturally: true);
        }
    }

    private void EndDialog(bool completedNaturally = false)
    {
        isDialogActive = false;
        dialogPanel.SetActive(false);

        Debug.Log($"[FroggyDialog] EndDialog llamado. completedNaturally={completedNaturally}, froggyBoss={(froggyBoss != null ? "asignado" : "NULL")}");

        if (completedNaturally)
        {
            // El dialogo termino de forma natural (no por salir del rango): arranca el combate
            dialogMark.SetActive(false);
            combatStarted = true; // a partir de aqui, ya no se puede volver a interactuar con el dialogo

            if (froggyBoss != null)
            {
                froggyBoss.StartCombat();
            }

            onDialogComplete?.Invoke();
        }
        else if (isPlayerInRange && !combatStarted)
        {
            dialogMark.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (combatStarted) return; // ya empezo el combate, ignorar el trigger

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
                EndDialog(completedNaturally: false);
            }
        }
    }
}