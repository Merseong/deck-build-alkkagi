using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AkgRigidbody))]
public class Guard : MonoBehaviour, AkgRigidbodyInterface
{
    private int guardId;
    private bool isBelongLocal;
    private bool isCollided = false;

    private AkgRigidbody akgRigidbody;
    
    public void SetGuardData(int id, bool isLocal)
    {
        guardId = id;
        isBelongLocal = isLocal;
        akgRigidbody = GetComponent<AkgRigidbody>();
        akgRigidbody.Init(1f);
        SetSide(isLocal);
    }

    private void SetSide(bool isBelongLocal)
    {
        if(isBelongLocal) 
        {
            gameObject.transform.GetComponent<MeshRenderer>().material.color = Color.green;
            //gameObject.layer = LayerMask.NameToLayer("LocalGuard");
            akgRigidbody.layerMask = AkgPhysicsManager.AkgLayerMaskEnum.LOCALGUARD;
        }
        else
        {
            gameObject.transform.GetComponent<MeshRenderer>().material.color = Color.red;
            //gameObject.layer = LayerMask.NameToLayer("OppoGuard");
            akgRigidbody.layerMask = AkgPhysicsManager.AkgLayerMaskEnum.OPPOGUARD;
        } 
    }

    public void OnCollide(AkgRigidbody collider, Vector3 collidePoint)
    {
        if (isCollided) return;
        if (collider.gameObject.CompareTag("Stone"))
        {
            if (collider.GetComponent<StoneBehaviour>().BelongingPlayer != 
                (isBelongLocal ? GameManager.PlayerEnum.LOCAL : GameManager.PlayerEnum.OPPO))
                return;

            isCollided = true;
            GameManager.Inst.rigidbodyRecorder.AppendEventRecord(new MyNetworkData.EventRecord
            {
                stoneId = guardId,
                time = Time.time,
                eventEnum = MyNetworkData.EventEnum.GUARDCOLLIDE,
            });

            akgRigidbody.BeforeDestroy();
            GameManager.Inst.GameBoard.RemoveLocalGuard(guardId);
        }
    }
}