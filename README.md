# ClampCamera2D
オブジェクトの座標を自然に制限する。

# 【HyperNovaで使用している様子】
![photo](https://user-images.githubusercontent.com/62167170/135420826-ba6d20b7-b309-40db-996b-9e0dcee97285.png)

# 【簡易リファレンス】
**Public 変数**
* Vector3 Horizontal
* Vector3 Vertical

**Public 関数**
* Vector3 LocalHorizontal (Vector3 pos, bool local)
* Vector3 LocalVertical (Vector3 pos, bool local)
* Vector3 TransformPosition (Vector3 localPos)
* Vector3 InverseTransformPosition (Vector3 pos)
* Vector3 ClampPosition (Vector3 pos, bool lockLocalZ = true)
* void UpdateEdgePoint ()

**Inspector 変数**
* [GameObject]
  - Camera cam
  - Transform target
* [Plane Setting]
  - Vector3 horizontal
  - Vector3 vertical
  - bool axisOrthogonal
  - Vector3 center
  - bool lock Center
* [Margin]
  - rightMargin
  - leftMargin
  - upMargin
  - downMargin
* [Option]
  - bool clampOnUpdate
  - bool updatePlane
* [Gizmos]
  - bool drawCameraRange
  - bool drawPlaneAxis
  - float drawAxisSize


![inspector](https://user-images.githubusercontent.com/62167170/135424470-73991220-c987-4880-8ab4-7560d2b2d906.png)

# Example

```
using From3DTo2D.ClampCamera;

public class Sample : MonoBehaviour {
  public ClampCamera2D cc2d;
  public float speed = 10f;

  void Movement(Transform _transform, Vector2 stick) {
    Vector3 pos = _transform.position;
    pos += speed * (cc2d.LocalHorizontal(pos) * stick.x + cc2d.LocalVertical(pos) * stick.y) * Time.deltaTime;
    pos = cc2d.ClampPosition(position, false);
  }
}
```
