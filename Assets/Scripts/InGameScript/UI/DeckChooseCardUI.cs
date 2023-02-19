using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckChooseCardUI : MonoBehaviour
{
    [SerializeField] private Button cardButton;
    public int cardID;
    private void Start()
    {
        cardButton.onClick.AddListener(()=>{
            DeckChooseManager.Inst.CardSelection(cardID);
        });
    }

}
