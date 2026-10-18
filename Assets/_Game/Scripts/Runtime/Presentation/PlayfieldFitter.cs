using UnityEngine;

namespace ScrewAndCreate.Presentation
{
    /// <summary>Fits essential world-space interaction bounds into a reserved viewport region.</summary>
    public sealed class PlayfieldFitter : MonoBehaviour
    {
        public Camera TargetCamera;
        public Rect ViewportRegion = new Rect(0.06f, 0.28f, 0.88f, 0.54f);
        public float Padding = 1.06f;

        public void Fit(Bounds worldBounds, float aspect)
        {
            var camera = TargetCamera != null ? TargetCamera : GetComponent<Camera>();
            if (camera == null || !camera.orthographic || aspect <= 0f || ViewportRegion.width <= 0f || ViewportRegion.height <= 0f) return;

            var heightSize = worldBounds.size.y / (2f * ViewportRegion.height);
            var widthSize = worldBounds.size.x / (2f * aspect * ViewportRegion.width);
            camera.orthographicSize = Mathf.Max(0.1f, Mathf.Max(heightSize, widthSize) * Mathf.Max(1f, Padding));

            var viewportCenter = ViewportRegion.center;
            var worldHalfHeight = camera.orthographicSize;
            var worldHalfWidth = worldHalfHeight * aspect;
            var position = camera.transform.position;
            position.x = worldBounds.center.x - (viewportCenter.x - 0.5f) * 2f * worldHalfWidth;
            position.y = worldBounds.center.y - (viewportCenter.y - 0.5f) * 2f * worldHalfHeight;
            camera.transform.position = position;
        }
    }
}
