using System;
namespace LastRail.Battle {
public enum CombatPresentationMode { FirstUseFull, Full, Simplified }
// Pure decoration state. Never writes gameplay state or schedules an impact.
public sealed class CombatPresentationPolicy {
 readonly Func<bool> read; readonly Func<bool> write;
 bool consumed, uncertain;
 public CombatPresentationMode Mode {get;set;} = CombatPresentationMode.FirstUseFull;
 public string Token {get;private set;}
 public bool Full {get;private set;}
 public bool Active {get;private set;}
 public double Started {get;private set;}
 public double Duration {get;private set;}
 public CombatPresentationPolicy(Func<bool> readMarker,Func<bool> writeMarker,bool restoreUncertain=false){
  read=readMarker??throw new ArgumentNullException(nameof(readMarker));write=writeMarker??throw new ArgumentNullException(nameof(writeMarker));
  uncertain=restoreUncertain;try{consumed=read();}catch{uncertain=true;}
 }
 public bool Begin(string token,double now,double available,bool warning){
  if(string.IsNullOrEmpty(token)||token==Token||available<=0)return false;
  bool first=!consumed&&!uncertain;consumed=true;
  try{if(!write())uncertain=true;}catch{uncertain=true;}
  Token=token;Started=now;Full=!warning&&!uncertain&&(Mode==CombatPresentationMode.Full||(Mode==CombatPresentationMode.FirstUseFull&&first));
  Duration=Math.Min(available,Full?.40:.12);Active=true;return true;
 }
 public void Update(double now){if(now>=Started+Duration)Active=false;}
 public void Skip(){Active=false;}
 public bool TryReserveFullCinematic(){
  if(uncertain||Mode==CombatPresentationMode.Simplified||(Mode==CombatPresentationMode.FirstUseFull&&consumed))return false;
  try{if(!write()){uncertain=true;return false;}}catch{uncertain=true;return false;}
  consumed=true;return true;
 }
}
}
