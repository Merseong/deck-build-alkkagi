using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour
{

    public void SetSide(bool isBelongLocal)
    {
        if(isBelongLocal) gameObject.layer = LayerMask.NameToLayer("LocalGuard");
        else gameObject.layer = LayerMask.NameToLayer("OppoGuard");
    }

    private void OnCollisionExit(Collision coll)
    {
        if(coll.gameObject.CompareTag("Stone")) 
        {
            //TODO : Synchoronize destroy by network
            Destroy(this.gameObject);
        }
    }

}