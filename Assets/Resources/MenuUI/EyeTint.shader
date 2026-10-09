Shader "SecretsReborn/EyeTint"
{
    Properties { _MainTex ("Sprite", 2D) = "white" {} _Tint ("Iris", Color) = (0.4,0.8,0.4,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Tint;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 p=tex2D(_MainTex,i.uv);
                float bright=max(p.r,max(p.g,p.b)), dark=min(p.r,min(p.g,p.b));
                // Recolor chromatic iris pixels; keep white highlights and dark pupils.
                p.rgb=lerp(p.rgb,bright*_Tint.rgb,step(.05,bright-dark));
                return p*i.color;
            }
            ENDCG
        }
    }
}
