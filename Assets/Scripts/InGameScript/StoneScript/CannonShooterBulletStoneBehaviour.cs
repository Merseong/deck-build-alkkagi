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
        akgRigidbody.SetDragAccel(0);
        akgRigidbody.SetMass(.3f);

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
        Debug.Log("Disappear!!");
        RemoveStoneFromGame();
        Destroy(gameObject);
    }
}
