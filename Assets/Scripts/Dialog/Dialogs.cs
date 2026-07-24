using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Script reutilizable de diálogos para NPCs.
/// - Tiene un diálogo "principal" (el primero que se ve).
/// - Después de terminar el principal, se pueden mostrar diálogos "secundarios"
///   (charlas cortas que aparecen en interacciones posteriores).
/// - Al terminar cualquier diálogo, la opción de hablar (dialogMark) se oculta
///   por unos segundos (cooldown) en vez de hacer desaparecer al personaje.
/// </summary>
public class NPCDialog : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private GameObject dialogMark;   // el ícono/indicador de "podés hablarme"
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text dialogText;

    [Header("Diálogo Principal")]
    [Tooltip("Líneas del diálogo principal. Se muestran la primera vez que se habla con el NPC.")]
    [SerializeField, TextArea(4, 6)] private string[] mainDialogLines;

    [Header("Diálogos Secundarios (después del principal)")]
    [Tooltip("Diálogos que aparecen en interacciones posteriores, una vez que ya se vio el principal.")]
    [SerializeField] private DialogSequence[] secondaryDialogs;
    [Tooltip("Si está tildado, los diálogos secundarios se van repitiendo en orden (cíclico). " +
             "Si no está tildado, una vez que se llega al último se queda repitiendo ese.")]
    [SerializeField] private bool loopSecondaryDialogs = true;

    [Header("Cooldown de interacción")]
    [Tooltip("Cuánto tiempo (en segundos) se oculta la opción de hablar (dialogMark) después de terminar un diálogo.")]
    [SerializeField] private float interactionCooldown = 2f;

    private SpriteRenderer spriteRenderer;
    private Collider2D interactionCollider;

    private bool isPlayerInRange;
    private bool isDialogActive;
    private bool isOnCooldown;

    private string[] currentDialogLines; // el set de líneas que se está mostrando ahora
    private int currentLine;
    private bool currentDialogIsMain;

    private bool hasFinishedMainDialog;
    private int secondaryDialogIndex;

    /// <summary>
    /// Un diálogo secundario: un set de líneas con nombre opcional para identificarlo en el editor.
    /// </summary>
    [Serializable]
    public class DialogSequence
    {
        public string name; // solo para identificarlo prolijo en el inspector, no se usa en runtime
        [TextArea(4, 6)] public string[] lines;
    }

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        interactionCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        dialogMark.SetActive(false);
        dialogPanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        bool pressedKey = Keyboard.current.fKey.wasPressedThisFrame;

        if (isPlayerInRange && !isDialogActive && !isOnCooldown && pressedKey)
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
        currentDialogLines = GetLinesForThisInteraction();

        if (currentDialogLines == null || currentDialogLines.Length == 0) return;

        isDialogActive = true;
        currentLine = 0;

        dialogMark.SetActive(false);
        dialogPanel.SetActive(true);
        dialogText.text = currentDialogLines[currentLine];
    }

    /// <summary>
    /// Decide qué líneas mostrar: el principal si todavía no se vio,
    /// o el siguiente diálogo secundario que corresponda.
    /// </summary>
    private string[] GetLinesForThisInteraction()
    {
        if (!hasFinishedMainDialog)
        {
            currentDialogIsMain = true;
            return mainDialogLines;
        }

        currentDialogIsMain = false;

        if (secondaryDialogs == null || secondaryDialogs.Length == 0)
        {
            // No hay diálogos secundarios configurados: repite el principal.
            return mainDialogLines;
        }

        DialogSequence sequence = secondaryDialogs[secondaryDialogIndex];
        return sequence.lines;
    }

    private void NextLine()
    {
        currentLine++;

        if (currentLine < currentDialogLines.Length)
        {
            dialogText.text = currentDialogLines[currentLine];
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

        if (currentDialogIsMain)
        {
            hasFinishedMainDialog = true;
        }
        else
        {
            AdvanceSecondaryDialogIndex();
        }

        StartCoroutine(InteractionCooldownRoutine());
    }

    private void AdvanceSecondaryDialogIndex()
    {
        if (secondaryDialogs == null || secondaryDialogs.Length == 0) return;

        secondaryDialogIndex++;

        if (secondaryDialogIndex >= secondaryDialogs.Length)
        {
            secondaryDialogIndex = loopSecondaryDialogs ? 0 : secondaryDialogs.Length - 1;
        }
    }

    /// <summary>
    /// Oculta la opción de hablar (dialogMark) por "interactionCooldown" segundos.
    /// El personaje sigue ahí, visible y con su collider activo; solo no se puede
    /// volver a interactuar hasta que pase el cooldown.
    /// </summary>
    private IEnumerator InteractionCooldownRoutine()
    {
        isOnCooldown = true;
        dialogMark.SetActive(false);

        yield return new WaitForSeconds(interactionCooldown);

        isOnCooldown = false;

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

            if (!isDialogActive && !isOnCooldown)
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