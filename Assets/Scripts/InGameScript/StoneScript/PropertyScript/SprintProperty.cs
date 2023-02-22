using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprintProperty : StoneProperty
{
    private bool canSprint = true;

    public SprintProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override void OnAdded(bool isReplaced = false)
    {
        base.OnAdded(isReplaced);

        baseStone.BelongingPlayer.OnTurnStart += ResetCanSprint;
        baseStone.OnShootEnter += UseSprint;
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);

        baseStone.BelongingPlayer.OnTurnStart -= ResetCanSprint;
        baseStone.OnShootEnter -= UseSprint;
    }

    public override bool CanSprint(bool value) { return value || canSprint; }

    private void ResetCanSprint()
    {
        canSprint = true;

        baseStone.transform.GetChild(4).GetChild(3).gameObject.SetActive(true);
    }

    private void UseSprint()
    {
        canSprint = false;

        baseStone.transform.GetChild(4).GetChild(3).gameObject.SetActive(false);
    }
}
