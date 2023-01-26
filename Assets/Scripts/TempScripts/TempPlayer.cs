using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TempPlayer : MonoBehaviour
{
    public GameObject stone;
    public TextMeshProUGUI txt;
    public GameObject canvas;
    private GameObject dragObject;
    private float clickTime;
    private bool isClick;
    private bool isDrag;
    private void Update()
    {
        TouchCard();
        if (isClick)
        {
            clickTime += Time.deltaTime;
        }
        else
        {
            clickTime = 0;
        }
        if (clickTime > 1.0f)
        {
            if (dragObject != null)
            {
                isDrag = true;
                //배치 가능한 포인트 가져오기

                //카드 옮기기
                DragCard();
            }
        }
    }

    private void TouchCard()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (touch.phase == TouchPhase.Began)
            {
                isClick = true;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.tag == "Card")
                    {
                        dragObject = hit.collider.gameObject;
                        txt.text = "CardTouch";
                    }
                    else if (hit.collider.tag == "Board")
                    {
                        bool isCan = true; //= GameBoard.IsPossibleToPut(hit.point + new Vector3(0,1.4f,0), 1);
                        Debug.Log(isCan);
                    }
                    else
                    {
                        canvas.SetActive(false);
                        dragObject = null;
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isClick = false;
                if (dragObject != null && dragObject.tag == "Card"&&!isDrag)
                {
                    canvas.SetActive(true);
                }
                dragObject = null;
                isDrag = false;
            }
        }
    }
    private void DragCard()
    {
        ChangeToStone();
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 pos = Camera.main.ScreenToWorldPoint(touch.position);
            dragObject.transform.position = new Vector3(pos.x,dragObject.transform.position.y,pos.z);
            //일정거리 찾기
        }
    }
    private void ChangeToStone()
    {
        
    }
}
