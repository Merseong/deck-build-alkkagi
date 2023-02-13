using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSceneControlManager : SingletonBehavior<MenuSceneControlManager>
{
    private void Start()
    {
        NetworkManager.Inst.RefreshUI();
        NetworkManager.Inst.RefreshReceiveDelegate();
    }
}
