using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AkgRigidbody : MonoBehaviour
{
    [Header("Physics")]
    public Vector3 velocity = Vector3.zero;
    private Vector3 oldVelocity = Vector3.zero;
    public bool IsMoving => velocity.magnitude > 0;

    private StoneBehaviour stone = null;
    public bool isDisableCollide = false;
    [SerializeField] private bool isStatic = false;
    [SerializeField] private float mass = 1.0f;
    [SerializeField] private float dragAccel = 1f;
    
    public virtual bool IsStatic
    {
        get
        {
            if (stone == null)
                return isStatic;
            else
                return stone.IsStatic(isStatic);
        }
        private set => isStatic = value;
    }
    public virtual float Mass
    {
        get
        {
            if (stone == null)
                return mass;
            else
                return stone.GetMass(mass);
        }
        private set => mass = value;
    }
    private float effectMass => 1 + Mass * 5;
    public virtual float DragAccel
    {
        get
        {
            if (stone == null)
                return dragAccel;
            else
                return stone.GetDragAccel(dragAccel);
        }
        private set => dragAccel = value;
    }

    [Tooltip("반발계수")]
    [SerializeField] private float cor = 0.5f;

    [Header("Collider")]
    [SerializeField] private ColliderTypeEnum colliderType;
    public ColliderTypeEnum ColliderType => colliderType;
    public enum ColliderTypeEnum
    {
        Circle,
        Rect
    }
    public Vector3 CircleCenterPosition => transform.position + circleCenterOffset;
    public Vector3 circleCenterOffset;
    public float circleRadius;
    [Tooltip("x, y에 작은 x, 작은 z를 / z, w에 큰 x, 큰 z를 넣는다")]
    public Vector4 rectPoints;

    public AkgLayerMask layerMask = AkgLayerMask.DEFAULT;

    private AkgRigidbody[] collidableList;
    private bool isForecasting = false;
    private readonly int collidableRaycastForecastLimit = 3;
    private readonly int collidableNearbyForecastLimit = 0;
    private int CollidableForecastLimit => collidableRaycastForecastLimit + collidableNearbyForecastLimit;
    private List<int> collidedList;
    public HashSet<AkgRigidbody> IgnoreCollide;

    public void Init() => Init(0, 1f);

    private void Awake()
    {
        collidedList = new();
        collidableList = new AkgRigidbody[CollidableForecastLimit];
        IgnoreCollide = new();
    }

    public void Init(float initRadius, float initMass)
    {
        Mass = initMass;
        circleRadius = initRadius;
        DragAccel = AkgPhysics.dragAccel;

        stone = GetComponent<StoneBehaviour>();

        AkgPhysicsManager.Inst.AddAkgRigidbody(this);
    }

    private void Update()
    {
        if (IsStatic) return;
        if (velocity == Vector3.zero)
        {
            isForecasting = false;
            return;
        }
        if (isDisableCollide) return;

        GetNearbyCollidable(out AkgRigidbody nearby);
        if (IgnoreCollide.Contains(nearby)) return;
        if (!CheckCollide(nearby, out var point)) return;

        CollisionActions(nearby, point);
        while (CheckCollide(nearby, out _))
        {
            Move();
            nearby.Move();
            GetNearbyCollidable(out var newNear);
            if (nearby != newNear) break;
        }
        /*
        // TODO2: 매번 제일 가까운 돌과의 충돌도 체크해야될듯 (fixed나 late써야하나)
        if (isDisableCollide) return;
        if (AkgPhysicsManager.Inst.IsRecordPlaying) return;
        if (!isForecasting) return;

        for (int i = 0; i < CollidableForecastLimit; ++i)
        {
            if (!collidableList[i]) continue;
            if (i > 0 && collidableList[i] == collidableList[i - 1]) continue;

            if (CheckCollide(collidableList[i], out var point))
            {
                isForecasting = false;
                var colAkgObject = collidableList[i];
                if (collidedList.Contains(colAkgObject.GetInstanceID())) continue;
                collidedList.Add(colAkgObject.GetInstanceID());
                colAkgObject.collidedList.Add(GetInstanceID());

                CollisionActions(colAkgObject, point);
                Move();
                colAkgObject.Move();
            }
        }*/
    }

    private void CollisionActions(AkgRigidbody collide, Vector3 point)
    {
        if (AkgPhysicsManager.Inst.rigidbodyRecorder.IsPlaying) return;

        AudioManager.Inst.HitSound(collide);

        collide.OnCollision(this, point, $"{stone.StoneId} COL");
        OnCollision(collide, point, $"{(collide.isStatic ? "STA" : collide.stone.StoneId)}");

        if (collide.TryGetComponent<IAkgRigidbodyInterface>(out var akgI))
            akgI.OnCollide(this, point, true);
        if (TryGetComponent<IAkgRigidbodyInterface>(out var localAkgI))
            localAkgI.OnCollide(collide, point, false);
    }

    private void LateUpdate()
    {
        if (IsStatic) return;

        collidedList.Clear();
        if (!IsMoving) return;

        oldVelocity = velocity;

        float speed = velocity.magnitude;
        if (speed > 0)
        {
            velocity = Mathf.Max(speed - Time.deltaTime * DragAccel * effectMass, 0) * velocity.normalized;
        }

        Move(Time.deltaTime * velocity);
    }

    public void BeforeDestroy()
    {
        if (AkgPhysicsManager.IsEnabled)
            AkgPhysicsManager.Inst.RemoveAkgRigidbody(this);
        isForecasting = false;
    }

    public void Move()
    {
        Move(0.001f * Time.deltaTime * velocity);
    }

    public void Move(Vector3 next)
    {
        transform.position += next;
    }

    public void SetDragAccel(float newDragAccel)
    {
        DragAccel = newDragAccel;
    }

    public bool CheckPointCollide(Vector3 point)
    {
        return ColliderType switch
        {
            ColliderTypeEnum.Circle => Vector3.Distance(transform.position + circleCenterOffset, point) < circleRadius,
            ColliderTypeEnum.Rect => (point.x > transform.position.x + rectPoints.x && point.x < transform.position.x + rectPoints.z &&
                                        point.z > transform.position.z + rectPoints.y && point.z < transform.position.z + rectPoints.w),
            _ => false,
        };
    }

    /// <summary>
    /// 일단은 원형-원형, 원형-사각만 처리하도록
    /// </summary>
    public bool CheckCollide(AkgRigidbody targetAkg, out Vector3 point)
    {
        point = transform.position;

        if (!AkgPhysicsManager.Inst.GetLayerCollide(layerMask, targetAkg.layerMask)) return false;

        if (ColliderType != ColliderTypeEnum.Circle) return false;

        var collideCenter = CircleCenterPosition;

        switch (targetAkg.ColliderType)
        {
            case ColliderTypeEnum.Circle:
                var targetCenter = targetAkg.CircleCenterPosition;
                point = Vector3.Lerp(collideCenter, targetCenter, circleRadius / (circleRadius + targetAkg.circleRadius));
                return Vector3.Distance(collideCenter, targetCenter) < circleRadius + targetAkg.circleRadius;
            case ColliderTypeEnum.Rect:
                var targetVec4 = new Vector4(targetAkg.transform.position.x, targetAkg.transform.position.z, targetAkg.transform.position.x, targetAkg.transform.position.z)
                    + targetAkg.rectPoints;
                if (targetVec4.x < collideCenter.x && collideCenter.x < targetVec4.z)
                {
                    if (collideCenter.z > targetVec4.w) point = new Vector3(collideCenter.x, 0, targetVec4.w);
                    else if (collideCenter.z < targetVec4.y) point = new Vector3(collideCenter.x, 0, targetVec4.y);
                    else point = collideCenter;

                    return (targetVec4.y - circleRadius < collideCenter.z) &&
                        (targetVec4.w + circleRadius > collideCenter.z);
                }
                if (targetVec4.y < collideCenter.z && collideCenter.z < targetVec4.w)
                {
                    if (collideCenter.x > targetVec4.z) point = new Vector3(targetVec4.z, 0, collideCenter.z);
                    else if (collideCenter.x < targetVec4.x) point = new Vector3(targetVec4.x, 0, collideCenter.z);
                    else point = collideCenter;

                    return (targetVec4.x - circleRadius < collideCenter.x) &&
                        (targetVec4.z + circleRadius > collideCenter.x);
                }
                var checkingPoints = new Vector3[] {
                    new Vector3(targetVec4.x, 0, targetVec4.y),
                    new Vector3(targetVec4.z, 0, targetVec4.y),
                    new Vector3(targetVec4.x, 0, targetVec4.w),
                    new Vector3(targetVec4.z, 0, targetVec4.w),
                };
                var minDist = Vector3.Distance(collideCenter, checkingPoints[0]);
                point = checkingPoints[0];
                for (int i = 1; i < checkingPoints.Length; ++i)
                {
                    var dist = Vector3.Distance(collideCenter, checkingPoints[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        point = checkingPoints[i];
                    }
                }
                return minDist < circleRadius;
        }
        return false;
    }

    public bool Raycast(Vector3 start, Vector3 direction, out AkgRigidbody collideObject, float interval = 0.1f, float rayLimit = 100f)
    {
        collideObject = null;
        var normDir = direction.normalized;
        var normaledX = normDir.x >= 0 ? 1 : -1;
        var normaledZ = normDir.z >= 0 ? 1 : -1;

        var candidates = AkgPhysicsManager.Inst.GetFilteredRigidbodies((e) =>
        {
            if (e == this) return false;
            if (collidedList.Contains(e.GetInstanceID())) return false;
            if (!AkgPhysicsManager.Inst.GetLayerCollide(layerMask, e.layerMask)) return false;
            return true;
            //return e.transform.position.x * normaledX >= start.x * normaledX &&
            //       e.transform.position.z * normaledZ >= start.z * normaledZ;
        });

        for (float i = 0; i < rayLimit; i += interval)
        {
            foreach (var targetAkg in candidates)
            {
                if (!targetAkg)
                {
                    Debug.Log(targetAkg.GetInstanceID());
                    Debug.Break();
                    return false;
                }

                if (targetAkg.CheckPointCollide(start + normDir * i))
                {
                    collideObject = targetAkg;
                }
                if (collideObject) break;
            }
            if (collideObject) break;
        }

        return collideObject != null;
    }

    private void GetNearbyCollidable(out AkgRigidbody collideObject)
    {
        var candidates = AkgPhysicsManager.Inst.GetFilteredRigidbodies((e) =>
        {
            if (e == this) return false;
            if (collidedList.Contains(e.GetInstanceID())) return false;
            if (!AkgPhysicsManager.Inst.GetLayerCollide(layerMask, e.layerMask)) return false;
            return true;
        });
        if (candidates.Length == 0)
        {
            collideObject = null;
            return;
        }

        collideObject = candidates[0];
        var closestDist = Vector3.Distance(collideObject.CircleCenterPosition, CircleCenterPosition);

        foreach (var targetAkg in candidates)
        {
            if (!targetAkg)
            {
                Debug.Log(targetAkg.GetInstanceID());
                Debug.Break();
                break;
            }

            var dist = Vector3.Distance(targetAkg.CircleCenterPosition, CircleCenterPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                collideObject = targetAkg;
            }
        }
    }

    private void CollideForecast()
    {
        if (IsStatic) return;
        if (isForecasting) return;
        if (ColliderType != ColliderTypeEnum.Circle) return;

        var rightPoint = transform.position + circleCenterOffset + Vector3.Cross(velocity, Vector3.up).normalized * circleRadius;
        var leftPoint = transform.position + circleCenterOffset + Vector3.Cross(Vector3.up, velocity).normalized * circleRadius;

        var startings = new Vector3[collidableRaycastForecastLimit];
        startings[0] = transform.position + circleCenterOffset;
        startings[1] = rightPoint;
        startings[2] = leftPoint;

        for (int i = 0; i < collidableRaycastForecastLimit; ++i)
        {
            Raycast(startings[i], velocity, out collidableList[i]);
        }

        isForecasting = true;
    }

    public void AddForce(Vector3 force)
    {
        if (IsStatic) return;

        Vector3 acceleration = force / Mass;
        velocity += Time.fixedDeltaTime * acceleration;

        RecordVelocity();
        //CollideForecast();
    }

    public void SetVelocity(Vector3 vel, Vector3 pos, bool doRecord = false)
    {
        if (IsStatic) return;

        velocity = vel;
        transform.position = pos;

        if (doRecord)
            RecordVelocity();
    }

    public void OnCollision(AkgRigidbody akg, Vector3 point, string optionMessage)
    {
        if (IsStatic) return;
        if (isDisableCollide) return;

        Debug.Log($"Collision/ {transform.position} {point} {akg.velocity}");

        Vector3 normal = transform.position + circleCenterOffset - point;
        normal.y = 0;
        if (normal.magnitude == 0) return;
        normal = normal.normalized;

        Vector3 normalVelocity = Vector3.Dot(normal, velocity) * normal;
        Vector3 tangentialVelocity = velocity - normalVelocity;

        if (akg.IsStatic)
        {
            normalVelocity = -akg.cor * normalVelocity;
        }
        else
        {
            if (velocity == Vector3.zero)
            {
                StoneBehaviour stone = gameObject.GetComponent<StoneBehaviour>();
                gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = stone.GetSpriteState("Hit");

                float randnum = UnityEngine.Random.Range(-30.0f, 30.0f);
                if (GameManager.Inst.isLocalGoFirst)
                {
                    gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, randnum, 0);
                }
                else 
                {
                    gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>().transform.rotation = Quaternion.Euler(90, 180+randnum, 0);
                }
            }

            Vector3 otherNormalVelocity = Vector3.Dot(normal, akg.oldVelocity) * normal;
            normalVelocity = Mass * normalVelocity + akg.Mass * otherNormalVelocity + akg.Mass * cor * (otherNormalVelocity - normalVelocity);
            normalVelocity = normalVelocity / (Mass + akg.Mass);
        }

        velocity = normalVelocity + tangentialVelocity;
        RecordVelocity();
        RecordCollideEvent(EventEnum.COLLIDE, point, optionMessage);
        //CollideForecast();
    }

    private void RecordVelocity()
    {
        if (!TryGetComponent<StoneBehaviour>(out var stone)) return;

        AkgPhysicsManager.Inst.rigidbodyRecorder.AppendVelocity(new VelocityRecord
        {
            stoneId = stone.StoneId,
            time = Time.time,
            xPosition = Util.FloatToSlicedString(transform.position.x),
            zPosition = Util.FloatToSlicedString(transform.position.z),
            xVelocity = Util.FloatToSlicedString(velocity.x),
            zVelocity = Util.FloatToSlicedString(velocity.z),
        });
    }

    private void RecordCollideEvent(EventEnum eventEnum, Vector3 collidePosition, string optionMessage)
    {
        if (!TryGetComponent<StoneBehaviour>(out var stone)) return;

        AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(new EventRecord
        {
            stoneId = stone.StoneId,
            time = Time.time,
            eventEnum = eventEnum,
            xPosition = Util.FloatToSlicedString(collidePosition.x),
            zPosition = Util.FloatToSlicedString(collidePosition.z),
            eventMessage = optionMessage,
        });
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //GUI.color = velocity.magnitude >= AkgPhysics.dragThreshold ? Color.red : Color.blue;
        GUI.color = Color.red;
        switch (ColliderType)
        {
            case ColliderTypeEnum.Circle:
                Handles.Label(transform.position + Vector3.up * .2f, velocity.magnitude.ToString());
                Gizmos.DrawWireSphere(CircleCenterPosition, circleRadius);
                Gizmos.DrawLine(CircleCenterPosition, velocity);
                break;
            case ColliderTypeEnum.Rect:
                Gizmos.DrawWireCube(transform.position, new Vector3(rectPoints.z - rectPoints.x, 0.2f, rectPoints.w - rectPoints.y));
                break;
        }
    }
#endif
}
