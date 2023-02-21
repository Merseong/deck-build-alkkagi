using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StoneProperty
{
    public static (bool, T) IsAvailable<T>(StoneBehaviour stone, T newProperty) where T : StoneProperty
    {
        int turn = newProperty.remainingTurn;

        foreach (StoneProperty property in stone.Properties)
        {
            if (property is T castedProperty)
                return (castedProperty.remainingTurn != -1 && (turn == -1 || castedProperty.remainingTurn < turn), castedProperty);
        }

        return (true, null);
    }

    protected StoneBehaviour baseStone;
    protected int remainingTurn;  // -1: infinite, 0: end

    public StoneProperty(StoneBehaviour stone, int turn = -1)
    {
        baseStone = stone;
        remainingTurn = turn;
    }

    // for sprint
    public virtual bool CanSprint(bool value) { return value; }
    // for ghost
    public virtual bool IsGhost(bool value) { return value; }
    // for shield
    public virtual int ShieldCount(int value) { return value; }
    // for accel shield
    public virtual bool HasAccelShield(bool value) { return value; }
    // for cursed
    public virtual float GetMass(float value) { return value; }
    // for pinned
    public virtual bool IsStatic(bool value) { return value; }
    // for greased
    public virtual float GetDragAccel(float value) { return value; }

    // 애니메이션 같은거 여기다 넣으면 될듯
    public virtual void OnAdded(bool isReplaced = false)
    {
        Debug.Log($"{this.GetType().Name} is added to {baseStone.CardData?.name}");
        baseStone.BelongingPlayer.OnTurnStart += DecreaseRemainingTurn;

        if (!isReplaced)
        {
            // 부여 애니메이션
        }
    }

    public virtual void OnRemoved(bool isReplaced = false)
    {
        Debug.Log($"{this.GetType().Name} is removed from {baseStone.CardData.name}");
        baseStone.BelongingPlayer.OnTurnStart -= DecreaseRemainingTurn;

        if (!isReplaced)
        {
            // 해제 애니메이션
        }
    }

    private void DecreaseRemainingTurn()
    {
        if (remainingTurn > 0)
            remainingTurn--;

        if (remainingTurn == 0)
            RemoveProperty();
    }

    protected void RemoveProperty()
    {
        baseStone.RemoveProperty(this);
    }
}
