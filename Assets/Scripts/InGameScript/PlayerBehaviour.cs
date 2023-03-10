using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public abstract class PlayerBehaviour : MonoBehaviour
{
    [Header("Player Behaviour")]
    [SerializeField] private GameManager.PlayerEnum player;
    public GameManager.PlayerEnum Player => player;

    [SerializeField] private uint uid = 0;
    public uint Uid => uid;

    [SerializeField] protected ushort cost;
    public ushort Cost
    {
        get => cost;

        protected set
        {
            if (value < 0)
                Debug.LogError("Cost is less than 0! Fix this!");

            cost = value;
            RefreshUI();
            GameManager.Inst.UpdateTurnEndButtonText();
        }
    }

    [SerializeField] private ushort deckCount;
    public ushort DeckCount
    {
        get => deckCount;
        protected set
        {
            deckCount = value;
            RefreshUI();
            GameManager.Inst.UpdateTurnEndButtonText();
        }
    }

    [SerializeField] protected ushort handCount;
    public ushort HandCount
    {
        get => handCount;
        protected set
        {
            if (value < 0)
                Debug.LogError("Hand Count is less than 0! Fix this!");

            handCount = value;
            RefreshUI();
            GameManager.Inst.UpdateTurnEndButtonText();
        }
    }

    public Dictionary<int, StoneBehaviour> Stones = new();

    [SerializeField] protected int nextStoneId;

    [SerializeField] private bool shootTokenAvailable;
    public bool ShootTokenAvailable
    {
        get => shootTokenAvailable;
        set
        {
            shootTokenAvailable = value;
            RefreshUI();
        }
    }

    public event Action OnTurnStart;
    public event Action OnTurnEnd;
    public Action<StoneBehaviour> OnStoneEnter;
    public Action<StoneBehaviour> OnStoneExit;
    [Tooltip("충돌한 돌, 돌이 충돌한 상대 AkgRigidbody")]
    public Action<StoneBehaviour, AkgRigidbody> OnStoneHit;

    //플레이어가 발사한 스톤, 다른 스톤들에서는 이 스톤의 충돌 여부를 확인하여 타격 여부를 결정하여야 함
    protected StoneBehaviour strikingStone;
    public StoneBehaviour StrikingStone
    {
        get => strikingStone;
        set => strikingStone = value;
    }

    public void StartTurn()
    {
        DrawCards(1);
        ResetCost();
        ResetShootToken();
        OnTurnStart?.Invoke();
    }

    public void EndTurn()
    {
        OnTurnEnd?.Invoke();
    }

    public virtual void InitPlayer(GameManager.PlayerEnum pEnum, uint uid)
    {
        player = pEnum;
        this.uid = uid;
        Stones.Clear();
        if (GameManager.Inst.isLocalGoFirst && player == GameManager.PlayerEnum.LOCAL ||
            !GameManager.Inst.isLocalGoFirst && player == GameManager.PlayerEnum.OPPO)
        {
            nextStoneId = 0;
        }
        else
        {
            nextStoneId = 1;
        }
        ShootTokenAvailable = true;
        
        GameManager.Inst.SetPlayerData(() =>
        {
            GameManager.Inst.players[(int)Player] = this;
        });
    }

    public void ResetShootToken()
    {
        ShootTokenAvailable = true;
    }

    public void OnShootExit()
    {
        strikingStone.OnShootExit -= OnShootExit;
        strikingStone = null;
    }

    #region UI actions
    public virtual void RefreshUI() { }
    #endregion

    #region Cost functions

    ///<summary>
    ///return true when succesfully spend costs<br></br>return false when current cost is less then parameter
    ///</summary>
    ///<param name = "used">Cost to spend</param>
    protected bool SpendCost(int used) => SpendCost((ushort)used);
    protected bool SpendCost(ushort used)
    {
        if(Cost < used) return false;
        Cost -= used;
        return true;
    }

    public void ResetCost(int resetTo = -1)
    {
        if (resetTo > 0)
        {
            Cost = (ushort)resetTo;
        }
        else if (GameManager.Inst.TurnCount == 0)
        {
            Cost = GameManager.Inst.initialTurnCost;
        }
        else
        {
            Cost = GameManager.Inst.normalTurnCost;
        }
            
    }

    public void GetCost(int gained) => GetCost((ushort)gained);
    public void GetCost(ushort gained)
    {
        Cost += gained;
    }

    #endregion

    #region Card functions
    public virtual void InitDeck(string deckCode)
    {
        if (!ushort.TryParse(deckCode, out deckCount))
        {
            deckCount = 100;
        }
    }

    public virtual void DrawCards(int number)
    {
        HandCount += (ushort)number;
        DeckCount -= (ushort)number;
    }
    public virtual void CardToHand(CardData cardData, int number)
    {
        HandCount += (ushort)number;
    }

    protected virtual void RemoveCards(int idx)
    {
        HandCount--;
    }
    public virtual List<Card> GetHandCard()
    {
        return null;
    }
    public virtual void RemoveHandCardAndArrange(Card card, Vector3 pos, int stoneId)
    {
    }

    /// <param name="cardData"></param>
    /// <param name="spawnPosition"></param>
    /// <param name="stoneId">use only in oppo code</param>
    /// <returns>spawned stone's stoneID</returns>
    public abstract int SpawnStone(CardData cardData, Vector3 spawnPosition, int stoneId = -1, bool ignoreSpawnPos = false);
    #endregion

    #region Stone List Control
    public int AddStone(StoneBehaviour stone)
    {
        var toReturn = nextStoneId;
        Stones.Add(nextStoneId, stone);
        GameManager.Inst.AllStones.Add(nextStoneId, stone);
        nextStoneId += 2;
        return toReturn;
    }

    public void RemoveStone(int stoneId)
    {
        Stones.Remove(stoneId);
        GameManager.Inst.AllStones.Remove(stoneId);

        if (Stones.Count == 0)
        {
            // Game over
            GameManager.Inst.GameOverAction(player);
        }
    }

    public StoneBehaviour FindStone(int stoneId)
    {
        var isFound = Stones.TryGetValue(stoneId, out var stone);

        if (!isFound)
        {
            Debug.LogError($"[GAME] stone id {stoneId} not found");
        }

        return stone;
    }
    #endregion

    protected Vector3 StringToVector3(string vec3)
    {
        var stringList = vec3.Split('|');
        return new Vector3(float.Parse(stringList[1]), float.Parse(stringList[2]), float.Parse(stringList[3]));
    }

    protected string Vector3ToString(Vector3 vec3)
    {
        return $"|{vec3.x}|{vec3.y}|{vec3.z}|";
    }
}
