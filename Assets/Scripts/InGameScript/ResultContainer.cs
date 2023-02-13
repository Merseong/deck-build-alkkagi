using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultContainer : MonoBehaviour
{
    public bool isLocalWin;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
