using UnityEngine;
namespace LastRail.Presentation.Audio
{
    // Captured from the existing event/disclosure owner; no transform lookup or generation guess.
    public sealed class CombatAudioPosition
    {
        public string Owner {get;} public long Generation {get;} public Vector3? Point {get;}
        public bool Audible {get;} public bool DirectionDisclosed {get;} public float? Distance {get;}
        public CombatAudioPosition(string owner,long generation,Vector3? point,bool audible,bool directionDisclosed,float? distance)
        {Owner=owner;Generation=generation;Point=point;Audible=audible;DirectionDisclosed=directionDisclosed;Distance=distance;}
    }
}
