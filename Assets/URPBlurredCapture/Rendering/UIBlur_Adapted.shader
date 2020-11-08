Shader "Custom/Default_Adapted"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            
        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                half4 color     : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 pos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D_X(_GrabBlurTexture);
            SAMPLER(sampler_GrabBlurTexture);
        
            uniform half4 _Color;
            uniform half4 _TextureSampleAdd;
            uniform float4 _ClipRect;
            uniform float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // uGUIのメッシュの位置をスクリーン位置に変換する
                OUT.worldPosition = v.vertex;
                OUT.vertex = mul(unity_MatrixVP, mul(unity_ObjectToWorld, half4(OUT.worldPosition.xyz, 1.0h)));
                OUT.pos = ComputeScreenPos(OUT.vertex);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            half4 frag(v2f IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                // デバイス正規化座標系とするため`w`で除算する
                // が、ComputeScreenPosの段階で正常な値が入っているっぽいが、
                // シーンビューでちらつくのでこうしておく
                float2 uv = IN.pos.xy / IN.pos.w;

                // キャプチャ時に反転しているのでUVを反転してフェッチするようにする
                // uv.y = 1.0 - uv.y;
                half4 color = (SAMPLE_TEXTURE2D_X(_GrabBlurTexture, sampler_GrabBlurTexture, uv) + _TextureSampleAdd) * IN.color;

                // half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // #ifdef UNITY_UI_CLIP_RECT
                // color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                // #endif
                //
                // #ifdef UNITY_UI_ALPHACLIP
                // clip (color.a - 0.001);
                // #endif

               // uGUIのImageに設定されたテクスチャをマスク画像として利用する
                // 今回の例ではアルファ値でマスクしているが、白黒画像やその他の画像で独自にマスク位置を変更したい場合はここをいじる
                half4 mask = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, IN.texcoord);
                color.a = 1.0;

                return color;
            }
        ENDHLSL
        }
    }
}
