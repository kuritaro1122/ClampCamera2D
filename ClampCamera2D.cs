//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

namespace From3DTo2D.ClampCamera {
    public class ClampCamera2D : MonoBehaviour {
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
        [SerializeField] Vector3 horizontal = Vector3.right;
        [SerializeField] Vector3 vertical = Vector3.up;
        public Vector3 Horizontal { get { return (horizontal != Vector3.zero) ? horizontal.normalized : Vector3.right; } }
        public Vector3 Vertical { get { return (vertical != Vector3.zero) ? vertical.normalized : Vector3.up; } }
        private Vector3 _horizontal, _vertical;
        [SerializeField] bool axisOrthogonal = true;
        [SerializeField] bool naturalAxisOnCamera = false; //未実装
        [Tooltip("use center / calculate from this position.")]
        [SerializeField] Vector3 center = Vector3.zero;
        [SerializeField] bool lockCenter = false;
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
        [SerializeField] private SquarePlane movingAreaPlane = new SquarePlane();

        public Vector3 LocalHorizontal(Vector3 pos, bool local = false) { //面の歪みを考慮したもの
            Vector3 localPos = local ? pos : movingAreaPlane.GetInverseHomographyPosition(pos);
            //Vector3 localRight = localPos + Horizontal;
            Vector3 localRight = localPos + Vector3.right;
            Vector3 center = movingAreaPlane.GetHomographyPosition(localPos);
            Vector3 right = movingAreaPlane.GetHomographyPosition(localRight);
            return (right - center).normalized;
        }
        public Vector3 LocalVertical(Vector3 pos, bool local = false) { //面の歪みを考慮したもの
            Vector3 localPos = local ? pos : movingAreaPlane.GetInverseHomographyPosition(pos);
            //Vector3 localUp = localPos + Vertical;
            Vector3 localUp = localPos + Vector3.up;
            Vector3 center = movingAreaPlane.GetHomographyPosition(localPos);
            Vector3 up = movingAreaPlane.GetHomographyPosition(localUp);
            return (up - center).normalized;
        }
        public Vector3 LocalPosition(Vector3 pos) {
            return movingAreaPlane.GetHomographyPosition(pos);
        }

        // Start is called before the first frame update
        void Start() {
            FixAxisLength();
            UpdateEdgePoint();
            //movingAreaPlane.UpdatePlane();
            //Debug.Log(movingAreaPlane.GetInverseHomographyPosition(Vector3.zero));
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
            if (!axisOrthogonal) return;
            Matrix4x4 plane = movingAreaPlane.GetPlaneMatrix();
            Vector3 planeBack = plane * Vector3.back;
            if (_horizontal != horizontal) {
                vertical = Vector3.Cross(horizontal, planeBack);
            }
            if (_vertical != vertical) {
                horizontal = Vector3.Cross(planeBack, vertical);
            }
            _vertical = vertical;
            _horizontal = horizontal;
        }

        public Vector3 ClampPosition(Vector3 pos) {
            return movingAreaPlane.ClampPosOnPlane(pos, lockCenter);
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
            Vector3 GetPointInArea(Vector2 normalizedPos) { //こいつが悪い気がする。
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
            Matrix4x4 plane = movingAreaPlane.GetPlaneMatrix(Vector3.up);
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

            //Gizmos.DrawWireSphere(movingAreaPlane.GetHomographyPosition(new Vector3(px, py, pz)), 0.5f);
        }

        /*[SerializeField, Range(0, 1)] float px;
        [SerializeField, Range(0, 1)] float py;
        [SerializeField, Range(0, 1)] float pz;*/
        //[SerializeField] Vector2 p1;
        //[SerializeField] float s;
    }
}