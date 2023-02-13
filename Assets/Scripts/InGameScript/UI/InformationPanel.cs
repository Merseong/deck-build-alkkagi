using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InformationPanel : MonoBehaviour
{
    [SerializeField] private Image Sprite;
    [SerializeField] private TextMeshProUGUI cardSize;
    [SerializeField] private TextMeshProUGUI cardWeight;
    [SerializeField] private TextMeshProUGUI cardDescription;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI cardCost;

    public void SetInformation(CardData data)
    {
        Sprite.sprite = Util.GetSpriteState(data, "Idle");
        cardSize.text = data.stoneSize.ToString();
        cardWeight.text = data.stoneWeight.ToString();
        cardDescription.text = data.description.ToString();
        cardName.text = data.cardName.ToString();
        cardCost.text = data.cardCost.ToString();
    }

}
