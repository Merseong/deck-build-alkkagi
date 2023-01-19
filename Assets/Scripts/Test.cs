using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public SyncVar<int> testVar;
    void Start()
    {
        testVar = new SyncVar<int>();

        testVar.Data = 10;
        var callback = new SyncVar<int>.OnChangeEventHandler(PrintNumber);
        testVar.OnReceiveData += callback;
        testVar.OnReceiveData += callback;
        testVar.OnSendData += callback;

        Debug.Log(testVar.Data);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            testVar.Data += 10;
        }
    }

    private void PrintNumber(int data)
    {
        Debug.Log(data);
    }
}
