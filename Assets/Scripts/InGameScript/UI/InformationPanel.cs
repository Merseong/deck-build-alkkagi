using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.U2D;

public class InformationPanel : MonoBehaviour
{
    [SerializeField] private Image Sprite;
    [SerializeField] private TextMeshProUGUI cardSize;
    [SerializeField] private TextMeshProUGUI cardWeight;
    [SerializeField] private TextMeshProUGUI cardDescription;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI cardCost;
    [SerializeField] private Image weightImage;
    [SerializeField ]private Image sizeImage;

    public void SetInformation(CardData data, SpriteAtlas stoneAtlas, SpriteAtlas UIAtlas)
    {
        Sprite.sprite = Util.GetSpriteState(data, "Idle", stoneAtlas);
        cardSize.text = Util.GetStringSize(data);
        cardWeight.text = Util.GetStringWeight(data);
        cardDescription.text = data.description.ToString();
        cardName.text = data.cardName.ToString();
        cardCost.text = data.cardCost.ToString();
        weightImage.sprite = Util.GetSpriteWeight(data, UIAtlas);
        sizeImage.sprite = Util.GetSpriteSize(data, UIAtlas);
    }

}
