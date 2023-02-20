using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{   
    [SerializeField] private BoardData boardData;
    [SerializeField] private GameObject guardPrefab;
    public BoardData BoardData => boardData;

    public GameObject putMark;
    public float nearbyRadius = 10.0f;
    public Vector3 isNullPos = new Vector3(0, 100, 0);

    private GameObject[] player1PutMarks;
    private GameObject[] player2PutMarks;
    private GameObject[] localPlayerPutMarks
    {
        get
        {
            if (GameManager.Inst.isLocalGoFirst)
                return player1PutMarks;
            else
                return player2PutMarks;
        }
    }
    private GameObject[] oppoPlayerPutMarks
    {
        get
        {
            if (GameManager.Inst.isLocalGoFirst)
                return player2PutMarks;
            else
                return player1PutMarks;
        }
    }
    [SerializeField] private Dictionary<int, GameObject> playerGuards = new();
    [SerializeField] private int guardHorizontalCnt;
    [SerializeField] private int guardVerticalCnt;

    public void InitGameBoard()
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

        guardHorizontalCnt = (int)(BoardData.width / 0.2);
        guardVerticalCnt = (int)(BoardData.height / 0.2);
        SetGuard();
    }

    // 올라와있는 스톤 (각 진영별) 체크

    // 떨어짐 판정

    // 스톤을 놓을 수 있는 모든 위치 제공
    public void HighlightPossiblePos(GameManager.PlayerEnum player, float stoneRadius)
    {
        if (player == GameManager.PlayerEnum.LOCAL)
        {
            for (int i = 0; i < localPlayerPutMarks.Length; i++)
            {
                if (IsPossibleToPut(localPlayerPutMarks[i].transform.position, stoneRadius))
                {
                    localPlayerPutMarks[i].SetActive(true);
                    localPlayerPutMarks[i].GetComponent<SpriteRenderer>().color = Color.yellow;
                }
                else
                {
                    localPlayerPutMarks[i].SetActive(false);
                    localPlayerPutMarks[i].GetComponent<SpriteRenderer>().color = Color.red;
                }
            }
        }
        else
        {
            for (int i = 0; i < oppoPlayerPutMarks.Length; i++)
            {
                if (IsPossibleToPut(oppoPlayerPutMarks[i].transform.position, stoneRadius))
                {
                    oppoPlayerPutMarks[i].GetComponent<SpriteRenderer>().color = Color.yellow;
                }
                else
                {
                    oppoPlayerPutMarks[i].GetComponent<SpriteRenderer>().color = Color.red;
                }
            }
        }
    }

    public void UnhightlightPossiblePos()
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
    public Transform GiveNearbyPos(Vector3 pos, GameManager.PlayerEnum player, float stoneRadius)
    {
        var localCanStone = GameManager.Inst.isLocalGoFirst ? BoardData.player1CanStone : BoardData.player2CanStone;
        var oppoCanStone = GameManager.Inst.isLocalGoFirst ? BoardData.player2CanStone : BoardData.player1CanStone;

        if (player == GameManager.PlayerEnum.LOCAL)
        {
            foreach (BoardPos boardPos in localCanStone)
            {
                Vector3 nearbyPos = new Vector3(boardPos.x, 0, boardPos.y);
                // Debug.Log(nearbyPos + ", " + IsPossibleToPut(nearbyPos,stoneRadius));
                if (Vector3.Distance(pos, nearbyPos) <= nearbyRadius && IsPossibleToPut(nearbyPos,stoneRadius))
                {
                    // Debug.Log(nearbyPos);
                    return localPlayerPutMarks[System.Array.IndexOf(localCanStone, boardPos)].transform;
                }
            }
        }
        else
        {
            foreach (BoardPos boardPos in oppoCanStone)
            {
                Vector3 nearbyPos = new Vector3(boardPos.x, 0, boardPos.y);
                if (Vector3.Distance(pos, nearbyPos) <= nearbyRadius && IsPossibleToPut(nearbyPos, stoneRadius))
                {
                    return oppoPlayerPutMarks[System.Array.IndexOf(localCanStone, boardPos)].transform;
                }
            }
        }
        return null;
    }

    // 지정된 위치에 가능한지 판별
    public bool IsPossibleToPut(Vector3 pos, float stoneRadius)
    {
        var stoneOnBoard = GameManager.Inst.AllStones.Values;
        foreach (StoneBehaviour stone in stoneOnBoard)
        {
            if (Vector3.Distance(pos, stone.gameObject.transform.position) <= stoneRadius)
            {
                return false;
            }
        }
        var guards = playerGuards.Values;
        foreach (GameObject guard in guards)
        {
            if (Vector3.Distance(pos, guard.transform.position) <= stoneRadius)
            {
                return false;
            }
        }
        return true;
    }

    public void ResetMarkState()
    {
        foreach(var mark in localPlayerPutMarks)
        {
            mark.GetComponent<SpriteRenderer>().material.color = Color.yellow;
        }
    }

    private void SetGuard()
    {
        if(guardPrefab == null) return;
        
        Vector3 position;
        Quaternion rotation;
        // Rotated가 true일때, 서로 반대
        var isRotated = (GameManager.Inst.LocalPlayer as LocalPlayerBehaviour).IsLocalRotated;
        
        rotation = Quaternion.Euler(0, 0, 0);
        for (int i = (int)(guardVerticalCnt/2)-1; i >= 0 ; i--)
        {
            //left local
            position = new Vector3(-(.035f + boardData.width / 2), 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2)) / guardVerticalCnt) * 10f;
            if (isRotated) AddOppoGuard(position, rotation);
            else AddLocalGuard(position, rotation);

            //right oppo
            position = new Vector3(-(.035f + boardData.width / 2), 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2)) / guardVerticalCnt) * -10f;
            if (isRotated) AddLocalGuard(position, rotation);
            else AddOppoGuard(position, rotation);
        }

        rotation = Quaternion.Euler(0, 90, 0);
        for (int i=guardHorizontalCnt-1; i >=0 ; i--)
        {
            //lower local
            position = new Vector3(boardData.width * (i - (float)guardHorizontalCnt / 2 + 0.5f) / guardHorizontalCnt, 0, .035f + boardData.height / 2) * -10f;
            if (isRotated) AddOppoGuard(position, rotation);
            else AddLocalGuard(position, rotation);

            //upper oppo
            position = new Vector3(boardData.width * (i - (float)guardHorizontalCnt / 2 + 0.5f) / guardHorizontalCnt, 0, .035f + boardData.height / 2) * 10f;
            if (isRotated) AddLocalGuard(position, rotation);
            else AddOppoGuard(position, rotation);

        }

        rotation = Quaternion.Euler(0, 0, 0);
        for (int i=0; i< guardVerticalCnt/2; i++)
        {
            //right local
            position = new Vector3(.035f + boardData.width / 2, 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2)) / guardVerticalCnt) * 10f;
            if (isRotated) AddOppoGuard(position, rotation);
            else AddLocalGuard(position, rotation);

            //left oppo
            position = new Vector3(.035f + boardData.width / 2, 0, boardData.height * (0.5f + i - (int)(guardVerticalCnt / 2)) / guardVerticalCnt) * -10f;
            if (isRotated) AddLocalGuard(position, rotation);
            else AddOppoGuard(position, rotation);
        }
    }

    public void AddLocalGuard(Vector3 position, Quaternion rotation)
    {
        var go = Instantiate(guardPrefab, position, rotation);
        var scale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.height / guardVerticalCnt / go.transform.localScale.z);
        go.transform.localScale = scale;
        go.GetComponent<AkgRigidbody>().rectPoints = rotation == Quaternion.identity ?
            new Vector4(-scale.x / 2, -scale.z / 2, scale.x / 2, scale.z / 2) :
            new Vector4(-scale.z / 2, -scale.x / 2, scale.z / 2, scale.x / 2);
        var idx = playerGuards.Count;
        go.GetComponent<Guard>().SetGuardData(idx, true);
        playerGuards.Add(idx, go);
    }

    public void AddOppoGuard(Vector3 position, Quaternion rotation)
    {
        var go = Instantiate(guardPrefab, position, rotation);
        var scale = new Vector3(go.transform.localScale.x, 1f, 10 * boardData.height / guardVerticalCnt / go.transform.localScale.z);
        go.transform.localScale = scale;
        go.GetComponent<AkgRigidbody>().rectPoints = rotation == Quaternion.identity ?
            new Vector4(-scale.x / 2, -scale.z / 2, scale.x / 2, scale.z / 2) :
            new Vector4(-scale.z / 2, -scale.x / 2, scale.z / 2, scale.x / 2);
        var idx = playerGuards.Count;
        go.GetComponent<Guard>().SetGuardData(idx, false);
        playerGuards.Add(idx, go);
    }

    public void RemoveGuard(int id)
    {
        //playerGuards.TryGetValue(id, out var guard);
        //playerGuards.Remove(id);

        //Destroy(guard);

        playerGuards.TryGetValue(id, out var guard);
        guard.GetComponent<Renderer>().enabled = false;
        AkgRigidbody akgRigidbody = guard.GetComponent<AkgRigidbody>();
        akgRigidbody.layerMask |= AkgLayerMask.COLLIDED;
    }
}
