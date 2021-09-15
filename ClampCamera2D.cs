//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

namespace ClampCamera {
    public class ClampCamera2D : MonoBehaviour {
        private enum FixAxisType { None, fixHorizontal, fixVertical/*, CameraUp*/ } //ログをとれば、限定しなくても補正できるのでは？？boolで済むかも
        [Header("--- GameObject ---")]
        [SerializeField] Camera cam;
        [Tooltip("use this transform when null.")]
        [SerializeField] Transform target = null;
        private Transform Target { get { return (target != null) ? target : this.transform; } }
        [Header("--- Option ---")]
        [Tooltip("call ClampPosition() manually when false.")]
        //[SerializeField] bool fixOnStart = true;
        [SerializeField] bool clampOnUpdate = true;
        [SerializeField] bool UpdatePlane = false;
        //targetからverticalとhorizontalを取得する
        [Header("--- Plane Setting ---")]
        [SerializeField] FixAxisType fix = FixAxisType.None;
        [SerializeField] Vector3 horizontal = Vector3.right;
        [SerializeField] Vector3 vertical = Vector3.up;
        public Vector3 Horizontal { get { return (horizontal != Vector3.zero) ? horizontal.normalized : Vector3.right; } }
        public Vector3 Vertical { get { return (vertical != Vector3.zero) ? vertical.normalized : Vector3.up; } }
        [Tooltip("use center / calculate from this position.")]
        [SerializeField] bool lockCenter = false;
        [SerializeField] Vector3 center = Vector3.zero;
        private Vector3 Center { get { return (lockCenter) ? center : Target.position; } }
        [Header("--- Margin ---")]
        [SerializeField, Range(0, 2)] float rightMargin = 0f;
        [SerializeField, Range(0, 2)] float leftMargin = 0f;
        [SerializeField, Range(0, 2)] float upMargin = 0f;
        [SerializeField, Range(0, 2)] float downMargin = 0f;
        [Header("--- Gizmos ---")]
        [SerializeField] bool drawCameraRange = false;
        [SerializeField] bool drawPlaneAxis = false; //サイズ調整
        [SerializeField, Min(0)] float drawAxisSize = 1f;
        [SerializeField] bool drawMoveAxis = false;

        private SquarePlane cameraRangePlane = new SquarePlane();
        private SquarePlane movingAreaPlane = new SquarePlane();

        [System.Serializable]
        public struct SquarePlane {
            [SerializeField] public Vector3 rightUp;
            [SerializeField] public Vector3 leftUp;
            [SerializeField] public Vector3 rightDown;
            [SerializeField] public Vector3 leftDown;
            public Vector3 Normal { get { return Vector3.Cross(rightDown - leftDown, rightUp - rightDown); } }
            public SquarePlane(Vector3 rightUp, Vector3 leftUp, Vector3 rightDown, Vector3 leftDown) {
                this.rightUp = rightUp;
                this.leftUp = leftUp;
                this.rightDown = rightDown;
                this.leftDown = leftDown;
            }
            public void Set(Vector3 rightUp, Vector3 leftUp, Vector3 rightDown, Vector3 leftDown) {
                this.rightUp = rightUp;
                this.leftUp = leftUp;
                this.rightDown = rightDown;
                this.leftDown = leftDown;
            }
            public Matrix4x4 GetPlaneMatrix(Vector3 upwards) {
                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.LookRotation(Normal, upwards);
                return Matrix4x4.TRS(pos, rot, Vector3.one);
            }
            public bool PlaneIsInversion(Vector3 forwardVector) {
                Matrix4x4 localToWorld = GetPlaneMatrix(Vector3.up);
                Vector3 pos = (Vector3)(localToWorld * Vector3.zero) + forwardVector;
                Vector3 localPos = localToWorld.inverse * pos;
                return localPos.z < 0f;
            }
            public Vector3 ClampPosOnPlane(Vector3 pos, Vector3 upwards, bool lockLocalZ = true) {
                Matrix4x4 plane = GetPlaneMatrix(upwards);
                Vector2 localRightUp = GetLocalPosOnPlane(rightUp);
                Vector2 localLeftUp = GetLocalPosOnPlane(leftUp);
                Vector2 localRightDown = GetLocalPosOnPlane(rightDown);
                Vector2 localLeftDown = GetLocalPosOnPlane(leftDown);
                Vector3 localPos = GetLocalPosOnPlane(pos);
                Vector2 localPos2 = localPos;
                float yMax = Mathf.Lerp(localLeftUp.y, localRightUp.y, (localPos2 - localLeftUp).x / (localRightUp - localLeftUp).x); //省略できそう
                float yMin = Mathf.Lerp(localLeftDown.y, localRightDown.y, (localPos2 - localLeftDown).x / (localRightDown - localLeftDown).x);
                float xMax = Mathf.Lerp(localRightDown.x, localRightUp.x, (localPos2 - localRightDown).y / (localRightUp - localRightDown).y);
                float xMin = Mathf.Lerp(localLeftDown.x, localLeftUp.x, (localPos2 - localLeftDown).y / (localLeftUp - localLeftDown).y);
                if (xMax >= xMin) localPos.x = Mathf.Clamp(localPos.x, xMin, xMax);
                else localPos.x = Mathf.Clamp(localPos.x, xMax, xMin);
                localPos.y = Mathf.Clamp(localPos.y, yMin, yMax);
                if (lockLocalZ) localPos.z = 0f;
                return GetPlaneMatrix(upwards).MultiplyPoint3x4(localPos);
                Vector3 GetLocalPosOnPlane(Vector3 _pos) {
                    return plane.inverse.MultiplyPoint(_pos);
                }
            }
            public void DrawPlane() {
                Gizmos.DrawLine(rightUp, leftUp);
                Gizmos.DrawLine(leftUp, leftDown);
                Gizmos.DrawLine(leftDown, rightDown);
                Gizmos.DrawLine(rightDown, rightUp);
            }
        }
        
        // Start is called before the first frame update
        void Start() {
            FixAxisLength();
            UpdateEdgePoint();
        }

        // Update is called once per frame
        void Update() {
            if (clampOnUpdate) {
                Target.position = ClampPosition(Target.position);
            }
            if (UpdatePlane) UpdateEdgePoint();
        }

        void OnValidate() {
            UpdateEdgePoint();
            FixAxisVector();
        }

        private void FixAxisLength() {
            if (horizontal.sqrMagnitude <= 0f) horizontal = Vector3.right;
            if (vertical.sqrMagnitude <= 0f) vertical = Vector3.up;
            horizontal.Normalize();
            vertical.Normalize();
        }
        private void FixAxisVector() {
            if (cam == null) return;
            Matrix4x4 plane = movingAreaPlane.GetPlaneMatrix(cam.transform.up);
            //Vector3 planeUp = plane * Vector3.up;
            switch (fix) {
                case FixAxisType.None:
                    break;
                case FixAxisType.fixHorizontal:
                    horizontal = Vector3.Cross(plane * Vector3.back, vertical); //変化がない時に代入しない
                    break;
                case FixAxisType.fixVertical:
                    vertical = Vector3.Cross(horizontal, plane * Vector3.back);
                    break;
                //case FixAxisType.CameraUp:
                //    break;
            }
        }

        public Vector3 ClampPosition(Vector3 pos) {
            return movingAreaPlane.ClampPosOnPlane(pos, cam.transform.up, lockCenter);
        }

        public void ClampPosition(ref Vector3 pos) {
            pos = ClampPosition(pos);
        }

        public void UpdateEdgePoint() {
            if (cam == null) return;
            Vector3 camPos = cam.transform.position;
            Quaternion camRot = cam.transform.rotation;
            float distance = 1f; //何でもいい
            float frustumHeight = 2.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * cam.aspect;
            cameraRangePlane.Set(
                GetPointInArea(Vector2.right + Vector2.up),
                GetPointInArea(Vector2.left + Vector2.up),
                GetPointInArea(Vector2.right + Vector2.down),
                GetPointInArea(Vector2.left + Vector2.down)
                );
            movingAreaPlane.Set(
                GetPointInArea(Vector2.right + Vector2.up + new Vector2(-rightMargin, -upMargin)),
                GetPointInArea(Vector2.left + Vector2.up + new Vector2(leftMargin, -upMargin)),
                GetPointInArea(Vector2.right + Vector2.down + new Vector2(-rightMargin, downMargin)),
                GetPointInArea(Vector2.left + Vector2.down + new Vector2(leftMargin, downMargin))
                );
            Vector3 GetPointInArea(Vector2 normalizedPos) {
                Vector3 vectorFromCam = GetVectorFromCam(normalizedPos);
                return GetIntersectionOfPlaneAndLine(Center, Vector3.Cross(Horizontal, Vertical), camPos, vectorFromCam);
            }
            Vector3 GetVectorFromCam(Vector2 pos) {
                return camRot * new Vector3(pos.x * frustumWidth / 2, pos.y * frustumHeight / 2, distance);
            }
        }

        private static Vector3 GetIntersectionOfPlaneAndLine(Vector3 planeCenter, Vector3 planeNormalVector, Vector3 lineCenter, Vector3 lineVector) {
            /* line : (x, y, z) = (x0, y0, z0) + (l, m, n)t
             * plane : p(x - x'0) + q(y - y'0) + r(z - z'0) = 0
             * t = (p(x'0 - x0) + q(y'0 - y0) + r(z'0 - z0)) / (pl + qm + rn)
             */
            float t = Vector3.Dot(planeNormalVector, planeCenter - lineCenter) / Vector3.Dot(planeNormalVector, lineVector);
            return lineCenter + lineVector * t;
        }

        void OnDrawGizmos() {
            UpdateEdgePoint();
            if (!PlaneIsInversion()) Gizmos.color = Color.green;
            else Gizmos.color = Color.red;
            if (drawCameraRange) cameraRangePlane.DrawPlane();
            movingAreaPlane.DrawPlane();
            Matrix4x4 plane = movingAreaPlane.GetPlaneMatrix(cam.transform.up);
            float size = drawAxisSize;
            if (drawPlaneAxis) {
                DrawAxisOnPlane(Vector3.right, Color.red);
                DrawAxisOnPlane(Vector3.forward, Color.blue);
                DrawAxisOnPlane(Vector3.up, Color.green);
            }
            if (drawMoveAxis) {
                DrawAxisOnPlane(Horizontal * size / 2 + plane.MultiplyPoint3x4(Vector3.zero), Color.yellow, false);
                DrawAxisOnPlane(Vertical * size / 2 + plane.MultiplyPoint3x4(Vector4.zero), Color.white, false);
            }
            void DrawAxisOnPlane(Vector3 pos1, Color color, bool local = true) {
                Gizmos.color = color;
                Vector3 pos = local ? plane.MultiplyPoint3x4(pos1 * size) : pos1;
                Gizmos.DrawLine(plane.MultiplyPoint3x4(Vector3.zero) + Center, pos + Center);
            }
            bool PlaneIsInversion() => movingAreaPlane.PlaneIsInversion(cam.transform.forward);
        }
    }
}
