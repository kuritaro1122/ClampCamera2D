using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Enemy;
using From3DTo2D.ClampCamera;
using UnityEngine.InputSystem;

namespace HyperNova.Player {
    [AddComponentMenu("HyperNova/Player/Player_Movement")]
    public class PlayerMovement : MonoBehaviour {
        [Header("--- GameObject ---")]
        [SerializeField] Rigidbody rb;
        [SerializeField] ClampCamera2D cc2d;
        [Header("--- Movement ---")]
        [SerializeField] float speed = 20f;
        [Header("--- Rotate ---")]
        [SerializeField] Transform rotateObj;
        [SerializeField] Vector3 rotateAxis = Vector3.right;
        [SerializeField] float smoothTime = 0.1f;
        [SerializeField] float maxAngle = 30f;
        private float angle = 0f;
        private float currentVelocity;

        // Update is called once per frame
        void Update() {
            Vector2 stick = InputControl.MoveValue;
            bool allowInput = PlayerStatus.AllowInput;
            if (allowInput && Time.timeScale > 0f) Movement(stick);
            Rotate(stick.y, allowInput);
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
        private void Rotate(float stick, bool allowInput = true) {
            float _stick = allowInput ? stick : 0f;
            float _angle = _stick * maxAngle;
            _angle = Mathf.Clamp(_angle, -maxAngle, maxAngle);
            angle = Mathf.SmoothDampAngle(angle, _angle, ref currentVelocity, smoothTime);
            rotateObj.transform.rotation = Quaternion.Euler(Vector3.right * angle);
        }

        /*private void OnDrawGizmos() {
            Gizmos.DrawWireSphere(cc2d.TransformPosition(p), 1f);
        }
        [SerializeField] Vector3 p;*/
    }
}