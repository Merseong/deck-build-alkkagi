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

    [SerializeField] private bool isStatic = false;
    [SerializeField] private float mass = 1.0f;
    [SerializeField] private float drag = 1f;
    public float Mass => mass;
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

    public Vector3 circleCenter;
    public float circleRadius;
    [Tooltip("x, y에 작은 x, 작은 z를 / z, w에 큰 x, 큰 z를 넣는다")]
    public Vector4 rectPoints;
    public AkgPhysicsManager.AkgLayerMaskEnum layerMask = AkgPhysicsManager.AkgLayerMaskEnum.DEFAULT;

    private AkgRigidbody[] collidableList;
    private bool isForecasting = false;
    private int collidableForecastLimit = 3;
    private List<int> collidedList;

    public void Init(float initMass = -1)
    {
        if (initMass > 0)
        {
            mass = initMass;
        }
        if (mass < 0)
        {
            mass = 0;
        }
        drag = AkgPhysics.movingDragAccleration;

        collidableList = new AkgRigidbody[collidableForecastLimit];
        collidedList = new();
        AkgPhysicsManager.Inst.AddAkgRigidbody(this);
    }

    private void Update()
    {
        if (isStatic) return;
        if (velocity == Vector3.zero)
        {
            isForecasting = false;
            return;
        }

        oldVelocity = velocity;

        float speed = velocity.magnitude;
        if (speed > AkgPhysics.dragThreshold)
        {
            velocity = Mathf.Max(speed - Time.deltaTime * drag, 0) * velocity.normalized;
        }
        else if (speed > 0)
        {
            velocity = Mathf.Max(speed - Time.deltaTime * AkgPhysics.dragAccleration, 0) * velocity.normalized;
        }

        Move(Time.deltaTime * velocity);

        if (!isForecasting) return;

        for (int i = 0; i < collidableForecastLimit; ++i)
        {
            if (!collidableList[i]) continue;
            if (i > 0 && collidableList[i] == collidableList[i - 1]) continue;

            if (CheckCollide(collidableList[i], out var point))
            {
                isForecasting = false;
                var colAkgObject = collidableList[i];
                if (collidedList.Contains(colAkgObject.GetInstanceID())) continue;
                collidedList.Add(colAkgObject.GetInstanceID());

                colAkgObject.OnCollision(this, point);
                OnCollision(colAkgObject, point);

                if (colAkgObject.TryGetComponent<AkgRigidbodyInterface>(out var akgI))
                    akgI.OnCollide(this, point, true);
                if (TryGetComponent<AkgRigidbodyInterface>(out var localAkgI))
                    localAkgI.OnCollide(colAkgObject, point, false);

                Move(Time.deltaTime * velocity);
            }
        }
    }

    private void LateUpdate()
    {
        if (isStatic) return;

        collidedList.Clear();
    }

    public void BeforeDestroy()
    {
        if (AkgPhysicsManager.IsEnabled)
            AkgPhysicsManager.Inst.RemoveAkgRigidbody(this);
        isForecasting = false;
    }

    public void Move(Vector3 next)
    {
        transform.position += next;
    }

    public void SetDrag(float newDrag)
    {
        drag = newDrag;
    }

    public bool CheckPointCollide(Vector3 point)
    {
        return ColliderType switch
        {
            ColliderTypeEnum.Circle => Vector3.Distance(transform.position + circleCenter, point) < circleRadius,
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

        var collideCenter = transform.position + circleCenter;

        switch (targetAkg.ColliderType)
        {
            case ColliderTypeEnum.Circle:
                var targetCenter = targetAkg.transform.position + targetAkg.circleCenter;
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

    private void CollideForecast()
    {
        if (isStatic) return;
        if (isForecasting) return;
        if (ColliderType != ColliderTypeEnum.Circle) return;

        var rightPoint = transform.position + circleCenter + Vector3.Cross(velocity, Vector3.up).normalized * circleRadius;
        var leftPoint = transform.position + circleCenter + Vector3.Cross(Vector3.up, velocity).normalized * circleRadius;

        var startings = new Vector3[collidableForecastLimit];
        startings[0] = transform.position + circleCenter;
        startings[1] = rightPoint;
        startings[2] = leftPoint;

        for (int i = 0; i < collidableForecastLimit; ++i)
        {
            Raycast(startings[i], velocity, out collidableList[i]);
        }

        isForecasting = true;
    }

    public void AddForce(Vector3 force)
    {
        if (isStatic) return;

        Vector3 acceleration = force / mass;
        velocity += Time.fixedDeltaTime * acceleration;

        RecordVelocity();
        CollideForecast();
    }

    public void SetVelocity(Vector3 vel)
    {
        if (isStatic) return;

        velocity = vel;
    }

    public void OnCollision(AkgRigidbody akg, Vector3 point)
    {
        if (isStatic) return;

        if (AkgPhysicsManager.Inst.rigidbodyRecorder.IsPlaying) return;

        Debug.Log($"Collision/ {transform.position} {point} {akg.velocity}");

        Vector3 normal = transform.position + circleCenter - point;
        normal.y = 0;
        normal = normal.normalized;

        Vector3 normalVelocity = Vector3.Dot(normal, velocity) * normal;
        Vector3 tangentialVelocity = velocity - normalVelocity;

        if (akg.isStatic)
        {
            normalVelocity = -cor * normalVelocity;
        }
        else
        {
            if (velocity == Vector3.zero)
            {
                StoneBehaviour stone = gameObject.GetComponent<StoneBehaviour>();
                gameObject.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = stone.GetSpriteState("Idle");

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
            normalVelocity = mass * normalVelocity + akg.Mass * otherNormalVelocity + akg.Mass * cor * (otherNormalVelocity - normalVelocity);
            normalVelocity = normalVelocity / (Mass + akg.Mass);
        }

        velocity = normalVelocity + tangentialVelocity;
        RecordVelocity();
        RecordCollideEvent(MyNetworkData.EventEnum.COLLIDE);
        CollideForecast();
    }

    private void RecordVelocity()
    {
        if (!TryGetComponent<StoneBehaviour>(out var stone)) return;

        AkgPhysicsManager.Inst.rigidbodyRecorder.AppendVelocity(new MyNetworkData.VelocityRecord
        {
            stoneId = stone.StoneId,
            time = Time.time,
            xVelocity = velocity.x,
            zVelocity = velocity.z,
        });
    }

    private void RecordCollideEvent(MyNetworkData.EventEnum eventEnum)
    {
        if (!TryGetComponent<StoneBehaviour>(out var stone)) return;

        AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(new MyNetworkData.EventRecord
        {
            stoneId = stone.StoneId,
            time = Time.time,
            eventEnum = eventEnum,
        });
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        GUI.color = velocity.magnitude >= AkgPhysics.dragThreshold ? Color.red : Color.blue;
        Handles.Label(transform.position + Vector3.up * .2f, velocity.magnitude.ToString());
        switch (ColliderType)
        {
            case ColliderTypeEnum.Circle:
                Gizmos.DrawWireSphere(transform.position + circleCenter, circleRadius);
                break;
            case ColliderTypeEnum.Rect:
                Gizmos.DrawWireCube(transform.position, new Vector3(rectPoints.z - rectPoints.x, 0.2f, rectPoints.w - rectPoints.y));
                break;
        }
    }
#endif
}
