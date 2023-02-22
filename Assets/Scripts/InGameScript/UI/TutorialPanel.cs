using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanel : MonoBehaviour
{
    [SerializeField] List<Sprite> tutorialSprites = new();
    [SerializeField] Button toNextSlide;
    [SerializeField] Image imageSlot;

    int curIdx = 0;

    private void Start()
    {
        toNextSlide.onClick.AddListener(()=>{
            ToNext();
        });
    }

    public void Initialize()
    {
        if(tutorialSprites.Count > 0)
        imageSlot.sprite = tutorialSprites[0];
        curIdx = 0;
    }

    public void ToNext()
    {   
        curIdx++;
        if(tutorialSprites.Count-1 < curIdx)
        {
            gameObject.SetActive(false);
            return;
        }
        imageSlot.sprite = tutorialSprites[curIdx];
    }

}