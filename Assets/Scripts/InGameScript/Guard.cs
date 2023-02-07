using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour
{
    private int guardId;
    
    public void SetGuardData(int id, bool isLocal)
    {
        guardId = id;
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
        if(coll.gameObject.CompareTag("Stone")) 
        {
            GameManager.Inst.rigidbodyRecorder.AppendEventRecord(new MyNetworkData.EventRecord
            {
                stoneId = guardId,
                time = Time.time,
                eventEnum = MyNetworkData.EventEnum.GUARDCOLLIDE,
            });

            Destroy(this.gameObject);
        }
    }
}