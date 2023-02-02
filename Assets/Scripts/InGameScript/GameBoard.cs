using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{   
    [SerializeField] private BoardData boardData;
    [SerializeField] private GameObject guardObject;
    public BoardData BoardData => boardData;

    public GameObject putMark;
    public float nearbyRadius = 10.0f;
    public Vector3 isNullPos = new Vector3(0, 100, 0);

    private static GameObject[] player1PutMarks;
    private static GameObject[] player2PutMarks;
    [SerializeField] private List<GameObject> localPlayerGuard = new();
    [SerializeField] private List<GameObject> oppoPlayerGuard = new();
    [SerializeField] private int guardHorizontalCnt;
    [SerializeField] private int guardVerticalCnt;
    

    public void Awake()
    {
        gameObject.transform.localScale = new Vector3(BoardData.width, 1, BoardData.height);

        player1PutMarks = new GameObject[BoardData.player1CanStone.Length];
        player2PutMarks = new GameObject[BoardData.player2CanStone.Length];

        int cnt = 0;
        foreach (BoardPos boardPos in BoardData.player1CanStone)
        {
            Vector3 putPos = new Vector3(boardPos.x, 0f, boardPos.y);
            player1PutMarks[cnt++] = Instantiate(putMark, putPos, Quaternion.Euler(90,0,0));
            player1PutMarks[cnt-1].SetActive(false);
        }
        cnt = 0;
        foreach (BoardPos boardPos in BoardData.player2CanStone)
        {
            Vector3 putPos = new Vector3(boardPos.x, 0f, boardPos.y);
            player2PutMarks[cnt++] = Instantiate(putMark, putPos, Quaternion.Euler(90,0,0));
            player2PutMarks[cnt-1].SetActive(false);
        }

        SetGuard();
    }

    // 올라와있는 스톤 (각 진영별) 체크

    // 떨어짐 판정

    // 스톤을 놓을 수 있는 모든 위치 제공
    public static void HighlightPossiblePos(int player, float stoneRadius)
    {
        if (player == 1)
        {
            for (int i = 0; i < player1PutMarks.Length; i++)
            {
                if (IsPossibleToPut(player1PutMarks[i].transform.position, stoneRadius))
                {
                    player1PutMarks[i].SetActive(true);
                    player1PutMarks[i].GetComponent<SpriteRenderer>().color = Color.yellow;
                }
                else
                {
                    player1PutMarks[i].SetActive(false);
                    player1PutMarks[i].GetComponent<SpriteRenderer>().color = Color.red;
                }
            }
        }
        else
        {
            for (int i = 0; i < player2PutMarks.Length; i++)
            {
                if (IsPossibleToPut(player2PutMarks[i].transform.position, stoneRadius))
                {
                    player2PutMarks[i].GetComponent<SpriteRenderer>().color = Color.yellow;
                }
                else
                {
                    player2PutMarks[i].GetComponent<SpriteRenderer>().color = Color.red;
                }
            }
        }
    }

    public static void UnhightlightPossiblePos()
    {
        for (int i = 0; i < player1PutMarks.Length; i++)
        {
            player1PutMarks[i].SetActive(false);
        }
        for (int i = 0; i < player2PutMarks.Length; i++)
        {
            player2PutMarks[i].SetActive(false);
        }
    }

    // 해당 위치 근처에 스톤 놓을 수 있는 위치 제공
    public Vector3 GiveNearbyPos(Vector3 pos, int player, float stoneRadius)
    {
        if (player == 1)
        {
            foreach (BoardPos boardPos in BoardData.player1CanStone)
            {
                Vector3 nearbyPos = new Vector3(boardPos.x, 0, boardPos.y);
                // Debug.Log(nearbyPos + ", " + IsPossibleToPut(nearbyPos,stoneRadius));
                if (Vector3.Distance(pos, nearbyPos) <= nearbyRadius && IsPossibleToPut(nearbyPos,stoneRadius))
                {
                    return nearbyPos;
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
                    return nearbyPos;
                }
            }
        }
        return isNullPos;
    }

    // 지정된 위치에 가능한지 판별
    public static bool IsPossibleToPut(Vector3 pos, float stoneRadius)
    {
        Collider[] hitcolliders = Physics.OverlapSphere(pos, stoneRadius, LayerMask.NameToLayer("Stone"));
        if (hitcolliders.Length > 0)
        {
            return false;
        }
        else 
        {
            return true;
        }
    }

    private void SetGuard()
    {
        if(guardObject == null) return;
        
        GameObject go;

        for(int i = (int)(guardVerticalCnt/2)-1; i >= 0 ; i--)
        {
            //left local
            go = Instantiate(guardObject, new Vector3(-(.035f + boardData.width / 2), 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2))/ guardVerticalCnt) * 10f, Quaternion.Euler(0,0,0));
            go.transform.localScale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.height / guardVerticalCnt / go.transform.localScale.z);
            go.GetComponent<Guard>().SetSide(true);
            localPlayerGuard.Add(go);

            //right oppo
            go = Instantiate(guardObject, new Vector3(-(.035f + boardData.width / 2), 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2))/ guardVerticalCnt) * -10f, Quaternion.Euler(0,0,0));
            go.transform.localScale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.height / guardVerticalCnt / go.transform.localScale.z);
            go.GetComponent<Guard>().SetSide(false);
            oppoPlayerGuard.Add(go);
        }

        for(int i=guardHorizontalCnt-1; i >=0 ; i--)
        {
            //lower local
            go = Instantiate(guardObject, new Vector3(boardData.width * (i - (float)guardHorizontalCnt / 2 + 0.5f)/ guardHorizontalCnt, 0, .035f + boardData.height / 2) * -10f, Quaternion.Euler(0,90,0));
            go.transform.localScale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.width / guardHorizontalCnt / go.transform.localScale.z);
            go.GetComponent<Guard>().SetSide(true);
            localPlayerGuard.Add(go);
            
            //upper oppo
            go = Instantiate(guardObject, new Vector3(boardData.width * (i - (float)guardHorizontalCnt / 2 + 0.5f)/ guardHorizontalCnt, 0, .035f + boardData.height / 2) * 10f, Quaternion.Euler(0,90,0));
            go.transform.localScale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.width / guardHorizontalCnt / go.transform.localScale.z);
            go.GetComponent<Guard>().SetSide(false);
            oppoPlayerGuard.Add(go);

        }

        for(int i=0; i< guardVerticalCnt/2; i++)
        {
            //right local
            go = Instantiate(guardObject, new Vector3(.035f + boardData.width / 2, 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2))/ guardVerticalCnt) * 10f, Quaternion.Euler(0,0,0));
            go.transform.localScale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.height / guardVerticalCnt / go.transform.localScale.z);
            go.GetComponent<Guard>().SetSide(true);
            localPlayerGuard.Add(go);
            
            //left oppo
            go = Instantiate(guardObject, new Vector3(.035f + boardData.width / 2, 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2))/ guardVerticalCnt) * -10f, Quaternion.Euler(0,0,0));
            go.transform.localScale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.height / guardVerticalCnt / go.transform.localScale.z);
            go.GetComponent<Guard>().SetSide(false);
            oppoPlayerGuard.Add(go);
        }
    }
}
