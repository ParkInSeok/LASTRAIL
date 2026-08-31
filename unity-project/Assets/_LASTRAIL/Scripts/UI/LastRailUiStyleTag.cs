#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace LastRail.UI
{
    public sealed class LastRailUiStyleTag : MonoBehaviour
    {
        [SerializeField] private string componentKey;
        [SerializeField] private string componentRole;
        [SerializeField] private string styleVariant;

        public string ComponentKey => componentKey ?? string.Empty;
        public string ComponentRole => componentRole ?? string.Empty;
        public string StyleVariant => styleVariant ?? string.Empty;
    }
}
#endif