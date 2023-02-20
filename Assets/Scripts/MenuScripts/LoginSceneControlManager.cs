using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginSceneControlManager : MonoBehaviour
{
    [SerializeField] private GameObject WaitPanelObject;
    [SerializeField] private GameObject LoginCanvasObject;
    [SerializeField] private GameObject RegisterCanvasObject;

    [Header("Login data field")]
    [SerializeField] private bool isLoginDataSent;
    [SerializeField] private string idValue;
    [SerializeField] private string passwordValue;
    [SerializeField] private bool isAutoLogin;

    [Header("Register only field")]
    [SerializeField] private bool isRegisterSent;
    [SerializeField] private string passwordCheckValue;
    [SerializeField] private string nicknameValue;

    [Header("Alert object")]
    [SerializeField] private Image alertBackground;
    [SerializeField] private TextMeshProUGUI alertText;


    private void Awake()
    {
        LoginCanvasObject.SetActive(false);
        RegisterCanvasObject.SetActive(false);
        WaitPanelObject.SetActive(true);
    }

    private void Start()
    {
        NetworkManager.Inst.RefreshReceiveDelegate();
        NetworkManager.Inst.OnConnected = () =>
        {
            WaitPanelObject.SetActive(false);
            LoginCanvasObject.SetActive(true);
        };
        NetworkManager.Inst.AddReceiveDelegate(LoginDataReceiveNetworkAction);
        NetworkManager.Inst.ConnectServer();
    }

    #region UI actions
    public void SetIdValue(string id)
    {
        idValue = id;
    }

    public void SetPasswordValue(string pass)
    {
        passwordValue = pass;
    }

    public void SetPasswordCheckValue(string passc)
    {
        passwordCheckValue = passc;
    }

    public void SetNicknameValue(string nickname)
    {
        nicknameValue = nickname;
    }

    public void SetAutoLogin(bool auto)
    {
        isAutoLogin = auto;
    }
    #endregion

    public bool ValidateRegisterForm(out string problem)
    {
        problem = "";
        if (!(idValue.Length > 0))
        {
            problem = "ID is empty";
            return false;
        }
        if (!(passwordValue.Length > 0))
        {
            problem = "Password is empty";
            return false;
        }
        if (passwordValue != passwordCheckValue)
        {
            problem = "Password and password check not matching";
            return false;
        }
        if (!(nicknameValue.Length > 0))
        {
            problem = "Nickname is empty";
            return false;
        }
        return true;
    }

    public void OnRegisterWindowButtonClicked()
    {
        WaitPanelObject.SetActive(false);
        LoginCanvasObject.SetActive(false);
        RegisterCanvasObject.SetActive(true);
        idValue = "";
        passwordValue = "";
        passwordCheckValue = "";
        nicknameValue = "";
    }

    public void OnCancelButtonClicked()
    {
        WaitPanelObject.SetActive(false);
        LoginCanvasObject.SetActive(true);
        RegisterCanvasObject.SetActive(false);
        idValue = "";
        passwordValue = "";
        passwordCheckValue = "";
        nicknameValue = "";
    }

    public void OnRegisterButtonClicked()
    {
        if (!ValidateRegisterForm(out string problem))
        {
            Alert(problem, Color.red);
            return;
        }

        RegisterSendNetworkAction(idValue, passwordValue, nicknameValue);
    }

    public void OnLoginButtonClicked()
    {
        if (idValue.Length == 0)
        {
            Alert("ID를 확인해주세요", Color.red);
            return;
        }
        if (passwordValue.Length == 0)
        {
            Alert("비밀번호를 확인해주세요", Color.red);
            return;
        }
        LoginDataSendNetworkAction(idValue, passwordValue);
    }

    private void LoginDataSendNetworkAction(string loginId, string password)
    {
        if (isLoginDataSent) return;
        isLoginDataSent = true;
        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = 1,
            message = $"{loginId}@{password}",
        }, PacketType.USER_LOGIN);
        LoginCanvasObject.SetActive(false);
        WaitPanelObject.SetActive(true);
    }

    private void RegisterSendNetworkAction(string loginId, string password, string nickname)
    {
        if (isRegisterSent) return;
        isRegisterSent = true;
        NetworkManager.Inst.SendData(new MessagePacket
        {
            senderID = 0,
            message = $"{loginId} {password} {nickname}",
        }, PacketType.USER_LOGIN);
        WaitPanelObject.SetActive(true);
    }

    private void LoginDataReceiveNetworkAction(Packet p)
    {
        if (p.Type != (short)PacketType.USER_LOGIN) return;

        if (isLoginDataSent)
        {
            var msg = UserDataPacket.Deserialize(p.Data);
            if (msg.isSuccess)
            {
                isLoginDataSent = false;
                NetworkManager.Inst.UserData = msg;
                NetworkManager.Inst.SetNetworkId(msg.uid);
                NetworkManager.Inst.AddReceiveDelegate(NetworkManager.Inst.ParsePacketAction);
                NetworkManager.Inst.RemoveReceiveDelegate(LoginDataReceiveNetworkAction);
                // 씬이동
                SceneManager.LoadScene("DeckChooseScene");
            }
            else
            {
                Alert("Failed,", Color.red);
                OnCancelButtonClicked();
            }
            isLoginDataSent = false;
        }
        else if(isRegisterSent)
        {
            var msg = MessagePacket.Deserialize(p.Data);
            if (msg.message == "true")
            {
                Alert("Success,", Color.green);
                OnCancelButtonClicked();
            }
            else
            {
                Alert("Failed,", Color.red);
                OnRegisterWindowButtonClicked();
            }
            isRegisterSent = false;
        }
    }

    private void Alert(string alertMessage, Color baseColor)
    {
        alertText.text = alertMessage;
        alertBackground.gameObject.SetActive(true);
        StartCoroutine(EAlert(baseColor));
    }

    private IEnumerator EAlert(Color baseColor)
    {
        float timer = 0;
        float duration = 0.5f;
        var newColor = baseColor;
        newColor.a = 1;
        alertBackground.color = newColor;
        alertText.color = newColor;

        yield return new WaitForSeconds(0.5f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            newColor.a = 1 - timer / duration;
            alertBackground.color = newColor;
            alertText.color = newColor;
            yield return null;
        }

        alertBackground.gameObject.SetActive(false);
    }
}
