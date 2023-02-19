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
    [SerializeField] private Image image;

    public void Show(string message)
    {
        notificationTextMesh.text = message;
        Sequence sequence = DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutQuad))
            .AppendInterval(0.9f)
            .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad));
    }
    public void Show(Sprite sprite, string message)
    {
        image.sprite = sprite;
        notificationTextMesh.text = message;
        Sequence sequence = DOTween.Sequence()
            .Append(transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutQuad))
            .AppendInterval(0.9f)
            .Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad));
    }

    private void Start() => ScaleZero();

    [ContextMenu("ScaleZero")]
    void ScaleZero() => transform.localScale = Vector3.zero;
    void ScaleOne() => transform.localScale = Vector3.one;
}
