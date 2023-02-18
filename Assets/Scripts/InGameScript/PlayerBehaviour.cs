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

    public virtual void InitPlayer(GameManager.PlayerEnum pEnum)
    {
        player = pEnum;
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
        shootTokenAvailable = true;
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
    #endregion

    #region Card functions
    public virtual void DrawCards(int number)
    {
        HandCount += (ushort)number;
        DeckCount -= (ushort)number;
    }

    protected virtual void RemoveCards(int idx)
    {
        HandCount--;
    }

    /// <param name="cardData"></param>
    /// <param name="spawnPosition"></param>
    /// <param name="stoneId">use only in oppo code</param>
    /// <returns>spawned stone's stoneID</returns>
    public abstract int SpawnStone(CardData cardData, Vector3 spawnPosition, int stoneId = -1);
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