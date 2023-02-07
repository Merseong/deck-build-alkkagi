using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour
{
    private int guardId;
    private bool isBelongLocal;
    private bool isCollided = false;
    
    public void SetGuardData(int id, bool isLocal)
    {
        guardId = id;
        isBelongLocal = isLocal;
        SetSide(isLocal);
    }

    private void SetSide(bool isBelongLocal)
    {
        if(isBelongLocal) 
        {
            gameObject.transform.GetComponent<MeshRenderer>().material.color = Color.green;
            gameObject.layer = LayerMask.NameToLayer("LocalGuard");
        }
        else
        {
            gameObject.transform.GetComponent<MeshRenderer>().material.color = Color.red;
            gameObject.layer = LayerMask.NameToLayer("OppoGuard");
        } 
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (isCollided) return;
        if (coll.gameObject.CompareTag("Stone")) 
        {
            isCollided = true;
            GameManager.Inst.rigidbodyRecorder.AppendEventRecord(new MyNetworkData.EventRecord
            {
                stoneId = guardId,
                time = Time.time,
                eventEnum = MyNetworkData.EventEnum.GUARDCOLLIDE,
            });

            GameManager.Inst.GameBoard.RemoveLocalGuard(guardId);
        }
    }
}