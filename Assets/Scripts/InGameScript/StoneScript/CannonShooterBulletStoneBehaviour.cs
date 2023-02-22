using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonShooterBulletStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        GetComponent<AkgRigidbody>().layerMask = AkgLayerMask.STONE;
        OnShootExit += Disappear;
        OnHit += Disappear;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnShootExit -= Disappear;
        OnHit -= Disappear;

        base.OnExit(calledByPacket, options);
    }

    private void Disappear(AkgRigidbody akgRigid) => Disappear();
    private void Disappear()
    {
        RemoveStoneFromGame();
        Destroy(gameObject);
    }
}
