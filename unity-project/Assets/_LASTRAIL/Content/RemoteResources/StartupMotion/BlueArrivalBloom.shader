Shader "LastRail/UI/BlueArrivalBloom" {
 Properties { _Progress("Progress",Range(0,1))=0 _Reveal("Reveal",Range(0,1))=0 _Reduced("Reduced",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 float _Progress,_Reveal,_Reduced;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {
 float2 q=(i.uv-.5)*float2(1,1.77778);
 float d=length(q);
 float p=saturate(_Progress);
 // Overlapping optical falloffs, without a visible circular wipe boundary.
 float radius=lerp(.025,1.25,p*p);
 float core=exp(-d*d/(radius*radius*.42));
 float halo=exp(-d*d/(radius*radius*2.8));
 float streak=exp(-abs(q.y)/(radius*.035+.004))*exp(-abs(q.x)/(radius*1.8+.02));
 float energy=saturate(core*1.1+halo*.38+streak*.12);
 float full=smoothstep(.55,1,p);
 float a=lerp(saturate(energy*p*1.6),1,full);
 // A near-white exposure peak; blue is retained in the falloff, not a flat cyan fill.
 float white=saturate(core*.85+full*.94+streak*.15);
 float3 c=lerp(float3(.14,.29,.46),float3(.96,.985,1),white);
 c+=float3(.015,.045,.065)*halo*(1-white);
 // The center clears first, leaving a restrained peripheral blue afterglow.
 float clear=smoothstep(d*.16,d*.16+.78,_Reveal);
 a*=1-clear;
 if(_Reduced>.5) { a=p*(1-_Reveal); c=float3(.87,.92,.96); }
 float grain=(frac(sin(dot(i.vertex.xy,float2(12.9898,78.233)))*43758.5453)-.5)/255;
 return fixed4(saturate(c+grain),a*i.color.a);
 }
 ENDCG }
 }}