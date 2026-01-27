using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Makes a UI panel draggable and resizable.
/// Attach this to a panel with a RectTransform.
/// Drag from the header to move, drag from edges/corners to resize.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DraggableResizablePanel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Drag Settings")]
    [SerializeField] private float headerHeight = 25f;
    [SerializeField] private bool enableDrag = true;

    [Header("Resize Settings")]
    [SerializeField] private bool enableResize = true;
    [SerializeField] private float resizeEdgeThreshold = 10f;
    [SerializeField] private Vector2 minSize = new Vector2(200f, 150f);
    [SerializeField] private Vector2 maxSize = new Vector2(800f, 600f);

    [Header("Visual Feedback")]
    [SerializeField] private bool showResizeCursor = true;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private CanvasScaler _canvasScaler;

    private bool _isDragging;
    private bool _isResizing;
    private Vector2 _dragOffset;
    private ResizeDirection _resizeDir;

    private enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasScaler = _canvas?.GetComponent<CanvasScaler>();
    }

    void Start()
    {
        // Ensure we have a graphic for raycasting
        if (GetComponent<Graphic>() == null)
        {
            Image img = gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Bring to front
        transform.SetAsLastSibling();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

        // Check if we're in the resize zone
        _resizeDir = GetResizeDirection(localPoint);

        if (_resizeDir != ResizeDirection.None && enableResize)
        {
            _isResizing = true;
            _isDragging = false;
        }
        else if (IsInHeaderZone(localPoint) && enableDrag)
        {
            _isDragging = true;
            _isResizing = false;
            _dragOffset = (Vector2)_rectTransform.position - eventData.position;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            HandleDrag(eventData);
        }
        else if (_isResizing)
        {
            HandleResize(eventData);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Could change cursor here if needed
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isDragging && !_isResizing)
        {
            // Reset cursor
        }
    }

    private void HandleDrag(PointerEventData eventData)
    {
        Vector2 newPos = eventData.position + _dragOffset;

        // Clamp to screen bounds
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 panelSize = _rectTransform.sizeDelta * GetCanvasScale();

        // Get pivot-adjusted bounds
        float minX = panelSize.x * _rectTransform.pivot.x;
        float maxX = screenSize.x - panelSize.x * (1 - _rectTransform.pivot.x);
        float minY = panelSize.y * _rectTransform.pivot.y;
        float maxY = screenSize.y - panelSize.y * (1 - _rectTransform.pivot.y);

        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        _rectTransform.position = newPos;
    }

    private void HandleResize(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform ?? _rectTransform,
            eventData.position, eventData.pressEventCamera, out localPoint);

        Vector2 currentSize = _rectTransform.sizeDelta;
        Vector2 currentPos = _rectTransform.anchoredPosition;
        Vector2 delta = eventData.delta / GetCanvasScale();

        switch (_resizeDir)
        {
            case ResizeDirection.Right:
                currentSize.x += delta.x;
                break;

            case ResizeDirection.Left:
                currentSize.x -= delta.x;
                currentPos.x += delta.x;
                break;

            case ResizeDirection.Top:
                currentSize.y += delta.y;
                break;

            case ResizeDirection.Bottom:
                currentSize.y -= delta.y;
                currentPos.y += delta.y;
                break;

            case ResizeDirection.TopRight:
                currentSize.x += delta.x;
                currentSize.y += delta.y;
                break;

            case ResizeDirection.TopLeft:
                currentSize.x -= delta.x;
                currentSize.y += delta.y;
                currentPos.x += delta.x;
                break;

            case ResizeDirection.BottomRight:
                currentSize.x += delta.x;
                currentSize.y -= delta.y;
                currentPos.y += delta.y;
                break;

            case ResizeDirection.BottomLeft:
                currentSize.x -= delta.x;
                currentSize.y -= delta.y;
                currentPos.x += delta.x;
                currentPos.y += delta.y;
                break;
        }

        // Clamp size
        currentSize.x = Mathf.Clamp(currentSize.x, minSize.x, maxSize.x);
        currentSize.y = Mathf.Clamp(currentSize.y, minSize.y, maxSize.y);

        _rectTransform.sizeDelta = currentSize;
        _rectTransform.anchoredPosition = currentPos;
    }

    private bool IsInHeaderZone(Vector2 localPoint)
    {
        // Header is at the top of the panel
        float top = _rectTransform.rect.yMax;
        float headerBottom = top - headerHeight;

        return localPoint.y >= headerBottom && localPoint.y <= top;
    }

    private ResizeDirection GetResizeDirection(Vector2 localPoint)
    {
        Rect rect = _rectTransform.rect;
        float threshold = resizeEdgeThreshold;

        bool left = localPoint.x >= rect.xMin && localPoint.x <= rect.xMin + threshold;
        bool right = localPoint.x >= rect.xMax - threshold && localPoint.x <= rect.xMax;
        bool top = localPoint.y >= rect.yMax - threshold && localPoint.y <= rect.yMax;
        bool bottom = localPoint.y >= rect.yMin && localPoint.y <= rect.yMin + threshold;

        // Corners first
        if (top && left) return ResizeDirection.TopLeft;
        if (top && right) return ResizeDirection.TopRight;
        if (bottom && left) return ResizeDirection.BottomLeft;
        if (bottom && right) return ResizeDirection.BottomRight;

        // Then edges
        if (left) return ResizeDirection.Left;
        if (right) return ResizeDirection.Right;
        if (top) return ResizeDirection.Top;
        if (bottom) return ResizeDirection.Bottom;

        return ResizeDirection.None;
    }

    private float GetCanvasScale()
    {
        if (_canvas == null) return 1f;

        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return _canvas.scaleFactor;
        }

        return 1f;
    }

    /// <summary>
    /// Resets the panel to its default position and size.
    /// </summary>
    public void ResetToDefault(Vector2 defaultPosition, Vector2 defaultSize)
    {
        _rectTransform.anchoredPosition = defaultPosition;
        _rectTransform.sizeDelta = defaultSize;
    }
}
