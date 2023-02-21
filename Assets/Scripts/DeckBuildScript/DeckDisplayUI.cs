using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckDisplayUI : MonoBehaviour
{
    public TextMeshProUGUI deckName;
    public RectTransform CardList;
    public int deckIdx;
    [SerializeField] Button selectButton;

    private void Start()
    {
        selectButton.onClick.AddListener(() => {
            DeckChooseManager.Inst.DeckSelection(deckIdx);
        });
    }
    
    public void SetValidity(bool validity)
    {
        if(validity)
        {
            GetComponent<Image>().color = Color.white;
        }
        else
        {
            GetComponent<Image>().color = Color.grey;
        }
    }

    public void SetActivation(bool value)
    {
        if(value)
        {
            selectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "선택됨";
            GetComponent<Image>().color = new Color(212/255f, 255/255f, 177/255f, 1f);
        }
        else
        {
            selectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "선택";
            GetComponent<Image>().color = Color.white;
        }
    }

}
