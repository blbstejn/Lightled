using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera controller for panning and zooming in a 2D tactical game.
/// Supports WASD/Arrow keys, middle mouse drag, and scroll wheel zoom.
/// Uses the new Input System.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float panBorderThickness = 10f; // Screen edge panning
    [SerializeField] private bool enableEdgePanning = true;
    [SerializeField] private bool enableMiddleMousePan = true;
    [SerializeField] private bool enableRightMousePan = true;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;

    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 30f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 20f;

    private Camera _camera;
    private Vector3 _dragOrigin;
    private bool _isDragging;

    void Start()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    void Update()
    {
        HandleKeyboardPan();
        HandleMousePan();
        HandleEdgePan();
        HandleZoom();
        ClampPosition();
    }

    /// <summary>
    /// Handle WASD and Arrow key panning.
    /// </summary>
    private void HandleKeyboardPan()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        Vector3 move = Vector3.zero;

        // WASD keys
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            move.y += 1;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            move.y -= 1;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            move.x -= 1;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            move.x += 1;

        if (move != Vector3.zero)
        {
            transform.position += move.normalized * panSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Handle middle mouse button or right mouse button drag panning.
    /// </summary>
    private void HandleMousePan()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        bool shouldStartDrag = (enableMiddleMousePan && mouse.middleButton.wasPressedThisFrame) ||
                               (enableRightMousePan && mouse.rightButton.wasPressedThisFrame);
        bool shouldContinueDrag = (enableMiddleMousePan && mouse.middleButton.isPressed) ||
                                  (enableRightMousePan && mouse.rightButton.isPressed);
        bool shouldEndDrag = (enableMiddleMousePan && mouse.middleButton.wasReleasedThisFrame) ||
                             (enableRightMousePan && mouse.rightButton.wasReleasedThisFrame);

        Vector2 mousePos = mouse.position.ReadValue();

        if (shouldStartDrag)
        {
            _dragOrigin = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            _isDragging = true;
        }

        if (_isDragging && shouldContinueDrag)
        {
            Vector3 currentPos = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            Vector3 difference = _dragOrigin - currentPos;
            transform.position += difference;
        }

        if (shouldEndDrag)
        {
            _isDragging = false;
        }
    }

    /// <summary>
    /// Handle screen edge panning (when mouse is near screen edges).
    /// </summary>
    private void HandleEdgePan()
    {
        if (!enableEdgePanning)
            return;

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector3 move = Vector3.zero;
        Vector2 mousePos = mouse.position.ReadValue();

        if (mousePos.x < panBorderThickness)
            move.x -= 1;
        if (mousePos.x > Screen.width - panBorderThickness)
            move.x += 1;
        if (mousePos.y < panBorderThickness)
            move.y -= 1;
        if (mousePos.y > Screen.height - panBorderThickness)
            move.y += 1;

        if (move != Vector3.zero)
        {
            transform.position += move.normalized * panSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Handle mouse scroll wheel zoom.
    /// </summary>
    private void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        float scroll = mouse.scroll.ReadValue().y / 120f; // Normalize scroll value

        if (Mathf.Abs(scroll) > 0.01f && _camera != null && _camera.orthographic)
        {
            Vector2 mousePos = mouse.position.ReadValue();

            // Zoom towards mouse position
            Vector3 mouseWorldPosBefore = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));

            _camera.orthographicSize -= scroll * zoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minZoom, maxZoom);

            // Adjust position to zoom towards mouse
            Vector3 mouseWorldPosAfter = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            transform.position += mouseWorldPosBefore - mouseWorldPosAfter;
        }
    }

    /// <summary>
    /// Clamp camera position within bounds if enabled.
    /// </summary>
    private void ClampPosition()
    {
        if (!useBounds)
            return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    /// <summary>
    /// Sets the camera bounds based on a grid size. Called by GridManager.
    /// </summary>
    public void SetBounds(int gridWidth, int gridHeight, float padding = 5f)
    {
        useBounds = true;
        minX = -padding;
        maxX = gridWidth + padding;
        minY = -padding;
        maxY = gridHeight + padding;
    }

    /// <summary>
    /// Centers the camera on a specific position.
    /// </summary>
    public void CenterOn(Vector3 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    /// <summary>
    /// Centers the camera on a specific unit.
    /// </summary>
    public void CenterOnUnit(BaseUnit unit)
    {
        if (unit != null && unit.OccupiedTile != null)
        {
            CenterOn(unit.OccupiedTile.transform.position);
        }
    }

    /// <summary>
    /// Resets zoom to default.
    /// </summary>
    public void ResetZoom()
    {
        if (_camera != null && _camera.orthographic)
        {
            _camera.orthographicSize = (minZoom + maxZoom) / 2f;
        }
    }
}
