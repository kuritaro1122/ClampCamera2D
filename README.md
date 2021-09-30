# ClampCamera2D
オブジェクトの座標を自然に制限する。

【HyperNovaで使用している様子】
![photo](https://user-images.githubusercontent.com/62167170/135420826-ba6d20b7-b309-40db-996b-9e0dcee97285.png)

# 【簡易リファレンス】
Inspector 変数
・

# Public 変数
* Vector3 Horizontal
* Vector3 Vertical

# Public 関数
* Vector3 LocalHorizontal(Vector3 pos, bool local)
* Vector3 LocalVertical(Vector3 pos, bool local)
* Vector3 TransformPosition(Vector3 localPos)
* Vector3 InverseTransformPosition(Vector3 pos)
* void UpdateEdgePoint()
