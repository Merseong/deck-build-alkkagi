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
    [SerializeField] private TextMeshProUGUI cardKeyword;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI cardCost;
    [SerializeField] private Image weightImage;
    [SerializeField] private Image sizeImage;

    [SerializeField]
    private Dictionary<string, string> keywordExplanations = new Dictionary<string, string>
    {
        ["등장"] = "스톤이 보드에 소환될 때 발동 (배치턴 제외)",
        ["유령"] = "다른 스톤과 충돌하지 않습니다.",
        ["퇴장"] = "스톤이 보드에서 나갈 때 발동",
        ["보호막"] = "알이 보드 밖으로 나갈때 방어",
        ["이중보호막"] = "두겹의 보호막, 2회까지 방어받음",
        ["추진보호막"] = "알이 발사중일때만 보호막 부여",
        ["저주"] = "무게 감소",
        ["고정"] = "발사 불가",
        ["기름칠"] = "마찰력이 감소함",
        ["질주"] = "턴당 1회, 발사시 발사토큰을 소모하지 않음",
        ["타격"] = "이 스톤을 발사해 충돌시 발동"
    };

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
        SetKeywordExplanation(data.description.ToString());
    }

    private void SetKeywordExplanation(string description)
    {
        cardKeyword.text = "";
        foreach (var (key, value) in keywordExplanations)
        {
            if (description.Contains(key))
                cardKeyword.text += $"{key}: {value}\n";
        }
    }
}
