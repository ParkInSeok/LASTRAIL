using System;
using UnityEngine;
namespace LastRail.Presentation.Battle
{
    public static class UltimateFrameLayout
    {
        // Pure placement: no clock/player/DAILY command. Authored normalized protected region is mandatory.
        // Outside decoration may extend behind insets; whole-image protection gives letterboxing.
        public static Rect Fit(Vector2 image,Rect protectedRegion,Rect safe)
        {
            if(!float.IsFinite(image.x)||!float.IsFinite(image.y)||image.x<=0||image.y<=0||
               !Finite(protectedRegion)||protectedRegion.width<=0||protectedRegion.height<=0||protectedRegion.xMin<0||protectedRegion.yMin<0||protectedRegion.xMax>1||protectedRegion.yMax>1||
               !Finite(safe)||safe.width<=0||safe.height<=0)throw new ArgumentException("Valid source, authored normalized protection and actual safe area required.");
            float scale=Mathf.Min(safe.width/(image.x*protectedRegion.width),safe.height/(image.y*protectedRegion.height));
            var size=image*scale;
            return new Rect(safe.center-Vector2.Scale(protectedRegion.center,size),size);
        }
        static bool Finite(Rect r)=>float.IsFinite(r.x)&&float.IsFinite(r.y)&&float.IsFinite(r.width)&&float.IsFinite(r.height);
    }
}
