//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

namespace From3DTo2D {
    [System.Serializable]
    public struct SquarePlane { //頂点が入れ替わった時に使えなくなる。
        [SerializeField] Vector3 rightUp;
        [SerializeField] Vector3 leftUp;
        [SerializeField] Vector3 rightDown;
        [SerializeField] Vector3 leftDown;
        public Vector3 Normal { get { return Vector3.Cross(rightDown - leftDown, rightUp - rightDown); } }
        public Vector3 Upwards { get { return ((rightUp + leftUp) - (rightDown + leftDown)).normalized; } }
        private Matrix4x4 homographyMatrix;
        private Matrix4x4 inverseHomographyMatrix;
        public SquarePlane(Vector3 rightUp, Vector3 leftUp, Vector3 rightDown, Vector3 leftDown) {
            this.rightUp = rightUp;
            this.leftUp = leftUp;
            this.rightDown = rightDown;
            this.leftDown = leftDown;
            homographyMatrix = Matrix4x4.identity;
            inverseHomographyMatrix = Matrix4x4.identity;
            UpdatePlane();
        }
        public void Set(Vector3 rightUp, Vector3 leftUp, Vector3 rightDown, Vector3 leftDown) {
            this.rightUp = rightUp;
            this.leftUp = leftUp;
            this.rightDown = rightDown;
            this.leftDown = leftDown;
            UpdatePlane();
        }

        public void UpdatePlane() {
            homographyMatrix = GetHomographyMatrix();
            inverseHomographyMatrix = GetInverseHomographyMatrix();
        }

        public Matrix4x4 GetPlaneMatrix(Vector3? upwards = null) {
            Vector3 pos = Vector3.zero;
            return Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one) * Matrix4x4.LookAt(Vector3.zero, Normal, upwards ?? Upwards);
        }
        public bool PlaneIsInversion(Vector3 forwardVector) {
            Matrix4x4 localToWorld = GetPlaneMatrix();
            Vector3 pos = (Vector3)(localToWorld * Vector3.zero) + forwardVector;
            Vector3 localPos = localToWorld.inverse * pos;
            return localPos.z < 0f;
        }

        private Matrix4x4 GetHomographyMatrix() {
            Matrix4x4 planeInverse = GetPlaneMatrix().inverse;
            Vector2 p11 = GetLocalPosOnPlane(rightUp);
            Vector2 p01 = GetLocalPosOnPlane(leftUp);
            Vector2 p10 = GetLocalPosOnPlane(rightDown);
            Vector2 p00 = GetLocalPosOnPlane(leftDown);
            Vector2 a = p10 - p11;
            Vector2 b = p01 - p11;
            Vector2 c = p00 - p01 - p10 + p11;
            Matrix4x4 homographyMatrix = Matrix4x4.identity;
            homographyMatrix.m02 = p00.x;
            homographyMatrix.m12 = p00.y;
            homographyMatrix.m21 = (c.x * a.y - a.x * c.y) / (b.x * a.y - a.x * b.y);
            homographyMatrix.m20 = (c.x * b.y - b.x * c.y) / (a.x * b.y - b.x * a.y);
            homographyMatrix.m00 = p10.x - p00.x + homographyMatrix.m20 * p10.x;
            homographyMatrix.m01 = p01.x - p00.x + homographyMatrix.m21 * p01.x;
            homographyMatrix.m10 = p10.y - p00.y + homographyMatrix.m20 * p10.y;
            homographyMatrix.m11 = p01.y - p00.y + homographyMatrix.m21 * p01.y;
            homographyMatrix.m22 = 1;
            return homographyMatrix;
            Vector3 GetLocalPosOnPlane(Vector3 _pos) {
                return planeInverse.MultiplyPoint(_pos);
            }
        }
        private Matrix4x4 GetInverseHomographyMatrix() {
            Matrix4x4 i = GetHomographyMatrix();
            float a = 1f / i.determinant;
            Matrix4x4 o = Matrix4x4.identity;
            o.m00 = (i.m11 * i.m22 - i.m12 * i.m21) / a;
            o.m01 = (-i.m01 * i.m22 + i.m02 * i.m21) / a;
            o.m02 = (i.m01 * i.m12 - i.m02 * i.m11) / a;
            o.m10 = (-i.m10 * i.m22 + i.m12 * i.m20) / a;
            o.m11 = (i.m00 * i.m22 - i.m02 * i.m20) / a;
            o.m12 = (-i.m00 * i.m12 + i.m02 * i.m10) / a;
            o.m20 = (i.m10 * i.m21 - i.m11 * i.m20) / a;
            o.m21 = (-i.m00 * i.m21 + i.m01 * i.m20) / a;
            o.m22 = (i.m00 * i.m11 - i.m01 * i.m10) / a;
            return o;
        }

        public Vector3 GetHomographyPosition(Vector3 localPos) { //ok
            Vector3 _pos = new Vector3(localPos.x, localPos.y, 1);
            Matrix4x4 mat = homographyMatrix;
            float s = Vector3.Dot(_pos, mat.GetRow(2));
            float x = Vector3.Dot(_pos, mat.GetRow(0)) / s;
            float y = Vector3.Dot(_pos, mat.GetRow(1)) / s;
            return GetPlaneMatrix().MultiplyPoint3x4(new Vector3(x, y, localPos.z));
        }
        public Vector3 GetInverseHomographyPosition(Vector3 pos) {
            Vector3 localPos = GetPlaneMatrix().inverse.MultiplyPoint3x4(pos);

            Vector3 _localPos = new Vector3(localPos.x, localPos.y, 1);
            Matrix4x4 mat = inverseHomographyMatrix;
            float s = Vector3.Dot(_localPos, mat.GetRow(2));
            float x = Vector3.Dot(_localPos, mat.GetRow(0)) / s;
            float y = Vector3.Dot(_localPos, mat.GetRow(1)) / s;
            return new Vector3(x, y, 0);
        }

        public Vector3 ClampPosOnPlane(Vector3 pos, bool lockLocalZ = true) {
            Matrix4x4 plane = GetPlaneMatrix();
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
            return GetPlaneMatrix().MultiplyPoint3x4(localPos);
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
        //public void DrawXYZAxis(){}
    }
}