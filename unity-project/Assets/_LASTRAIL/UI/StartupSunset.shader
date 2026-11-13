Shader "LASTRAIL/StartupSunset"
{
    Properties {
        [PerRendererData] _MainTex("Exact supplied reference",2D)="white"{}
        _CleanPlate("Occluded ground",2D)="white"{}
        _JourneyTime("Journey seconds",Float)=0
        _Bloom("Sun transition",Range(0,1))=0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            sampler2D _MainTex, _CleanPlate;
            float _JourneyTime, _Bloom;
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
            float trainMask(float2 p) {
                float edge = p.y<.39 ? lerp(.56,.53,saturate((p.y-.35)/.04))
                    : p.y<.44 ? lerp(.53,.34,(p.y-.39)/.05)
                    : p.y<.48 ? lerp(.34,.15,(p.y-.44)/.04)
                    : lerp(.15,-.04,saturate((p.y-.48)/.05));
                return (1-smoothstep(edge-.012,edge+.008,p.x))*(1-smoothstep(.52,.54,p.y));
            }
            float deckMask(float2 p) {
                float top=lerp(.601,.575, saturate((abs(p.x-.5)-.415)*70));
                return smoothstep(top-.002,top+.002,p.y);
            }
            float3 world(float2 p) {
                float occlusion=max(trainMask(p),deckMask(p));
                return lerp(tex2D(_MainTex,float2(p.x,1-p.y)).rgb,
                    tex2D(_CleanPlate,float2(p.x,1-p.y)).rgb,occlusion);
            }
            fixed4 frag(Output i):SV_Target {
                float2 p=float2(i.uv.x,1-i.uv.y);
                float t=min(_JourneyTime,8);
                float2 vp=float2(.554,.348);
                float depth=max(0,p.y-vp.y);
                // Finite forward travel along the reference perspective; no loop or whole-image zoom.
                float2 sampleP=vp+(p-vp)/(1+t*.32*depth);
                float ground=smoothstep(.35,.405,p.y);
                float train=trainMask(p), deck=deckMask(p);
                float3 original=tex2D(_MainTex,i.uv).rgb;
                float3 c=lerp(original,world(sampleP),ground*(1-train)*(1-deck));
                float2 vibration=float2(sin(t*8.1)*.00045,sin(t*12.3)*.00055);
                float3 carriage=tex2D(_MainTex,saturate(i.uv+vibration)).rgb;
                c=lerp(c,carriage,max(train*ground,deck));
                // Restrained drifting dust in the existing trackside haze.
                float dust=0;
                for(int n=0;n<5;n++) {
                    float f=frac(t*.10+n*.217);
                    float2 center=float2(.23+n*.135 + .03*sin(t+n),.41+f*.17);
                    float2 d=(p-center)/float2(.07+f*.06,.012+f*.025);
                    dust+=exp(-dot(d,d)*2)*sin(f*3.14159);
                }
                c=lerp(c,float3(.80,.59,.34),saturate(dust*.09)*ground*(1-deck));
                // Light starts at the reference sun and consumes the entire screen at peak.
                float dist=length((p-float2(.755,.259))*float2(.5625,1));
                float halo=exp(-dist*dist/max(.001,.004+_Bloom*.8))*_Bloom;
                float white=saturate(halo*1.8+pow(_Bloom,5)*1.2);
                c=lerp(c,float3(1,.98,.91),saturate(halo*.65));
                c=lerp(c,float3(1,1,1),white);
                return fixed4(c,1)*i.color;
            }
            ENDCG
        }
    }
}
