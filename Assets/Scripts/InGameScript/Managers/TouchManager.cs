using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;

public class TouchManager : SingletonBehavior<TouchManager>
{
    private PlayerInput playerInput;

    public InputAction touchPressAction;
    private InputAction touchPositionAction;
    private InputAction touchStartPositionAction;
    private InputAction primaryTouchPressAction;
    private InputAction touchDeltaAction;
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();

#if     UNITY_EDITOR
        Debug.Log("dfdf");
        touchPositionAction = playerInput.actions["_TouchPosition"];
        touchPressAction = playerInput.actions["_TouchPress"];
        touchDeltaAction = playerInput.actions["_TouchDelta"];

#elif   UNITY_STANDALONE
        touchPositionAction = playerInput.actions["TouchPosition"];
        touchPressAction = playerInput.actions["TouchPress"];
        touchDeltaAction = playerInput.actions["TouchDelta"];
#endif

        primaryTouchPressAction = playerInput.actions["PrimaryTouch"];
        touchStartPositionAction = playerInput.actions["TouchStartPosition"];
    }

    ///<summary>
    ///Return Current Touch Position(Screen)
    ///</summary>
    public Vector3 GetTouchPosition()
    {
        if(Util.IsPointerOverUIObject()) return new Vector2(-9999, -9999);
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