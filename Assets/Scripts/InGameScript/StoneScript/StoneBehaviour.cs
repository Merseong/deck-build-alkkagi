using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StoneBehaviour : MonoBehaviour, IAkgRigidbodyInterface
{
    #region StoneTyes
    public static Type GetStoneWithID(int id)
    {
        
        Type type = Type.GetType(Util.GetCardDataFromID(id, GameManager.Inst.CardDatas).cardEngName + "StoneBehaviour");
        // Debug.Log(id + ", " + type);
        if(type == null)
        {
            return typeof(StoneBehaviour);
        } 
        else
        {
            return type;
        } 
    }
    #endregion

    /// <param name="options">"@ @ @ ... @" 꼴, Split(' ')으로 쪼개서 사용하면됨</param>
    public virtual void OnEnter(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnStoneEnter?.Invoke(this);

        if (!calledByPacket)
        {
            StoneActionSendNetworkAction("ENTER/ " + options);
        }
    }

    /// <param name="options">"@ @ @ ... @" 꼴, Split(' ')으로 쪼개서 사용하면됨</param>
    public virtual void OnExit(bool calledByPacket = false, string options = "")
    {
        GameManager.Inst.localDeadStones.Add(CardData.CardID);

        BelongingPlayer.OnStoneExit?.Invoke(this);
    }

    public event Action OnShootEnter;
    public Action OnShootExit;
    public event Action<AkgRigidbody> OnHit;

    [SerializeField] private int stoneId;
    public int StoneId => stoneId;
    [SerializeField] private Transform boardTransform;
    [SerializeField] private CardData cardData;
    [SerializeField] private GameObject collideParticle;
    [SerializeField] private GameObject directExitParticle;
    public CardData CardData => cardData;

    protected AkgRigidbody akgRigidbody;
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

    [SerializeField] private GameManager.PlayerEnum belongingPlayerEnum;
    public GameManager.PlayerEnum BelongingPlayerEnum => belongingPlayerEnum;
    public PlayerBehaviour BelongingPlayer => GameManager.Inst.GetPlayer(belongingPlayerEnum);

    public bool isExiting = false;
    public bool isExitingByPlaying = false;
    [SerializeField] float indirectExitTime = 1f;
    [SerializeField] float indirectExitSpeed;

    public List<StoneProperty> Properties { get; private set; }

    private void Awake()
    {
        boardTransform = GameObject.Find("Board").transform;
        akgRigidbody = GetComponent<AkgRigidbody>();

        collideParticle = ParticleManager.Inst.collideParticlePrefab;
        directExitParticle = ParticleManager.Inst.directExitParticlePrefab;
        followingStone = ParticleManager.Inst.followingStonePrefab;
        ParticleManager.Inst.RegisterParticle(collideParticle);
        ParticleManager.Inst.RegisterParticle(directExitParticle);
    }

    private void Update()
    {
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

    private void LateUpdate()
    {
        if (!CheckStoneDropByTransform())
        {
            RemoveStoneFromGame();
            StartCoroutine(EIndirectExit());
        }
    }

    public void RemoveStoneFromGame()
    {
        isExiting = true;
        if (!AkgPhysicsManager.Inst.rigidbodyRecorder.IsPlaying)
            AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(new EventRecord
            {
                stoneId = stoneId,
                eventEnum = EventEnum.DROPOUT,
                time = Time.time,
                xPosition = Util.FloatToSlicedString(transform.position.x),
                zPosition = Util.FloatToSlicedString(transform.position.z),
            });
        OnExit();
        BelongingPlayer.RemoveStone(stoneId);
        akgRigidbody.SetDragAccel(0);
        akgRigidbody.BeforeDestroy();
    }

    public void SetCardData(CardData data, int id, GameManager.PlayerEnum owner)
    {
        cardData = data;
        stoneId = id;
        belongingPlayerEnum = owner;
        if (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL)
        {
            akgRigidbody.layerMask = AkgLayerMask.LOCAL | AkgLayerMask.STONE;
            OnEnter();
        }
        else
        {
            akgRigidbody.layerMask = AkgLayerMask.OPPO | AkgLayerMask.STONE;
            // oppo stone의 경우, 이후 추가 action을 수신하면 그때 OnEnter를 호출한다.
        }
    }

    public virtual void ParseActionString(string actionStr)
    {
        if (actionStr.StartsWith("ENTER/"))
        {
            OnEnter(true, actionStr.Substring(7)); // 기본적으로 space가 하나 들어감
        }
    }

    protected virtual void StoneActionSendNetworkAction(string eventString)
    {
        if (isExiting) return;
        AkgPhysicsManager.Inst.rigidbodyRecorder.SendEventOnly(new EventRecord
        {
            eventEnum = EventEnum.POWER,
            stoneId = StoneId,
            eventMessage = eventString,
            time = Time.time,
        });
    }

    #region Stone Properties

    public virtual void InitProperty()
    {
        Properties = new List<StoneProperty>();
    }

    public void AddProperty<T>(T property) where T : StoneProperty
    {
        (bool result, T oldProperty) = StoneProperty.IsAvailable<T>(this, property);
        if (result)
        {
            if (oldProperty != null)
            {
                oldProperty.OnRemoved(true);
                Properties.Remove(oldProperty);
            }

            Properties.Add(property);
            property.OnAdded(oldProperty != null);
        }
    }

    public void RemoveProperty(StoneProperty property)
    {
        property.OnRemoved();
        Properties.Remove(property);
    }

    public void PrintProperty()
    {
        Debug.Log($"{cardData?.cardEngName} has {Properties.Count} properties");
        foreach (StoneProperty property in Properties)
        {
            Debug.Log($"{property.GetType().Name}");
        }
    }

    public bool CanSprint(bool init = false)
    {
        foreach (StoneProperty property in Properties)
        {
            init = property.CanSprint(init);
        }

        return init;
    }

    public bool IsGhost(bool init = false)
    {
        foreach (StoneProperty property in Properties)
        {
            init = property.IsGhost(init);
        }

        return init;
    }

    public int ShieldCount(int init = 0)
    {
        foreach (StoneProperty property in Properties)
        {
            init = property.ShieldCount(init);
        }

        return init;
    }

    public bool HasAccelShield(bool init = false)
    {
        foreach (StoneProperty property in Properties)
        {
            init = property.HasAccelShield(init);
        }

        return init;
    }

    public virtual float GetMass(float init)
    {
        foreach (StoneProperty property in Properties)
        {
            init = property.GetMass(init);
        }

        return init;
    }

    public bool IsStatic(bool init)
    {
        foreach (StoneProperty property in Properties)
        {
            init = property.IsStatic(init);
        }

        return init;
    }

    public float GetDragAccel(float init)
    {
        foreach (StoneProperty property in Properties)
        {
            init = property.GetDragAccel(init);
        }

        return init;
    }

    #endregion

    public void Shoot(Vector3 vec, bool isRotated, bool local, bool callOnShootExit = true)
    {
        if (!CanSprint())
            BelongingPlayer.ShootTokenAvailable = false;
        ChangeSpriteAndRot("Shoot", isRotated);

        if (local)
        {
            GameManager.Inst.SetLocalDoAction();

            GameManager.Inst.StartCoroutine(EShoot(isRotated, callOnShootExit));
            GetComponent<AkgRigidbody>().AddForce(vec);
        }

        OnShootEnter?.Invoke();
    }

    public void _Shoot(Vector3 vec, bool isRotated)
    {
        ChangeSpriteAndRot("Shoot", isRotated);
        StartCoroutine(EShoot(isRotated));
        GetComponent<AkgRigidbody>().AddForce(vec);

        OnShootEnter?.Invoke();
    }

    private IEnumerator EShoot(bool isRotated, bool callOnShootExit = true)
    {
        var recorder = AkgPhysicsManager.Inst.rigidbodyRecorder;
        recorder.StartRecord(Time.time);

        AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(new EventRecord
        {
            eventEnum = EventEnum.SHOOT,
            stoneId = StoneId,
            eventMessage = BelongingPlayer.ShootTokenAvailable.ToString() + " " + isRotated.ToString(),
            time = Time.time,
        });

        //if (!ShootTokenAvailable)
        //{
        //    recorder.AppendEventRecord(new EventRecord
        //    {
        //        time = Time.time,
        //        stoneId = firedStone.StoneId,
        //        eventEnum = EventEnum.SPENDTOKEN,
        //    });
        //}

        yield return null;
        yield return new WaitUntil(() =>
                (GameManager.Inst.AllStones.Count == 0 ||
                GameManager.Inst.AllStones.Values.All(x => !x.isMoving))
            );

        // shoot end
        foreach (StoneBehaviour stone in GameManager.Inst.AllStones.Values)
        {
            stone.ChangeSpriteAndRot("Idle", isRotated);
        }

        if (callOnShootExit)
            OnShootExit?.Invoke();

        // send physics records, stone final poses, event list
        recorder.EndRecord(out var velocityRecords, out var eventRecords);
        recorder.SendRecord(velocityRecords, eventRecords);
    }

    public virtual Sprite GetSpriteState(string state)
    {
        Sprite sprite = GameManager.Inst.stoneAtlas.GetSprite($"{cardData.cardEngName}_{state}");
        if(sprite == null)
        {
            sprite = GameManager.Inst.stoneAtlas.GetSprite($"{cardData.cardEngName}_Idle");
            Debug.Log($"There is no sprite named \"{cardData.cardEngName}_{state}\""); 
        } 
        return sprite;
    }

    public virtual void ChangeSpriteAndRot(string state, bool isRotated)
    {
        transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = GetSpriteState(state);
        if (isRotated)
            transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180, 0);
        else
            transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    private bool CheckStoneDropByTransform()
    {
        if (isExiting) return true;
        if (isExitingByPlaying) return false;
        if (AkgPhysicsManager.Inst.IsRecordPlaying) return true;
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
        ParticleManager.Inst.StartCoroutine(ParticleManager.Inst.PlayParticle(collideParticle, transform.position));
        Destroy(gameObject);
    }

    private void DirectExit()
    {
        // Debug.Log("Stone is Directly Exited!");
        
        ParticleManager.Inst.StartCoroutine(ParticleManager.Inst.PlayParticle(directExitParticle, transform.position * 1.1f, -akgRigidbody.velocity));
        Destroy(gameObject);
    }

    public IEnumerator EIndirectExit(bool isDirected = false)
    {
        transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = GetSpriteState("Break");

        isExiting = true;
        float curTime = indirectExitTime;
        while(curTime >= 0)
        {
            if(isStoneLeaveScreen() || isDirected)
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

    public void OnCollide(AkgRigidbody collider, Vector3 collidePoint, bool isCollided, bool calledByPacket = false)
    {
        Debug.Log($"{cardData.name} is collided with {collider.GetComponent<StoneBehaviour>()?.cardData.name}: {isCollided}");
        OnHit?.Invoke(collider);
        BelongingPlayer.OnStoneHit?.Invoke(this, collider);

        if (!collider.layerMask.HasFlag(AkgLayerMask.STONE))
        {
            if (HasAccelShield())
            {
                foreach (StoneProperty property in Properties)
                {
                    if (property is AccelShieldProperty accelShield)
                    {
                        accelShield.UseAccelShield();
                        return;
                    }
                }
            }
            else if (ShieldCount() > 0)
            {
                foreach (StoneProperty property in Properties)
                {
                    if (property is ShieldProperty shield)
                    {
                        shield.DecreaseShieldCount();
                        return;
                    }
                }
            }
        }

        //TODO : should prevent doubly occuring particle between two stone collision
        StartCoroutine(ParticleManager.Inst.PlayParticle(collideParticle, collidePoint, curVelocity / 20f, curVelocity / 20f));
        if (isCollided)
        {
            // if collided, change sprite to collided
        }
    }
}
