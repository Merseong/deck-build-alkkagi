using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _inst;
    public static T Inst
    {
        get
        {
            if (_inst)
            {
                return _inst;
            }
            _inst = FindObjectOfType<T>();
            if (!_inst)
            {
                GameObject obj = new GameObject();
                obj.name = typeof(T).FullName + "_Singleton";
                DontDestroyOnLoad(obj);
                _inst = obj.AddComponent<T>();
            }
            return _inst;
        }
    }
}
