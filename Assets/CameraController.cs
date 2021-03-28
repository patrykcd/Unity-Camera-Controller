using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private int movementSpeed = 5;
    [SerializeField] private int boost = 10;
    [SerializeField] private string speedometer = string.Empty;

    private enum CurrentAction
    {
        None,
        Move,
        Pan
    }

    private CurrentAction _currentAction = CurrentAction.None;

    private int _speed;
    private Vector3 _horizontalDirection = Vector3.zero;
    private Vector3 _verticalDirection = Vector3.zero;
    private Vector3 _lastFramePosition = Vector3.zero;
    private Quaternion _rotation = Quaternion.identity;

    private InputAction _startMovementInputAction;
    private InputAction[] _movementInputActions;

    private InputAction _startPanInputAction;
    private InputAction _shiftInputAction;

    private void Awake()
    {
        _speed = movementSpeed;

        var playerInput = GetComponent<PlayerInput>();
        var actions = playerInput.actions;

        _startMovementInputAction = actions.FindAction("StartMovement", true);
        var moveHorizontallyAction = actions.FindAction("MoveHorizontally", true);
        var moveVerticallyAction = actions.FindAction("MoveVertically", true);
        var boostAction = actions.FindAction("Boost", true);
        var rotateAction = actions.FindAction("Rotate", true);
        _startPanInputAction = actions.FindAction("StartPan", true);
        _shiftInputAction = actions.FindAction("Pan", this);

        _startMovementInputAction.performed += OnStartMovement;
        moveHorizontallyAction.performed += OnMoveHorizontally;
        moveVerticallyAction.performed += OnMoveVertically;
        boostAction.performed += OnBoost;
        rotateAction.performed += OnRotate;
        _startPanInputAction.performed += OnStartPan;
        _shiftInputAction.performed += OnPan;

        _movementInputActions = new[]
        {
            moveHorizontallyAction,
            moveVerticallyAction,
            boostAction,
            rotateAction
        };

        foreach (var action in _movementInputActions)
        {
            action.Disable();
        }

        _shiftInputAction.Disable();

        _rotation = transform.rotation;
    }

    private void Update()
    {
        var moveDirection = Vector3.zero;
        switch (_currentAction)
        {
            case CurrentAction.Move:
                moveDirection = (_horizontalDirection + _verticalDirection).normalized;
                break;
            case CurrentAction.Pan:
                moveDirection = _verticalDirection;
                break;
            case CurrentAction.None:
                return;
        }

        var delta = Time.deltaTime;
        var t = transform;
        t.Translate(moveDirection * _speed * delta);
        t.rotation = _rotation;

        if (_currentAction == CurrentAction.Pan)
        {
            _verticalDirection = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        var delta = Time.deltaTime;
        var position = transform.position;
        var distance = Vector3.Distance(_lastFramePosition, position);
        _lastFramePosition = position;
        var diff = 1 / delta;
        speedometer = $"{distance * diff}m/{delta * diff}s";
    }

    private void OnStartMovement(InputAction.CallbackContext context)
    {
        if (_currentAction != CurrentAction.None && _currentAction != CurrentAction.Move) return;

        var pressed = context.ReadValueAsButton();
        foreach (var action in _movementInputActions)
        {
            if (pressed)
            {
                action.Enable();
                _currentAction = CurrentAction.Move;
            }
            else
            {
                action.Disable();
                _currentAction = CurrentAction.None;
            }
        }

        if (!pressed)
        {
            _horizontalDirection = Vector3.zero;
            _verticalDirection = Vector3.zero;
        }
    }

    private void OnMoveHorizontally(InputAction.CallbackContext context)
    {
        _horizontalDirection = Quaternion.AngleAxis(90, Vector3.right) * context.ReadValue<Vector2>();
    }

    private void OnMoveVertically(InputAction.CallbackContext context)
    {
        _verticalDirection = context.ReadValue<Vector2>();
    }

    private void OnBoost(InputAction.CallbackContext context)
    {
        _speed = movementSpeed + (Convert.ToInt32(context.ReadValueAsButton()) * boost);
    }

    private void OnRotate(InputAction.CallbackContext context)
    {
        var rotation = _rotation;
        var delta = context.ReadValue<Vector2>();

        rotation.eulerAngles += new Vector3(0, delta.x, 0);
        rotation *= Quaternion.Euler(-delta.y, 0, 0);

        var clamped = rotation.eulerAngles;
        if (clamped.x < 180)
        {
            clamped.x = Mathf.Clamp(clamped.x + clamped.z, 0, 90);
        }
        else
        {
            clamped.x = Mathf.Clamp(clamped.x - clamped.z, 270, 360);
        }

        rotation.eulerAngles = clamped;
        _rotation = rotation;
    }

    private void OnStartPan(InputAction.CallbackContext context)
    {
        if (_currentAction != CurrentAction.None && _currentAction != CurrentAction.Pan) return;

        if (context.ReadValueAsButton())
        {
            _shiftInputAction.Enable();
            _currentAction = CurrentAction.Pan;
        }
        else
        {
            _shiftInputAction.Disable();
            _currentAction = CurrentAction.None;
        }
    }

    private void OnPan(InputAction.CallbackContext context)
    {
        Vector3 direction = context.ReadValue<Vector2>();
        _verticalDirection = direction;
    }
}