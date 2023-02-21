using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AkgRigidbody))]
public class Guard : MonoBehaviour, IAkgRigidbodyInterface
{
    private int guardId;

    private AkgRigidbody akgRigidbody;

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
            gameObject.transform.GetComponent<MeshRenderer>().material.color = Color.green;
            //gameObject.layer = LayerMask.NameToLayer("LocalGuard");
            akgRigidbody.layerMask = AkgLayerMask.LOCAL;
        }
        else
        {
            gameObject.transform.GetComponent<MeshRenderer>().material.color = Color.red;
            //gameObject.layer = LayerMask.NameToLayer("OppoGuard");
            akgRigidbody.layerMask = AkgLayerMask.OPPO;
        } 
    }

    public void OnCollide(AkgRigidbody collider, Vector3 collidePoint, bool isCollided)
    {
        //if (isAlreadyCollided) return;
        if (collider.gameObject.CompareTag("Stone"))
        {
            AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(new EventRecord
            {
                stoneId = guardId,
                time = Time.time,
                eventEnum = EventEnum.GUARDCOLLIDE,
            });

            GameManager.Inst.GameBoard.RemoveGuard(guardId);
        }
    }
}