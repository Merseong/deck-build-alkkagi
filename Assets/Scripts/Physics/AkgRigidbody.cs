using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AkgRigidbody : MonoBehaviour
{
    public bool isStatic = false;
    public float mass = 1.0f;
    public float cor = 0.9f;    // 반발계수

    [HideInInspector]
    public Vector3 velocity = Vector3.zero;
    private Vector3 oldVelocity = Vector3.zero;

    new private Rigidbody rigidbody;

    // TODO: 동기화를 위한 충돌 없이 이동하기 등 (point to point)
    // TODO(?): 각속도

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (isStatic || velocity == Vector3.zero) return;

        oldVelocity = velocity;

        float speed = velocity.magnitude;
        if (speed > AkgPhysics.dragThreshold)
        {
            velocity = Mathf.Max(speed - Time.fixedDeltaTime * AkgPhysics.movingDragAccleration, 0) * velocity.normalized;
        }
        else if (speed > 0)
        {
            velocity = Mathf.Max(speed - Time.fixedDeltaTime * AkgPhysics.dragAccleration, 0) * velocity.normalized;
        }

        rigidbody.MovePosition(transform.position + Time.fixedDeltaTime * velocity);
    }

    public void AddForce(Vector3 force)
    {
        if (isStatic) return;

        Vector3 acceleration = force / mass;
        velocity += Time.fixedDeltaTime * acceleration;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isStatic) return;

        AkgRigidbody other = collision.transform.GetComponent<AkgRigidbody>();
        if (other == null) return;

        ContactPoint cp = collision.GetContact(0);
        
        Vector3 normal = cp.normal;
        normal.y = 0;
        normal = normal.normalized;

        Vector3 normalVelocity = Vector3.Dot(normal, velocity) * normal;
        Vector3 tangentialVelocity = velocity - normalVelocity;

        if (other.isStatic)
        {
            normalVelocity = -cor * normalVelocity;
        }
        else
        {
            Vector3 otherNormalVelocity = Vector3.Dot(normal, other.oldVelocity) * normal;
            normalVelocity = mass * normalVelocity + other.mass * otherNormalVelocity + other.mass * cor * (otherNormalVelocity - normalVelocity);
            normalVelocity = normalVelocity / (mass + other.mass);
        }

        velocity = normalVelocity + tangentialVelocity;
    }

    private void OnDrawGizmos()
    {
        GUI.color = velocity.magnitude >= AkgPhysics.dragThreshold ? Color.red : Color.blue;
        Handles.Label(transform.position + Vector3.up * .2f, velocity.magnitude.ToString());
    }
}
