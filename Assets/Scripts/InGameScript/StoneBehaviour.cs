using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneBehaviour : MonoBehaviour, AkgRigidbodyInterface
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

    [SerializeField] private int stoneId;
    public int StoneId => stoneId;
    [SerializeField] private Transform boardTransform;
    [SerializeField] private CardData cardData;
    [SerializeField] private GameObject collideParticle;
    public CardData CardData => cardData;

    private AkgRigidbody akgRigidbody;
    public bool isMoving
    {
        get
        {
            return akgRigidbody.velocity.magnitude > 0f;
        }
    }

    public float curVelocity
    {
        get
        {
            return akgRigidbody.velocity.magnitude;
        }
    }
    private Vector3 nowPos, nextPos;
    public ParticleSystem followingStone;
    private ParticleSystem nowParticle = null;
    public float _ChasingSpeed = 0.1f;
    public bool isClicked = false;

    [SerializeField] private GameManager.PlayerEnum belongingPlayer;
    public GameManager.PlayerEnum BelongingPlayer => belongingPlayer;

    public bool isExiting = false;
    public bool isExitingByPlaying = false;
    [SerializeField] float indirectExitTime;
    [SerializeField] float indirectExitSpeed;

    // temp:
    [SerializeField] private GameObject enemySign;

    private void Awake()
    {
        boardTransform = GameObject.Find("Board").transform;
        akgRigidbody = GetComponent<AkgRigidbody>();
        ParticleManager.Inst.RegisterParticle(collideParticle);
    }

    private void Update()
    {
        if (!CheckStoneDropByTransform())
        {
            RemoveStoneFromGame();
            StartCoroutine(EIndirectExit());
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
            float angle = Quaternion.FromToRotation(new Vector3(0,0,1), akgRigidbody.velocity.normalized).eulerAngles.y;
            nowParticle.transform.rotation = Quaternion.Euler(new Vector3(0,angle-180,0));
        }
    }

    public void RemoveStoneFromGame()
    {
        isExiting = true;
        if (!AkgPhysicsManager.Inst.rigidbodyRecorder.IsPlaying)
            AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(new MyNetworkData.EventRecord
            {
                stoneId = stoneId,
                eventEnum = MyNetworkData.EventEnum.DROPOUT,
                time = Time.time,
                xPosition = transform.position.x,
                zPosition = transform.position.z,
            });

        GameManager.Inst.players[(int)BelongingPlayer].RemoveStone(stoneId);
        akgRigidbody.BeforeDestroy();
    }

    public void SetCardData(CardData data, int id, GameManager.PlayerEnum owner)
    {
        cardData = data;
        stoneId = id;
        belongingPlayer = owner;
        if (BelongingPlayer == GameManager.PlayerEnum.LOCAL)
        {
            akgRigidbody.layerMask = AkgPhysicsManager.AkgLayerMaskEnum.LOCALSTONE;
            enemySign.SetActive(false);
        }
        else
        {
            akgRigidbody.layerMask = AkgPhysicsManager.AkgLayerMaskEnum.OPPOSTONE;
            enemySign.SetActive(true);
        }
    }

    private bool CheckStoneDropByTransform()
    {
        if (isExiting) return true;
        if (AkgPhysicsManager.Inst.IsRecordPlaying)
        {
            if (isExitingByPlaying) return false;
            return true;
        }
        if (transform.position.x > boardTransform.transform.position.x + boardTransform.transform.localScale.x * 5f) return false;
        if (transform.position.x < boardTransform.transform.position.x - boardTransform.transform.localScale.x * 5f) return false;
        if (transform.position.z > boardTransform.transform.position.z + boardTransform.transform.localScale.z * 5f) return false;
        if (transform.position.z < boardTransform.transform.position.z - boardTransform.transform.localScale.z * 5f) return false;
        return true;
    }

    private bool isStoneLeaveScreen()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.transform.position.z);
        Vector3 screenHeight = new Vector3(Screen.width / 2, Screen.height, Camera.main.transform.position.z);
        Vector3 screenWidth = new Vector3(Screen.width, Screen.height/2, Camera.main.transform.position.z);
        Vector3 goscreen = Camera.main.WorldToScreenPoint(transform.position);
 
        float distX = Vector3.Distance(new Vector3(Screen.width / 2, 0f, 0f), new Vector3(goscreen.x, 0f,0f));
        float distY = Vector3.Distance(new Vector3(0f, Screen.height / 2, 0f), new Vector3(0f, goscreen.y, 0f));
 
        return distX > Screen.width / 2 || distY > Screen.height / 2;
    }

    private void IndirectExit()
    {
        // Debug.Log("Stone is Indirectly Exited!");

        //temp particle
        StartCoroutine(ParticleManager.Inst.PlayParticle(collideParticle, transform.position));
        Destroy(gameObject);
    }

    private void DirectExit()
    {
        // Debug.Log("Stone is Directly Exited!");
        Destroy(gameObject);
    }

    private IEnumerator EIndirectExit()
    {
        transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = CardData.breakSprite;

        isExiting = true;
        float curTime = indirectExitTime;
        while(curTime >= 0)
        {
            if(isStoneLeaveScreen())
            {
                DirectExit();
                yield break;
            }
            curTime -= Time.deltaTime;
            float var = Util.GetRadiusFromStoneSize(cardData.stoneSize) * curTime / indirectExitTime;
            transform.Rotate(Vector3.up, Time.deltaTime * indirectExitSpeed);
            transform.localScale = new Vector3(var, 1f, var);
            yield return null;
        }
        IndirectExit();
    }

    public void OnCollide(AkgRigidbody collider, Vector3 collidePoint)
    {
        //TODO : should prevent doubly occuring particle between two stone collision
        StartCoroutine(ParticleManager.Inst.PlayParticle(collideParticle, collidePoint, curVelocity / 20f, curVelocity / 20f));
    }

}