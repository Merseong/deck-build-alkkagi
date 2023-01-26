using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HistoryAction
{
    // 액션(알까기, 패놓기 등) 히스토리를 위한 개별 액션
    // TODO: 구조가 어떻게 돼야 하는지는 기획이 필요할 듯?
    // 왠지 필요할 것 같은 함수 두개 짰는데 이것도 필요없을 수 있음

    public new abstract string ToString();
    public abstract void ToGUI();
}
