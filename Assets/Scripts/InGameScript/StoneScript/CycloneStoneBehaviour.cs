using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycloneStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        PlayerBehaviour player = GameManager.Inst.players[(int)BelongingPlayer];
        foreach (StoneBehaviour stone in player.Stones.Values)
        {
            if (SprintProperty.IsAvailable(stone))
                stone.AddProperty(new SprintProperty(stone));
        }
    }
}
