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
        Debug.Log(id + ", " + type);
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
        if (!calledByPacket)
        {
            StoneActionSendNetworkAction("ENTER/ " + options);
        }
    }

    /// <param name="options">"@ @ @ ... @" 꼴, Split(' ')으로 쪼개서 사용하면됨</param>
    public virtual void OnExit(bool calledByPacket = false, string options = "")
    {
        if (!calledByPacket)
        {
            StoneActionSendNetworkAction("EXIT/ " + options);
        }
    }

    public event Action OnShootEnter;
    public event Action OnShootExit;
    public event Action<AkgRigidbody> OnHit;

    [SerializeField] private int stoneId;
    public int StoneId => stoneId;
    [SerializeField] private Transform boardTransform;
    [SerializeField] private CardData cardData;
    [SerializeField] private GameObject collideParticle;
    [SerializeField] private GameObject directExitParticle;
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

    public void InvokeShootEnter()
    {
        OnShootEnter?.Invoke();
    }

    public void InvokeShootExit()
    {
        OnShootExit?.Invoke();
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
                xPosition = transform.position.x,
                zPosition = transform.position.z,
            });
        OnExit();
        GameManager.Inst.players[(int)BelongingPlayer].InvokeCowardGhosts();
        GameManager.Inst.players[(int)BelongingPlayer].RemoveStone(stoneId);
        akgRigidbody.SetDragAccel(0);
        akgRigidbody.BeforeDestroy();
    }

    public void SetCardData(CardData data, int id, GameManager.PlayerEnum owner)
    {
        cardData = data;
        stoneId = id;
        belongingPlayer = owner;
        if (BelongingPlayer == GameManager.PlayerEnum.LOCAL)
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
        else if (actionStr.StartsWith("EXIT/"))
        {
            OnExit(true, actionStr.Substring(6));
        }
    }

    protected virtual void StoneActionSendNetworkAction(string eventString)
    {
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

    public float GetMass(float init)
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

    public void Shoot(Vector3 vec, bool isRotated)
    {
        PlayerBehaviour player = GameManager.Inst.GetPlayer(BelongingPlayer);

        GameManager.Inst.SetLocalDoAction();
        if (!CanSprint())
            player.ShootTokenAvailable = false;
        ChangeSpriteAndRot("Shoot", isRotated);
        OnShootEnter?.Invoke();
        StartCoroutine(EShoot(isRotated));
        GetComponent<AkgRigidbody>().AddForce(vec);
    }

    private IEnumerator EShoot(bool isRotated)
    {
        var recorder = AkgPhysicsManager.Inst.rigidbodyRecorder;
        recorder.StartRecord(Time.time);

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
        bool isAllStoneStop = false;

        while (!isAllStoneStop)
        {
            yield return new WaitUntil(() =>
                (GameManager.Inst.AllStones.Count == 0 ||
                GameManager.Inst.AllStones.Values.All(x => !x.isMoving))
            );

            isAllStoneStop = true;
        }

        // shoot end
        foreach (StoneBehaviour stone in GameManager.Inst.AllStones.Values)
        {
            stone.ChangeSpriteAndRot("Idle", isRotated);
        }

        // send physics records, stone final poses, event list
        recorder.EndRecord(out var velocityRecords, out var eventRecords);
        recorder.SendRecord(velocityRecords, eventRecords);

        OnShootExit?.Invoke();
    }

    public virtual Sprite GetSpriteState(string state)
    {
        Sprite sprite = GameManager.Inst.stoneAtlas.GetSprite($"{cardData.cardEngName}_{state}");
        while (sprite == null)
        {
            Debug.LogError($"There is no sprite named \"{cardData.cardEngName}_{state}\"");
            switch (state)
            {
                case "Shoot":
                case "Hit":
                    state = "Idle";
                    break;
                case "Ready":
                case "Break":
                    state = "Shoot";
                    break;
                case "Idle":
                    return null;
                default:
                    state = "Idle";
                    break;
            }
            sprite = GameManager.Inst.stoneAtlas.GetSprite($"{cardData.cardEngName}_{state}");
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

    public void OnCollide(AkgRigidbody collider, Vector3 collidePoint, bool isCollided)
    {
        Debug.Log($"{cardData.name} is collided with {collider.GetComponent<StoneBehaviour>()?.cardData.name}: {isCollided}");
        OnHit?.Invoke(collider);

        if (collider.layerMask.HasFlag(AkgLayerMask.COLLIDED))
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

        //Fuction for make additional action when collided at individual stone behaviour
        StoneCollisionProperty(collider, collidePoint, isCollided);

        //TODO : should prevent doubly occuring particle between two stone collision
        StartCoroutine(ParticleManager.Inst.PlayParticle(collideParticle, collidePoint, curVelocity / 20f, curVelocity / 20f));
        if (isCollided)
        {
            // if collided, change sprite to collided
        }
    }

    protected virtual void StoneCollisionProperty(AkgRigidbody collider, Vector3 collidePoint, bool isCollided){}
}
