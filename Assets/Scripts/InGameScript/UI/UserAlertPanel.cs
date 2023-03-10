using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UserAlertPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    private Image panelBackground;
    [SerializeField] private float panelFadeOutDuration = .5f;

    private bool isOnAlert = false;
    private float localTimer = 0f;

    private void Awake()
    {
        panelBackground = GetComponent<Image>();
    }

    public void Alert(string alertContent, float alertDuration = 1f)
    {
        var panelInitColor = new Color(1, 0.8f, 0.8f, 1);
        var textInitColor = new Color(1, 0.6f, 0.6f, 1);

        panelBackground.enabled = true;
        panelBackground.color = panelInitColor;

        textMesh.gameObject.SetActive(true);
        textMesh.text = alertContent;
        textMesh.color = textInitColor;

        if (isOnAlert)
        {
            localTimer = 0f;
            return;
        }

        StartCoroutine(EPanelFadeControl(alertDuration));
    }

    IEnumerator EPanelFadeControl(float alertDuration)
    {
        isOnAlert = true;
        localTimer = 0f;
        while (localTimer < alertDuration + panelFadeOutDuration)
        {
            if (localTimer >= alertDuration)
            {
                var panelColor = new Color(1, 0.8f, 0.8f, Mathf.Lerp(1, 0, (localTimer - alertDuration) / panelFadeOutDuration));
                var textColor = new Color(1, 0.6f, 0.6f, Mathf.Lerp(1, 0, (localTimer - alertDuration) / panelFadeOutDuration));
                panelBackground.color = panelColor;
                textMesh.color = textColor;
            }

            localTimer += Time.deltaTime;
            yield return null;
        }

        panelBackground.enabled = false;
        textMesh.gameObject.SetActive(false);
        isOnAlert = false;
    }
}
