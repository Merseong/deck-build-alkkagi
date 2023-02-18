using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultPanel : MonoBehaviour
{
    public void OnToMenuClicked()
    {
        SceneManager.LoadScene("ResultScene");
    }
}
