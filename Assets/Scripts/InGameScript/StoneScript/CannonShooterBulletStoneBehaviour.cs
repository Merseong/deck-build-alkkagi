using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonShooterBulletStoneBehaviour : StoneBehaviour
{
    public override void InitProperty()
    {
        base.InitProperty();
        GetComponent<AkgRigidbody>().layerMask = AkgLayerMask.STONEONLY;
    }

    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnShootExit += Disappear;
        OnHit += Disappear;

        //base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnShootExit -= Disappear;
        OnHit -= Disappear;

        //base.OnExit(calledByPacket, options);
    }

    private void Disappear(AkgRigidbody akgRigid)
    {
        GetComponent<AkgRigidbody>().isDisableCollide = true;
        Disappear();
    }

    private void Disappear()
    {
        if (BelongingPlayerEnum != GameManager.PlayerEnum.LOCAL) return;
        RemoveStoneFromGame();
        Destroy(gameObject);
    }
}
