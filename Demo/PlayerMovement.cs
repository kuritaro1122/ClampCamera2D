using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using From3DTo2D.ClampCamera;

public class PlayerMovement : MonoBehaviour {
    [Header("--- GameObject ---")]
    [SerializeField] ClampCamera2D cc2d;
    [Header("--- Movement ---")]
    [SerializeField] float speed = 10f;

    void Update() {
        Movement();
    }

    private void Movement() {
        Vector2 stick;
        stick.x = Input.GetAxis("Horizontal");
        stick.y = Input.GetAxis("Vertical");
        Vector3 pos = this.transform.position;
        Vector3 velocity = speed * (cc2d.LocalHorizontal(pos) * stick.x + cc2d.LocalVertical(pos) * stick.y);
        pos += velocity * Time.deltaTime;
        pos = cc2d.ClampPosition(pos);
        this.transform.position = pos;
    }
}
