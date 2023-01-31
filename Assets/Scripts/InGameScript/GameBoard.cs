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

    private static GameObject[] player1PutMarks;
    private static GameObject[] player2PutMarks;

    public void Awake()
    {
        gameObject.transform.localScale = new Vector3(BoardData.width, 1, BoardData.height);

        player1PutMarks = new GameObject[BoardData.player1CanStone.Length];
        player2PutMarks = new GameObject[BoardData.player2CanStone.Length];

        int cnt = 0;
        foreach (BoardPos boardPos in BoardData.player1CanStone)
        {
            Vector3 putPos = new Vector3(boardPos.x, 0, boardPos.y);
            player1PutMarks[cnt++] = Instantiate(putMark, putPos, Quaternion.identity);
        }
        cnt = 0;
        foreach (BoardPos boardPos in BoardData.player2CanStone)
        {
            Vector3 putPos = new Vector3(boardPos.x, 0, boardPos.y);
            player2PutMarks[cnt++] = Instantiate(putMark, putPos, Quaternion.identity);
        }
    }

    // 올라와있는 스톤 (각 진영별) 체크

    // 떨어짐 판정

    // 스톤을 놓을 수 있는 모든 위치 제공
    public static void MarkPossiblePoses(int player, float stoneRadius)
    {
        if (player == 1)
        {
            for (int i = 0; i < player1PutMarks.Length; i++)
            {
                if (IsPossibleToPut(player1PutMarks[i].transform.position, stoneRadius))
                {
                    player1PutMarks[i].GetComponent<Renderer>().material.color = Color.yellow;
                }
            }
        }
        else
        {
            for (int i = 0; i < player2PutMarks.Length; i++)
            {
                if (IsPossibleToPut(player2PutMarks[i].transform.position, stoneRadius))
                {
                    player2PutMarks[i].GetComponent<Renderer>().material.color = Color.yellow;
                }
            }
        }
    }

    // 해당 위치 근처에 스톤 놓을 수 있는 위치 제공
    public Vector3 GiveNearbyPos(Vector3 pos, int player,float stoneRadius)
    {
        if (player == 1)
        {
            foreach (BoardPos boardPos in BoardData.player1CanStone)
            {
                Vector3 nearbyPos = new Vector3(boardPos.x, 0, boardPos.y);
                if (Vector3.Distance(pos, nearbyPos) <= nearbyRadius && IsPossibleToPut(nearbyPos,stoneRadius))
                {
                    return nearbyPos + new Vector3(0,2,0);
                }
            }
        }
        else
        {
            foreach (BoardPos boardPos in BoardData.player2CanStone)
            {
                Vector3 nearbyPos = new Vector3(boardPos.x, 0, boardPos.y);
                if (Vector3.Distance(pos, nearbyPos) <= nearbyRadius && IsPossibleToPut(nearbyPos, stoneRadius))
                {
                    return nearbyPos + new Vector3(0, 2, 0);
                }
            }
        }
        return isNullPos;
    }

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
