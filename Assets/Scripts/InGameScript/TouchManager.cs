using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TouchManager : SingletonBehavior<TouchManager>
{
    private PlayerInput playerInput;

    public InputAction touchPressAction;
    private InputAction touchPositionAction;
    private InputAction touchStartPositionAction;
    private InputAction primaryTouchPressAction;
    private InputAction touchDeltaAction;
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        touchPositionAction = playerInput.actions["TouchPosition"];
        touchStartPositionAction = playerInput.actions["TouchStartPosition"];
        touchPressAction = playerInput.actions["TouchPress"];
        primaryTouchPressAction = playerInput.actions["PrimaryTouch"];
        touchDeltaAction = playerInput.actions["TouchDelta"];
    }

    ///<summary>
    ///Return Current Touch Position(Screen)
    ///</summary>
    public Vector3 GetTouchPosition()
    {
        return touchPositionAction.ReadValue<Vector2>();
    }

    ///<summary>
    ///Return Current Touch Start Position(Screen)
    ///</summary>
    public Vector3 GetTouchStartPosition()
    {
        return touchStartPositionAction.ReadValue<Vector2>();
    }

    ///<summary>
    ///Return difference of position between last frame and current fram(delta)
    ///</summary>
    public Vector3 GetTouchDelta()
    {
        return touchDeltaAction.ReadValue<Vector2>();
    }
}