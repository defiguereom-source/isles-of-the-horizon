using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class GregoryDialog : MonoBehaviour
{
    [SerializeField] private GameObject dialogMark;
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text dialogText;

    [SerializeField, TextArea(4, 6)] private string[] dialogLines;

    [Header("Pantalla de Misión Completada")]
    [Tooltip("Panel de UI con el mensaje 'Misión Completada'. Se activa apenas termina el diálogo.")]
    [SerializeField] private GameObject missionCompletePanel;
    [Tooltip("Si está tildado, Gregory desaparece y se muestra el panel apenas termina el diálogo (una sola vez).")]
    [SerializeField] private bool showMissionCompleteAfterDialog = true;
    [Tooltip("Cuánto tiempo (en segundos) se queda visible el panel antes de cerrarse solo. Se ignora si 'Wait For Input To Close' está tildado.")]
    [SerializeField] private float missionCompleteDuration = 2.5f;
    [Tooltip("Si está tildado, el panel no se cierra solo: espera a que el jugador presione F.")]
    [SerializeField] private bool waitForInputToClose = false;

    private SpriteRenderer spriteRenderer;
    private Collider2D interactionCollider;

    private bool isPlayerInRange;
    private bool isDialogActive;
    private int currentLine;

    private bool hasFinished;  // para que la secuencia de misión completada solo se dispare una vez
    private bool isFinishing;  // true mientras se muestra la pantalla de misión completada

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        interactionCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        dialogMark.SetActive(false);
        dialogPanel.SetActive(false);

        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
    }

    void Update()
    {
        if (isFinishing) return; // mientras se muestra la pantalla de misión completada, no acepta más input de diálogo

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

        // El diálogo terminó completo (se pasaron todas las líneas): dispara la pantalla de misión completada.
        if (showMissionCompleteAfterDialog && !hasFinished)
        {
            hasFinished = true;
            StartCoroutine(MissionCompleteRoutine());
            return;
        }

        if (isPlayerInRange)
        {
            dialogMark.SetActive(true);
        }
    }

    private IEnumerator MissionCompleteRoutine()
    {
        isFinishing = true;
        dialogMark.SetActive(false);

        // Ya no se puede volver a interactuar con él.
        if (interactionCollider != null)
            interactionCollider.enabled = false;

        // Gregory desaparece visualmente ni bien arranca la pantalla de misión completada.
        // (Se apaga solo el sprite, no todo el GameObject, para que esta corrutina pueda seguir corriendo.)
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(true);

        if (waitForInputToClose)
        {
            yield return null; // espera al menos un frame para no cerrar con la misma tecla que cerró el diálogo
            while (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(missionCompleteDuration);
        }

        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);

        // Ahora sí, se puede desactivar todo el GameObject.
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFinishing) return;

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
        if (isFinishing) return;

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