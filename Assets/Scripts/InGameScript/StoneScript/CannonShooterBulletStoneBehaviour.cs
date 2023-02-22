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
        OnHit += Disappear;

        //base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnHit -= Disappear;

        //base.OnExit(calledByPacket, options);
    }

    public override float GetMass(float init)
    {
        return 0.3f;
    }

    public override float GetDragAccel(float init)
    {
        return 0.0f;
    }

    private void Disappear(AkgRigidbody akgRigid)
    {
        GetComponent<AkgRigidbody>().isDisableCollide = true;
        RemoveStoneFromGame();
        Destroy(gameObject);
    }
}
