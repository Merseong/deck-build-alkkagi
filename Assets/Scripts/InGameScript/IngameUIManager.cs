using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngameUIManager : SingletonBehavior<IngameUIManager>
{
    [SerializeField] private AskingPanel askingPanel;
    public AskingPanel AskingPanel => askingPanel;


}
