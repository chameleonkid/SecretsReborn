Shader "SecretsReborn/UI/GrayscaleIcon"
{
    Properties { _MainTex ("Icon", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            Output vert(Input input)
            {
                Output output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            fixed4 frag(Output input) : SV_Target
            {
                fixed4 pixel = tex2D(_MainTex, input.uv);
                fixed gray = dot(pixel.rgb, fixed3(.299, .587, .114));
                return fixed4(gray, gray, gray, pixel.a) * input.color;
            }
            ENDCG
        }
    }
}
