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

    [SerializeField] private Transform boardTransform;
    [SerializeField] private CardData cardData;
    public CardData CardData => cardData;

    private AkgRigidbody akgRigidbody;
    private Vector3 nowPos, nextPos;
    public ParticleSystem followingStone;
    private ParticleSystem nowParticle = null;
    public float _ChasingSpeed = 0.1f;
    public bool isClicked = false;

    private void Start()
    {
        boardTransform = GameObject.Find("Board").transform;
        akgRigidbody = GetComponent<AkgRigidbody>();
    }

    private void Update()
    {
        if(!CheckStoneDropByTransform())
        {
            Destroy(nowParticle);
            Destroy(gameObject);
        }
        if (akgRigidbody.velocity == Vector3.zero || !isClicked)
        {
            if (nowParticle == null) return;
            
            isClicked = false;

            if (nowParticle.isStopped)
            {
                nowParticle.Stop();
                Destroy(nowParticle.gameObject);
                nowParticle = null;
            }
        }
        else
        {
            if (nowParticle == null)
            {
                nowParticle = Instantiate(followingStone,transform.position,Quaternion.identity);
                nowParticle.Play();
            }
            //속력에 따른 파티클 양 조절
            
            ParticleSystem[] nowParticles = nowParticle.gameObject.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem now in nowParticles)
            {
                var em = now.emission;
                em.rateOverDistanceMultiplier = akgRigidbody.velocity.magnitude;
            }

            nowPos = transform.position;
            nextPos = Camera.main.ScreenToWorldPoint(nowPos);
            nowParticle.transform.position = Vector3.Lerp(nowParticle.transform.position, nowPos, _ChasingSpeed);
            float angle = Quaternion.FromToRotation(new Vector3(0,1,0), akgRigidbody.velocity.normalized - new Vector3(0, 0, 1)).eulerAngles.y;
            nowParticle.transform.rotation = Quaternion.Euler(new Vector3(0,angle-180,0));
        }
    }

    public void SetCardData(CardData data)
    {
        cardData = data;
    }

    private bool CheckStoneDropByTransform()
    {
        if(transform.position.x > boardTransform.transform.position.x + boardTransform.transform.localScale.x * 5f) return false;
        if(transform.position.x < boardTransform.transform.position.x - boardTransform.transform.localScale.x * 5f) return false;
        if(transform.position.z > boardTransform.transform.position.z + boardTransform.transform.localScale.z * 5f) return false;
        if(transform.position.z < boardTransform.transform.position.z - boardTransform.transform.localScale.z * 5f) return false;
        return true;
    }
}