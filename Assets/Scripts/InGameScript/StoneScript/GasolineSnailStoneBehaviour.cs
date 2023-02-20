using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasolineSnailStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        OnHit += (akg) =>
        {
            StoneBehaviour stone = akg.gameObject.GetComponent<StoneBehaviour>();
            if (stone.BelongingPlayer == GameManager.PlayerEnum.OPPO)
            {
                stone.AddProperty(new GreasedProperty(stone));
            }
        };
    }
}
