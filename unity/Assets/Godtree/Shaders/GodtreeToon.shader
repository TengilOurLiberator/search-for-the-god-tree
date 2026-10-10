// Soft cel shading for the painterly anime action-RPG look (canon/art-bible.md, decision 0014):
// a light side and a shadow side with a soft edge, coloured shadows, a cool rim light on the
// edge, and the painted base-colour texture left to do the rest. One main light, with shadows.
// Works in single-pass instanced VR.
Shader "Godtree/Toon"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Tint", Color) = (0.60, 0.58, 0.84, 1)
        _RampSoftness ("Shadow Edge Softness", Range(0.01, 0.5)) = 0.14
        _RimColor ("Rim Color", Color) = (0.80, 0.93, 1.0, 1)
        _RimPower ("Rim Sharpness", Range(1, 8)) = 3.2
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.30
        _SkyFill ("Sky Fill", Range(0, 1)) = 0.35
        _Brightness ("Brightness", Range(0.5, 2)) = 1.08
        // Show only the part of the mesh between two heights along an axis (object space).
        // Lets one model be drawn as separate moving pieces, as the training dummy is.
        _ClipAxis ("Slice Axis", Vector) = (0, 1, 0, 0)
        _ClipMin ("Slice From", Float) = -100000
        _ClipMax ("Slice To", Float) = 100000
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // One material layout shared by every pass, as the SRP Batcher requires.
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half _RampSoftness;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half _SkyFill;
            half _Brightness;
            float4 _ClipAxis;
            float _ClipMin;
            float _ClipMax;
        CBUFFER_END

        float SliceCoord(float3 positionOS) { return dot(positionOS, _ClipAxis.xyz); }
        void ClipSlice(float coord) { clip(min(coord - _ClipMin, _ClipMax - coord)); }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float slice : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                output.slice = SliceCoord(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                ClipSlice(input.slice);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half facing = dot(normal, mainLight.direction);
                // Light side against shadow side, with a soft edge instead of a hard line.
                half lit = smoothstep(-_RampSoftness, _RampSoftness, facing) * mainLight.shadowAttenuation;

                // Shadows are tinted, never grey; upward faces pick up a little sky.
                half3 shade = lerp(_ShadowColor.rgb, mainLight.color.rgb, lit);
                half skyFill = saturate(normal.y * 0.5 + 0.5) * _SkyFill * (1.0 - lit);
                half3 color = albedo.rgb * (shade + skyFill * half3(0.55, 0.70, 0.95)) * _Brightness;

                // Cool rim on the edge of the form, strongest on the lit side.
                half rim = pow(1.0 - saturate(dot(normal, view)), _RimPower) * _RimStrength;
                color += _RimColor.rgb * rim * (0.35 + 0.65 * lit);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float slice : TEXCOORD0;
            };

            ShadowVaryings shadowVert(ShadowAttributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                ShadowVaryings output;
                output.positionCS = positionCS;
                output.slice = SliceCoord(input.positionOS.xyz);
                return output;
            }

            half4 shadowFrag(ShadowVaryings input) : SV_Target
            {
                ClipSlice(input.slice);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float slice : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings depthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.slice = SliceCoord(input.positionOS.xyz);
                return output;
            }

            half4 depthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipSlice(input.slice);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
