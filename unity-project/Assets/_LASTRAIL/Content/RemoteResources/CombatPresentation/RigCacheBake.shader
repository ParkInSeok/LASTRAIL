Shader "Hidden/LastRail/RigCacheBake" {
 Properties { _MainTex("Source",2D)="white"{} }
 SubShader { Pass { ZWrite Off ZTest Always Cull Off Blend One Zero
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
 sampler2D _MainTex;
 v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);return fixed4(c.rgb*c.a,c.a);}
 ENDCG
 } }
}
