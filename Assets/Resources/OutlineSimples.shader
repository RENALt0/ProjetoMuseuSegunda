Shader "Custom/OutlineSimples"
{
    Properties
    {
        _Color     ("Cor do Contorno", Color) = (1,1,1,1)
        _Espessura ("Espessura (pixels)", Float) = 2.5
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float  _Espessura;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                // Projeta posição e normal no espaço de clip (tela)
                o.pos = UnityObjectToClipPos(v.vertex);

                // Normal transformada para clip-space
                float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);
                float4 clipNormal  = mul(UNITY_MATRIX_VP, float4(worldNormal, 0));

                // Desloca em pixels na tela — espessura uniforme independente da distância
                o.pos.xy += normalize(clipNormal.xy) * (_Espessura / _ScreenParams.xy) * o.pos.w * 2.0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
