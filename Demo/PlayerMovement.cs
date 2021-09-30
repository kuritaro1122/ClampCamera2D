using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using From3DTo2D.ClampCamera;

public class PlayerMovement : MonoBehaviour {
    [Header("--- GameObject ---")]
    [SerializeField] Rigidbody rb;
    [SerializeField] ClampCamera2D cc2d;
    [Header("--- Movement ---")]
    [SerializeField] float speed = 20f;

    // Update is called once per frame
    void Update() {
        Vector2 stick;
        if (Time.timeScale > 0f) Movement(stick);
    }

    private void Movement(Vector2 stick) {
        Vector3 pos = rb.position;
        Vector3 velocity = speed * (cc2d.LocalHorizontal(pos) * stick.x + cc2d.LocalVertical(pos) * stick.y);
        Vector3 localPos_ = cc2d.InverseTransformPosition(pos + velocity * Time.deltaTime);
        localPos_.x = Mathf.Clamp01(localPos_.x);
        localPos_.y = Mathf.Clamp01(localPos_.y);
        Vector3 pos_ = cc2d.TransformPosition(localPos_);
        rb.velocity = (pos_ - pos) / Time.deltaTime;
    }
}
