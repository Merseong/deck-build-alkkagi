using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.U2D;

public class NotificationPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text notificationTextMesh;
    [SerializeField] private Image centerImage;
    [SerializeField] private GameObject rejectPanel;

    public void Show(string message)
    {
        notificationTextMesh.text = message;
        Sequence sequence = DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutQuad))
            .AppendInterval(0.9f)
            .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad));
    }
    public void Show(bool isLocalHonor)
    {
        ScaleOne();
        if (isLocalHonor)
        {
            centerImage.sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_Honor_1");
            Sequence sequence = DOTween.Sequence()
                .Append(centerImage.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutQuad))
                .AppendInterval(0.9f)
                .Append(centerImage.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad))
                .AppendInterval(0.01f)
                .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad));


        }
        else
        {
            centerImage.sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_Honor_0");
            Sequence sequence = DOTween.Sequence()
                .Append(centerImage.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad))
                .AppendInterval(0.9f)
                .Append(centerImage.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutQuad))
                .AppendInterval(0.9f)
                .Append(centerImage.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad))
                .AppendInterval(0.01f)
                .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad));
        }
    }
    public void Show()
    {
        ScaleOne();
        Sequence sequence = DOTween.Sequence()
            .Append(rejectPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutQuad))
            .AppendInterval(0.9f)
            .Append(rejectPanel.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad))
            .AppendInterval(0.01f)
            .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad));
    }

    private void Start() => ScaleZero();

    [ContextMenu("ScaleZero")]
    void ScaleZero() => transform.localScale = Vector3.zero;
    void ScaleOne() => transform.localScale = Vector3.one;
}
