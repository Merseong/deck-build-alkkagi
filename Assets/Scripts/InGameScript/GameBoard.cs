using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{   
    [SerializeField] private BoardData boardData;
    public BoardData BoardData => boardData;

    public void Awake()
    {
        gameObject.transform.localScale = new Vector3(BoardData.width, 1, BoardData.height);
    }

    // 올라와있는 스톤 (각 진영별) 체크

    // 떨어짐 판정

    // 지정된 위치에 가능한지 판별

    public static bool IsPossibleToPut(Vector3 pos, float stoneRadius)
    {
        Collider[] hitcolliders = Physics.OverlapSphere(pos, stoneRadius);
        if (hitcolliders.Length > 0)
        {
            return false;
        }
        else 
        {
            return true;
        }
    }
}
