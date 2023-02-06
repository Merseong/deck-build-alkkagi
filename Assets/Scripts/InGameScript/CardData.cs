using UnityEngine;

[CreateAssetMenu(fileName = "Card Data",menuName = "Scriptable Object/Card Data")]
public class CardData : ScriptableObject
{
    // 고유ID
    [SerializeField]
    private int cardID;
    public int CardID => cardID;
    // 크기
    public enum StoneSize
    {
        Small,
        Medium,
        Large,
        SuperLarge
    }
    public StoneSize stoneSize;
    // 무게
    public enum StoneWeight
    {
        Light,
        Standard,
        Heavy
    }
    public StoneWeight stoneWeight;
    // 비용
    public int cardCost;
    // 덱 포함 개수
    public int inDeckNumber;
    // 스프라이트
    public Sprite idleSprite;
    public Sprite readySprite;
    public Sprite shootSprite;
    public Sprite hitSprite;
    public Sprite breakSprite;
    // 설명
    public string description;
}
