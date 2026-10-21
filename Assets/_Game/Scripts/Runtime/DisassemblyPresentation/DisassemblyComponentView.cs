using UnityEngine;

namespace ScrewAndCreate.DisassemblyPresentation
{
    public sealed class DisassemblyComponentView : MonoBehaviour
    {
        public string Id = string.Empty;
        public Transform ReleasedPose;
        [HideInInspector] public Vector3 AttachedPosition;
        [HideInInspector] public Quaternion AttachedRotation;
    }
}
