using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastRail.Presentation.Battle
{
    /// <summary>
    /// Attach to the existing skip surface. Controller supplies independent next/summary text
    /// in the approved safe area; this view does not build a screen or own a hold/game command.
    /// Controller explicitly rebinds after enable, scene replacement, or pool checkout.
    /// </summary>
    public sealed class UltimateSequenceView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        UltimatePresentationSequence sequence;
        TMP_Text nextActor, summaries;
        Func<bool> press;
        int pointer;
        RectTransform protectionSurface;
        Vector2 authoredSize;
        Rect protectedRegion,previousSafe;
        public void BindLayout(RectTransform surface,Vector2 sourceSize,Rect normalizedProtection)
        {
            UltimateFrameLayout.Fit(sourceSize,normalizedProtection,new Rect(0,0,1,1));
            protectionSurface=surface??throw new ArgumentNullException(nameof(surface));
            authoredSize=sourceSize;protectedRegion=normalizedProtection;
            InvalidateLayoutInput();
        }
        public void ApplyLayout(Rect safeInParentCoordinates)
        {
            if(protectionSurface==null)return;
            var fit=UltimateFrameLayout.Fit(authoredSize,protectedRegion,safeInParentCoordinates);
            if(previousSafe!=safeInParentCoordinates)InvalidateLayoutInput();
            previousSafe=safeInParentCoordinates;
            protectionSurface.anchorMin=protectionSurface.anchorMax=Vector2.zero;
            protectionSurface.pivot=Vector2.zero;
            protectionSurface.anchoredPosition=fit.position;
            protectionSurface.sizeDelta=fit.size;
        }
        public void InvalidateLayoutInput(){press=null;}
        public void Bind(UltimatePresentationSequence owner, TMP_Text nextActorLabel, TMP_Text summaryLabel)
        {
            Unbind();
            sequence = owner ?? throw new ArgumentNullException(nameof(owner));
            nextActor = nextActorLabel; summaries = summaryLabel;
            if (nextActor != null) nextActor.raycastTarget = false;
            if (summaries != null) summaries.raycastTarget = false;
            sequence.Changed += Render;
            Render();
        }
        void Render()
        {
            if (nextActor != null) nextActor.text = sequence?.NextActor == null ? "" : "다음: " + sequence.NextActor;
            if (summaries != null) summaries.text = sequence == null ? "" : string.Join(" · ", sequence.SummaryActors);
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (sequence == null || data == null || data.button != PointerEventData.InputButton.Left || press != null) return;
            pointer = data.pointerId; press = sequence.CaptureSkipInput();
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (data == null || data.pointerId != pointer || data.button != PointerEventData.InputButton.Left) return;
            var command = press; press = null; command?.Invoke();
        }
        public void OnPointerExit(PointerEventData data)
        {
            if (data != null && data.pointerId == pointer) press = null;
        }
        public void Unbind()
        {
            press = null;
            if (sequence != null) sequence.Changed -= Render;
            sequence = null; Render(); nextActor = null; summaries = null;
            protectionSurface=null;
        }
        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();
    }
}
