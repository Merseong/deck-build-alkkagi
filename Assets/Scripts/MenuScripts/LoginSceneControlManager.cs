using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginSceneControlManager : MonoBehaviour
{
    [Header("Login data")]
    [SerializeField] private string idValue;
    [SerializeField] private string passwordValue;
    [SerializeField] private bool isAutoLogin;

    public void SetIdValue(string id)
    {
        idValue = id;
    }

    public void SetPasswordValue(string pass)
    {
        passwordValue = pass;
    }

    public void SetAutoLogin(bool auto)
    {
        isAutoLogin = auto;
    }

    public void OnLoginButtonClicked()
    {
        Debug.Log($"{idValue} / {passwordValue} / {isAutoLogin}");
        NetworkManager.Inst.ConnectServer();
    }
}
