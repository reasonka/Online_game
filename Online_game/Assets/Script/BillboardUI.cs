using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // 如果你希望 UI 完全正对摄像机（包括上下），用这段：
        // transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
        //                  cam.transform.rotation * Vector3.up);

        // 如果你只想让它水平转向摄像机（不会跟着上下点头），用这段更适合头顶UI：
        Vector3 toCam = cam.transform.position - transform.position;
        toCam.y = 0f; // 锁定Y轴，避免仰头/低头
        if (toCam.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-toCam);
            // 注意：如果 UI 反过来，就把 -toCam 换成 toCam 试一下
        }
    }
}

