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
            transform.parent.parent.parent.GetComponent<DeckChooseManager>().DeckSelection(deckIdx);
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
        Debug.Log(value);
        if(value)
        {
            selectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Selected";
            GetComponent<Image>().color = new Color(212/255, 255/255, 177/255, 1f);
        }
        else
        {
            selectButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Select";
            GetComponent<Image>().color = Color.white;
        }
    }

}
