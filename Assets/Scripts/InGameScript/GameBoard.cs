using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{   
    [SerializeField] private BoardData boardData;
    public BoardData BoardData => boardData;

    public GameObject putMark;
    public float nearbyRadius = 10.0f;
    public Vector3 isNullPos = new Vector3(0, 100, 0);

    public void Awake()
    {
        gameObject.transform.localScale = new Vector3(BoardData.width, 1, BoardData.height);
        foreach (BoardPos boardPos in BoardData.player1CanStone)
        {
            Vector3 putPos = new Vector3(boardPos.x, 0, boardPos.y);
            Instantiate(putMark, putPos, new Quaternion(0, 0, 0, 0));
        }
        foreach (BoardPos boardPos in BoardData.player2CanStone)
        {
            Vector3 putPos = new Vector3(boardPos.x, 0, boardPos.y);
            Instantiate(putMark, putPos, new Quaternion(0, 0, 0, 0));
        }
    }

    // 올라와있는 스톤 (각 진영별) 체크

    // 떨어짐 판정

    // 해당 위치 근처에 스톤 놓을 수 있는 위치 제공
    public Vector3 GiveNearbyPos(Vector3 pos, int player)
    {
        if (player == 1)
        {
            foreach (BoardPos boardPos in BoardData.player1CanStone)
            {
                Vector3 nearbyPos = new Vector3(boardPos.x, 0, boardPos.y);
                if (Vector3.Distance(pos, nearbyPos) <= nearbyRadius)
                {
                    return nearbyPos + new Vector3(0,14,0);
                }
            }
        }
        else
        {
            foreach (BoardPos boardPos in BoardData.player2CanStone)
            {
                Vector3 nearbyPos = new Vector3(boardPos.x, 0, boardPos.y);
                if (Vector3.Distance(pos, nearbyPos) <= nearbyRadius)
                {
                    return nearbyPos + new Vector3(0, 14, 0);
                }
            }
        }
        return isNullPos;
    }

    // 지정된 위치에 가능한지 판별

    public bool IsPossibleToPut(Vector3 pos, float stoneRadius)
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
