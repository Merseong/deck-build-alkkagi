using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class ResultPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] TextMeshProUGUI names;
    [SerializeField] TextMeshProUGUI localScore;
    [SerializeField] TextMeshProUGUI oppoScore;
    [SerializeField] TextMeshProUGUI moneyChange;
    [SerializeField] Image localHSTokenImage;
    [SerializeField] Image oppoHSTokenImage;

    private void Start()
    {
        localHSTokenImage.gameObject.SetActive(false);
        oppoHSTokenImage.gameObject.SetActive(false);
    }

    public void SetData(bool isWin)
    {
        var localData = NetworkManager.Inst.UserData;
        var oppoData = (GameManager.Inst.OppoPlayer as OppoPlayerBehaviour).oppoUserData;

        title.text = isWin ? "Victory!" : "Defeat...";
        names.text = $"{localData.nickname} vs {oppoData.nickname}";
        moneyChange.text = isWin ? "+3G" : "+1G";
        if (GameManager.Inst.isHSPerformed)
        {
            localScore.text = $"{localData.rating} {(isWin ? "+40" : "-20")}";
            oppoScore.text = $"{oppoData.rating} {(isWin ? "-20" : "+40")}";
            if (GameManager.Inst.isLocalHS)
            {
                localHSTokenImage.gameObject.SetActive(true);
                if (!isWin) // win with hs
                {
                    localHSTokenImage.color = new Color(1, 1, 1, 0.5f);
                }
                else
                {
                    moneyChange.text += " +3 Honor";
                }
            }
            else
            {
                oppoHSTokenImage.gameObject.SetActive(true);
                if (isWin)
                {
                    oppoHSTokenImage.color = new Color(1, 1, 1, 0.5f);
                }
            }
        }
        else
        {
            localScore.text = $"{localData.rating} {(isWin ? "+20" : "-10")}";
            oppoScore.text = $"{oppoData.rating} {(isWin ? "-10" : "+20")}";
        }
    }

    public void OnToMenuClicked()
    {
        NetworkManager.Inst.UpdateUserData((_) =>
        {
            SceneManager.LoadScene("DeckChooseScene");
        });
    }
}
