using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasolineSnailStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnHit += (akg) =>
        {
            StoneBehaviour stone = akg.gameObject.GetComponent<StoneBehaviour>();
            if (stone.BelongingPlayerEnum == GameManager.PlayerEnum.OPPO)
            {
                stone.AddProperty(new GreasedProperty(stone));
            }
        };

        base.OnEnter(calledByPacket, options);
    }
}
