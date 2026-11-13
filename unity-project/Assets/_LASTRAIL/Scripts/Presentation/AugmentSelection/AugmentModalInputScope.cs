using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastRail.Presentation.AugmentSelection
{
    // Restore borrowed groups and remove only this scope's temporary groups.
    internal sealed class AugmentModalInputScope : IDisposable
    {
        readonly List<(CanvasGroup Group, bool Owned, bool Interactable, bool Raycasts)> groups = new List<(CanvasGroup,bool,bool,bool)>();
        EventSystem events;
        GameObject previous;
        public AugmentModalInputScope(Transform modal, Transform boundary, GameObject focus)
        {
            events = EventSystem.current;
            previous = events != null ? events.currentSelectedGameObject : null;
            for (var branch = modal; branch != null && branch != boundary; branch = branch.parent)
            {
                var parent = branch.parent;
                if (parent == null) break;
                foreach (Transform sibling in parent)
                {
                    if (sibling == branch) continue;
                    var group = sibling.GetComponent<CanvasGroup>();
                    bool owned = group == null;
                    if (owned) group = sibling.gameObject.AddComponent<CanvasGroup>();
                    groups.Add((group, owned, group.interactable, group.blocksRaycasts));
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }
            }
            if (events != null) events.SetSelectedGameObject(focus);
        }
        public void Dispose()
        {
            foreach (var saved in groups)
            {
                var group = saved.Group;
                if (group == null) continue;
                group.interactable = saved.Interactable;
                group.blocksRaycasts = saved.Raycasts;
                if (saved.Owned) {
                    // UI close/reopen can happen in one frame. Deferred removal
                    // would destroy a group borrowed by the newly opened scope.
                    // This owned component is removed synchronously, never assets.
                    UnityEngine.Object.DestroyImmediate(group);
                }
            }
            groups.Clear();
            if (events != null)
            {
                var selectable = previous != null ? previous.GetComponent<Selectable>() : null;
                events.SetSelectedGameObject(previous != null && previous.activeInHierarchy &&
                    (selectable == null || selectable.IsInteractable()) ? previous : null);
            }
            events = null;
            previous = null;
        }
    }
}
