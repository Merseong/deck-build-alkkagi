using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TouchManager : SingletonBehavior<TouchManager>
{
    private PlayerInput playerInput;

    public InputAction touchPressAction;
    public InputAction touchPositionAction;
    private InputAction touchStartPositionAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        touchPositionAction = playerInput.actions["TouchPosition"];
        touchStartPositionAction = playerInput.actions["TouchStartPosition"];
        touchPressAction = playerInput.actions["TouchPress"];
    }
}