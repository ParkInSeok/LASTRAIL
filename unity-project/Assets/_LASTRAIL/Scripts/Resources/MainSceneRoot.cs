using System;
using UnityEngine;

namespace LastRail.Resources
{
    public sealed class MainSceneRoot : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        public Transform ContentRoot => contentRoot != null ? contentRoot : throw new InvalidOperationException("Main content mount is missing.");
        private void Awake() { if (contentRoot != null) contentRoot.gameObject.SetActive(false); }
        public void Reveal() => ContentRoot.gameObject.SetActive(true);
        public void Hide() { if (contentRoot != null) contentRoot.gameObject.SetActive(false); }
    }
}
