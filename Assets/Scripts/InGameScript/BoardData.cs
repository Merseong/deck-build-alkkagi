using UnityEngine;

[CreateAssetMenu(fileName = "Board Data", menuName = "Scriptable Object/Board Data")]
public class BoardData : ScriptableObject
{
    //고유ID
    [SerializeField]
    private int boardID;
    public int BoardID => boardID;

    // 크기
    public float width;
    public float height;

    // 전진가속도
    public float movingDragAccleration;
    // 마찰가속도
    public float dragAccleration;

    // 놓을 수 있는 위치들
    public BoardPos[] player1CanStone;
    public BoardPos[] player2CanStone;
    //장애물 요소
    public ObstacleInfo[] obstacles;
}

[System.Serializable]
public class BoardPos
{
    public int x;
    public int y;
}

[System.Serializable]
public class ObstacleInfo
{
    public enum ObstacleKinds
    {
        
    }
    public BoardPos obstaclePos;
    public ObstacleKinds obstacleKinds;
}
