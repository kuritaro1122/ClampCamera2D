//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

namespace ClampCamera {
    public class ClampCamera2D : MonoBehaviour {
        [Header("--- GameObject ---")]
        [SerializeField] Camera cam;
        [Header("--- Option ---")]
        [SerializeField] bool callUpdate = true;
        [Header("--- Position ---")]
        [SerializeField] Vector3 vertical = Vector3.right;
        [SerializeField] Vector3 horizontal = Vector3.up;
        //[SerializeField] bool useCenter = true;
        [SerializeField] Vector3 center = Vector3.zero;
        [Header("--- margin ---")]
        [SerializeField, Range(0, 1)] float rightMargin = 0f;
        [SerializeField, Range(0, 1)] float leftMargin = 0f;
        [SerializeField, Range(0, 1)] float upMargin = 0f;
        [SerializeField, Range(0, 1)] float downMargin = 0f;
        //[Header("--- Gizmos ---")]

        private Vector3 rightUp;
        private Vector3 leftUp;
        private Vector3 rightDown;
        private Vector3 leftDown;

        private Vector3 mRightUp;
        private Vector3 mLeftUp;
        private Vector3 mRightDown;
        private Vector3 mLeftDown;

        private Matrix4x4 MovingAreaMatrix {
            get {
                Matrix4x4 matrix = new Matrix4x4();
                Vector3 _center = (rightUp + leftUp + rightDown + leftDown) / 4;
                Quaternion _rot = Quaternion.LookRotation(Vector3.Cross(vertical, horizontal), horizontal);
                matrix.SetTRS(_center, _rot, Vector3.one);
                return matrix;
            }
        }

        // Start is called before the first frame update
        void Start() {
            vertical.Normalize();
            horizontal.Normalize();
            UpdateEdgePoint();
        }

        // Update is called once per frame
        void Update() {
            if (callUpdate) {
                this.transform.position = ClampPosition(this.transform.position);
            }
        }

        void OnValidate() {
            UpdateEdgePoint();
        }

        public Vector3 ClampPosition(Vector3 pos) {
            Vector2 localRightUp = GetLocalPosInArea(mRightUp);
            Vector2 localLeftUp = GetLocalPosInArea(mLeftUp);
            Vector2 localRightDown = GetLocalPosInArea(mRightDown);
            Vector2 localLeftDown = GetLocalPosInArea(mLeftDown);

            Vector2 localPos = GetLocalPosInArea(pos);

            float yMax = Mathf.Lerp(localLeftUp.y, localRightUp.y, (localPos - localLeftUp).x / (localRightUp - localLeftUp).x);
            float yMin = Mathf.Lerp(localLeftDown.y, localRightDown.y, (localPos - localLeftDown).x / (localRightDown - localLeftDown).x);
            float xMax = Mathf.Lerp(localRightDown.x, localRightUp.x, (localPos - localRightDown).y / (localRightUp - localRightDown).y);
            float xMin = Mathf.Lerp(localLeftDown.x, localLeftUp.x, (localPos - localLeftDown).y / (localLeftUp - localLeftDown).y);

            localPos.x = Mathf.Clamp(localPos.x, xMin, xMax);
            localPos.y = Mathf.Clamp(localPos.y, yMin, yMax);

            return MovingAreaMatrix * localPos;
        }
        /*public Vector3 ClampPosition(Vector3 pos, float _rightMargin, float _leftMargin, float _upMargin, float _downMargin) {
            Vector2 localRightUp = GetLocalPosInArea(rightUp);
            Vector2 localLeftUp = GetLocalPosInArea(leftUp);
            Vector2 localRightDown = GetLocalPosInArea(rightDown);
            Vector2 localLeftDown = GetLocalPosInArea(leftDown);

            Vector2 localPos = GetLocalPosInArea(pos);

            float yMax = Mathf.Lerp(localLeftUp.y, localRightUp.y, (localPos - localLeftUp).x / (localRightUp - localLeftUp).x);
            float yMin = Mathf.Lerp(localLeftDown.y, localRightDown.y, (localPos - localLeftDown).x / (localRightDown - localLeftDown).x);
            float xMax = Mathf.Lerp(localRightDown.x, localRightUp.x, (localPos - localRightDown).y / (localRightUp - localRightDown).y);
            float xMin = Mathf.Lerp(localLeftDown.x, localLeftUp.x, (localPos - localLeftDown).y / (localLeftUp - localLeftDown).y);

            localPos.x = Mathf.Clamp(localPos.x, xMin + _leftMargin, xMax - _rightMargin);
            localPos.y = Mathf.Clamp(localPos.y, yMin + _downMargin, yMax - _upMargin);

            return MovingAreaMatrix * localPos;
        }*/
        public void ClampPosition(ref Vector3 pos) {
            pos = ClampPosition(pos);
        }
        /*public void ClampPosition(ref Vector3 pos, float _rightMargin, float _leftMargin, float _upMargin, float _downMargin) {
            pos = ClampPosition(pos, _rightMargin, _leftMargin, _upMargin, _downMargin);
        }*/

        private Vector2 GetLocalPosInArea(Vector3 _pos) {
            return Matrix4x4.Inverse(MovingAreaMatrix) * _pos;
        }

        private void UpdateEdgePoint() {
            if (cam == null) return;
            Vector3 camPos = cam.transform.position;
            Quaternion camRot = cam.transform.rotation;

            float distance = 1f; //何でもいい
            float frustumHeight = 2.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * cam.aspect;

            rightUp = GetPointInArea(Vector2.right + Vector2.up);
            leftUp = GetPointInArea(Vector2.left + Vector2.up);
            rightDown = GetPointInArea(Vector2.right + Vector2.down);
            leftDown = GetPointInArea(Vector2.left + Vector2.down);

            mRightUp = GetPointInArea(Vector2.right + Vector2.up + new Vector2(-rightMargin, -upMargin));
            mLeftUp = GetPointInArea(Vector2.left + Vector2.up + new Vector2(leftMargin, -upMargin));
            mRightDown = GetPointInArea(Vector2.right + Vector2.down + new Vector2(-rightMargin, downMargin));
            mLeftDown = GetPointInArea(Vector2.left + Vector2.down + new Vector2(leftMargin, downMargin));

            Vector3 GetPointInArea(Vector2 normalizedPos) {
                Vector3 vectorFromCam = GetVectorFromCam(normalizedPos);
                return GetIntersectionOfPlaneAndLine(center, Vector3.Cross(vertical, horizontal), camPos, vectorFromCam);
            }
            Vector3 GetVectorFromCam(Vector2 pos) {
                return camRot * new Vector3(pos.x * frustumWidth / 2, pos.y * frustumHeight / 2, distance);
            }
        }

        private static Vector3 GetIntersectionOfPlaneAndLine(Vector3 planeCenter, Vector3 planeNormalVector, Vector3 lineCenter, Vector3 lineVector) {
            /* 直線 : (x, y, z) = (x0, y0, z0) + (l, m, n)t
             * 平面 : p(x - x0) + q(y - y0) + r(z - z0) = 0
             */
            float t = Vector3.Dot(planeNormalVector, (planeCenter - lineCenter)) / Vector3.Dot(planeNormalVector, lineVector);
            Vector3 pos = lineCenter + lineVector * t;
            return pos;
        }

        void OnDrawGizmos() {
            UpdateEdgePoint();
            Gizmos.color = Color.green;

            Gizmos.DrawLine(rightUp, leftUp);
            Gizmos.DrawLine(leftUp, leftDown);
            Gizmos.DrawLine(leftDown, rightDown);
            Gizmos.DrawLine(rightDown, rightUp);

            Gizmos.DrawLine(mRightUp, mLeftUp);
            Gizmos.DrawLine(mLeftUp, mLeftDown);
            Gizmos.DrawLine(mLeftDown, mRightDown);
            Gizmos.DrawLine(mRightDown, mRightUp);
        }
    }
}