using System;
using UnityEngine;
namespace LastRail.Presentation.Audio
{
    // The host supplies the original battle camera projection and a measured, approved distance curve.
    // Projection returns signed viewport direction, not meters. There is no invented distance range.
    public sealed class CombatAudioSpatialPolicy
    {
        readonly float maximumPan;readonly Func<Vector3,float> project;readonly Func<float,float> distanceGain;
        public CombatAudioSpatialPolicy(float approvedMaximumPan,Func<Vector3,float> battleProjection,Func<float,float> approvedDistanceGain)
        {
            if(!float.IsFinite(approvedMaximumPan)||approvedMaximumPan<0||approvedMaximumPan>1)throw new ArgumentOutOfRangeException(nameof(approvedMaximumPan));
            maximumPan=approvedMaximumPan;project=battleProjection??throw new ArgumentNullException(nameof(battleProjection));distanceGain=approvedDistanceGain;
        }
        public Vector2? Evaluate(Vector3? originalPoint,bool audible,bool directionDisclosed,bool nonSpatialAllowed,float? originalDistance,int priority,bool mono)
        {
            if(priority<1||priority>4)throw new ArgumentOutOfRangeException(nameof(priority));
            if(!audible)return null;
            if(!directionDisclosed||!originalPoint.HasValue)return nonSpatialAllowed?new Vector2(0,1):(Vector2?)null;
            var point=originalPoint.Value;
            if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z))throw new ArgumentException("Finite original point required; zero is valid.");
            if(originalDistance.HasValue&&(!float.IsFinite(originalDistance.Value)||originalDistance.Value<0))throw new ArgumentException("Actual nonnegative distance required.");
            // Important warnings retain an unattenuated central identity. No new warning judgement.
            if(priority==1)return new Vector2(0,1);
            float gain=originalDistance.HasValue&&distanceGain!=null?distanceGain(originalDistance.Value):1;
            if(!float.IsFinite(gain)||gain<0||gain>1)throw new ArgumentException("Invalid approved distance gain.");
            float pan=0;
            if(!mono){float x=project(point);if(!float.IsFinite(x))throw new ArgumentException("Invalid battle camera projection.");pan=Mathf.Clamp(x,-1,1)*maximumPan;}
            return new Vector2(pan,gain);
        }
    }
}
