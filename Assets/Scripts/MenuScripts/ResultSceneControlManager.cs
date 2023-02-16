using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultSceneControlManager : SingletonBehavior<ResultSceneControlManager>
{
    private ResultContainer result;

    [SerializeField] private TextMeshProUGUI resultText;

    private void Start()
    {
        // show result
        result = FindObjectOfType<ResultContainer>();

        if (result == null)
        {
            Debug.LogError("[ERR] Result not found!");
            return;
        }

        resultText.text = result.isLocalWin ? "WIN!" : "LOSE...";

        Destroy(result.gameObject);
        result = null;

        // control network manager canvas
        NetworkManager.Inst.RefreshUI(true);
    }

    public void MoveToMenuScene()
    {
        SceneManager.LoadScene(0);
    }
}