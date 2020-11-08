Shader "Custom/BlurEffect_Adapted"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    static const int samplingCount = 10;

    TEXTURE2D_X(_MainTex);
    SAMPLER(sampler_MainTex);
    uniform half4 _Offsets;
    uniform half _Weights[samplingCount];

    struct appdata
    {
        half4 pos : POSITION;
        half2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        half4 pos : SV_POSITION;
        half2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    v2f vert(appdata v)
    {
        v2f o;

        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, half4(v.pos.xyz, 1.0h)));
        o.uv = UnityStereoTransformScreenSpaceTex(v.uv);

        return o;
    }

    half4 frag(v2f i) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        
        half4 col = 0;

        [unroll]
        for (int j = samplingCount - 1; j > 0; j--)
        {
            col += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv - (_Offsets.xy * j)) * _Weights[j];
        }

        [unroll]
        for (int k = 0; k < samplingCount; k++)
        {
            col += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv + (_Offsets.xy * k)) * _Weights[k];
        }

        half3 grad1 = half3(1.0, 0.95, 0.98);
        half3 grad2 = half3(0.95, 0.95, 1.0);
        half3 grad = lerp(grad1, grad2, i.uv.y);

        col.rgb *= grad;
        col *= 1.15;

        return col;
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            ZTest Off
            ZWrite Off
            Cull Back
            
            Fog
            {
                Mode Off
            }

            HLSLPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}