using UnityEngine;

/// <summary>
/// 环绕相机：左键拖动旋转、右键拖动平移、滚轮缩放。
/// 挂在预览场景的相机上，Play 模式下自由观察模型。
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 4.8f;
    public float minDistance = 1.5f;
    public float maxDistance = 12f;
    public float yaw = 0f;
    public float pitch = 12f;
    public float rotateSpeed = 220f;
    public float zoomSpeed = 1.2f;
    public float panSpeed = 0.004f;
    public Vector3 pivotOffset = new Vector3(0, 1.0f, 0);

    Vector3 _pivot;

    void Start()
    {
        if (target == null)
        {
            var smr = FindObjectOfType<SkinnedMeshRenderer>();
            if (smr != null) target = smr.transform;
        }
        if (target != null) _pivot = target.position + pivotOffset;
        else _pivot = pivotOffset;
        Apply();
    }

    void Update()
    {
        if (target != null) _pivot = target.position + pivotOffset;

        // 左键旋转
        if (Input.GetMouseButton(0))
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -60f, 80f);
        }
        // 右键平移
        if (Input.GetMouseButton(1))
        {
            var right = transform.right * (-Input.GetAxis("Mouse X") * panSpeed * distance * 10f);
            var up = transform.up * (-Input.GetAxis("Mouse Y") * panSpeed * distance * 10f);
            pivotOffset += right + up;
            _pivot = (target != null ? target.position : Vector3.zero) + pivotOffset;
        }
        // 滚轮缩放
        float sc = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(sc) > 0.001f)
        {
            distance = Mathf.Clamp(distance - sc * zoomSpeed, minDistance, maxDistance);
        }

        // F 键复位
        if (Input.GetKeyDown(KeyCode.F))
        {
            yaw = 0f; pitch = 12f; distance = 4.8f;
            pivotOffset = new Vector3(0, 1.0f, 0);
        }

        Apply();
    }

    void Apply()
    {
        var rot = Quaternion.Euler(pitch, yaw, 0);
        transform.position = _pivot - rot * Vector3.forward * distance;
        transform.rotation = rot;
    }
}
