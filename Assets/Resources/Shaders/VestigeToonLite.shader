// Toon léger pour le Web : remplace le Unity Toon Shader (com.unity.toonshader), dont la variante WebGL
// fait planter le processus graphique du navigateur en dessous de la qualité High.
// Reprend l'essentiel du rendu UTS : ombrage en deux tons (couleur de base / couleur d'ombre) avec seuil
// et fondu, couleur des lumières, ombres, lumières additionnelles (Forward+ compris), contour optionnel.
// Peu de textures (base + cartes d'ombres) : bien sous la limite WebGL de 16.
// Rangé dans Resources pour être toujours inclus dans le build (chargé par WebToonFallback).
Shader "Vestige/Toon Lite"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Color", Color) = (0.5, 0.5, 0.5, 1)
        _ShadeStep ("Shade Step", Range(0, 1)) = 0.5
        _ShadeFeather ("Shade Feather", Range(0.0001, 1)) = 0.0001
        _LightCap ("Light Cap (luminosité max / couleur de base)", Range(0.5, 3)) = 1.2
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        // Opacité façon UTS (1 + _Tweak_transparency). Volontairement indépendante de l'alpha de _BaseColor :
        // PlayerCameraFade modifie cet alpha, et dans l'éditeur (UTS) ça ne rendait pas le personnage transparent.
        _ToonOpacity ("Opacity", Range(0, 1)) = 1

        _OutlineWidth ("Outline Width (comme UTS)", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)

        // Rendu (réglés par WebToonFallback selon le matériau d'origine)
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _ZWrite ("__zw", Float) = 1
        [HideInInspector] _Cull ("__cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Même CBUFFER pour toutes les passes : compatible SRP Batcher
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _ShadeStep;
            half _ShadeFeather;
            half _LightCap;
            half4 _EmissionColor;
            half _ToonOpacity;
            float _OutlineWidth;
            half4 _OutlineColor;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
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
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 fogAndVertexLight : TEXCOORD3; // x : brouillard, yzw : lumières par sommet
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                half3 vertexLight = 0;
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    vertexLight = VertexLighting(positions.positionWS, normals.normalWS);
                #endif
                output.fogAndVertexLight = half4(ComputeFogFactor(positions.positionCS.z), vertexLight);
                return output;
            }

            // Ombrage toon : 0 = couleur d'ombre, 1 = couleur de base (même logique que UTS)
            half ToonRamp(half halfLambert)
            {
                half feather = max(_ShadeFeather, 0.0001h);
                return saturate((halfLambert - (_ShadeStep - feather)) / feather);
            }

            half3 ToonLight(Light light, half3 normalWS, half3 baseCol, half3 shadeCol)
            {
                half halfLambert = dot(normalWS, light.direction) * 0.5h + 0.5h;
                half ramp = ToonRamp(halfLambert * light.shadowAttenuation);
                return lerp(shadeCol, baseCol, ramp) * light.color * light.distanceAttenuation;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseCol = tex.rgb * _BaseColor.rgb;
                half3 shadeCol = baseCol * _ShadeColor.rgb;
                half alpha = _ToonOpacity;

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                half4 shadowMask = half4(1, 1, 1, 1);

                // Variable attendue par les macros de boucle de lumières (Forward+)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS), input.positionWS, shadowMask);
                half3 color = ToonLight(mainLight, normalWS, baseCol, shadeCol);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    #if USE_CLUSTER_LIGHT_LOOP
                        [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                        {
                            Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                            color += ToonLight(light, normalWS, baseCol, shadeCol);
                        }
                    #endif
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        color += ToonLight(light, normalWS, baseCol, shadeCol);
                    LIGHT_LOOP_END
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                    color += baseCol * input.fogAndVertexLight.yzw;
                #endif

                color += baseCol * SampleSH(normalWS);

                // Comme UTS, on plafonne la lumière : une lampe très proche (lanterne) ne brûle pas la
                // couleur. On réduit l'ensemble pour garder la teinte (torches orangées).
                half limit = max(max(baseCol.r, baseCol.g), max(baseCol.b, 0.0001h)) * _LightCap;
                half brightest = max(color.r, max(color.g, color.b));
                color *= min(1.0h, limit / max(brightest, 0.0001h));

                color += _EmissionColor.rgb;
                color = MixFog(color, input.fogAndVertexLight.x);
                return half4(color, alpha);
            }
            ENDHLSL
        }

        // Contour (coque inversée), comme l'outline d'UTS. Désactivé par matériau quand la largeur est nulle.
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Front

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                // Même échelle qu'UTS : _Outline_Width * 0.001 le long de la normale, en espace objet
                float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * (_OutlineWidth * 0.001);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return half4(MixFog(_OutlineColor.rgb, input.fogFactor), _ToonOpacity);
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
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 Frag() : SV_Target { return 0; }
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
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
