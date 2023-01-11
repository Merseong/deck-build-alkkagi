using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneBehaviour : MonoBehaviour
{
    // 매칭되는 카드
    // 기본수치

    // 돌이 나온 이후 추가된 버프

    // 실제 값들 = (기본수치 + 카드버프) + 돌버프
    // FixedUpdate -> 이동 관련
    // 충돌 확인 (트리거) -> 충돌한 돌에 다음 속도 전달

    // 능력
    // 
    // 소환 이벤트
    // 파괴 이벤트
    // 타격 이벤트

    private void Update()
    {
        //Debug.Log(CheckStoneDropByTransform());
    }

    [SerializeField] private Transform boardTransform;

    private bool CheckStoneDropByTransform()
    {
        if(transform.position.x > boardTransform.transform.position.x + boardTransform.transform.localScale.x * 5f) return false;
        if(transform.position.x < boardTransform.transform.position.x - boardTransform.transform.localScale.x * 5f) return false;
        if(transform.position.z > boardTransform.transform.position.z + boardTransform.transform.localScale.z * 5f) return false;
        if(transform.position.z < boardTransform.transform.position.z - boardTransform.transform.localScale.z * 5f) return false;
        return true;
    }


}
