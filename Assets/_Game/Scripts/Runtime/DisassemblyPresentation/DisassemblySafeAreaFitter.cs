using UnityEngine;

namespace ScrewAndCreate.DisassemblyPresentation
{
    public sealed class DisassemblySafeAreaFitter : MonoBehaviour
    {
        Rect _last;

        void OnEnable() => Apply();

        void Update()
        {
            if (_last != Screen.safeArea) Apply();
        }

        public void Apply()
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null || Screen.width < 1 || Screen.height < 1) return;
            _last = Screen.safeArea;
            rect.anchorMin = _last.position / new Vector2(Screen.width, Screen.height);
            rect.anchorMax = (_last.position + _last.size) / new Vector2(Screen.width, Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
