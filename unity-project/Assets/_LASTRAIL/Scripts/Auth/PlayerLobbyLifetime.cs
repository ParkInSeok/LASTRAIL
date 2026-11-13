using System;
using UnityEngine;

namespace LastRail.Auth
{
    [ExecuteAlways]
    public sealed class PlayerLobbyLifetime : MonoBehaviour
    {
        public Action Deactivated;
        public Action Activated;
        private void OnEnable() => Activated?.Invoke();
        private void OnDisable() => Deactivated?.Invoke();
        private void OnDestroy() { Deactivated?.Invoke(); Deactivated = null; Activated = null; }
    }
}
