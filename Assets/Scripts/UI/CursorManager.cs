using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    [SerializeField] private Texture2D cursorNormal;
    [SerializeField] private Texture2D cursorHover;
    [SerializeField] private Vector2 hotspotNormal = Vector2.zero;
    [SerializeField] private Vector2 hotspotHover = Vector2.zero;

    private bool isHovering = false;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        SetNormal();
    }

    void Update()
    {
        bool overButton = IsPointerOverButton();

        if (overButton && !isHovering)
        {
            SetHover();
            isHovering = true;
        }
        else if (!overButton && isHovering)
        {
            SetNormal();
            isHovering = false;
        }
    }

    private bool IsPointerOverButton()
    {
        if (EventSystem.current == null || Mouse.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject.GetComponentInParent<Button>() != null)
                return true;
        }
        return false;
    }

    private void SetNormal() => Cursor.SetCursor(cursorNormal, hotspotNormal, CursorMode.Auto);
    private void SetHover() => Cursor.SetCursor(cursorHover, hotspotHover, CursorMode.Auto);
}