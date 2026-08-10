// Spray-particle stamp shader for the Drawing Engine (ARQUITECTURA §4.1).
// Rendered via Graphics.DrawMeshInstanced into a RenderTexture from a CommandBuffer
// (NEVER SetPixels). Each instance is one soft spray dot; per-instance color and
// hardness come from a MaterialPropertyBlock array, so a whole burst is one draw call.
//
// It draws with a manually supplied ortho view/proj (canvas space [0,1]) straight into
// the target RT, so it does not need URP ScriptableRenderPass tags — it is pipeline
// agnostic on purpose (works identically under URP or Built-in).
Shader "PieceBook/SprayStamp"
{
    Properties
    {
        _Color    ("Color", Color) = (1,1,1,1)
        _Hardness ("Hardness", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float2 uv   : TEXCOORD0;
                float4 col  : COLOR;
                float  hard : TEXCOORD1;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(float, _Hardness)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.pos  = UnityObjectToClipPos(v.vertex);
                o.uv   = v.uv;
                o.col  = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                o.hard = UNITY_ACCESS_INSTANCED_PROP(Props, _Hardness);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // uv 0..1 across the quad; distance from center, 0 (center) -> 1 (edge).
                float d = length(i.uv - 0.5) * 2.0;
                // Solid core out to 'hard', feathered to nothing at the rim.
                float a = 1.0 - smoothstep(i.hard, 1.0, d);
                a = saturate(a) * i.col.a;
                return fixed4(i.col.rgb, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
