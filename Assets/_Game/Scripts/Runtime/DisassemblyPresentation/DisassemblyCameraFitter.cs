using UnityEngine;

namespace ScrewAndCreate.DisassemblyPresentation
{
    public sealed class DisassemblyCameraFitter : MonoBehaviour
    {
        public Camera TargetCamera;
        public Rect ViewportRegion = new Rect(0.08f, 0.22f, 0.84f, 0.55f);
        public float Padding = 1.08f;

        public void Fit(Bounds bounds, float aspect)
        {
            var camera = TargetCamera != null ? TargetCamera : GetComponent<Camera>();
            if (camera == null || !camera.orthographic || aspect <= 0f) return;
            var height = bounds.size.y / (2f * ViewportRegion.height);
            var width = bounds.size.x / (2f * aspect * ViewportRegion.width);
            camera.orthographicSize = Mathf.Max(0.1f, Mathf.Max(height, width) * Mathf.Max(1f, Padding));
            var halfHeight = camera.orthographicSize;
            var halfWidth = halfHeight * aspect;
            var center = ViewportRegion.center;
            var position = camera.transform.position;
            position.x = bounds.center.x - (center.x - 0.5f) * 2f * halfWidth;
            position.y = bounds.center.y - (center.y - 0.5f) * 2f * halfHeight;
            camera.transform.position = position;
        }
    }
}
