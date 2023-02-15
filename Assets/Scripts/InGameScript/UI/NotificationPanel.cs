using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class NotificationPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text notificationTextMesh;

    public void Show(string message)
    {
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
