using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using From3DTo2D.ClampCamera;

public class PlayerMovement_RigidBody : MonoBehaviour {
    [Header("--- GameObject ---")]
    [SerializeField] ClampCamera2D cc2d;
    [SerializeField] Rigidbody rb;
    [Header("--- Movement ---")]
    [SerializeField] float speed = 10f;

    void Start() {
        rb = this.GetComponent<Rigidbody>();
    }
    void Update() {
        Movement();
    }

    private void Movement() {
        Vector2 stick;
        stick.x = Input.GetAxis("Horizontal");
        stick.y = Input.GetAxis("Vertical");
        Vector3 pos = rb.position = cc2d.ClampPosition(rb.position);
        Vector3 velocity = speed * (cc2d.LocalHorizontal(pos) * stick.x + cc2d.LocalVertical(pos) * stick.y);
        float deltaTime = Time.deltaTime;
        Vector3 pos_ = cc2d.ClampPosition(pos + velocity * deltaTime);
        rb.velocity = (pos_ - pos) / deltaTime;
    }
}
