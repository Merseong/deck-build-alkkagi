using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AkgRigidbody))]
public class Guard : MonoBehaviour, IAkgRigidbodyInterface
{
    private int guardId;

    private AkgRigidbody akgRigidbody;

    [SerializeField] private Material[] guardMaterials;
    
    private void OnDestroy()
    {
        if (akgRigidbody) akgRigidbody.BeforeDestroy();
    }

    public void SetGuardData(int id, bool isLocal)
    {
        guardId = id;
        akgRigidbody = GetComponent<AkgRigidbody>();
        akgRigidbody.Init();
        SetSide(isLocal);
    }

    private void SetSide(bool isBelongLocal)
    {
        if(isBelongLocal) 
        {
            gameObject.transform.GetComponent<MeshRenderer>().material = guardMaterials[0];
            //gameObject.layer = LayerMask.NameToLayer("LocalGuard");
            akgRigidbody.layerMask = AkgLayerMask.LOCAL;
        }
        else
        {
            gameObject.transform.GetComponent<MeshRenderer>().material = guardMaterials[1];
            //gameObject.layer = LayerMask.NameToLayer("OppoGuard");
            akgRigidbody.layerMask = AkgLayerMask.OPPO;
        } 
    }

    public void OnCollide(AkgRigidbody collider, Vector3 collidePoint, bool isCollided, bool calledByPacket = false)
    {
        //if (isAlreadyCollided) return;
        if (collider.layerMask.HasFlag(AkgLayerMask.STONE))
        {
            StoneBehaviour stone = collider.GetComponent<StoneBehaviour>();
            if (stone.HasAccelShield() || stone.ShieldCount() > 0)
                return;

            if (!calledByPacket)
            {
                AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(new EventRecord
                {
                    stoneId = stone.StoneId,
                    time = Time.time,
                    eventMessage = guardId.ToString(),
                    eventEnum = EventEnum.STATICCOLLIDE,
                    xPosition = Util.FloatToSlicedString(collidePoint.x),
                    zPosition = Util.FloatToSlicedString(collidePoint.z),
                });
            }

            GameManager.Inst.GameBoard.RemoveGuard(this);
        }
    }
}
