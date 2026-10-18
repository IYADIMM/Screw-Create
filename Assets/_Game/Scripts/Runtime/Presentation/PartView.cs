using UnityEngine;

namespace ScrewAndCreate.Presentation
{
    public sealed class PartView : MonoBehaviour
    {
        public string Id = string.Empty;
        public Transform TrayTarget;
        public Transform SocketTarget;
        public Vector3 HomePosition;
        public Quaternion HomeRotation;
    }
}
