using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprintProperty : StoneProperty
{
    public static bool IsAvailable(StoneBehaviour stone)
    {
        foreach (StoneProperty property in stone.Properties)
        {
            if (property as SprintProperty != null)
                return false;
        }

        return true;
    }

    private bool canSprint = true;

    public SprintProperty(StoneBehaviour stone, int count = -1) : base(stone, count) { }

    public override void OnAdded()
    {
        base.OnAdded();

        GameManager.Inst.GetPlayer(baseStone.BelongingPlayer).OnTurnStart += ResetCanSprint;
    }

    public override void OnRemoved()
    {
        base.OnRemoved();

        GameManager.Inst.GetPlayer(baseStone.BelongingPlayer).OnTurnStart -= ResetCanSprint;
    }

    public override bool CanSprint(bool value) { return canSprint || value; }

    private void ResetCanSprint()
    {
        canSprint = true;
    }
}
