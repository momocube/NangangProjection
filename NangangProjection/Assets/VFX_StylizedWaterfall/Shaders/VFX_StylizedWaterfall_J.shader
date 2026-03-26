Shader "Shader Graphs/Effect/VFX_StylizedWaterfall_J"
{
    Properties
    {
        [HDR]_WaterBaseColor("WaterBaseColor", Color) = (1, 1, 1, 1)
        [HDR]_WaterTopColor("WaterTopColor", Color) = (1, 1, 1, 1)
        [HDR]_WaterBottomColor("WaterBottomColor", Color) = (0, 0, 0, 1)
        _WaterColorRange("WaterColorRange", Vector) = (0, 1, 0, 0)
        _NoiseMaskRange("NoiseMaskRange", Vector) = (0, 1, 0, 0)
        _WaterColorLerp("WaterColorLerp", Range(0, 1)) = 1
        _WaterColorDistortStr("WaterColorDistortStr", Float) = 5
        [HDR]_Noise01Color("Noise01Color", Color) = (1, 1, 1, 1)
        [HDR]_Noise02Color("Noise02Color", Color) = (1, 1, 1, 1)
        _WaterAlpha("WaterAlpha", Range(0, 1)) = 1
        _NoiseAlpha("NoiseAlpha", Range(0, 1)) = 1
        [NoScaleOffset]_NoiseTex("NoiseTex", 2D) = "white" {}
        _Noise01TilingOffset("Noise01TilingOffset", Vector) = (1, 1, 0, 0)
        _Noise01Speed("Noise01Speed", Vector) = (0, 0, 0, 0)
        _Noise01PowStr("Noise01PowStr", Vector) = (1, 1, 0, 0)
        _Noise01Smooth("Noise01Smooth", Vector) = (0, 1, 0, 0)
        _Noise02TilingOffset("Noise02TilingOffset", Vector) = (1, 1, 0, 0)
        _Noise02Speed("Noise02Speed", Vector) = (0, 0, 0, 0)
        _Noise02PowStr("Noise02PowStr", Vector) = (1, 1, 0, 0)
        _Noise02Smooth("Noise02Smooth", Vector) = (0, 1, 0, 0)
        [HDR]_TopFoamColor("TopFoamColor", Color) = (1, 1, 1, 1)
        [HDR]_BottomFoamColor("BottomFoamColor", Color) = (1, 1, 1, 1)
        _FoamLerp("FoamLerp", Range(0, 1)) = 1
        _TopFoamDistortStr("TopFoamDistortStr", Float) = 5
        _BottomFoamDistortStr("BottomFoamDistortStr", Float) = 5
        _TopFoamWidth("TopFoamWidth", Float) = 0.08
        _TopFoamPosition("TopFoamPosition", Float) = 0.8
        _TopFoamSmooth("TopFoamSmooth", Vector) = (0.9, 0.15, 0, 0)
        _BottomFoamSmooth("BottomFoamSmooth", Vector) = (0.5, 1, 0, 0)
        _FoamNoiseTilingOffset("FoamNoiseTilingOffset", Vector) = (1, 1, 0, 0)
        _FoamNoiseSpeed("FoamNoiseSpeed", Vector) = (0, 0, 0, 0)
        _FoamPowStr("FoamPowStr", Vector) = (1, 1, 0, 0)
        _FoamSmooth("FoamSmooth", Vector) = (0, 1, 0, 0)
        [HDR]_MaskFoamColor("MaskFoamColor", Color) = (1, 1, 1, 1)
        _MaskFoamLerp("MaskFoamLerp", Range(0, 1)) = 1
        _MaskWidthSmooth("MaskWidthSmooth", Vector) = (0, 0.6, 0, 0)
        _MaskFoamWidthSmooth("MaskFoamWidthSmooth", Vector) = (0, 2, 0, 0)
        _SidesWidthSmoothStr("SidesWidthSmoothStr", Vector) = (-0.1, 0.2, 1, 0)
        _EdgeWidthSmoothStr("EdgeWidthSmoothStr", Vector) = (-0.1, 0.2, 1, 0)
        [ToggleUI]_UseNormalDistort("UseNormalDistort", Float) = 0
        [ToggleUI]_UseRemapDistort("UseRemapDistort", Float) = 0
        _DistortNormalStr("DistortNormalStr", Float) = 1
        _DistortNormalOffset("DistortNormalOffset", Float) = 0.5
        [NoScaleOffset]_DistortTex("DistortTex", 2D) = "white" {}
        _DistortTilingOffset("DistortTilingOffset", Vector) = (1, 1, 0, 0)
        _DistortSpeed("DistortSpeed", Vector) = (0, 0, 0, 0)
        _DistortStr("DistortStr", Range(0, 1)) = 0
        [ToggleUI]_UseNormalRefrac("UseNormalRefrac", Float) = 0
        [ToggleUI]_UseRemapRefrac("UseRemapRefrac", Float) = 0
        _RefracNormalStr("RefracNormalStr", Float) = 1
        _RefracNormalOffset("RefracNormalOffset", Float) = 0.5
        [NoScaleOffset]_RefracTex("RefracTex", 2D) = "white" {}
        _RefractTilingOffset("RefractTilingOffset", Vector) = (1, 1, 0, 0)
        _RefractSpeed("RefractSpeed", Vector) = (0, 0, 0, 0)
        _RefracStr("RefracStr", Range(0, 1)) = 0
        _SineWaveFrequency("SineWaveFrequency", Float) = 20
        _SineWaveAmplitude("SineWaveAmplitude", Float) = 0.3
        _SineWaveSpeed("SineWaveSpeed", Float) = 1
        _SineWaveDistortStr("SineWaveDistortStr", Float) = 5
        [HideInInspector]_CastShadows("_CastShadows", Float) = 0
        [HideInInspector]_Surface("_Surface", Float) = 1
        [HideInInspector]_Blend("_Blend", Float) = 0
        [HideInInspector]_AlphaClip("_AlphaClip", Float) = 0
        [HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
        [HideInInspector]_DstBlend("_DstBlend", Float) = 0
        [HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 0
        [HideInInspector]_ZWriteControl("_ZWriteControl", Float) = 0
        [HideInInspector]_ZTest("_ZTest", Float) = 4
        [HideInInspector]_Cull("_Cull", Float) = 0
        [HideInInspector]_AlphaToMask("_AlphaToMask", Float) = 0
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent"
            "DisableBatching"="False"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalUnlitSubTarget"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                // LightMode: <None>
            }
        
        // Render State
        Cull [_Cull]
        Blend [_SrcBlend] [_DstBlend]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        AlphaToMask [_AlphaToMask]
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma shader_feature _ _SAMPLE_GI
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
        #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_UNLIT
        #define _FOG_FRAGMENT 1
        #define REQUIRE_OPAQUE_TEXTURE
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceTangent;
             float3 WorldSpacePosition;
             float2 NDCPosition;
             float2 PixelPosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float3 positionWS : INTERP2;
             float3 normalWS : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _BottomFoamColor;
        float4 _MaskFoamColor;
        float _UseNormalRefrac;
        float _BottomFoamDistortStr;
        float _UseRemapRefrac;
        float _WaterColorDistortStr;
        float2 _NoiseMaskRange;
        float _TopFoamDistortStr;
        float2 _BottomFoamSmooth;
        float3 _EdgeWidthSmoothStr;
        float2 _MaskFoamWidthSmooth;
        float2 _FoamSmooth;
        float _SineWaveAmplitude;
        float4 _NoiseTex_TexelSize;
        float4 _FoamNoiseTilingOffset;
        float2 _FoamNoiseSpeed;
        float4 _WaterBaseColor;
        float _RefracNormalStr;
        float _RefracNormalOffset;
        float _SidesEdgeWidth;
        float _NoiseAlpha;
        float4 _Noise01Color;
        float4 _Noise02Color;
        float _RefracStr;
        float4 _RefractTilingOffset;
        float2 _RefractSpeed;
        float4 _WaterBottomColor;
        float4 _Noise02TilingOffset;
        float _DistortNormalOffset;
        float2 _WaterColorRange;
        float2 _Noise02Speed;
        float4 _WaterTopColor;
        float4 _DistortTex_TexelSize;
        float2 _Noise02PowStr;
        float4 _DistortTilingOffset;
        float2 _Noise02Smooth;
        float2 _Noise01Speed;
        float2 _DistortSpeed;
        float4 _Noise01TilingOffset;
        float2 _Noise01PowStr;
        float2 _Noise01Smooth;
        float _DistortStr;
        float _UseRemapDistort;
        float _WaterAlpha;
        float _UseNormalDistort;
        float _DistortNormalStr;
        float _WaterColorLerp;
        float4 _RefracTex_TexelSize;
        float _SineWaveFrequency;
        float _SineWaveSpeed;
        float _SineWaveDistortStr;
        float4 _TopFoamColor;
        float2 _MaskWidthSmooth;
        float3 _SidesWidthSmoothStr;
        float _TopFoamPosition;
        float _TopFoamWidth;
        float2 _TopFoamSmooth;
        float _FoamLerp;
        float _MaskFoamLerp;
        float2 _FoamPowStr;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_DistortTex);
        SAMPLER(sampler_DistortTex);
        TEXTURE2D(_RefracTex);
        SAMPLER(sampler_RefracTex);
        

        //J
        int maxPositionNum = 20;
        int currentPositionNum;
        uniform float4 _ObjectPosition[20];
        float maxRadiusOffset;
        float _ObjectRadius;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_NormalFromTexture_float(TEXTURE2D_PARAM(Texture, Sampler), float2 UV, float Offset, float Strength, out float3 Out)
        {
            Offset = pow(Offset, 3) * 0.1;
            float2 offsetU = float2(UV.x + Offset, UV.y);
            float2 offsetV = float2(UV.x, UV.y + Offset);
            float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;
            float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;
            float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;
            float3 va = float3(1, 0, (uSample - normalSample) * Strength);
            float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
            Out = normalize(cross(va, vb));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_SceneColor_float(float4 UV, out float3 Out)
        {
            Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _Property_e780a9446a2741e38792b7f25b69d454_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBaseColor) : _WaterBaseColor;
            float4 _ScreenPosition_e8b3cecfe986409384ece71af174b90a_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
            float _Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean = _UseNormalRefrac;
            float _Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float = _RefracStr;
            UnityTexture2D _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RefracTex);
            float4 _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4 = IN.uv0;
            float _Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[0];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[1];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_B_3_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[2];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_A_4_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[3];
            float2 _Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2 = float2(_Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float, _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float);
            float4 _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4 = _RefractTilingOffset;
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2 = float2(_Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float, _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float);
            float _Split_a2feec9f6a18404fa7513bc90ae72516_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2 = float2(_Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float, _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float);
            float2 _Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2 = _RefractSpeed;
            float2 _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2);
            float2 _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2);
            float2 _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2;
            Unity_Add_float2(_Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2);
            float2 _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2, _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2, _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2);
            float _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float = _RefracNormalOffset;
            float _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float = _RefracNormalStr;
            float3 _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate), _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2), _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float, _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float, _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3);
            float3 _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxx), _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3);
            float _Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean = _UseRemapRefrac;
            float _Float_a592706948e447c097be687bfae791c6_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2) );
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_R_4_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.r;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_G_5_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.g;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_B_6_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.b;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_A_7_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.a;
            float4 _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4;
            Unity_Add_float4((_Float_a592706948e447c097be687bfae791c6_Out_0_Float.xxxx), _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4);
            float _Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float = float(2);
            float4 _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4, (_Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float.xxxx), _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4);
            float4 _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4;
            Unity_Branch_float4(_Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean, _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4, _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4);
            float4 _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4, (_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxxx), _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4);
            float3 _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3;
            Unity_Branch_float3(_Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3, (_Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3);
            float3 _Add_c508082f79a344b9a5321986d84b3ed0_Out_2_Vector3;
            Unity_Add_float3((_ScreenPosition_e8b3cecfe986409384ece71af174b90a_Out_0_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Add_c508082f79a344b9a5321986d84b3ed0_Out_2_Vector3);
            float3 _SceneColor_981130b00a36430caf0945b493652bd4_Out_1_Vector3;
            Unity_SceneColor_float((float4(_Add_c508082f79a344b9a5321986d84b3ed0_Out_2_Vector3, 1.0)), _SceneColor_981130b00a36430caf0945b493652bd4_Out_1_Vector3);
            float3 _Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_e780a9446a2741e38792b7f25b69d454_Out_0_Vector4.xyz), _SceneColor_981130b00a36430caf0945b493652bd4_Out_1_Vector3, _Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3);
            float4 _Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBottomColor) : _WaterBottomColor;
            float4 _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterTopColor) : _WaterTopColor;
            float2 _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2 = _WaterColorRange;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[0];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[1];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_B_3_Float = 0;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_A_4_Float = 0;
            float _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float;
            Unity_Add_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float);
            float4 _UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4 = IN.uv0;
            float _Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float = _WaterColorDistortStr;
            float _Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean = _UseNormalDistort;
            float _Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float = _DistortStr;
            UnityTexture2D _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_DistortTex);
            float4 _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4 = IN.uv0;
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[0];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[1];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_B_3_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[2];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_A_4_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[3];
            float2 _Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2 = float2(_Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float, _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float);
            float4 _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4 = _DistortTilingOffset;
            float _Split_3d3070089d0844319f72a05a13112d78_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_3d3070089d0844319f72a05a13112d78_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_3d3070089d0844319f72a05a13112d78_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_3d3070089d0844319f72a05a13112d78_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2 = float2(_Split_3d3070089d0844319f72a05a13112d78_R_1_Float, _Split_3d3070089d0844319f72a05a13112d78_G_2_Float);
            float _Split_c094edcf73d543cb919e5d7826a05b1e_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2 = float2(_Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float, _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float);
            float2 _Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2 = _DistortSpeed;
            float2 _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2);
            float2 _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2);
            float2 _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2;
            Unity_Add_float2(_Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2);
            float2 _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2, _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2, _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2);
            float _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float = _DistortNormalOffset;
            float _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float = _DistortNormalStr;
            float3 _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate), _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2), _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float, _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float, _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3);
            float3 _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxx), _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3);
            float _Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean = _UseRemapDistort;
            float _Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2) );
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_R_4_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.r;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_G_5_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.g;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_B_6_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.b;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_A_7_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.a;
            float4 _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4;
            Unity_Add_float4((_Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float.xxxx), _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4);
            float _Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float = float(2);
            float4 _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4, (_Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float.xxxx), _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4);
            float4 _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4;
            Unity_Branch_float4(_Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean, _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4, _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4);
            float4 _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4, (_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxxx), _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4);
            float3 _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3;
            Unity_Branch_float3(_Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3, (_Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4.xyz), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3);
            float3 _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float.xxx), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3);
            float3 _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3;
            Unity_Add_float3((_UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4.xyz), _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3, _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3);
            float _Split_5904126eeadf4862b44d432382f26e43_R_1_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[0];
            float _Split_5904126eeadf4862b44d432382f26e43_G_2_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[1];
            float _Split_5904126eeadf4862b44d432382f26e43_B_3_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[2];
            float _Split_5904126eeadf4862b44d432382f26e43_A_4_Float = 0;
            float _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float;
            Unity_Saturate_float(_Split_5904126eeadf4862b44d432382f26e43_G_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float);
            float _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float;
            Unity_Smoothstep_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float, _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float);
            float _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float, _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float);
            float4 _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4;
            Unity_Lerp_float4(_Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4, _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4, (_Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float.xxxx), _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4);
            float _Split_5c957c23ba944f22a3599824406a1f36_R_1_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[0];
            float _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[1];
            float _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[2];
            float _Split_5c957c23ba944f22a3599824406a1f36_A_4_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[3];
            float3 _Vector3_3bbe8fbcce7942e9a916864352103c7b_Out_0_Vector3 = float3(_Split_5c957c23ba944f22a3599824406a1f36_R_1_Float, _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float, _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float);
            float3 _Add_d4a50d0c58be40b1ae39cba71d7cc26f_Out_2_Vector3;
            Unity_Add_float3(_Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3, _Vector3_3bbe8fbcce7942e9a916864352103c7b_Out_0_Vector3, _Add_d4a50d0c58be40b1ae39cba71d7cc26f_Out_2_Vector3);
            float _Property_7e0c8d20034d46c2a7106e3365b6f6d7_Out_0_Float = _WaterColorLerp;
            float3 _Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3;
            Unity_Lerp_float3(_Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3, _Add_d4a50d0c58be40b1ae39cba71d7cc26f_Out_2_Vector3, (_Property_7e0c8d20034d46c2a7106e3365b6f6d7_Out_0_Float.xxx), _Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3);
            float4 _Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise01Color) : _Noise01Color;
            float2 _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2 = _NoiseMaskRange;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[0];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[1];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_B_3_Float = 0;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_A_4_Float = 0;
            float _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float;
            Unity_Add_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float);
            float4 _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4 = IN.uv0;
            float _Split_2f15213772824def9fb752d1d60e12dc_R_1_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[0];
            float _Split_2f15213772824def9fb752d1d60e12dc_G_2_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[1];
            float _Split_2f15213772824def9fb752d1d60e12dc_B_3_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[2];
            float _Split_2f15213772824def9fb752d1d60e12dc_A_4_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[3];
            float _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float;
            Unity_Saturate_float(_Split_2f15213772824def9fb752d1d60e12dc_G_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float);
            float _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float;
            Unity_Smoothstep_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float, _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float);
            float _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float, _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float);
            float2 _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2 = _Noise01Smooth;
            float _Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[0];
            float _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[1];
            float _Split_7edd56182b1b4da88913294d5de35a2d_B_3_Float = 0;
            float _Split_7edd56182b1b4da88913294d5de35a2d_A_4_Float = 0;
            float _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float;
            Unity_Add_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float);
            UnityTexture2D _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4 = IN.uv0;
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[0];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[1];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_B_3_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[2];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_A_4_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[3];
            float2 _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2 = float2(_Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float, _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float);
            float2 _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2, _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2);
            float4 _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4 = _Noise01TilingOffset;
            float _Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_76666e838777429a8e6d8162a0d7ab16_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_76666e838777429a8e6d8162a0d7ab16_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2 = float2(_Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float, _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float);
            float _Split_e5bebaba69094dca8731554a45fb3b66_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_e5bebaba69094dca8731554a45fb3b66_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2 = float2(_Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float, _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float);
            float2 _Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2 = _Noise01Speed;
            float2 _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2);
            float2 _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2);
            float2 _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2;
            Unity_Add_float2(_Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2);
            float2 _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2, _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2, _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2);
            float4 _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.tex, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.samplerstate, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2) );
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.r;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_G_5_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.g;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_B_6_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.b;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_A_7_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.a;
            float _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float, _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float);
            float2 _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2 = _Noise01PowStr;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[0];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[1];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_B_3_Float = 0;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_A_4_Float = 0;
            float _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float;
            Unity_Power_float(_Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float, _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float);
            float _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float;
            Unity_Multiply_float_float(_Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float, _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float);
            float _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float;
            Unity_Saturate_float(_Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float);
            float _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float;
            Unity_Smoothstep_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float);
            float _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float, _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float);
            float4 _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4, (_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float.xxxx), _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4);
            float4 _Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise02Color) : _Noise02Color;
            float2 _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2 = _Noise02Smooth;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[0];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[1];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_B_3_Float = 0;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_A_4_Float = 0;
            float _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float;
            Unity_Add_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float);
            UnityTexture2D _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4 = IN.uv0;
            float _Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[0];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[1];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_B_3_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[2];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_A_4_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[3];
            float2 _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2 = float2(_Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float, _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float);
            float2 _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2, _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2);
            float4 _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4 = _Noise02TilingOffset;
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2 = float2(_Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float, _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float);
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2 = float2(_Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float, _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float);
            float2 _Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2 = _Noise02Speed;
            float2 _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2);
            float2 _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2);
            float2 _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2;
            Unity_Add_float2(_Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2);
            float2 _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2, _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2, _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2);
            float4 _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.tex, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.samplerstate, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2) );
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_R_4_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.r;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.g;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_B_6_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.b;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_A_7_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.a;
            float _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float, _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float);
            float2 _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2 = _Noise02PowStr;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[0];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[1];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_B_3_Float = 0;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_A_4_Float = 0;
            float _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float;
            Unity_Power_float(_Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float, _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float);
            float _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float;
            Unity_Multiply_float_float(_Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float, _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float);
            float _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float;
            Unity_Saturate_float(_Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float);
            float _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float;
            Unity_Smoothstep_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float);
            float _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float);
            float4 _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4, (_Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float.xxxx), _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4);
            float4 _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4, _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4, _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4);
            float _Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[0];
            float _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[1];
            float _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[2];
            float _Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[3];
            float3 _Vector3_7b07222035a943c29d95ac1bd4a96152_Out_0_Vector3 = float3(_Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float, _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float, _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float);
            float3 _Add_5ec00f71abb1420684280658e02df427_Out_2_Vector3;
            Unity_Add_float3(_Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3, _Vector3_7b07222035a943c29d95ac1bd4a96152_Out_0_Vector3, _Add_5ec00f71abb1420684280658e02df427_Out_2_Vector3);
            float _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float = _NoiseAlpha;
            float3 _Lerp_d45ba3f9a27445598409aebc9cfd8fde_Out_3_Vector3;
            Unity_Lerp_float3(_Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3, _Add_5ec00f71abb1420684280658e02df427_Out_2_Vector3, (_Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float.xxx), _Lerp_d45ba3f9a27445598409aebc9cfd8fde_Out_3_Vector3);
            float4 _Property_782c1c56767f4cdf9002f6ee96b2737a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_TopFoamColor) : _TopFoamColor;
            UnityTexture2D _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4 = IN.uv0;
            float _Split_81bacd708f454c8e8278db250d11f917_R_1_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[0];
            float _Split_81bacd708f454c8e8278db250d11f917_G_2_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[1];
            float _Split_81bacd708f454c8e8278db250d11f917_B_3_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[2];
            float _Split_81bacd708f454c8e8278db250d11f917_A_4_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[3];
            float2 _Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2 = float2(_Split_81bacd708f454c8e8278db250d11f917_R_1_Float, _Split_81bacd708f454c8e8278db250d11f917_G_2_Float);
            float4 _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4 = _FoamNoiseTilingOffset;
            float _Split_7fa49dda2d1849788afc51482698f959_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_7fa49dda2d1849788afc51482698f959_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_7fa49dda2d1849788afc51482698f959_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_7fa49dda2d1849788afc51482698f959_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2 = float2(_Split_7fa49dda2d1849788afc51482698f959_R_1_Float, _Split_7fa49dda2d1849788afc51482698f959_G_2_Float);
            float _Split_9cdfd6498d294ee08069eb99bef702b6_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2 = float2(_Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float, _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float);
            float2 _Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2 = _FoamNoiseSpeed;
            float2 _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2);
            float2 _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2);
            float2 _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2;
            Unity_Add_float2(_Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2);
            float2 _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2, _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2, _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2);
            float4 _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.tex, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.samplerstate, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2) );
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_R_4_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.r;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_G_5_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.g;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.b;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_A_7_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.a;
            float2 _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2 = _TopFoamSmooth;
            float _Split_211514ce3a224223a8f91e00d09582ca_R_1_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[0];
            float _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[1];
            float _Split_211514ce3a224223a8f91e00d09582ca_B_3_Float = 0;
            float _Split_211514ce3a224223a8f91e00d09582ca_A_4_Float = 0;
            float _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float;
            Unity_Add_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float);
            float4 _UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4 = IN.uv0;
            float _Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float = _TopFoamDistortStr;
            float3 _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3;
            Unity_Add_float3(_Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3);
            float3 _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3);
            float3 _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3, _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3);
            float _Split_c8e32710f8a04315886fca7a242e3c39_R_1_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[0];
            float _Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[1];
            float _Split_c8e32710f8a04315886fca7a242e3c39_B_3_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[2];
            float _Split_c8e32710f8a04315886fca7a242e3c39_A_4_Float = 0;
            float _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float = _TopFoamPosition;
            float _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float;
            Unity_Subtract_float(_Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float, _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float, _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float);
            float _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float;
            Unity_Absolute_float(_Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float, _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float);
            float _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float = _TopFoamWidth;
            float _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float;
            Unity_Subtract_float(_Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float, _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float, _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float);
            float _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float;
            Unity_OneMinus_float(_Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float);
            float _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float;
            Unity_Smoothstep_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float);
            float _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float, _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float);
            float4 _Lerp_f6b36a295d744335a44fd22b6d54c229_Out_3_Vector4;
            Unity_Lerp_float4(float4(1, 1, 1, 1), _Property_782c1c56767f4cdf9002f6ee96b2737a_Out_0_Vector4, (_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float.xxxx), _Lerp_f6b36a295d744335a44fd22b6d54c229_Out_3_Vector4);
            float4 _Property_be775130a97d4e24836839df9f737302_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_BottomFoamColor) : _BottomFoamColor;
            float2 _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2 = _BottomFoamSmooth;
            float _Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[0];
            float _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[1];
            float _Split_bd10349e771946a2829f2a8d3408e00d_B_3_Float = 0;
            float _Split_bd10349e771946a2829f2a8d3408e00d_A_4_Float = 0;
            float _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float;
            Unity_Add_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float);
            float _Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float = _BottomFoamDistortStr;
            float3 _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3);
            float3 _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3, _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3);
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_R_1_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[0];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[1];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_B_3_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[2];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_A_4_Float = 0;
            float _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float;
            Unity_OneMinus_float(_Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float);
            float _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float;
            Unity_Smoothstep_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float);
            float4 _Lerp_09bd43e3c00e4fe2a325afda6ae74aad_Out_3_Vector4;
            Unity_Lerp_float4(float4(1, 1, 1, 1), _Property_be775130a97d4e24836839df9f737302_Out_0_Vector4, (_Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float.xxxx), _Lerp_09bd43e3c00e4fe2a325afda6ae74aad_Out_3_Vector4);
            float4 _Multiply_8a0aa67416d74845898e4efd236cb9ac_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Lerp_f6b36a295d744335a44fd22b6d54c229_Out_3_Vector4, _Lerp_09bd43e3c00e4fe2a325afda6ae74aad_Out_3_Vector4, _Multiply_8a0aa67416d74845898e4efd236cb9ac_Out_2_Vector4);
            float2 _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2 = _FoamSmooth;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[0];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[1];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_B_3_Float = 0;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_A_4_Float = 0;
            float _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float;
            Unity_Add_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float);
            float _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float;
            Unity_Add_float(_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float, _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float);
            float _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float;
            Unity_Saturate_float(_Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float, _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float);
            float2 _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2 = _FoamPowStr;
            float _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[0];
            float _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[1];
            float _Split_93b2b90a6f164ce18354940ff78c4947_B_3_Float = 0;
            float _Split_93b2b90a6f164ce18354940ff78c4947_A_4_Float = 0;
            float _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float;
            Unity_Power_float(_Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float, _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float, _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float);
            float _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float;
            Unity_Multiply_float_float(_Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float, _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float, _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float);
            float _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float;
            Unity_Saturate_float(_Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float);
            float _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float;
            Unity_Smoothstep_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float, _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float);
            float _Property_1782f5c6311f4c728243ba230290ec34_Out_0_Float = _FoamLerp;
            float _Remap_a5bfe07e3cc448128114ed7682c146e3_Out_3_Float;
            Unity_Remap_float(_Property_1782f5c6311f4c728243ba230290ec34_Out_0_Float, float2 (0, 1), float2 (-1, 0), _Remap_a5bfe07e3cc448128114ed7682c146e3_Out_3_Float);
            float _Add_9bc3a31748244227a64c30048f29256f_Out_2_Float;
            Unity_Add_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Remap_a5bfe07e3cc448128114ed7682c146e3_Out_3_Float, _Add_9bc3a31748244227a64c30048f29256f_Out_2_Float);
            float _Saturate_0aff1362ce964ee28b33749c12c627e9_Out_1_Float;
            Unity_Saturate_float(_Add_9bc3a31748244227a64c30048f29256f_Out_2_Float, _Saturate_0aff1362ce964ee28b33749c12c627e9_Out_1_Float);
            float3 _Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3;
            Unity_Lerp_float3(_Lerp_d45ba3f9a27445598409aebc9cfd8fde_Out_3_Vector3, (_Multiply_8a0aa67416d74845898e4efd236cb9ac_Out_2_Vector4.xyz), (_Saturate_0aff1362ce964ee28b33749c12c627e9_Out_1_Float.xxx), _Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3);
            float4 _Property_989d344731ee4985bd672849bd49a7c3_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MaskFoamColor) : _MaskFoamColor;
            float4 _Property_35f4f97ab17f44bb924767bfb6dc4192_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MaskFoamColor) : _MaskFoamColor;
            float2 _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2 = _MaskFoamWidthSmooth;
            float _Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[0];
            float _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[1];
            float _Split_0083280626cc439aa48131cbd3c99c0b_B_3_Float = 0;
            float _Split_0083280626cc439aa48131cbd3c99c0b_A_4_Float = 0;
            float _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float;
            Unity_Add_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float);
            
            
            //J
            surface.Alpha = 1;
            float3 surfaceBaseColorTotal;
            float3 surfaceBaseColor;

            for (int i = 0; i < maxPositionNum; i++)
            {
                if (i >= currentPositionNum && currentPositionNum != 0) 
                    break;

                if(currentPositionNum == 0)
                    _ObjectPosition[0] = float4(-10000,0,0,0);
                    

                float3 _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3 = _ObjectPosition[i];
                float3 _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3;
                Unity_Subtract_float3(IN.WorldSpacePosition, _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3, _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3);
                float _Split_ccebce80c1f44409877640fc36481eac_R_1_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[0];
                float _Split_ccebce80c1f44409877640fc36481eac_G_2_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[1];
                float _Split_ccebce80c1f44409877640fc36481eac_B_3_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[2];
                float _Split_ccebce80c1f44409877640fc36481eac_A_4_Float = 0;
                float _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float;
                Unity_Maximum_float(_Split_ccebce80c1f44409877640fc36481eac_G_2_Float, float(0), _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float);
                float3 _Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3 = float3(_Split_ccebce80c1f44409877640fc36481eac_R_1_Float, _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float, _Split_ccebce80c1f44409877640fc36481eac_B_3_Float);
                float _Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float = _SineWaveFrequency;
                float _Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float = _SineWaveDistortStr;
                float3 _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3;
                Unity_Multiply_float3_float3(_Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, (_Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float.xxx), _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3);
                float4 _UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4 = IN.uv0;
                float3 _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3;
                Unity_Add_float3(_Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3, (_UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4.xyz), _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3);
                float _Split_19d7afb52dfb48f59a76c4e19ca1c030_R_1_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[0];
                float _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[1];
                float _Split_19d7afb52dfb48f59a76c4e19ca1c030_B_3_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[2];
                float _Split_19d7afb52dfb48f59a76c4e19ca1c030_A_4_Float = 0;
                float _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float;
                Unity_Multiply_float_float(_Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float, _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float, _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float);
                float _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float = _SineWaveSpeed;
                float _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float;
                Unity_Multiply_float_float(IN.TimeParameters.x, _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float);
                float _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float;
                Unity_Add_float(_Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float, _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float);
                float _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float;
                Unity_Sine_float(_Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float, _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float);
                float _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float = _SineWaveAmplitude;
                float _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float;
                Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float);
                float3 _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3;
                Unity_Multiply_float3_float3(IN.WorldSpaceTangent, (_Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float.xxx), _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3);
                float3 _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3;
                Unity_Subtract_float3(_Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3, _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3, _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3);
                float _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float;
                Unity_Length_float3(_Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3, _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float);
                float _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = _ObjectRadius;

                _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float += _Time.y * 0.1;
                _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = min(_Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, maxRadiusOffset);

                float _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float;
                Unity_Subtract_float(_Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float, _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float);
                float _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float;
                Unity_Smoothstep_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float);
                float _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float;
                Unity_Saturate_float(_Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float, _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float);
                float3 _Lerp_6241646eb6ce4b2bb84393117ba66ae2_Out_3_Vector3;
                Unity_Lerp_float3((_Property_35f4f97ab17f44bb924767bfb6dc4192_Out_0_Vector4.xyz), _Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3, (_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float.xxx), _Lerp_6241646eb6ce4b2bb84393117ba66ae2_Out_3_Vector3);
                float3 _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3 = _EdgeWidthSmoothStr;
                float _Split_455b73b72b364d559a420ef8b3344938_R_1_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[0];
                float _Split_455b73b72b364d559a420ef8b3344938_G_2_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[1];
                float _Split_455b73b72b364d559a420ef8b3344938_B_3_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[2];
                float _Split_455b73b72b364d559a420ef8b3344938_A_4_Float = 0;
                float _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float;
                Unity_Add_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Split_455b73b72b364d559a420ef8b3344938_G_2_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float);
                float4 _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4 = IN.uv0;
                float _Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[0];
                float _Split_56c51d0d274b483a9ed8a994f03b74ad_G_2_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[1];
                float _Split_56c51d0d274b483a9ed8a994f03b74ad_B_3_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[2];
                float _Split_56c51d0d274b483a9ed8a994f03b74ad_A_4_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[3];
                float _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float;
                Unity_Subtract_float(_Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float, float(0.5), _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float);
                float _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float;
                Unity_Multiply_float_float(_Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, 0.05, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float);
                float _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float;
                Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float, _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float);
                float _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float;
                Unity_Multiply_float_float(_Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float, 0.5, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float);
                float _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float;
                Unity_Subtract_float(_Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float, _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float);
                float _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float;
                Unity_Absolute_float(_Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float, _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float);
                float _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float;
                Unity_Subtract_float(_Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float, float(0.5), _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float);
                float _Property_e192f06194264eccbd544e066faf25eb_Out_0_Float = _SineWaveAmplitude;
                float _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float;
                Unity_Multiply_float_float(_Property_e192f06194264eccbd544e066faf25eb_Out_0_Float, 0.5, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float);
                float _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float;
                Unity_Add_float(_Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float);
                float _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float;
                Unity_Smoothstep_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float);
                float _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float;
                Unity_OneMinus_float(_Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float, _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float);
                float _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float;
                Unity_Multiply_float_float(_OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float, _Split_455b73b72b364d559a420ef8b3344938_B_3_Float, _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float);
                float _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float;
                Unity_Saturate_float(_Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float, _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float);
                float3 _Lerp_58bbbddec7294f3fb276f7f670dc8c28_Out_3_Vector3;
                Unity_Lerp_float3((_Property_989d344731ee4985bd672849bd49a7c3_Out_0_Vector4.xyz), _Lerp_6241646eb6ce4b2bb84393117ba66ae2_Out_3_Vector3, (_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float.xxx), _Lerp_58bbbddec7294f3fb276f7f670dc8c28_Out_3_Vector3);
                float _Property_b7755809b2f44df39015c1ad27320edc_Out_0_Float = _MaskFoamLerp;
                float3 _Lerp_d2fd6a4a86344139a2925e99d45b4ba2_Out_3_Vector3;
                Unity_Lerp_float3(_Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3, _Lerp_58bbbddec7294f3fb276f7f670dc8c28_Out_3_Vector3, (_Property_b7755809b2f44df39015c1ad27320edc_Out_0_Float.xxx), _Lerp_d2fd6a4a86344139a2925e99d45b4ba2_Out_3_Vector3);
                float3 _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3 = _SidesWidthSmoothStr;
                float _Split_36856a9943d7412593732e444047eb04_R_1_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[0];
                float _Split_36856a9943d7412593732e444047eb04_G_2_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[1];
                float _Split_36856a9943d7412593732e444047eb04_B_3_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[2];
                float _Split_36856a9943d7412593732e444047eb04_A_4_Float = 0;
                float _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float;
                Unity_Add_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Split_36856a9943d7412593732e444047eb04_G_2_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float);
                float _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float;
                Unity_Smoothstep_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float);
                float _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float;
                Unity_OneMinus_float(_Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float, _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float);
                float _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float;
                Unity_Multiply_float_float(_OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float, _Split_36856a9943d7412593732e444047eb04_B_3_Float, _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float);
                float _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float;
                Unity_Saturate_float(_Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float);
                float _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float;
                Unity_Multiply_float_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float);
                float2 _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2 = _MaskWidthSmooth;
                float _Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[0];
                float _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[1];
                float _Split_3b52bf3756d5491bb0ed074f060f421b_B_3_Float = 0;
                float _Split_3b52bf3756d5491bb0ed074f060f421b_A_4_Float = 0;
                float _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float;
                Unity_Add_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float);
                float _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float;
                Unity_Smoothstep_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float);
                float _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float;
                Unity_Multiply_float_float(_Split_5c957c23ba944f22a3599824406a1f36_A_4_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float);
                float _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float;
                Unity_Multiply_float_float(_Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float);
                float _Property_525f3b66b77449758f64a237743c6372_Out_0_Float = 1;//_WaterAlpha;
                float _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float;
                Unity_Multiply_float_float(_Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float, _Property_525f3b66b77449758f64a237743c6372_Out_0_Float, _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float);
                float _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float;
                Unity_Multiply_float_float(_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float);
                float _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float;
                Unity_Multiply_float_float(_Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float, _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float);
                float _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float;
                Unity_Multiply_float_float(_Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float, _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float);
                float _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float;
                Unity_Add_float(_Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float, _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float);
                float _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float;
                Unity_Saturate_float(_Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float);
                float _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float;
                Unity_Add_float(_Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float, _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float);
                float _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float;
                Unity_OneMinus_float(_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float);
                float _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float;
                Unity_Add_float(_Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float, _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float);
                float _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float;
                Unity_OneMinus_float(_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float);
                float _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float;
                Unity_Add_float(_Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float, _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float);
                float _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float;
                Unity_Saturate_float(_Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float, _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float);
                float _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float;
                Unity_Multiply_float_float(_Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float);
                float _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float;
                Unity_Multiply_float_float(_Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float, _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float);
                float _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
                Unity_Saturate_float(_Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float, _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float);
                surfaceBaseColor = _Lerp_d2fd6a4a86344139a2925e99d45b4ba2_Out_3_Vector3;

                surfaceBaseColorTotal += surfaceBaseColor * _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float * _WaterAlpha;
                surface.Alpha *= _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;

                if(currentPositionNum == 0) break;
            }
                      
            if(currentPositionNum != 0)
                surfaceBaseColorTotal /= currentPositionNum;

            
            surface.AlphaClipThreshold = float(0.5);
            surface.Alpha *= _WaterAlpha;

            surface.BaseColor = surfaceBaseColorTotal * (1 - surface.Alpha) + surfaceBaseColorTotal * surface.Alpha;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
            // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
        // Render State
        Cull [_Cull]
        ZTest LEqual
        ZWrite On
        ColorMask R
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceTangent;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float3 positionWS : INTERP2;
             float3 normalWS : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _BottomFoamColor;
        float4 _MaskFoamColor;
        float _UseNormalRefrac;
        float _BottomFoamDistortStr;
        float _UseRemapRefrac;
        float _WaterColorDistortStr;
        float2 _NoiseMaskRange;
        float _TopFoamDistortStr;
        float2 _BottomFoamSmooth;
        float3 _EdgeWidthSmoothStr;
        float2 _MaskFoamWidthSmooth;
        float2 _FoamSmooth;
        float _SineWaveAmplitude;
        float4 _NoiseTex_TexelSize;
        float4 _FoamNoiseTilingOffset;
        float2 _FoamNoiseSpeed;
        float4 _WaterBaseColor;
        float _RefracNormalStr;
        float _RefracNormalOffset;
        float _SidesEdgeWidth;
        float _NoiseAlpha;
        float4 _Noise01Color;
        float4 _Noise02Color;
        float _RefracStr;
        float4 _RefractTilingOffset;
        float2 _RefractSpeed;
        float4 _WaterBottomColor;
        float4 _Noise02TilingOffset;
        float _DistortNormalOffset;
        float2 _WaterColorRange;
        float2 _Noise02Speed;
        float4 _WaterTopColor;
        float4 _DistortTex_TexelSize;
        float2 _Noise02PowStr;
        float4 _DistortTilingOffset;
        float2 _Noise02Smooth;
        float2 _Noise01Speed;
        float2 _DistortSpeed;
        float4 _Noise01TilingOffset;
        float2 _Noise01PowStr;
        float2 _Noise01Smooth;
        float _DistortStr;
        float _UseRemapDistort;
        float _WaterAlpha;
        float _UseNormalDistort;
        float _DistortNormalStr;
        float _WaterColorLerp;
        float4 _RefracTex_TexelSize;
        float _SineWaveFrequency;
        float _SineWaveSpeed;
        float _SineWaveDistortStr;
        float4 _TopFoamColor;
        float2 _MaskWidthSmooth;
        float3 _SidesWidthSmoothStr;
        float _TopFoamPosition;
        float _TopFoamWidth;
        float2 _TopFoamSmooth;
        float _FoamLerp;
        float _MaskFoamLerp;
        float2 _FoamPowStr;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_DistortTex);
        SAMPLER(sampler_DistortTex);
        TEXTURE2D(_RefracTex);
        SAMPLER(sampler_RefracTex);
        float3 _ObjectPosition;
        float _ObjectRadius;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_NormalFromTexture_float(TEXTURE2D_PARAM(Texture, Sampler), float2 UV, float Offset, float Strength, out float3 Out)
        {
            Offset = pow(Offset, 3) * 0.1;
            float2 offsetU = float2(UV.x + Offset, UV.y);
            float2 offsetV = float2(UV.x, UV.y + Offset);
            float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;
            float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;
            float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;
            float3 va = float3(1, 0, (uSample - normalSample) * Strength);
            float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
            Out = normalize(cross(va, vb));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float2 _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2 = _FoamSmooth;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[0];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[1];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_B_3_Float = 0;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_A_4_Float = 0;
            float _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float;
            Unity_Add_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float);
            UnityTexture2D _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4 = IN.uv0;
            float _Split_81bacd708f454c8e8278db250d11f917_R_1_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[0];
            float _Split_81bacd708f454c8e8278db250d11f917_G_2_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[1];
            float _Split_81bacd708f454c8e8278db250d11f917_B_3_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[2];
            float _Split_81bacd708f454c8e8278db250d11f917_A_4_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[3];
            float2 _Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2 = float2(_Split_81bacd708f454c8e8278db250d11f917_R_1_Float, _Split_81bacd708f454c8e8278db250d11f917_G_2_Float);
            float4 _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4 = _FoamNoiseTilingOffset;
            float _Split_7fa49dda2d1849788afc51482698f959_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_7fa49dda2d1849788afc51482698f959_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_7fa49dda2d1849788afc51482698f959_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_7fa49dda2d1849788afc51482698f959_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2 = float2(_Split_7fa49dda2d1849788afc51482698f959_R_1_Float, _Split_7fa49dda2d1849788afc51482698f959_G_2_Float);
            float _Split_9cdfd6498d294ee08069eb99bef702b6_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2 = float2(_Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float, _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float);
            float2 _Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2 = _FoamNoiseSpeed;
            float2 _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2);
            float2 _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2);
            float2 _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2;
            Unity_Add_float2(_Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2);
            float2 _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2, _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2, _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2);
            float4 _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.tex, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.samplerstate, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2) );
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_R_4_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.r;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_G_5_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.g;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.b;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_A_7_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.a;
            float2 _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2 = _TopFoamSmooth;
            float _Split_211514ce3a224223a8f91e00d09582ca_R_1_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[0];
            float _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[1];
            float _Split_211514ce3a224223a8f91e00d09582ca_B_3_Float = 0;
            float _Split_211514ce3a224223a8f91e00d09582ca_A_4_Float = 0;
            float _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float;
            Unity_Add_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float);
            float4 _UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4 = IN.uv0;
            float _Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float = _TopFoamDistortStr;
            float _Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean = _UseNormalRefrac;
            float _Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float = _RefracStr;
            UnityTexture2D _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RefracTex);
            float4 _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4 = IN.uv0;
            float _Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[0];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[1];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_B_3_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[2];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_A_4_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[3];
            float2 _Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2 = float2(_Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float, _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float);
            float4 _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4 = _RefractTilingOffset;
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2 = float2(_Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float, _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float);
            float _Split_a2feec9f6a18404fa7513bc90ae72516_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2 = float2(_Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float, _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float);
            float2 _Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2 = _RefractSpeed;
            float2 _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2);
            float2 _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2);
            float2 _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2;
            Unity_Add_float2(_Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2);
            float2 _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2, _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2, _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2);
            float _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float = _RefracNormalOffset;
            float _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float = _RefracNormalStr;
            float3 _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate), _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2), _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float, _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float, _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3);
            float3 _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxx), _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3);
            float _Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean = _UseRemapRefrac;
            float _Float_a592706948e447c097be687bfae791c6_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2) );
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_R_4_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.r;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_G_5_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.g;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_B_6_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.b;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_A_7_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.a;
            float4 _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4;
            Unity_Add_float4((_Float_a592706948e447c097be687bfae791c6_Out_0_Float.xxxx), _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4);
            float _Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float = float(2);
            float4 _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4, (_Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float.xxxx), _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4);
            float4 _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4;
            Unity_Branch_float4(_Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean, _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4, _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4);
            float4 _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4, (_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxxx), _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4);
            float3 _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3;
            Unity_Branch_float3(_Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3, (_Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3);
            float _Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean = _UseNormalDistort;
            float _Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float = _DistortStr;
            UnityTexture2D _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_DistortTex);
            float4 _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4 = IN.uv0;
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[0];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[1];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_B_3_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[2];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_A_4_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[3];
            float2 _Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2 = float2(_Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float, _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float);
            float4 _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4 = _DistortTilingOffset;
            float _Split_3d3070089d0844319f72a05a13112d78_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_3d3070089d0844319f72a05a13112d78_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_3d3070089d0844319f72a05a13112d78_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_3d3070089d0844319f72a05a13112d78_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2 = float2(_Split_3d3070089d0844319f72a05a13112d78_R_1_Float, _Split_3d3070089d0844319f72a05a13112d78_G_2_Float);
            float _Split_c094edcf73d543cb919e5d7826a05b1e_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2 = float2(_Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float, _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float);
            float2 _Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2 = _DistortSpeed;
            float2 _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2);
            float2 _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2);
            float2 _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2;
            Unity_Add_float2(_Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2);
            float2 _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2, _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2, _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2);
            float _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float = _DistortNormalOffset;
            float _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float = _DistortNormalStr;
            float3 _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate), _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2), _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float, _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float, _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3);
            float3 _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxx), _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3);
            float _Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean = _UseRemapDistort;
            float _Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2) );
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_R_4_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.r;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_G_5_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.g;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_B_6_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.b;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_A_7_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.a;
            float4 _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4;
            Unity_Add_float4((_Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float.xxxx), _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4);
            float _Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float = float(2);
            float4 _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4, (_Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float.xxxx), _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4);
            float4 _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4;
            Unity_Branch_float4(_Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean, _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4, _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4);
            float4 _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4, (_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxxx), _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4);
            float3 _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3;
            Unity_Branch_float3(_Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3, (_Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4.xyz), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3);
            float3 _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3;
            Unity_Add_float3(_Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3);
            float3 _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3);
            float3 _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3, _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3);
            float _Split_c8e32710f8a04315886fca7a242e3c39_R_1_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[0];
            float _Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[1];
            float _Split_c8e32710f8a04315886fca7a242e3c39_B_3_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[2];
            float _Split_c8e32710f8a04315886fca7a242e3c39_A_4_Float = 0;
            float _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float = _TopFoamPosition;
            float _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float;
            Unity_Subtract_float(_Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float, _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float, _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float);
            float _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float;
            Unity_Absolute_float(_Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float, _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float);
            float _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float = _TopFoamWidth;
            float _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float;
            Unity_Subtract_float(_Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float, _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float, _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float);
            float _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float;
            Unity_OneMinus_float(_Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float);
            float _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float;
            Unity_Smoothstep_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float);
            float _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float, _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float);
            float2 _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2 = _BottomFoamSmooth;
            float _Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[0];
            float _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[1];
            float _Split_bd10349e771946a2829f2a8d3408e00d_B_3_Float = 0;
            float _Split_bd10349e771946a2829f2a8d3408e00d_A_4_Float = 0;
            float _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float;
            Unity_Add_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float);
            float _Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float = _BottomFoamDistortStr;
            float3 _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3);
            float3 _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3, _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3);
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_R_1_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[0];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[1];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_B_3_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[2];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_A_4_Float = 0;
            float _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float;
            Unity_OneMinus_float(_Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float);
            float _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float;
            Unity_Smoothstep_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float);
            float _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float;
            Unity_Add_float(_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float, _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float);
            float _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float;
            Unity_Saturate_float(_Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float, _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float);
            float2 _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2 = _FoamPowStr;
            float _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[0];
            float _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[1];
            float _Split_93b2b90a6f164ce18354940ff78c4947_B_3_Float = 0;
            float _Split_93b2b90a6f164ce18354940ff78c4947_A_4_Float = 0;
            float _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float;
            Unity_Power_float(_Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float, _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float, _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float);
            float _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float;
            Unity_Multiply_float_float(_Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float, _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float, _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float);
            float _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float;
            Unity_Saturate_float(_Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float);
            float _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float;
            Unity_Smoothstep_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float, _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float);
            float3 _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3 = _SidesWidthSmoothStr;
            float _Split_36856a9943d7412593732e444047eb04_R_1_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[0];
            float _Split_36856a9943d7412593732e444047eb04_G_2_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[1];
            float _Split_36856a9943d7412593732e444047eb04_B_3_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[2];
            float _Split_36856a9943d7412593732e444047eb04_A_4_Float = 0;
            float _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float;
            Unity_Add_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Split_36856a9943d7412593732e444047eb04_G_2_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float);
            float4 _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4 = IN.uv0;
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[0];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_G_2_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[1];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_B_3_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[2];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_A_4_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[3];
            float _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float;
            Unity_Subtract_float(_Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float, float(0.5), _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float);
            float _Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float = _SineWaveFrequency;
            float _Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float = _SineWaveDistortStr;
            float3 _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3;
            Unity_Multiply_float3_float3(_Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, (_Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float.xxx), _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3);
            float4 _UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4 = IN.uv0;
            float3 _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3;
            Unity_Add_float3(_Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3, (_UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4.xyz), _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3);
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_R_1_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[0];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[1];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_B_3_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[2];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_A_4_Float = 0;
            float _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float;
            Unity_Multiply_float_float(_Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float, _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float, _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float);
            float _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float = _SineWaveSpeed;
            float _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float);
            float _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float;
            Unity_Add_float(_Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float, _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float);
            float _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float;
            Unity_Sine_float(_Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float, _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float);
            float _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float;
            Unity_Multiply_float_float(_Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, 0.05, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float);
            float _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float, _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float);
            float _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float, 0.5, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float);
            float _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float;
            Unity_Subtract_float(_Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float, _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float);
            float _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float;
            Unity_Absolute_float(_Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float, _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float);
            float _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float;
            Unity_Subtract_float(_Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float, float(0.5), _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float);
            float _Property_e192f06194264eccbd544e066faf25eb_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float;
            Unity_Multiply_float_float(_Property_e192f06194264eccbd544e066faf25eb_Out_0_Float, 0.5, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float);
            float _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float;
            Unity_Add_float(_Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float);
            float _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float;
            Unity_Smoothstep_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float);
            float _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float, _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float);
            float _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float, _Split_36856a9943d7412593732e444047eb04_B_3_Float, _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float);
            float _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float;
            Unity_Saturate_float(_Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float);
            float _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float);
            float4 _Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBottomColor) : _WaterBottomColor;
            float4 _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterTopColor) : _WaterTopColor;
            float2 _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2 = _WaterColorRange;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[0];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[1];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_B_3_Float = 0;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_A_4_Float = 0;
            float _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float;
            Unity_Add_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float);
            float4 _UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4 = IN.uv0;
            float _Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float = _WaterColorDistortStr;
            float3 _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float.xxx), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3);
            float3 _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3;
            Unity_Add_float3((_UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4.xyz), _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3, _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3);
            float _Split_5904126eeadf4862b44d432382f26e43_R_1_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[0];
            float _Split_5904126eeadf4862b44d432382f26e43_G_2_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[1];
            float _Split_5904126eeadf4862b44d432382f26e43_B_3_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[2];
            float _Split_5904126eeadf4862b44d432382f26e43_A_4_Float = 0;
            float _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float;
            Unity_Saturate_float(_Split_5904126eeadf4862b44d432382f26e43_G_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float);
            float _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float;
            Unity_Smoothstep_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float, _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float);
            float _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float, _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float);
            float4 _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4;
            Unity_Lerp_float4(_Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4, _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4, (_Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float.xxxx), _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4);
            float _Split_5c957c23ba944f22a3599824406a1f36_R_1_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[0];
            float _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[1];
            float _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[2];
            float _Split_5c957c23ba944f22a3599824406a1f36_A_4_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[3];
            float2 _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2 = _MaskWidthSmooth;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[0];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[1];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_B_3_Float = 0;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_A_4_Float = 0;
            float _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float;
            Unity_Add_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float);
            float3 _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3 = _ObjectPosition;
            float3 _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3;
            Unity_Subtract_float3(IN.WorldSpacePosition, _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3, _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3);
            float _Split_ccebce80c1f44409877640fc36481eac_R_1_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[0];
            float _Split_ccebce80c1f44409877640fc36481eac_G_2_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[1];
            float _Split_ccebce80c1f44409877640fc36481eac_B_3_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[2];
            float _Split_ccebce80c1f44409877640fc36481eac_A_4_Float = 0;
            float _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float;
            Unity_Maximum_float(_Split_ccebce80c1f44409877640fc36481eac_G_2_Float, float(0), _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float);
            float3 _Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3 = float3(_Split_ccebce80c1f44409877640fc36481eac_R_1_Float, _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float, _Split_ccebce80c1f44409877640fc36481eac_B_3_Float);
            float _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float);
            float3 _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.WorldSpaceTangent, (_Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float.xxx), _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3);
            float3 _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3;
            Unity_Subtract_float3(_Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3, _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3, _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3);
            float _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float;
            Unity_Length_float3(_Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3, _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float);
            float _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = _ObjectRadius;
            float _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float;
            Unity_Subtract_float(_Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float, _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float);
            float _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float;
            Unity_Smoothstep_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float);
            float _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float;
            Unity_Multiply_float_float(_Split_5c957c23ba944f22a3599824406a1f36_A_4_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float);
            float _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float);
            float _Property_525f3b66b77449758f64a237743c6372_Out_0_Float = _WaterAlpha;
            float _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float, _Property_525f3b66b77449758f64a237743c6372_Out_0_Float, _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float);
            float4 _Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise01Color) : _Noise01Color;
            float2 _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2 = _NoiseMaskRange;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[0];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[1];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_B_3_Float = 0;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_A_4_Float = 0;
            float _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float;
            Unity_Add_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float);
            float4 _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4 = IN.uv0;
            float _Split_2f15213772824def9fb752d1d60e12dc_R_1_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[0];
            float _Split_2f15213772824def9fb752d1d60e12dc_G_2_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[1];
            float _Split_2f15213772824def9fb752d1d60e12dc_B_3_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[2];
            float _Split_2f15213772824def9fb752d1d60e12dc_A_4_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[3];
            float _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float;
            Unity_Saturate_float(_Split_2f15213772824def9fb752d1d60e12dc_G_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float);
            float _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float;
            Unity_Smoothstep_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float, _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float);
            float _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float, _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float);
            float2 _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2 = _Noise01Smooth;
            float _Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[0];
            float _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[1];
            float _Split_7edd56182b1b4da88913294d5de35a2d_B_3_Float = 0;
            float _Split_7edd56182b1b4da88913294d5de35a2d_A_4_Float = 0;
            float _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float;
            Unity_Add_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float);
            UnityTexture2D _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4 = IN.uv0;
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[0];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[1];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_B_3_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[2];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_A_4_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[3];
            float2 _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2 = float2(_Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float, _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float);
            float2 _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2, _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2);
            float4 _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4 = _Noise01TilingOffset;
            float _Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_76666e838777429a8e6d8162a0d7ab16_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_76666e838777429a8e6d8162a0d7ab16_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2 = float2(_Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float, _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float);
            float _Split_e5bebaba69094dca8731554a45fb3b66_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_e5bebaba69094dca8731554a45fb3b66_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2 = float2(_Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float, _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float);
            float2 _Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2 = _Noise01Speed;
            float2 _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2);
            float2 _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2);
            float2 _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2;
            Unity_Add_float2(_Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2);
            float2 _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2, _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2, _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2);
            float4 _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.tex, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.samplerstate, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2) );
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.r;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_G_5_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.g;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_B_6_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.b;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_A_7_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.a;
            float _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float, _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float);
            float2 _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2 = _Noise01PowStr;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[0];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[1];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_B_3_Float = 0;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_A_4_Float = 0;
            float _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float;
            Unity_Power_float(_Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float, _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float);
            float _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float;
            Unity_Multiply_float_float(_Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float, _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float);
            float _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float;
            Unity_Saturate_float(_Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float);
            float _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float;
            Unity_Smoothstep_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float);
            float _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float, _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float);
            float4 _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4, (_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float.xxxx), _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4);
            float4 _Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise02Color) : _Noise02Color;
            float2 _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2 = _Noise02Smooth;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[0];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[1];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_B_3_Float = 0;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_A_4_Float = 0;
            float _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float;
            Unity_Add_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float);
            UnityTexture2D _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4 = IN.uv0;
            float _Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[0];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[1];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_B_3_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[2];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_A_4_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[3];
            float2 _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2 = float2(_Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float, _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float);
            float2 _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2, _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2);
            float4 _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4 = _Noise02TilingOffset;
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2 = float2(_Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float, _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float);
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2 = float2(_Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float, _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float);
            float2 _Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2 = _Noise02Speed;
            float2 _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2);
            float2 _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2);
            float2 _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2;
            Unity_Add_float2(_Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2);
            float2 _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2, _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2, _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2);
            float4 _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.tex, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.samplerstate, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2) );
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_R_4_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.r;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.g;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_B_6_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.b;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_A_7_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.a;
            float _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float, _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float);
            float2 _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2 = _Noise02PowStr;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[0];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[1];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_B_3_Float = 0;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_A_4_Float = 0;
            float _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float;
            Unity_Power_float(_Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float, _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float);
            float _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float;
            Unity_Multiply_float_float(_Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float, _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float);
            float _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float;
            Unity_Saturate_float(_Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float);
            float _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float;
            Unity_Smoothstep_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float);
            float _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float);
            float4 _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4, (_Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float.xxxx), _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4);
            float4 _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4, _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4, _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4);
            float _Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[0];
            float _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[1];
            float _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[2];
            float _Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[3];
            float _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float);
            float _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float;
            Unity_Multiply_float_float(_Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float, _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float);
            float _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float = _NoiseAlpha;
            float _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float, _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float);
            float _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float;
            Unity_Add_float(_Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float, _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float);
            float _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float;
            Unity_Saturate_float(_Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float);
            float _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float;
            Unity_Add_float(_Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float, _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float);
            float2 _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2 = _MaskFoamWidthSmooth;
            float _Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[0];
            float _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[1];
            float _Split_0083280626cc439aa48131cbd3c99c0b_B_3_Float = 0;
            float _Split_0083280626cc439aa48131cbd3c99c0b_A_4_Float = 0;
            float _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float;
            Unity_Add_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float);
            float _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float;
            Unity_Smoothstep_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float);
            float _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float, _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float);
            float _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float;
            Unity_OneMinus_float(_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float);
            float _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float;
            Unity_Add_float(_Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float, _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float);
            float3 _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3 = _EdgeWidthSmoothStr;
            float _Split_455b73b72b364d559a420ef8b3344938_R_1_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[0];
            float _Split_455b73b72b364d559a420ef8b3344938_G_2_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[1];
            float _Split_455b73b72b364d559a420ef8b3344938_B_3_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[2];
            float _Split_455b73b72b364d559a420ef8b3344938_A_4_Float = 0;
            float _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float;
            Unity_Add_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Split_455b73b72b364d559a420ef8b3344938_G_2_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float);
            float _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float;
            Unity_Smoothstep_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float);
            float _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float, _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float);
            float _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float, _Split_455b73b72b364d559a420ef8b3344938_B_3_Float, _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float);
            float _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float;
            Unity_Saturate_float(_Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float, _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float);
            float _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float;
            Unity_OneMinus_float(_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float);
            float _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float;
            Unity_Add_float(_Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float, _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float);
            float _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float;
            Unity_Saturate_float(_Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float, _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float);
            float _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float);
            float _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float, _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float);
            float _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            Unity_Saturate_float(_Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float, _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float);
            surface.Alpha = _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
            // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }
        
        // Render State
        Cull [_Cull]
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
        #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
        #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceTangent;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float3 positionWS : INTERP2;
             float3 normalWS : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _BottomFoamColor;
        float4 _MaskFoamColor;
        float _UseNormalRefrac;
        float _BottomFoamDistortStr;
        float _UseRemapRefrac;
        float _WaterColorDistortStr;
        float2 _NoiseMaskRange;
        float _TopFoamDistortStr;
        float2 _BottomFoamSmooth;
        float3 _EdgeWidthSmoothStr;
        float2 _MaskFoamWidthSmooth;
        float2 _FoamSmooth;
        float _SineWaveAmplitude;
        float4 _NoiseTex_TexelSize;
        float4 _FoamNoiseTilingOffset;
        float2 _FoamNoiseSpeed;
        float4 _WaterBaseColor;
        float _RefracNormalStr;
        float _RefracNormalOffset;
        float _SidesEdgeWidth;
        float _NoiseAlpha;
        float4 _Noise01Color;
        float4 _Noise02Color;
        float _RefracStr;
        float4 _RefractTilingOffset;
        float2 _RefractSpeed;
        float4 _WaterBottomColor;
        float4 _Noise02TilingOffset;
        float _DistortNormalOffset;
        float2 _WaterColorRange;
        float2 _Noise02Speed;
        float4 _WaterTopColor;
        float4 _DistortTex_TexelSize;
        float2 _Noise02PowStr;
        float4 _DistortTilingOffset;
        float2 _Noise02Smooth;
        float2 _Noise01Speed;
        float2 _DistortSpeed;
        float4 _Noise01TilingOffset;
        float2 _Noise01PowStr;
        float2 _Noise01Smooth;
        float _DistortStr;
        float _UseRemapDistort;
        float _WaterAlpha;
        float _UseNormalDistort;
        float _DistortNormalStr;
        float _WaterColorLerp;
        float4 _RefracTex_TexelSize;
        float _SineWaveFrequency;
        float _SineWaveSpeed;
        float _SineWaveDistortStr;
        float4 _TopFoamColor;
        float2 _MaskWidthSmooth;
        float3 _SidesWidthSmoothStr;
        float _TopFoamPosition;
        float _TopFoamWidth;
        float2 _TopFoamSmooth;
        float _FoamLerp;
        float _MaskFoamLerp;
        float2 _FoamPowStr;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_DistortTex);
        SAMPLER(sampler_DistortTex);
        TEXTURE2D(_RefracTex);
        SAMPLER(sampler_RefracTex);
        float3 _ObjectPosition;
        float _ObjectRadius;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_NormalFromTexture_float(TEXTURE2D_PARAM(Texture, Sampler), float2 UV, float Offset, float Strength, out float3 Out)
        {
            Offset = pow(Offset, 3) * 0.1;
            float2 offsetU = float2(UV.x + Offset, UV.y);
            float2 offsetV = float2(UV.x, UV.y + Offset);
            float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;
            float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;
            float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;
            float3 va = float3(1, 0, (uSample - normalSample) * Strength);
            float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
            Out = normalize(cross(va, vb));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float2 _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2 = _FoamSmooth;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[0];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[1];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_B_3_Float = 0;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_A_4_Float = 0;
            float _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float;
            Unity_Add_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float);
            UnityTexture2D _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4 = IN.uv0;
            float _Split_81bacd708f454c8e8278db250d11f917_R_1_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[0];
            float _Split_81bacd708f454c8e8278db250d11f917_G_2_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[1];
            float _Split_81bacd708f454c8e8278db250d11f917_B_3_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[2];
            float _Split_81bacd708f454c8e8278db250d11f917_A_4_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[3];
            float2 _Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2 = float2(_Split_81bacd708f454c8e8278db250d11f917_R_1_Float, _Split_81bacd708f454c8e8278db250d11f917_G_2_Float);
            float4 _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4 = _FoamNoiseTilingOffset;
            float _Split_7fa49dda2d1849788afc51482698f959_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_7fa49dda2d1849788afc51482698f959_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_7fa49dda2d1849788afc51482698f959_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_7fa49dda2d1849788afc51482698f959_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2 = float2(_Split_7fa49dda2d1849788afc51482698f959_R_1_Float, _Split_7fa49dda2d1849788afc51482698f959_G_2_Float);
            float _Split_9cdfd6498d294ee08069eb99bef702b6_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2 = float2(_Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float, _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float);
            float2 _Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2 = _FoamNoiseSpeed;
            float2 _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2);
            float2 _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2);
            float2 _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2;
            Unity_Add_float2(_Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2);
            float2 _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2, _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2, _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2);
            float4 _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.tex, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.samplerstate, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2) );
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_R_4_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.r;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_G_5_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.g;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.b;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_A_7_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.a;
            float2 _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2 = _TopFoamSmooth;
            float _Split_211514ce3a224223a8f91e00d09582ca_R_1_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[0];
            float _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[1];
            float _Split_211514ce3a224223a8f91e00d09582ca_B_3_Float = 0;
            float _Split_211514ce3a224223a8f91e00d09582ca_A_4_Float = 0;
            float _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float;
            Unity_Add_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float);
            float4 _UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4 = IN.uv0;
            float _Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float = _TopFoamDistortStr;
            float _Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean = _UseNormalRefrac;
            float _Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float = _RefracStr;
            UnityTexture2D _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RefracTex);
            float4 _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4 = IN.uv0;
            float _Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[0];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[1];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_B_3_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[2];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_A_4_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[3];
            float2 _Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2 = float2(_Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float, _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float);
            float4 _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4 = _RefractTilingOffset;
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2 = float2(_Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float, _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float);
            float _Split_a2feec9f6a18404fa7513bc90ae72516_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2 = float2(_Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float, _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float);
            float2 _Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2 = _RefractSpeed;
            float2 _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2);
            float2 _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2);
            float2 _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2;
            Unity_Add_float2(_Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2);
            float2 _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2, _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2, _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2);
            float _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float = _RefracNormalOffset;
            float _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float = _RefracNormalStr;
            float3 _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate), _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2), _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float, _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float, _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3);
            float3 _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxx), _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3);
            float _Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean = _UseRemapRefrac;
            float _Float_a592706948e447c097be687bfae791c6_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2) );
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_R_4_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.r;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_G_5_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.g;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_B_6_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.b;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_A_7_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.a;
            float4 _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4;
            Unity_Add_float4((_Float_a592706948e447c097be687bfae791c6_Out_0_Float.xxxx), _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4);
            float _Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float = float(2);
            float4 _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4, (_Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float.xxxx), _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4);
            float4 _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4;
            Unity_Branch_float4(_Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean, _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4, _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4);
            float4 _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4, (_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxxx), _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4);
            float3 _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3;
            Unity_Branch_float3(_Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3, (_Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3);
            float _Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean = _UseNormalDistort;
            float _Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float = _DistortStr;
            UnityTexture2D _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_DistortTex);
            float4 _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4 = IN.uv0;
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[0];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[1];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_B_3_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[2];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_A_4_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[3];
            float2 _Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2 = float2(_Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float, _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float);
            float4 _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4 = _DistortTilingOffset;
            float _Split_3d3070089d0844319f72a05a13112d78_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_3d3070089d0844319f72a05a13112d78_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_3d3070089d0844319f72a05a13112d78_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_3d3070089d0844319f72a05a13112d78_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2 = float2(_Split_3d3070089d0844319f72a05a13112d78_R_1_Float, _Split_3d3070089d0844319f72a05a13112d78_G_2_Float);
            float _Split_c094edcf73d543cb919e5d7826a05b1e_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2 = float2(_Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float, _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float);
            float2 _Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2 = _DistortSpeed;
            float2 _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2);
            float2 _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2);
            float2 _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2;
            Unity_Add_float2(_Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2);
            float2 _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2, _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2, _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2);
            float _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float = _DistortNormalOffset;
            float _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float = _DistortNormalStr;
            float3 _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate), _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2), _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float, _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float, _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3);
            float3 _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxx), _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3);
            float _Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean = _UseRemapDistort;
            float _Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2) );
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_R_4_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.r;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_G_5_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.g;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_B_6_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.b;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_A_7_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.a;
            float4 _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4;
            Unity_Add_float4((_Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float.xxxx), _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4);
            float _Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float = float(2);
            float4 _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4, (_Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float.xxxx), _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4);
            float4 _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4;
            Unity_Branch_float4(_Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean, _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4, _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4);
            float4 _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4, (_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxxx), _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4);
            float3 _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3;
            Unity_Branch_float3(_Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3, (_Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4.xyz), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3);
            float3 _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3;
            Unity_Add_float3(_Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3);
            float3 _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3);
            float3 _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3, _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3);
            float _Split_c8e32710f8a04315886fca7a242e3c39_R_1_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[0];
            float _Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[1];
            float _Split_c8e32710f8a04315886fca7a242e3c39_B_3_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[2];
            float _Split_c8e32710f8a04315886fca7a242e3c39_A_4_Float = 0;
            float _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float = _TopFoamPosition;
            float _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float;
            Unity_Subtract_float(_Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float, _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float, _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float);
            float _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float;
            Unity_Absolute_float(_Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float, _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float);
            float _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float = _TopFoamWidth;
            float _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float;
            Unity_Subtract_float(_Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float, _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float, _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float);
            float _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float;
            Unity_OneMinus_float(_Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float);
            float _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float;
            Unity_Smoothstep_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float);
            float _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float, _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float);
            float2 _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2 = _BottomFoamSmooth;
            float _Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[0];
            float _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[1];
            float _Split_bd10349e771946a2829f2a8d3408e00d_B_3_Float = 0;
            float _Split_bd10349e771946a2829f2a8d3408e00d_A_4_Float = 0;
            float _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float;
            Unity_Add_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float);
            float _Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float = _BottomFoamDistortStr;
            float3 _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3);
            float3 _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3, _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3);
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_R_1_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[0];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[1];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_B_3_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[2];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_A_4_Float = 0;
            float _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float;
            Unity_OneMinus_float(_Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float);
            float _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float;
            Unity_Smoothstep_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float);
            float _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float;
            Unity_Add_float(_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float, _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float);
            float _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float;
            Unity_Saturate_float(_Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float, _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float);
            float2 _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2 = _FoamPowStr;
            float _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[0];
            float _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[1];
            float _Split_93b2b90a6f164ce18354940ff78c4947_B_3_Float = 0;
            float _Split_93b2b90a6f164ce18354940ff78c4947_A_4_Float = 0;
            float _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float;
            Unity_Power_float(_Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float, _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float, _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float);
            float _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float;
            Unity_Multiply_float_float(_Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float, _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float, _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float);
            float _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float;
            Unity_Saturate_float(_Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float);
            float _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float;
            Unity_Smoothstep_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float, _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float);
            float3 _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3 = _SidesWidthSmoothStr;
            float _Split_36856a9943d7412593732e444047eb04_R_1_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[0];
            float _Split_36856a9943d7412593732e444047eb04_G_2_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[1];
            float _Split_36856a9943d7412593732e444047eb04_B_3_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[2];
            float _Split_36856a9943d7412593732e444047eb04_A_4_Float = 0;
            float _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float;
            Unity_Add_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Split_36856a9943d7412593732e444047eb04_G_2_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float);
            float4 _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4 = IN.uv0;
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[0];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_G_2_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[1];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_B_3_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[2];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_A_4_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[3];
            float _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float;
            Unity_Subtract_float(_Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float, float(0.5), _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float);
            float _Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float = _SineWaveFrequency;
            float _Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float = _SineWaveDistortStr;
            float3 _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3;
            Unity_Multiply_float3_float3(_Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, (_Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float.xxx), _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3);
            float4 _UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4 = IN.uv0;
            float3 _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3;
            Unity_Add_float3(_Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3, (_UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4.xyz), _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3);
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_R_1_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[0];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[1];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_B_3_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[2];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_A_4_Float = 0;
            float _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float;
            Unity_Multiply_float_float(_Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float, _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float, _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float);
            float _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float = _SineWaveSpeed;
            float _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float);
            float _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float;
            Unity_Add_float(_Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float, _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float);
            float _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float;
            Unity_Sine_float(_Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float, _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float);
            float _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float;
            Unity_Multiply_float_float(_Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, 0.05, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float);
            float _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float, _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float);
            float _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float, 0.5, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float);
            float _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float;
            Unity_Subtract_float(_Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float, _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float);
            float _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float;
            Unity_Absolute_float(_Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float, _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float);
            float _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float;
            Unity_Subtract_float(_Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float, float(0.5), _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float);
            float _Property_e192f06194264eccbd544e066faf25eb_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float;
            Unity_Multiply_float_float(_Property_e192f06194264eccbd544e066faf25eb_Out_0_Float, 0.5, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float);
            float _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float;
            Unity_Add_float(_Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float);
            float _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float;
            Unity_Smoothstep_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float);
            float _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float, _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float);
            float _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float, _Split_36856a9943d7412593732e444047eb04_B_3_Float, _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float);
            float _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float;
            Unity_Saturate_float(_Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float);
            float _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float);
            float4 _Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBottomColor) : _WaterBottomColor;
            float4 _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterTopColor) : _WaterTopColor;
            float2 _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2 = _WaterColorRange;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[0];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[1];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_B_3_Float = 0;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_A_4_Float = 0;
            float _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float;
            Unity_Add_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float);
            float4 _UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4 = IN.uv0;
            float _Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float = _WaterColorDistortStr;
            float3 _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float.xxx), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3);
            float3 _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3;
            Unity_Add_float3((_UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4.xyz), _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3, _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3);
            float _Split_5904126eeadf4862b44d432382f26e43_R_1_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[0];
            float _Split_5904126eeadf4862b44d432382f26e43_G_2_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[1];
            float _Split_5904126eeadf4862b44d432382f26e43_B_3_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[2];
            float _Split_5904126eeadf4862b44d432382f26e43_A_4_Float = 0;
            float _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float;
            Unity_Saturate_float(_Split_5904126eeadf4862b44d432382f26e43_G_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float);
            float _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float;
            Unity_Smoothstep_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float, _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float);
            float _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float, _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float);
            float4 _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4;
            Unity_Lerp_float4(_Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4, _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4, (_Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float.xxxx), _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4);
            float _Split_5c957c23ba944f22a3599824406a1f36_R_1_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[0];
            float _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[1];
            float _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[2];
            float _Split_5c957c23ba944f22a3599824406a1f36_A_4_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[3];
            float2 _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2 = _MaskWidthSmooth;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[0];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[1];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_B_3_Float = 0;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_A_4_Float = 0;
            float _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float;
            Unity_Add_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float);
            float3 _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3 = _ObjectPosition;
            float3 _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3;
            Unity_Subtract_float3(IN.WorldSpacePosition, _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3, _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3);
            float _Split_ccebce80c1f44409877640fc36481eac_R_1_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[0];
            float _Split_ccebce80c1f44409877640fc36481eac_G_2_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[1];
            float _Split_ccebce80c1f44409877640fc36481eac_B_3_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[2];
            float _Split_ccebce80c1f44409877640fc36481eac_A_4_Float = 0;
            float _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float;
            Unity_Maximum_float(_Split_ccebce80c1f44409877640fc36481eac_G_2_Float, float(0), _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float);
            float3 _Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3 = float3(_Split_ccebce80c1f44409877640fc36481eac_R_1_Float, _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float, _Split_ccebce80c1f44409877640fc36481eac_B_3_Float);
            float _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float);
            float3 _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.WorldSpaceTangent, (_Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float.xxx), _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3);
            float3 _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3;
            Unity_Subtract_float3(_Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3, _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3, _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3);
            float _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float;
            Unity_Length_float3(_Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3, _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float);
            float _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = _ObjectRadius;
            float _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float;
            Unity_Subtract_float(_Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float, _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float);
            float _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float;
            Unity_Smoothstep_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float);
            float _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float;
            Unity_Multiply_float_float(_Split_5c957c23ba944f22a3599824406a1f36_A_4_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float);
            float _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float);
            float _Property_525f3b66b77449758f64a237743c6372_Out_0_Float = _WaterAlpha;
            float _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float, _Property_525f3b66b77449758f64a237743c6372_Out_0_Float, _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float);
            float4 _Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise01Color) : _Noise01Color;
            float2 _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2 = _NoiseMaskRange;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[0];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[1];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_B_3_Float = 0;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_A_4_Float = 0;
            float _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float;
            Unity_Add_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float);
            float4 _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4 = IN.uv0;
            float _Split_2f15213772824def9fb752d1d60e12dc_R_1_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[0];
            float _Split_2f15213772824def9fb752d1d60e12dc_G_2_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[1];
            float _Split_2f15213772824def9fb752d1d60e12dc_B_3_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[2];
            float _Split_2f15213772824def9fb752d1d60e12dc_A_4_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[3];
            float _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float;
            Unity_Saturate_float(_Split_2f15213772824def9fb752d1d60e12dc_G_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float);
            float _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float;
            Unity_Smoothstep_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float, _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float);
            float _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float, _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float);
            float2 _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2 = _Noise01Smooth;
            float _Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[0];
            float _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[1];
            float _Split_7edd56182b1b4da88913294d5de35a2d_B_3_Float = 0;
            float _Split_7edd56182b1b4da88913294d5de35a2d_A_4_Float = 0;
            float _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float;
            Unity_Add_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float);
            UnityTexture2D _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4 = IN.uv0;
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[0];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[1];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_B_3_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[2];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_A_4_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[3];
            float2 _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2 = float2(_Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float, _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float);
            float2 _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2, _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2);
            float4 _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4 = _Noise01TilingOffset;
            float _Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_76666e838777429a8e6d8162a0d7ab16_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_76666e838777429a8e6d8162a0d7ab16_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2 = float2(_Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float, _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float);
            float _Split_e5bebaba69094dca8731554a45fb3b66_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_e5bebaba69094dca8731554a45fb3b66_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2 = float2(_Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float, _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float);
            float2 _Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2 = _Noise01Speed;
            float2 _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2);
            float2 _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2);
            float2 _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2;
            Unity_Add_float2(_Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2);
            float2 _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2, _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2, _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2);
            float4 _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.tex, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.samplerstate, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2) );
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.r;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_G_5_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.g;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_B_6_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.b;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_A_7_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.a;
            float _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float, _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float);
            float2 _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2 = _Noise01PowStr;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[0];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[1];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_B_3_Float = 0;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_A_4_Float = 0;
            float _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float;
            Unity_Power_float(_Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float, _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float);
            float _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float;
            Unity_Multiply_float_float(_Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float, _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float);
            float _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float;
            Unity_Saturate_float(_Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float);
            float _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float;
            Unity_Smoothstep_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float);
            float _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float, _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float);
            float4 _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4, (_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float.xxxx), _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4);
            float4 _Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise02Color) : _Noise02Color;
            float2 _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2 = _Noise02Smooth;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[0];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[1];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_B_3_Float = 0;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_A_4_Float = 0;
            float _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float;
            Unity_Add_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float);
            UnityTexture2D _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4 = IN.uv0;
            float _Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[0];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[1];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_B_3_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[2];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_A_4_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[3];
            float2 _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2 = float2(_Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float, _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float);
            float2 _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2, _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2);
            float4 _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4 = _Noise02TilingOffset;
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2 = float2(_Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float, _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float);
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2 = float2(_Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float, _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float);
            float2 _Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2 = _Noise02Speed;
            float2 _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2);
            float2 _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2);
            float2 _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2;
            Unity_Add_float2(_Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2);
            float2 _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2, _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2, _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2);
            float4 _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.tex, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.samplerstate, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2) );
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_R_4_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.r;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.g;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_B_6_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.b;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_A_7_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.a;
            float _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float, _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float);
            float2 _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2 = _Noise02PowStr;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[0];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[1];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_B_3_Float = 0;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_A_4_Float = 0;
            float _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float;
            Unity_Power_float(_Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float, _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float);
            float _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float;
            Unity_Multiply_float_float(_Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float, _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float);
            float _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float;
            Unity_Saturate_float(_Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float);
            float _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float;
            Unity_Smoothstep_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float);
            float _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float);
            float4 _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4, (_Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float.xxxx), _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4);
            float4 _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4, _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4, _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4);
            float _Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[0];
            float _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[1];
            float _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[2];
            float _Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[3];
            float _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float);
            float _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float;
            Unity_Multiply_float_float(_Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float, _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float);
            float _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float = _NoiseAlpha;
            float _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float, _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float);
            float _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float;
            Unity_Add_float(_Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float, _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float);
            float _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float;
            Unity_Saturate_float(_Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float);
            float _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float;
            Unity_Add_float(_Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float, _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float);
            float2 _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2 = _MaskFoamWidthSmooth;
            float _Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[0];
            float _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[1];
            float _Split_0083280626cc439aa48131cbd3c99c0b_B_3_Float = 0;
            float _Split_0083280626cc439aa48131cbd3c99c0b_A_4_Float = 0;
            float _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float;
            Unity_Add_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float);
            float _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float;
            Unity_Smoothstep_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float);
            float _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float, _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float);
            float _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float;
            Unity_OneMinus_float(_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float);
            float _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float;
            Unity_Add_float(_Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float, _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float);
            float3 _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3 = _EdgeWidthSmoothStr;
            float _Split_455b73b72b364d559a420ef8b3344938_R_1_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[0];
            float _Split_455b73b72b364d559a420ef8b3344938_G_2_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[1];
            float _Split_455b73b72b364d559a420ef8b3344938_B_3_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[2];
            float _Split_455b73b72b364d559a420ef8b3344938_A_4_Float = 0;
            float _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float;
            Unity_Add_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Split_455b73b72b364d559a420ef8b3344938_G_2_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float);
            float _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float;
            Unity_Smoothstep_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float);
            float _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float, _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float);
            float _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float, _Split_455b73b72b364d559a420ef8b3344938_B_3_Float, _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float);
            float _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float;
            Unity_Saturate_float(_Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float, _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float);
            float _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float;
            Unity_OneMinus_float(_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float);
            float _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float;
            Unity_Add_float(_Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float, _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float);
            float _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float;
            Unity_Saturate_float(_Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float, _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float);
            float _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float);
            float _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float, _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float);
            float _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            Unity_Saturate_float(_Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float, _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float);
            surface.Alpha = _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
            // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull [_Cull]
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceTangent;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float3 positionWS : INTERP2;
             float3 normalWS : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _BottomFoamColor;
        float4 _MaskFoamColor;
        float _UseNormalRefrac;
        float _BottomFoamDistortStr;
        float _UseRemapRefrac;
        float _WaterColorDistortStr;
        float2 _NoiseMaskRange;
        float _TopFoamDistortStr;
        float2 _BottomFoamSmooth;
        float3 _EdgeWidthSmoothStr;
        float2 _MaskFoamWidthSmooth;
        float2 _FoamSmooth;
        float _SineWaveAmplitude;
        float4 _NoiseTex_TexelSize;
        float4 _FoamNoiseTilingOffset;
        float2 _FoamNoiseSpeed;
        float4 _WaterBaseColor;
        float _RefracNormalStr;
        float _RefracNormalOffset;
        float _SidesEdgeWidth;
        float _NoiseAlpha;
        float4 _Noise01Color;
        float4 _Noise02Color;
        float _RefracStr;
        float4 _RefractTilingOffset;
        float2 _RefractSpeed;
        float4 _WaterBottomColor;
        float4 _Noise02TilingOffset;
        float _DistortNormalOffset;
        float2 _WaterColorRange;
        float2 _Noise02Speed;
        float4 _WaterTopColor;
        float4 _DistortTex_TexelSize;
        float2 _Noise02PowStr;
        float4 _DistortTilingOffset;
        float2 _Noise02Smooth;
        float2 _Noise01Speed;
        float2 _DistortSpeed;
        float4 _Noise01TilingOffset;
        float2 _Noise01PowStr;
        float2 _Noise01Smooth;
        float _DistortStr;
        float _UseRemapDistort;
        float _WaterAlpha;
        float _UseNormalDistort;
        float _DistortNormalStr;
        float _WaterColorLerp;
        float4 _RefracTex_TexelSize;
        float _SineWaveFrequency;
        float _SineWaveSpeed;
        float _SineWaveDistortStr;
        float4 _TopFoamColor;
        float2 _MaskWidthSmooth;
        float3 _SidesWidthSmoothStr;
        float _TopFoamPosition;
        float _TopFoamWidth;
        float2 _TopFoamSmooth;
        float _FoamLerp;
        float _MaskFoamLerp;
        float2 _FoamPowStr;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_DistortTex);
        SAMPLER(sampler_DistortTex);
        TEXTURE2D(_RefracTex);
        SAMPLER(sampler_RefracTex);
        float3 _ObjectPosition;
        float _ObjectRadius;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_NormalFromTexture_float(TEXTURE2D_PARAM(Texture, Sampler), float2 UV, float Offset, float Strength, out float3 Out)
        {
            Offset = pow(Offset, 3) * 0.1;
            float2 offsetU = float2(UV.x + Offset, UV.y);
            float2 offsetV = float2(UV.x, UV.y + Offset);
            float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;
            float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;
            float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;
            float3 va = float3(1, 0, (uSample - normalSample) * Strength);
            float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
            Out = normalize(cross(va, vb));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float2 _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2 = _FoamSmooth;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[0];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[1];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_B_3_Float = 0;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_A_4_Float = 0;
            float _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float;
            Unity_Add_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float);
            UnityTexture2D _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4 = IN.uv0;
            float _Split_81bacd708f454c8e8278db250d11f917_R_1_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[0];
            float _Split_81bacd708f454c8e8278db250d11f917_G_2_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[1];
            float _Split_81bacd708f454c8e8278db250d11f917_B_3_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[2];
            float _Split_81bacd708f454c8e8278db250d11f917_A_4_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[3];
            float2 _Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2 = float2(_Split_81bacd708f454c8e8278db250d11f917_R_1_Float, _Split_81bacd708f454c8e8278db250d11f917_G_2_Float);
            float4 _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4 = _FoamNoiseTilingOffset;
            float _Split_7fa49dda2d1849788afc51482698f959_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_7fa49dda2d1849788afc51482698f959_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_7fa49dda2d1849788afc51482698f959_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_7fa49dda2d1849788afc51482698f959_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2 = float2(_Split_7fa49dda2d1849788afc51482698f959_R_1_Float, _Split_7fa49dda2d1849788afc51482698f959_G_2_Float);
            float _Split_9cdfd6498d294ee08069eb99bef702b6_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2 = float2(_Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float, _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float);
            float2 _Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2 = _FoamNoiseSpeed;
            float2 _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2);
            float2 _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2);
            float2 _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2;
            Unity_Add_float2(_Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2);
            float2 _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2, _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2, _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2);
            float4 _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.tex, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.samplerstate, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2) );
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_R_4_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.r;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_G_5_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.g;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.b;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_A_7_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.a;
            float2 _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2 = _TopFoamSmooth;
            float _Split_211514ce3a224223a8f91e00d09582ca_R_1_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[0];
            float _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[1];
            float _Split_211514ce3a224223a8f91e00d09582ca_B_3_Float = 0;
            float _Split_211514ce3a224223a8f91e00d09582ca_A_4_Float = 0;
            float _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float;
            Unity_Add_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float);
            float4 _UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4 = IN.uv0;
            float _Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float = _TopFoamDistortStr;
            float _Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean = _UseNormalRefrac;
            float _Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float = _RefracStr;
            UnityTexture2D _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RefracTex);
            float4 _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4 = IN.uv0;
            float _Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[0];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[1];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_B_3_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[2];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_A_4_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[3];
            float2 _Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2 = float2(_Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float, _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float);
            float4 _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4 = _RefractTilingOffset;
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2 = float2(_Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float, _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float);
            float _Split_a2feec9f6a18404fa7513bc90ae72516_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2 = float2(_Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float, _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float);
            float2 _Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2 = _RefractSpeed;
            float2 _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2);
            float2 _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2);
            float2 _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2;
            Unity_Add_float2(_Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2);
            float2 _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2, _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2, _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2);
            float _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float = _RefracNormalOffset;
            float _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float = _RefracNormalStr;
            float3 _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate), _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2), _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float, _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float, _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3);
            float3 _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxx), _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3);
            float _Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean = _UseRemapRefrac;
            float _Float_a592706948e447c097be687bfae791c6_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2) );
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_R_4_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.r;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_G_5_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.g;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_B_6_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.b;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_A_7_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.a;
            float4 _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4;
            Unity_Add_float4((_Float_a592706948e447c097be687bfae791c6_Out_0_Float.xxxx), _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4);
            float _Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float = float(2);
            float4 _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4, (_Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float.xxxx), _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4);
            float4 _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4;
            Unity_Branch_float4(_Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean, _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4, _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4);
            float4 _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4, (_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxxx), _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4);
            float3 _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3;
            Unity_Branch_float3(_Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3, (_Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3);
            float _Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean = _UseNormalDistort;
            float _Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float = _DistortStr;
            UnityTexture2D _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_DistortTex);
            float4 _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4 = IN.uv0;
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[0];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[1];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_B_3_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[2];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_A_4_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[3];
            float2 _Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2 = float2(_Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float, _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float);
            float4 _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4 = _DistortTilingOffset;
            float _Split_3d3070089d0844319f72a05a13112d78_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_3d3070089d0844319f72a05a13112d78_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_3d3070089d0844319f72a05a13112d78_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_3d3070089d0844319f72a05a13112d78_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2 = float2(_Split_3d3070089d0844319f72a05a13112d78_R_1_Float, _Split_3d3070089d0844319f72a05a13112d78_G_2_Float);
            float _Split_c094edcf73d543cb919e5d7826a05b1e_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2 = float2(_Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float, _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float);
            float2 _Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2 = _DistortSpeed;
            float2 _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2);
            float2 _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2);
            float2 _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2;
            Unity_Add_float2(_Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2);
            float2 _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2, _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2, _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2);
            float _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float = _DistortNormalOffset;
            float _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float = _DistortNormalStr;
            float3 _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate), _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2), _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float, _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float, _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3);
            float3 _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxx), _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3);
            float _Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean = _UseRemapDistort;
            float _Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2) );
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_R_4_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.r;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_G_5_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.g;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_B_6_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.b;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_A_7_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.a;
            float4 _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4;
            Unity_Add_float4((_Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float.xxxx), _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4);
            float _Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float = float(2);
            float4 _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4, (_Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float.xxxx), _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4);
            float4 _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4;
            Unity_Branch_float4(_Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean, _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4, _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4);
            float4 _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4, (_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxxx), _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4);
            float3 _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3;
            Unity_Branch_float3(_Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3, (_Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4.xyz), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3);
            float3 _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3;
            Unity_Add_float3(_Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3);
            float3 _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3);
            float3 _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3, _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3);
            float _Split_c8e32710f8a04315886fca7a242e3c39_R_1_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[0];
            float _Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[1];
            float _Split_c8e32710f8a04315886fca7a242e3c39_B_3_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[2];
            float _Split_c8e32710f8a04315886fca7a242e3c39_A_4_Float = 0;
            float _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float = _TopFoamPosition;
            float _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float;
            Unity_Subtract_float(_Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float, _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float, _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float);
            float _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float;
            Unity_Absolute_float(_Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float, _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float);
            float _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float = _TopFoamWidth;
            float _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float;
            Unity_Subtract_float(_Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float, _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float, _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float);
            float _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float;
            Unity_OneMinus_float(_Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float);
            float _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float;
            Unity_Smoothstep_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float);
            float _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float, _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float);
            float2 _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2 = _BottomFoamSmooth;
            float _Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[0];
            float _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[1];
            float _Split_bd10349e771946a2829f2a8d3408e00d_B_3_Float = 0;
            float _Split_bd10349e771946a2829f2a8d3408e00d_A_4_Float = 0;
            float _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float;
            Unity_Add_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float);
            float _Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float = _BottomFoamDistortStr;
            float3 _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3);
            float3 _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3, _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3);
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_R_1_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[0];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[1];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_B_3_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[2];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_A_4_Float = 0;
            float _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float;
            Unity_OneMinus_float(_Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float);
            float _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float;
            Unity_Smoothstep_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float);
            float _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float;
            Unity_Add_float(_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float, _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float);
            float _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float;
            Unity_Saturate_float(_Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float, _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float);
            float2 _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2 = _FoamPowStr;
            float _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[0];
            float _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[1];
            float _Split_93b2b90a6f164ce18354940ff78c4947_B_3_Float = 0;
            float _Split_93b2b90a6f164ce18354940ff78c4947_A_4_Float = 0;
            float _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float;
            Unity_Power_float(_Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float, _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float, _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float);
            float _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float;
            Unity_Multiply_float_float(_Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float, _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float, _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float);
            float _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float;
            Unity_Saturate_float(_Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float);
            float _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float;
            Unity_Smoothstep_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float, _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float);
            float3 _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3 = _SidesWidthSmoothStr;
            float _Split_36856a9943d7412593732e444047eb04_R_1_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[0];
            float _Split_36856a9943d7412593732e444047eb04_G_2_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[1];
            float _Split_36856a9943d7412593732e444047eb04_B_3_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[2];
            float _Split_36856a9943d7412593732e444047eb04_A_4_Float = 0;
            float _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float;
            Unity_Add_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Split_36856a9943d7412593732e444047eb04_G_2_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float);
            float4 _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4 = IN.uv0;
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[0];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_G_2_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[1];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_B_3_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[2];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_A_4_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[3];
            float _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float;
            Unity_Subtract_float(_Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float, float(0.5), _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float);
            float _Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float = _SineWaveFrequency;
            float _Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float = _SineWaveDistortStr;
            float3 _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3;
            Unity_Multiply_float3_float3(_Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, (_Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float.xxx), _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3);
            float4 _UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4 = IN.uv0;
            float3 _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3;
            Unity_Add_float3(_Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3, (_UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4.xyz), _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3);
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_R_1_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[0];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[1];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_B_3_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[2];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_A_4_Float = 0;
            float _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float;
            Unity_Multiply_float_float(_Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float, _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float, _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float);
            float _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float = _SineWaveSpeed;
            float _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float);
            float _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float;
            Unity_Add_float(_Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float, _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float);
            float _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float;
            Unity_Sine_float(_Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float, _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float);
            float _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float;
            Unity_Multiply_float_float(_Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, 0.05, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float);
            float _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float, _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float);
            float _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float, 0.5, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float);
            float _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float;
            Unity_Subtract_float(_Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float, _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float);
            float _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float;
            Unity_Absolute_float(_Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float, _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float);
            float _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float;
            Unity_Subtract_float(_Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float, float(0.5), _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float);
            float _Property_e192f06194264eccbd544e066faf25eb_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float;
            Unity_Multiply_float_float(_Property_e192f06194264eccbd544e066faf25eb_Out_0_Float, 0.5, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float);
            float _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float;
            Unity_Add_float(_Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float);
            float _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float;
            Unity_Smoothstep_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float);
            float _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float, _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float);
            float _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float, _Split_36856a9943d7412593732e444047eb04_B_3_Float, _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float);
            float _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float;
            Unity_Saturate_float(_Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float);
            float _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float);
            float4 _Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBottomColor) : _WaterBottomColor;
            float4 _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterTopColor) : _WaterTopColor;
            float2 _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2 = _WaterColorRange;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[0];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[1];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_B_3_Float = 0;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_A_4_Float = 0;
            float _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float;
            Unity_Add_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float);
            float4 _UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4 = IN.uv0;
            float _Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float = _WaterColorDistortStr;
            float3 _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float.xxx), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3);
            float3 _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3;
            Unity_Add_float3((_UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4.xyz), _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3, _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3);
            float _Split_5904126eeadf4862b44d432382f26e43_R_1_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[0];
            float _Split_5904126eeadf4862b44d432382f26e43_G_2_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[1];
            float _Split_5904126eeadf4862b44d432382f26e43_B_3_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[2];
            float _Split_5904126eeadf4862b44d432382f26e43_A_4_Float = 0;
            float _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float;
            Unity_Saturate_float(_Split_5904126eeadf4862b44d432382f26e43_G_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float);
            float _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float;
            Unity_Smoothstep_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float, _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float);
            float _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float, _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float);
            float4 _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4;
            Unity_Lerp_float4(_Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4, _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4, (_Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float.xxxx), _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4);
            float _Split_5c957c23ba944f22a3599824406a1f36_R_1_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[0];
            float _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[1];
            float _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[2];
            float _Split_5c957c23ba944f22a3599824406a1f36_A_4_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[3];
            float2 _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2 = _MaskWidthSmooth;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[0];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[1];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_B_3_Float = 0;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_A_4_Float = 0;
            float _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float;
            Unity_Add_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float);
            float3 _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3 = _ObjectPosition;
            float3 _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3;
            Unity_Subtract_float3(IN.WorldSpacePosition, _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3, _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3);
            float _Split_ccebce80c1f44409877640fc36481eac_R_1_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[0];
            float _Split_ccebce80c1f44409877640fc36481eac_G_2_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[1];
            float _Split_ccebce80c1f44409877640fc36481eac_B_3_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[2];
            float _Split_ccebce80c1f44409877640fc36481eac_A_4_Float = 0;
            float _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float;
            Unity_Maximum_float(_Split_ccebce80c1f44409877640fc36481eac_G_2_Float, float(0), _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float);
            float3 _Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3 = float3(_Split_ccebce80c1f44409877640fc36481eac_R_1_Float, _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float, _Split_ccebce80c1f44409877640fc36481eac_B_3_Float);
            float _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float);
            float3 _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.WorldSpaceTangent, (_Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float.xxx), _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3);
            float3 _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3;
            Unity_Subtract_float3(_Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3, _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3, _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3);
            float _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float;
            Unity_Length_float3(_Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3, _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float);
            float _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = _ObjectRadius;
            float _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float;
            Unity_Subtract_float(_Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float, _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float);
            float _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float;
            Unity_Smoothstep_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float);
            float _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float;
            Unity_Multiply_float_float(_Split_5c957c23ba944f22a3599824406a1f36_A_4_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float);
            float _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float);
            float _Property_525f3b66b77449758f64a237743c6372_Out_0_Float = _WaterAlpha;
            float _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float, _Property_525f3b66b77449758f64a237743c6372_Out_0_Float, _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float);
            float4 _Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise01Color) : _Noise01Color;
            float2 _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2 = _NoiseMaskRange;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[0];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[1];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_B_3_Float = 0;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_A_4_Float = 0;
            float _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float;
            Unity_Add_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float);
            float4 _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4 = IN.uv0;
            float _Split_2f15213772824def9fb752d1d60e12dc_R_1_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[0];
            float _Split_2f15213772824def9fb752d1d60e12dc_G_2_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[1];
            float _Split_2f15213772824def9fb752d1d60e12dc_B_3_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[2];
            float _Split_2f15213772824def9fb752d1d60e12dc_A_4_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[3];
            float _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float;
            Unity_Saturate_float(_Split_2f15213772824def9fb752d1d60e12dc_G_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float);
            float _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float;
            Unity_Smoothstep_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float, _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float);
            float _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float, _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float);
            float2 _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2 = _Noise01Smooth;
            float _Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[0];
            float _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[1];
            float _Split_7edd56182b1b4da88913294d5de35a2d_B_3_Float = 0;
            float _Split_7edd56182b1b4da88913294d5de35a2d_A_4_Float = 0;
            float _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float;
            Unity_Add_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float);
            UnityTexture2D _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4 = IN.uv0;
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[0];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[1];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_B_3_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[2];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_A_4_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[3];
            float2 _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2 = float2(_Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float, _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float);
            float2 _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2, _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2);
            float4 _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4 = _Noise01TilingOffset;
            float _Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_76666e838777429a8e6d8162a0d7ab16_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_76666e838777429a8e6d8162a0d7ab16_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2 = float2(_Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float, _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float);
            float _Split_e5bebaba69094dca8731554a45fb3b66_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_e5bebaba69094dca8731554a45fb3b66_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2 = float2(_Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float, _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float);
            float2 _Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2 = _Noise01Speed;
            float2 _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2);
            float2 _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2);
            float2 _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2;
            Unity_Add_float2(_Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2);
            float2 _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2, _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2, _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2);
            float4 _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.tex, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.samplerstate, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2) );
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.r;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_G_5_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.g;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_B_6_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.b;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_A_7_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.a;
            float _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float, _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float);
            float2 _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2 = _Noise01PowStr;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[0];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[1];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_B_3_Float = 0;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_A_4_Float = 0;
            float _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float;
            Unity_Power_float(_Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float, _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float);
            float _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float;
            Unity_Multiply_float_float(_Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float, _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float);
            float _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float;
            Unity_Saturate_float(_Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float);
            float _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float;
            Unity_Smoothstep_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float);
            float _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float, _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float);
            float4 _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4, (_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float.xxxx), _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4);
            float4 _Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise02Color) : _Noise02Color;
            float2 _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2 = _Noise02Smooth;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[0];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[1];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_B_3_Float = 0;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_A_4_Float = 0;
            float _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float;
            Unity_Add_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float);
            UnityTexture2D _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4 = IN.uv0;
            float _Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[0];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[1];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_B_3_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[2];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_A_4_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[3];
            float2 _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2 = float2(_Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float, _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float);
            float2 _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2, _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2);
            float4 _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4 = _Noise02TilingOffset;
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2 = float2(_Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float, _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float);
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2 = float2(_Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float, _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float);
            float2 _Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2 = _Noise02Speed;
            float2 _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2);
            float2 _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2);
            float2 _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2;
            Unity_Add_float2(_Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2);
            float2 _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2, _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2, _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2);
            float4 _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.tex, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.samplerstate, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2) );
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_R_4_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.r;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.g;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_B_6_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.b;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_A_7_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.a;
            float _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float, _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float);
            float2 _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2 = _Noise02PowStr;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[0];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[1];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_B_3_Float = 0;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_A_4_Float = 0;
            float _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float;
            Unity_Power_float(_Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float, _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float);
            float _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float;
            Unity_Multiply_float_float(_Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float, _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float);
            float _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float;
            Unity_Saturate_float(_Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float);
            float _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float;
            Unity_Smoothstep_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float);
            float _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float);
            float4 _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4, (_Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float.xxxx), _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4);
            float4 _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4, _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4, _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4);
            float _Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[0];
            float _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[1];
            float _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[2];
            float _Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[3];
            float _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float);
            float _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float;
            Unity_Multiply_float_float(_Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float, _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float);
            float _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float = _NoiseAlpha;
            float _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float, _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float);
            float _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float;
            Unity_Add_float(_Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float, _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float);
            float _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float;
            Unity_Saturate_float(_Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float);
            float _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float;
            Unity_Add_float(_Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float, _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float);
            float2 _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2 = _MaskFoamWidthSmooth;
            float _Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[0];
            float _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[1];
            float _Split_0083280626cc439aa48131cbd3c99c0b_B_3_Float = 0;
            float _Split_0083280626cc439aa48131cbd3c99c0b_A_4_Float = 0;
            float _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float;
            Unity_Add_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float);
            float _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float;
            Unity_Smoothstep_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float);
            float _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float, _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float);
            float _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float;
            Unity_OneMinus_float(_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float);
            float _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float;
            Unity_Add_float(_Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float, _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float);
            float3 _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3 = _EdgeWidthSmoothStr;
            float _Split_455b73b72b364d559a420ef8b3344938_R_1_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[0];
            float _Split_455b73b72b364d559a420ef8b3344938_G_2_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[1];
            float _Split_455b73b72b364d559a420ef8b3344938_B_3_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[2];
            float _Split_455b73b72b364d559a420ef8b3344938_A_4_Float = 0;
            float _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float;
            Unity_Add_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Split_455b73b72b364d559a420ef8b3344938_G_2_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float);
            float _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float;
            Unity_Smoothstep_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float);
            float _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float, _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float);
            float _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float, _Split_455b73b72b364d559a420ef8b3344938_B_3_Float, _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float);
            float _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float;
            Unity_Saturate_float(_Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float, _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float);
            float _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float;
            Unity_OneMinus_float(_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float);
            float _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float;
            Unity_Add_float(_Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float, _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float);
            float _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float;
            Unity_Saturate_float(_Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float, _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float);
            float _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float);
            float _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float, _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float);
            float _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            Unity_Saturate_float(_Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float, _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float);
            surface.Alpha = _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
            // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }
        
        // Render State
        Cull [_Cull]
        Blend [_SrcBlend] [_DstBlend]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
        #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_GBUFFER
        #define REQUIRE_OPAQUE_TEXTURE
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceTangent;
             float3 WorldSpacePosition;
             float2 NDCPosition;
             float2 PixelPosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP0;
            #endif
             float4 tangentWS : INTERP1;
             float4 texCoord0 : INTERP2;
             float3 positionWS : INTERP3;
             float3 normalWS : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _BottomFoamColor;
        float4 _MaskFoamColor;
        float _UseNormalRefrac;
        float _BottomFoamDistortStr;
        float _UseRemapRefrac;
        float _WaterColorDistortStr;
        float2 _NoiseMaskRange;
        float _TopFoamDistortStr;
        float2 _BottomFoamSmooth;
        float3 _EdgeWidthSmoothStr;
        float2 _MaskFoamWidthSmooth;
        float2 _FoamSmooth;
        float _SineWaveAmplitude;
        float4 _NoiseTex_TexelSize;
        float4 _FoamNoiseTilingOffset;
        float2 _FoamNoiseSpeed;
        float4 _WaterBaseColor;
        float _RefracNormalStr;
        float _RefracNormalOffset;
        float _SidesEdgeWidth;
        float _NoiseAlpha;
        float4 _Noise01Color;
        float4 _Noise02Color;
        float _RefracStr;
        float4 _RefractTilingOffset;
        float2 _RefractSpeed;
        float4 _WaterBottomColor;
        float4 _Noise02TilingOffset;
        float _DistortNormalOffset;
        float2 _WaterColorRange;
        float2 _Noise02Speed;
        float4 _WaterTopColor;
        float4 _DistortTex_TexelSize;
        float2 _Noise02PowStr;
        float4 _DistortTilingOffset;
        float2 _Noise02Smooth;
        float2 _Noise01Speed;
        float2 _DistortSpeed;
        float4 _Noise01TilingOffset;
        float2 _Noise01PowStr;
        float2 _Noise01Smooth;
        float _DistortStr;
        float _UseRemapDistort;
        float _WaterAlpha;
        float _UseNormalDistort;
        float _DistortNormalStr;
        float _WaterColorLerp;
        float4 _RefracTex_TexelSize;
        float _SineWaveFrequency;
        float _SineWaveSpeed;
        float _SineWaveDistortStr;
        float4 _TopFoamColor;
        float2 _MaskWidthSmooth;
        float3 _SidesWidthSmoothStr;
        float _TopFoamPosition;
        float _TopFoamWidth;
        float2 _TopFoamSmooth;
        float _FoamLerp;
        float _MaskFoamLerp;
        float2 _FoamPowStr;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_DistortTex);
        SAMPLER(sampler_DistortTex);
        TEXTURE2D(_RefracTex);
        SAMPLER(sampler_RefracTex);
        float3 _ObjectPosition;
        float _ObjectRadius;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_NormalFromTexture_float(TEXTURE2D_PARAM(Texture, Sampler), float2 UV, float Offset, float Strength, out float3 Out)
        {
            Offset = pow(Offset, 3) * 0.1;
            float2 offsetU = float2(UV.x + Offset, UV.y);
            float2 offsetV = float2(UV.x, UV.y + Offset);
            float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;
            float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;
            float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;
            float3 va = float3(1, 0, (uSample - normalSample) * Strength);
            float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
            Out = normalize(cross(va, vb));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_SceneColor_float(float4 UV, out float3 Out)
        {
            Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _Property_e780a9446a2741e38792b7f25b69d454_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBaseColor) : _WaterBaseColor;
            float4 _ScreenPosition_e8b3cecfe986409384ece71af174b90a_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
            float _Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean = _UseNormalRefrac;
            float _Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float = _RefracStr;
            UnityTexture2D _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RefracTex);
            float4 _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4 = IN.uv0;
            float _Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[0];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[1];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_B_3_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[2];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_A_4_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[3];
            float2 _Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2 = float2(_Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float, _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float);
            float4 _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4 = _RefractTilingOffset;
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2 = float2(_Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float, _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float);
            float _Split_a2feec9f6a18404fa7513bc90ae72516_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2 = float2(_Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float, _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float);
            float2 _Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2 = _RefractSpeed;
            float2 _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2);
            float2 _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2);
            float2 _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2;
            Unity_Add_float2(_Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2);
            float2 _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2, _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2, _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2);
            float _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float = _RefracNormalOffset;
            float _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float = _RefracNormalStr;
            float3 _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate), _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2), _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float, _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float, _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3);
            float3 _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxx), _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3);
            float _Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean = _UseRemapRefrac;
            float _Float_a592706948e447c097be687bfae791c6_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2) );
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_R_4_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.r;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_G_5_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.g;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_B_6_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.b;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_A_7_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.a;
            float4 _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4;
            Unity_Add_float4((_Float_a592706948e447c097be687bfae791c6_Out_0_Float.xxxx), _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4);
            float _Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float = float(2);
            float4 _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4, (_Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float.xxxx), _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4);
            float4 _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4;
            Unity_Branch_float4(_Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean, _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4, _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4);
            float4 _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4, (_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxxx), _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4);
            float3 _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3;
            Unity_Branch_float3(_Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3, (_Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3);
            float3 _Add_c508082f79a344b9a5321986d84b3ed0_Out_2_Vector3;
            Unity_Add_float3((_ScreenPosition_e8b3cecfe986409384ece71af174b90a_Out_0_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Add_c508082f79a344b9a5321986d84b3ed0_Out_2_Vector3);
            float3 _SceneColor_981130b00a36430caf0945b493652bd4_Out_1_Vector3;
            Unity_SceneColor_float((float4(_Add_c508082f79a344b9a5321986d84b3ed0_Out_2_Vector3, 1.0)), _SceneColor_981130b00a36430caf0945b493652bd4_Out_1_Vector3);
            float3 _Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_e780a9446a2741e38792b7f25b69d454_Out_0_Vector4.xyz), _SceneColor_981130b00a36430caf0945b493652bd4_Out_1_Vector3, _Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3);
            float4 _Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBottomColor) : _WaterBottomColor;
            float4 _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterTopColor) : _WaterTopColor;
            float2 _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2 = _WaterColorRange;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[0];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[1];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_B_3_Float = 0;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_A_4_Float = 0;
            float _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float;
            Unity_Add_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float);
            float4 _UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4 = IN.uv0;
            float _Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float = _WaterColorDistortStr;
            float _Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean = _UseNormalDistort;
            float _Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float = _DistortStr;
            UnityTexture2D _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_DistortTex);
            float4 _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4 = IN.uv0;
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[0];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[1];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_B_3_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[2];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_A_4_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[3];
            float2 _Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2 = float2(_Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float, _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float);
            float4 _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4 = _DistortTilingOffset;
            float _Split_3d3070089d0844319f72a05a13112d78_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_3d3070089d0844319f72a05a13112d78_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_3d3070089d0844319f72a05a13112d78_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_3d3070089d0844319f72a05a13112d78_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2 = float2(_Split_3d3070089d0844319f72a05a13112d78_R_1_Float, _Split_3d3070089d0844319f72a05a13112d78_G_2_Float);
            float _Split_c094edcf73d543cb919e5d7826a05b1e_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2 = float2(_Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float, _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float);
            float2 _Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2 = _DistortSpeed;
            float2 _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2);
            float2 _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2);
            float2 _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2;
            Unity_Add_float2(_Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2);
            float2 _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2, _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2, _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2);
            float _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float = _DistortNormalOffset;
            float _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float = _DistortNormalStr;
            float3 _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate), _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2), _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float, _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float, _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3);
            float3 _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxx), _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3);
            float _Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean = _UseRemapDistort;
            float _Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2) );
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_R_4_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.r;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_G_5_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.g;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_B_6_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.b;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_A_7_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.a;
            float4 _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4;
            Unity_Add_float4((_Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float.xxxx), _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4);
            float _Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float = float(2);
            float4 _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4, (_Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float.xxxx), _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4);
            float4 _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4;
            Unity_Branch_float4(_Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean, _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4, _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4);
            float4 _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4, (_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxxx), _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4);
            float3 _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3;
            Unity_Branch_float3(_Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3, (_Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4.xyz), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3);
            float3 _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float.xxx), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3);
            float3 _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3;
            Unity_Add_float3((_UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4.xyz), _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3, _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3);
            float _Split_5904126eeadf4862b44d432382f26e43_R_1_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[0];
            float _Split_5904126eeadf4862b44d432382f26e43_G_2_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[1];
            float _Split_5904126eeadf4862b44d432382f26e43_B_3_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[2];
            float _Split_5904126eeadf4862b44d432382f26e43_A_4_Float = 0;
            float _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float;
            Unity_Saturate_float(_Split_5904126eeadf4862b44d432382f26e43_G_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float);
            float _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float;
            Unity_Smoothstep_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float, _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float);
            float _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float, _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float);
            float4 _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4;
            Unity_Lerp_float4(_Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4, _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4, (_Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float.xxxx), _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4);
            float _Split_5c957c23ba944f22a3599824406a1f36_R_1_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[0];
            float _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[1];
            float _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[2];
            float _Split_5c957c23ba944f22a3599824406a1f36_A_4_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[3];
            float3 _Vector3_3bbe8fbcce7942e9a916864352103c7b_Out_0_Vector3 = float3(_Split_5c957c23ba944f22a3599824406a1f36_R_1_Float, _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float, _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float);
            float3 _Add_d4a50d0c58be40b1ae39cba71d7cc26f_Out_2_Vector3;
            Unity_Add_float3(_Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3, _Vector3_3bbe8fbcce7942e9a916864352103c7b_Out_0_Vector3, _Add_d4a50d0c58be40b1ae39cba71d7cc26f_Out_2_Vector3);
            float _Property_7e0c8d20034d46c2a7106e3365b6f6d7_Out_0_Float = _WaterColorLerp;
            float3 _Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3;
            Unity_Lerp_float3(_Multiply_613bd3675a3f40a7859213ec42f1745d_Out_2_Vector3, _Add_d4a50d0c58be40b1ae39cba71d7cc26f_Out_2_Vector3, (_Property_7e0c8d20034d46c2a7106e3365b6f6d7_Out_0_Float.xxx), _Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3);
            float4 _Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise01Color) : _Noise01Color;
            float2 _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2 = _NoiseMaskRange;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[0];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[1];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_B_3_Float = 0;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_A_4_Float = 0;
            float _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float;
            Unity_Add_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float);
            float4 _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4 = IN.uv0;
            float _Split_2f15213772824def9fb752d1d60e12dc_R_1_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[0];
            float _Split_2f15213772824def9fb752d1d60e12dc_G_2_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[1];
            float _Split_2f15213772824def9fb752d1d60e12dc_B_3_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[2];
            float _Split_2f15213772824def9fb752d1d60e12dc_A_4_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[3];
            float _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float;
            Unity_Saturate_float(_Split_2f15213772824def9fb752d1d60e12dc_G_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float);
            float _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float;
            Unity_Smoothstep_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float, _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float);
            float _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float, _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float);
            float2 _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2 = _Noise01Smooth;
            float _Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[0];
            float _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[1];
            float _Split_7edd56182b1b4da88913294d5de35a2d_B_3_Float = 0;
            float _Split_7edd56182b1b4da88913294d5de35a2d_A_4_Float = 0;
            float _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float;
            Unity_Add_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float);
            UnityTexture2D _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4 = IN.uv0;
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[0];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[1];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_B_3_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[2];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_A_4_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[3];
            float2 _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2 = float2(_Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float, _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float);
            float2 _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2, _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2);
            float4 _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4 = _Noise01TilingOffset;
            float _Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_76666e838777429a8e6d8162a0d7ab16_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_76666e838777429a8e6d8162a0d7ab16_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2 = float2(_Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float, _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float);
            float _Split_e5bebaba69094dca8731554a45fb3b66_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_e5bebaba69094dca8731554a45fb3b66_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2 = float2(_Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float, _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float);
            float2 _Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2 = _Noise01Speed;
            float2 _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2);
            float2 _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2);
            float2 _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2;
            Unity_Add_float2(_Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2);
            float2 _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2, _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2, _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2);
            float4 _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.tex, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.samplerstate, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2) );
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.r;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_G_5_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.g;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_B_6_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.b;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_A_7_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.a;
            float _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float, _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float);
            float2 _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2 = _Noise01PowStr;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[0];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[1];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_B_3_Float = 0;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_A_4_Float = 0;
            float _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float;
            Unity_Power_float(_Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float, _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float);
            float _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float;
            Unity_Multiply_float_float(_Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float, _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float);
            float _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float;
            Unity_Saturate_float(_Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float);
            float _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float;
            Unity_Smoothstep_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float);
            float _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float, _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float);
            float4 _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4, (_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float.xxxx), _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4);
            float4 _Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise02Color) : _Noise02Color;
            float2 _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2 = _Noise02Smooth;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[0];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[1];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_B_3_Float = 0;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_A_4_Float = 0;
            float _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float;
            Unity_Add_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float);
            UnityTexture2D _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4 = IN.uv0;
            float _Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[0];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[1];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_B_3_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[2];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_A_4_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[3];
            float2 _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2 = float2(_Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float, _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float);
            float2 _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2, _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2);
            float4 _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4 = _Noise02TilingOffset;
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2 = float2(_Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float, _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float);
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2 = float2(_Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float, _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float);
            float2 _Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2 = _Noise02Speed;
            float2 _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2);
            float2 _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2);
            float2 _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2;
            Unity_Add_float2(_Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2);
            float2 _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2, _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2, _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2);
            float4 _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.tex, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.samplerstate, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2) );
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_R_4_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.r;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.g;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_B_6_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.b;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_A_7_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.a;
            float _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float, _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float);
            float2 _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2 = _Noise02PowStr;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[0];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[1];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_B_3_Float = 0;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_A_4_Float = 0;
            float _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float;
            Unity_Power_float(_Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float, _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float);
            float _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float;
            Unity_Multiply_float_float(_Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float, _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float);
            float _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float;
            Unity_Saturate_float(_Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float);
            float _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float;
            Unity_Smoothstep_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float);
            float _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float);
            float4 _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4, (_Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float.xxxx), _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4);
            float4 _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4, _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4, _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4);
            float _Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[0];
            float _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[1];
            float _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[2];
            float _Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[3];
            float3 _Vector3_7b07222035a943c29d95ac1bd4a96152_Out_0_Vector3 = float3(_Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float, _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float, _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float);
            float3 _Add_5ec00f71abb1420684280658e02df427_Out_2_Vector3;
            Unity_Add_float3(_Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3, _Vector3_7b07222035a943c29d95ac1bd4a96152_Out_0_Vector3, _Add_5ec00f71abb1420684280658e02df427_Out_2_Vector3);
            float _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float = _NoiseAlpha;
            float3 _Lerp_d45ba3f9a27445598409aebc9cfd8fde_Out_3_Vector3;
            Unity_Lerp_float3(_Lerp_04c034fcbc1f4910a135e6d91d35570a_Out_3_Vector3, _Add_5ec00f71abb1420684280658e02df427_Out_2_Vector3, (_Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float.xxx), _Lerp_d45ba3f9a27445598409aebc9cfd8fde_Out_3_Vector3);
            float4 _Property_782c1c56767f4cdf9002f6ee96b2737a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_TopFoamColor) : _TopFoamColor;
            UnityTexture2D _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4 = IN.uv0;
            float _Split_81bacd708f454c8e8278db250d11f917_R_1_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[0];
            float _Split_81bacd708f454c8e8278db250d11f917_G_2_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[1];
            float _Split_81bacd708f454c8e8278db250d11f917_B_3_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[2];
            float _Split_81bacd708f454c8e8278db250d11f917_A_4_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[3];
            float2 _Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2 = float2(_Split_81bacd708f454c8e8278db250d11f917_R_1_Float, _Split_81bacd708f454c8e8278db250d11f917_G_2_Float);
            float4 _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4 = _FoamNoiseTilingOffset;
            float _Split_7fa49dda2d1849788afc51482698f959_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_7fa49dda2d1849788afc51482698f959_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_7fa49dda2d1849788afc51482698f959_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_7fa49dda2d1849788afc51482698f959_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2 = float2(_Split_7fa49dda2d1849788afc51482698f959_R_1_Float, _Split_7fa49dda2d1849788afc51482698f959_G_2_Float);
            float _Split_9cdfd6498d294ee08069eb99bef702b6_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2 = float2(_Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float, _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float);
            float2 _Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2 = _FoamNoiseSpeed;
            float2 _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2);
            float2 _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2);
            float2 _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2;
            Unity_Add_float2(_Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2);
            float2 _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2, _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2, _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2);
            float4 _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.tex, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.samplerstate, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2) );
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_R_4_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.r;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_G_5_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.g;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.b;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_A_7_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.a;
            float2 _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2 = _TopFoamSmooth;
            float _Split_211514ce3a224223a8f91e00d09582ca_R_1_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[0];
            float _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[1];
            float _Split_211514ce3a224223a8f91e00d09582ca_B_3_Float = 0;
            float _Split_211514ce3a224223a8f91e00d09582ca_A_4_Float = 0;
            float _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float;
            Unity_Add_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float);
            float4 _UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4 = IN.uv0;
            float _Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float = _TopFoamDistortStr;
            float3 _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3;
            Unity_Add_float3(_Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3);
            float3 _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3);
            float3 _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3, _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3);
            float _Split_c8e32710f8a04315886fca7a242e3c39_R_1_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[0];
            float _Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[1];
            float _Split_c8e32710f8a04315886fca7a242e3c39_B_3_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[2];
            float _Split_c8e32710f8a04315886fca7a242e3c39_A_4_Float = 0;
            float _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float = _TopFoamPosition;
            float _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float;
            Unity_Subtract_float(_Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float, _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float, _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float);
            float _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float;
            Unity_Absolute_float(_Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float, _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float);
            float _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float = _TopFoamWidth;
            float _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float;
            Unity_Subtract_float(_Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float, _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float, _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float);
            float _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float;
            Unity_OneMinus_float(_Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float);
            float _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float;
            Unity_Smoothstep_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float);
            float _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float, _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float);
            float4 _Lerp_f6b36a295d744335a44fd22b6d54c229_Out_3_Vector4;
            Unity_Lerp_float4(float4(1, 1, 1, 1), _Property_782c1c56767f4cdf9002f6ee96b2737a_Out_0_Vector4, (_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float.xxxx), _Lerp_f6b36a295d744335a44fd22b6d54c229_Out_3_Vector4);
            float4 _Property_be775130a97d4e24836839df9f737302_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_BottomFoamColor) : _BottomFoamColor;
            float2 _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2 = _BottomFoamSmooth;
            float _Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[0];
            float _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[1];
            float _Split_bd10349e771946a2829f2a8d3408e00d_B_3_Float = 0;
            float _Split_bd10349e771946a2829f2a8d3408e00d_A_4_Float = 0;
            float _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float;
            Unity_Add_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float);
            float _Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float = _BottomFoamDistortStr;
            float3 _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3);
            float3 _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3, _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3);
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_R_1_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[0];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[1];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_B_3_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[2];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_A_4_Float = 0;
            float _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float;
            Unity_OneMinus_float(_Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float);
            float _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float;
            Unity_Smoothstep_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float);
            float4 _Lerp_09bd43e3c00e4fe2a325afda6ae74aad_Out_3_Vector4;
            Unity_Lerp_float4(float4(1, 1, 1, 1), _Property_be775130a97d4e24836839df9f737302_Out_0_Vector4, (_Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float.xxxx), _Lerp_09bd43e3c00e4fe2a325afda6ae74aad_Out_3_Vector4);
            float4 _Multiply_8a0aa67416d74845898e4efd236cb9ac_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Lerp_f6b36a295d744335a44fd22b6d54c229_Out_3_Vector4, _Lerp_09bd43e3c00e4fe2a325afda6ae74aad_Out_3_Vector4, _Multiply_8a0aa67416d74845898e4efd236cb9ac_Out_2_Vector4);
            float2 _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2 = _FoamSmooth;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[0];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[1];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_B_3_Float = 0;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_A_4_Float = 0;
            float _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float;
            Unity_Add_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float);
            float _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float;
            Unity_Add_float(_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float, _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float);
            float _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float;
            Unity_Saturate_float(_Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float, _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float);
            float2 _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2 = _FoamPowStr;
            float _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[0];
            float _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[1];
            float _Split_93b2b90a6f164ce18354940ff78c4947_B_3_Float = 0;
            float _Split_93b2b90a6f164ce18354940ff78c4947_A_4_Float = 0;
            float _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float;
            Unity_Power_float(_Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float, _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float, _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float);
            float _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float;
            Unity_Multiply_float_float(_Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float, _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float, _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float);
            float _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float;
            Unity_Saturate_float(_Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float);
            float _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float;
            Unity_Smoothstep_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float, _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float);
            float _Property_1782f5c6311f4c728243ba230290ec34_Out_0_Float = _FoamLerp;
            float _Remap_a5bfe07e3cc448128114ed7682c146e3_Out_3_Float;
            Unity_Remap_float(_Property_1782f5c6311f4c728243ba230290ec34_Out_0_Float, float2 (0, 1), float2 (-1, 0), _Remap_a5bfe07e3cc448128114ed7682c146e3_Out_3_Float);
            float _Add_9bc3a31748244227a64c30048f29256f_Out_2_Float;
            Unity_Add_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Remap_a5bfe07e3cc448128114ed7682c146e3_Out_3_Float, _Add_9bc3a31748244227a64c30048f29256f_Out_2_Float);
            float _Saturate_0aff1362ce964ee28b33749c12c627e9_Out_1_Float;
            Unity_Saturate_float(_Add_9bc3a31748244227a64c30048f29256f_Out_2_Float, _Saturate_0aff1362ce964ee28b33749c12c627e9_Out_1_Float);
            float3 _Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3;
            Unity_Lerp_float3(_Lerp_d45ba3f9a27445598409aebc9cfd8fde_Out_3_Vector3, (_Multiply_8a0aa67416d74845898e4efd236cb9ac_Out_2_Vector4.xyz), (_Saturate_0aff1362ce964ee28b33749c12c627e9_Out_1_Float.xxx), _Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3);
            float4 _Property_989d344731ee4985bd672849bd49a7c3_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MaskFoamColor) : _MaskFoamColor;
            float4 _Property_35f4f97ab17f44bb924767bfb6dc4192_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MaskFoamColor) : _MaskFoamColor;
            float2 _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2 = _MaskFoamWidthSmooth;
            float _Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[0];
            float _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[1];
            float _Split_0083280626cc439aa48131cbd3c99c0b_B_3_Float = 0;
            float _Split_0083280626cc439aa48131cbd3c99c0b_A_4_Float = 0;
            float _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float;
            Unity_Add_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float);
            float3 _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3 = _ObjectPosition;
            float3 _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3;
            Unity_Subtract_float3(IN.WorldSpacePosition, _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3, _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3);
            float _Split_ccebce80c1f44409877640fc36481eac_R_1_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[0];
            float _Split_ccebce80c1f44409877640fc36481eac_G_2_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[1];
            float _Split_ccebce80c1f44409877640fc36481eac_B_3_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[2];
            float _Split_ccebce80c1f44409877640fc36481eac_A_4_Float = 0;
            float _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float;
            Unity_Maximum_float(_Split_ccebce80c1f44409877640fc36481eac_G_2_Float, float(0), _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float);
            float3 _Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3 = float3(_Split_ccebce80c1f44409877640fc36481eac_R_1_Float, _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float, _Split_ccebce80c1f44409877640fc36481eac_B_3_Float);
            float _Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float = _SineWaveFrequency;
            float _Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float = _SineWaveDistortStr;
            float3 _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3;
            Unity_Multiply_float3_float3(_Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, (_Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float.xxx), _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3);
            float4 _UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4 = IN.uv0;
            float3 _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3;
            Unity_Add_float3(_Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3, (_UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4.xyz), _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3);
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_R_1_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[0];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[1];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_B_3_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[2];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_A_4_Float = 0;
            float _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float;
            Unity_Multiply_float_float(_Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float, _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float, _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float);
            float _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float = _SineWaveSpeed;
            float _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float);
            float _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float;
            Unity_Add_float(_Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float, _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float);
            float _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float;
            Unity_Sine_float(_Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float, _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float);
            float _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float);
            float3 _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.WorldSpaceTangent, (_Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float.xxx), _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3);
            float3 _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3;
            Unity_Subtract_float3(_Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3, _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3, _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3);
            float _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float;
            Unity_Length_float3(_Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3, _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float);
            float _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = _ObjectRadius;
            float _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float;
            Unity_Subtract_float(_Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float, _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float);
            float _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float;
            Unity_Smoothstep_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float);
            float _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float, _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float);
            float3 _Lerp_6241646eb6ce4b2bb84393117ba66ae2_Out_3_Vector3;
            Unity_Lerp_float3((_Property_35f4f97ab17f44bb924767bfb6dc4192_Out_0_Vector4.xyz), _Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3, (_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float.xxx), _Lerp_6241646eb6ce4b2bb84393117ba66ae2_Out_3_Vector3);
            float3 _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3 = _EdgeWidthSmoothStr;
            float _Split_455b73b72b364d559a420ef8b3344938_R_1_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[0];
            float _Split_455b73b72b364d559a420ef8b3344938_G_2_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[1];
            float _Split_455b73b72b364d559a420ef8b3344938_B_3_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[2];
            float _Split_455b73b72b364d559a420ef8b3344938_A_4_Float = 0;
            float _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float;
            Unity_Add_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Split_455b73b72b364d559a420ef8b3344938_G_2_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float);
            float4 _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4 = IN.uv0;
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[0];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_G_2_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[1];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_B_3_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[2];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_A_4_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[3];
            float _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float;
            Unity_Subtract_float(_Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float, float(0.5), _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float);
            float _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float;
            Unity_Multiply_float_float(_Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, 0.05, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float);
            float _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float, _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float);
            float _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float, 0.5, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float);
            float _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float;
            Unity_Subtract_float(_Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float, _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float);
            float _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float;
            Unity_Absolute_float(_Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float, _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float);
            float _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float;
            Unity_Subtract_float(_Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float, float(0.5), _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float);
            float _Property_e192f06194264eccbd544e066faf25eb_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float;
            Unity_Multiply_float_float(_Property_e192f06194264eccbd544e066faf25eb_Out_0_Float, 0.5, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float);
            float _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float;
            Unity_Add_float(_Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float);
            float _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float;
            Unity_Smoothstep_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float);
            float _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float, _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float);
            float _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float, _Split_455b73b72b364d559a420ef8b3344938_B_3_Float, _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float);
            float _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float;
            Unity_Saturate_float(_Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float, _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float);
            float3 _Lerp_58bbbddec7294f3fb276f7f670dc8c28_Out_3_Vector3;
            Unity_Lerp_float3((_Property_989d344731ee4985bd672849bd49a7c3_Out_0_Vector4.xyz), _Lerp_6241646eb6ce4b2bb84393117ba66ae2_Out_3_Vector3, (_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float.xxx), _Lerp_58bbbddec7294f3fb276f7f670dc8c28_Out_3_Vector3);
            float _Property_b7755809b2f44df39015c1ad27320edc_Out_0_Float = _MaskFoamLerp;
            float3 _Lerp_d2fd6a4a86344139a2925e99d45b4ba2_Out_3_Vector3;
            Unity_Lerp_float3(_Lerp_0806e9b7f6134c86b9ade002628bd117_Out_3_Vector3, _Lerp_58bbbddec7294f3fb276f7f670dc8c28_Out_3_Vector3, (_Property_b7755809b2f44df39015c1ad27320edc_Out_0_Float.xxx), _Lerp_d2fd6a4a86344139a2925e99d45b4ba2_Out_3_Vector3);
            float3 _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3 = _SidesWidthSmoothStr;
            float _Split_36856a9943d7412593732e444047eb04_R_1_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[0];
            float _Split_36856a9943d7412593732e444047eb04_G_2_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[1];
            float _Split_36856a9943d7412593732e444047eb04_B_3_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[2];
            float _Split_36856a9943d7412593732e444047eb04_A_4_Float = 0;
            float _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float;
            Unity_Add_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Split_36856a9943d7412593732e444047eb04_G_2_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float);
            float _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float;
            Unity_Smoothstep_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float);
            float _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float, _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float);
            float _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float, _Split_36856a9943d7412593732e444047eb04_B_3_Float, _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float);
            float _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float;
            Unity_Saturate_float(_Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float);
            float _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float);
            float2 _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2 = _MaskWidthSmooth;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[0];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[1];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_B_3_Float = 0;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_A_4_Float = 0;
            float _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float;
            Unity_Add_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float);
            float _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float;
            Unity_Smoothstep_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float);
            float _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float;
            Unity_Multiply_float_float(_Split_5c957c23ba944f22a3599824406a1f36_A_4_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float);
            float _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float);
            float _Property_525f3b66b77449758f64a237743c6372_Out_0_Float = _WaterAlpha;
            float _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float, _Property_525f3b66b77449758f64a237743c6372_Out_0_Float, _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float);
            float _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float);
            float _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float;
            Unity_Multiply_float_float(_Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float, _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float);
            float _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float, _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float);
            float _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float;
            Unity_Add_float(_Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float, _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float);
            float _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float;
            Unity_Saturate_float(_Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float);
            float _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float;
            Unity_Add_float(_Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float, _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float);
            float _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float;
            Unity_OneMinus_float(_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float);
            float _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float;
            Unity_Add_float(_Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float, _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float);
            float _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float;
            Unity_OneMinus_float(_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float);
            float _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float;
            Unity_Add_float(_Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float, _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float);
            float _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float;
            Unity_Saturate_float(_Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float, _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float);
            float _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float);
            float _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float, _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float);
            float _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            Unity_Saturate_float(_Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float, _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float);
            surface.BaseColor = _Lerp_d2fd6a4a86344139a2925e99d45b4ba2_Out_3_Vector3;
            surface.Alpha = _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
            // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #else
            output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
            #endif
        
            output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
            output.NDCPosition.y = 1.0f - output.NDCPosition.y;
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceTangent;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float3 positionWS : INTERP2;
             float3 normalWS : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _BottomFoamColor;
        float4 _MaskFoamColor;
        float _UseNormalRefrac;
        float _BottomFoamDistortStr;
        float _UseRemapRefrac;
        float _WaterColorDistortStr;
        float2 _NoiseMaskRange;
        float _TopFoamDistortStr;
        float2 _BottomFoamSmooth;
        float3 _EdgeWidthSmoothStr;
        float2 _MaskFoamWidthSmooth;
        float2 _FoamSmooth;
        float _SineWaveAmplitude;
        float4 _NoiseTex_TexelSize;
        float4 _FoamNoiseTilingOffset;
        float2 _FoamNoiseSpeed;
        float4 _WaterBaseColor;
        float _RefracNormalStr;
        float _RefracNormalOffset;
        float _SidesEdgeWidth;
        float _NoiseAlpha;
        float4 _Noise01Color;
        float4 _Noise02Color;
        float _RefracStr;
        float4 _RefractTilingOffset;
        float2 _RefractSpeed;
        float4 _WaterBottomColor;
        float4 _Noise02TilingOffset;
        float _DistortNormalOffset;
        float2 _WaterColorRange;
        float2 _Noise02Speed;
        float4 _WaterTopColor;
        float4 _DistortTex_TexelSize;
        float2 _Noise02PowStr;
        float4 _DistortTilingOffset;
        float2 _Noise02Smooth;
        float2 _Noise01Speed;
        float2 _DistortSpeed;
        float4 _Noise01TilingOffset;
        float2 _Noise01PowStr;
        float2 _Noise01Smooth;
        float _DistortStr;
        float _UseRemapDistort;
        float _WaterAlpha;
        float _UseNormalDistort;
        float _DistortNormalStr;
        float _WaterColorLerp;
        float4 _RefracTex_TexelSize;
        float _SineWaveFrequency;
        float _SineWaveSpeed;
        float _SineWaveDistortStr;
        float4 _TopFoamColor;
        float2 _MaskWidthSmooth;
        float3 _SidesWidthSmoothStr;
        float _TopFoamPosition;
        float _TopFoamWidth;
        float2 _TopFoamSmooth;
        float _FoamLerp;
        float _MaskFoamLerp;
        float2 _FoamPowStr;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_DistortTex);
        SAMPLER(sampler_DistortTex);
        TEXTURE2D(_RefracTex);
        SAMPLER(sampler_RefracTex);
        float3 _ObjectPosition;
        float _ObjectRadius;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_NormalFromTexture_float(TEXTURE2D_PARAM(Texture, Sampler), float2 UV, float Offset, float Strength, out float3 Out)
        {
            Offset = pow(Offset, 3) * 0.1;
            float2 offsetU = float2(UV.x + Offset, UV.y);
            float2 offsetV = float2(UV.x, UV.y + Offset);
            float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;
            float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;
            float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;
            float3 va = float3(1, 0, (uSample - normalSample) * Strength);
            float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
            Out = normalize(cross(va, vb));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float2 _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2 = _FoamSmooth;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[0];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[1];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_B_3_Float = 0;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_A_4_Float = 0;
            float _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float;
            Unity_Add_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float);
            UnityTexture2D _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4 = IN.uv0;
            float _Split_81bacd708f454c8e8278db250d11f917_R_1_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[0];
            float _Split_81bacd708f454c8e8278db250d11f917_G_2_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[1];
            float _Split_81bacd708f454c8e8278db250d11f917_B_3_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[2];
            float _Split_81bacd708f454c8e8278db250d11f917_A_4_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[3];
            float2 _Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2 = float2(_Split_81bacd708f454c8e8278db250d11f917_R_1_Float, _Split_81bacd708f454c8e8278db250d11f917_G_2_Float);
            float4 _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4 = _FoamNoiseTilingOffset;
            float _Split_7fa49dda2d1849788afc51482698f959_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_7fa49dda2d1849788afc51482698f959_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_7fa49dda2d1849788afc51482698f959_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_7fa49dda2d1849788afc51482698f959_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2 = float2(_Split_7fa49dda2d1849788afc51482698f959_R_1_Float, _Split_7fa49dda2d1849788afc51482698f959_G_2_Float);
            float _Split_9cdfd6498d294ee08069eb99bef702b6_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2 = float2(_Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float, _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float);
            float2 _Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2 = _FoamNoiseSpeed;
            float2 _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2);
            float2 _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2);
            float2 _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2;
            Unity_Add_float2(_Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2);
            float2 _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2, _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2, _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2);
            float4 _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.tex, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.samplerstate, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2) );
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_R_4_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.r;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_G_5_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.g;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.b;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_A_7_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.a;
            float2 _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2 = _TopFoamSmooth;
            float _Split_211514ce3a224223a8f91e00d09582ca_R_1_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[0];
            float _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[1];
            float _Split_211514ce3a224223a8f91e00d09582ca_B_3_Float = 0;
            float _Split_211514ce3a224223a8f91e00d09582ca_A_4_Float = 0;
            float _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float;
            Unity_Add_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float);
            float4 _UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4 = IN.uv0;
            float _Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float = _TopFoamDistortStr;
            float _Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean = _UseNormalRefrac;
            float _Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float = _RefracStr;
            UnityTexture2D _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RefracTex);
            float4 _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4 = IN.uv0;
            float _Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[0];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[1];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_B_3_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[2];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_A_4_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[3];
            float2 _Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2 = float2(_Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float, _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float);
            float4 _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4 = _RefractTilingOffset;
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2 = float2(_Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float, _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float);
            float _Split_a2feec9f6a18404fa7513bc90ae72516_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2 = float2(_Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float, _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float);
            float2 _Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2 = _RefractSpeed;
            float2 _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2);
            float2 _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2);
            float2 _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2;
            Unity_Add_float2(_Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2);
            float2 _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2, _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2, _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2);
            float _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float = _RefracNormalOffset;
            float _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float = _RefracNormalStr;
            float3 _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate), _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2), _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float, _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float, _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3);
            float3 _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxx), _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3);
            float _Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean = _UseRemapRefrac;
            float _Float_a592706948e447c097be687bfae791c6_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2) );
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_R_4_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.r;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_G_5_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.g;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_B_6_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.b;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_A_7_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.a;
            float4 _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4;
            Unity_Add_float4((_Float_a592706948e447c097be687bfae791c6_Out_0_Float.xxxx), _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4);
            float _Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float = float(2);
            float4 _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4, (_Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float.xxxx), _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4);
            float4 _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4;
            Unity_Branch_float4(_Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean, _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4, _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4);
            float4 _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4, (_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxxx), _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4);
            float3 _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3;
            Unity_Branch_float3(_Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3, (_Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3);
            float _Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean = _UseNormalDistort;
            float _Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float = _DistortStr;
            UnityTexture2D _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_DistortTex);
            float4 _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4 = IN.uv0;
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[0];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[1];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_B_3_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[2];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_A_4_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[3];
            float2 _Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2 = float2(_Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float, _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float);
            float4 _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4 = _DistortTilingOffset;
            float _Split_3d3070089d0844319f72a05a13112d78_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_3d3070089d0844319f72a05a13112d78_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_3d3070089d0844319f72a05a13112d78_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_3d3070089d0844319f72a05a13112d78_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2 = float2(_Split_3d3070089d0844319f72a05a13112d78_R_1_Float, _Split_3d3070089d0844319f72a05a13112d78_G_2_Float);
            float _Split_c094edcf73d543cb919e5d7826a05b1e_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2 = float2(_Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float, _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float);
            float2 _Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2 = _DistortSpeed;
            float2 _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2);
            float2 _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2);
            float2 _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2;
            Unity_Add_float2(_Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2);
            float2 _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2, _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2, _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2);
            float _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float = _DistortNormalOffset;
            float _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float = _DistortNormalStr;
            float3 _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate), _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2), _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float, _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float, _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3);
            float3 _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxx), _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3);
            float _Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean = _UseRemapDistort;
            float _Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2) );
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_R_4_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.r;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_G_5_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.g;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_B_6_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.b;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_A_7_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.a;
            float4 _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4;
            Unity_Add_float4((_Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float.xxxx), _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4);
            float _Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float = float(2);
            float4 _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4, (_Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float.xxxx), _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4);
            float4 _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4;
            Unity_Branch_float4(_Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean, _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4, _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4);
            float4 _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4, (_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxxx), _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4);
            float3 _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3;
            Unity_Branch_float3(_Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3, (_Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4.xyz), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3);
            float3 _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3;
            Unity_Add_float3(_Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3);
            float3 _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3);
            float3 _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3, _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3);
            float _Split_c8e32710f8a04315886fca7a242e3c39_R_1_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[0];
            float _Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[1];
            float _Split_c8e32710f8a04315886fca7a242e3c39_B_3_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[2];
            float _Split_c8e32710f8a04315886fca7a242e3c39_A_4_Float = 0;
            float _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float = _TopFoamPosition;
            float _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float;
            Unity_Subtract_float(_Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float, _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float, _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float);
            float _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float;
            Unity_Absolute_float(_Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float, _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float);
            float _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float = _TopFoamWidth;
            float _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float;
            Unity_Subtract_float(_Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float, _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float, _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float);
            float _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float;
            Unity_OneMinus_float(_Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float);
            float _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float;
            Unity_Smoothstep_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float);
            float _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float, _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float);
            float2 _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2 = _BottomFoamSmooth;
            float _Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[0];
            float _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[1];
            float _Split_bd10349e771946a2829f2a8d3408e00d_B_3_Float = 0;
            float _Split_bd10349e771946a2829f2a8d3408e00d_A_4_Float = 0;
            float _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float;
            Unity_Add_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float);
            float _Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float = _BottomFoamDistortStr;
            float3 _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3);
            float3 _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3, _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3);
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_R_1_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[0];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[1];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_B_3_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[2];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_A_4_Float = 0;
            float _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float;
            Unity_OneMinus_float(_Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float);
            float _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float;
            Unity_Smoothstep_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float);
            float _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float;
            Unity_Add_float(_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float, _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float);
            float _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float;
            Unity_Saturate_float(_Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float, _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float);
            float2 _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2 = _FoamPowStr;
            float _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[0];
            float _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[1];
            float _Split_93b2b90a6f164ce18354940ff78c4947_B_3_Float = 0;
            float _Split_93b2b90a6f164ce18354940ff78c4947_A_4_Float = 0;
            float _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float;
            Unity_Power_float(_Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float, _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float, _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float);
            float _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float;
            Unity_Multiply_float_float(_Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float, _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float, _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float);
            float _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float;
            Unity_Saturate_float(_Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float);
            float _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float;
            Unity_Smoothstep_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float, _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float);
            float3 _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3 = _SidesWidthSmoothStr;
            float _Split_36856a9943d7412593732e444047eb04_R_1_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[0];
            float _Split_36856a9943d7412593732e444047eb04_G_2_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[1];
            float _Split_36856a9943d7412593732e444047eb04_B_3_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[2];
            float _Split_36856a9943d7412593732e444047eb04_A_4_Float = 0;
            float _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float;
            Unity_Add_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Split_36856a9943d7412593732e444047eb04_G_2_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float);
            float4 _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4 = IN.uv0;
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[0];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_G_2_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[1];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_B_3_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[2];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_A_4_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[3];
            float _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float;
            Unity_Subtract_float(_Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float, float(0.5), _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float);
            float _Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float = _SineWaveFrequency;
            float _Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float = _SineWaveDistortStr;
            float3 _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3;
            Unity_Multiply_float3_float3(_Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, (_Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float.xxx), _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3);
            float4 _UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4 = IN.uv0;
            float3 _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3;
            Unity_Add_float3(_Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3, (_UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4.xyz), _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3);
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_R_1_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[0];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[1];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_B_3_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[2];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_A_4_Float = 0;
            float _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float;
            Unity_Multiply_float_float(_Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float, _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float, _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float);
            float _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float = _SineWaveSpeed;
            float _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float);
            float _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float;
            Unity_Add_float(_Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float, _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float);
            float _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float;
            Unity_Sine_float(_Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float, _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float);
            float _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float;
            Unity_Multiply_float_float(_Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, 0.05, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float);
            float _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float, _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float);
            float _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float, 0.5, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float);
            float _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float;
            Unity_Subtract_float(_Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float, _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float);
            float _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float;
            Unity_Absolute_float(_Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float, _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float);
            float _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float;
            Unity_Subtract_float(_Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float, float(0.5), _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float);
            float _Property_e192f06194264eccbd544e066faf25eb_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float;
            Unity_Multiply_float_float(_Property_e192f06194264eccbd544e066faf25eb_Out_0_Float, 0.5, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float);
            float _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float;
            Unity_Add_float(_Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float);
            float _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float;
            Unity_Smoothstep_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float);
            float _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float, _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float);
            float _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float, _Split_36856a9943d7412593732e444047eb04_B_3_Float, _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float);
            float _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float;
            Unity_Saturate_float(_Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float);
            float _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float);
            float4 _Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBottomColor) : _WaterBottomColor;
            float4 _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterTopColor) : _WaterTopColor;
            float2 _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2 = _WaterColorRange;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[0];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[1];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_B_3_Float = 0;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_A_4_Float = 0;
            float _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float;
            Unity_Add_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float);
            float4 _UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4 = IN.uv0;
            float _Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float = _WaterColorDistortStr;
            float3 _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float.xxx), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3);
            float3 _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3;
            Unity_Add_float3((_UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4.xyz), _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3, _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3);
            float _Split_5904126eeadf4862b44d432382f26e43_R_1_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[0];
            float _Split_5904126eeadf4862b44d432382f26e43_G_2_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[1];
            float _Split_5904126eeadf4862b44d432382f26e43_B_3_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[2];
            float _Split_5904126eeadf4862b44d432382f26e43_A_4_Float = 0;
            float _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float;
            Unity_Saturate_float(_Split_5904126eeadf4862b44d432382f26e43_G_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float);
            float _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float;
            Unity_Smoothstep_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float, _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float);
            float _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float, _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float);
            float4 _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4;
            Unity_Lerp_float4(_Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4, _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4, (_Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float.xxxx), _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4);
            float _Split_5c957c23ba944f22a3599824406a1f36_R_1_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[0];
            float _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[1];
            float _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[2];
            float _Split_5c957c23ba944f22a3599824406a1f36_A_4_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[3];
            float2 _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2 = _MaskWidthSmooth;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[0];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[1];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_B_3_Float = 0;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_A_4_Float = 0;
            float _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float;
            Unity_Add_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float);
            float3 _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3 = _ObjectPosition;
            float3 _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3;
            Unity_Subtract_float3(IN.WorldSpacePosition, _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3, _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3);
            float _Split_ccebce80c1f44409877640fc36481eac_R_1_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[0];
            float _Split_ccebce80c1f44409877640fc36481eac_G_2_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[1];
            float _Split_ccebce80c1f44409877640fc36481eac_B_3_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[2];
            float _Split_ccebce80c1f44409877640fc36481eac_A_4_Float = 0;
            float _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float;
            Unity_Maximum_float(_Split_ccebce80c1f44409877640fc36481eac_G_2_Float, float(0), _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float);
            float3 _Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3 = float3(_Split_ccebce80c1f44409877640fc36481eac_R_1_Float, _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float, _Split_ccebce80c1f44409877640fc36481eac_B_3_Float);
            float _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float);
            float3 _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.WorldSpaceTangent, (_Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float.xxx), _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3);
            float3 _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3;
            Unity_Subtract_float3(_Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3, _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3, _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3);
            float _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float;
            Unity_Length_float3(_Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3, _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float);
            float _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = _ObjectRadius;
            float _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float;
            Unity_Subtract_float(_Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float, _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float);
            float _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float;
            Unity_Smoothstep_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float);
            float _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float;
            Unity_Multiply_float_float(_Split_5c957c23ba944f22a3599824406a1f36_A_4_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float);
            float _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float);
            float _Property_525f3b66b77449758f64a237743c6372_Out_0_Float = _WaterAlpha;
            float _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float, _Property_525f3b66b77449758f64a237743c6372_Out_0_Float, _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float);
            float4 _Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise01Color) : _Noise01Color;
            float2 _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2 = _NoiseMaskRange;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[0];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[1];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_B_3_Float = 0;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_A_4_Float = 0;
            float _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float;
            Unity_Add_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float);
            float4 _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4 = IN.uv0;
            float _Split_2f15213772824def9fb752d1d60e12dc_R_1_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[0];
            float _Split_2f15213772824def9fb752d1d60e12dc_G_2_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[1];
            float _Split_2f15213772824def9fb752d1d60e12dc_B_3_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[2];
            float _Split_2f15213772824def9fb752d1d60e12dc_A_4_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[3];
            float _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float;
            Unity_Saturate_float(_Split_2f15213772824def9fb752d1d60e12dc_G_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float);
            float _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float;
            Unity_Smoothstep_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float, _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float);
            float _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float, _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float);
            float2 _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2 = _Noise01Smooth;
            float _Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[0];
            float _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[1];
            float _Split_7edd56182b1b4da88913294d5de35a2d_B_3_Float = 0;
            float _Split_7edd56182b1b4da88913294d5de35a2d_A_4_Float = 0;
            float _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float;
            Unity_Add_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float);
            UnityTexture2D _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4 = IN.uv0;
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[0];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[1];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_B_3_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[2];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_A_4_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[3];
            float2 _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2 = float2(_Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float, _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float);
            float2 _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2, _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2);
            float4 _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4 = _Noise01TilingOffset;
            float _Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_76666e838777429a8e6d8162a0d7ab16_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_76666e838777429a8e6d8162a0d7ab16_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2 = float2(_Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float, _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float);
            float _Split_e5bebaba69094dca8731554a45fb3b66_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_e5bebaba69094dca8731554a45fb3b66_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2 = float2(_Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float, _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float);
            float2 _Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2 = _Noise01Speed;
            float2 _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2);
            float2 _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2);
            float2 _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2;
            Unity_Add_float2(_Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2);
            float2 _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2, _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2, _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2);
            float4 _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.tex, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.samplerstate, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2) );
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.r;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_G_5_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.g;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_B_6_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.b;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_A_7_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.a;
            float _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float, _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float);
            float2 _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2 = _Noise01PowStr;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[0];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[1];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_B_3_Float = 0;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_A_4_Float = 0;
            float _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float;
            Unity_Power_float(_Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float, _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float);
            float _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float;
            Unity_Multiply_float_float(_Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float, _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float);
            float _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float;
            Unity_Saturate_float(_Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float);
            float _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float;
            Unity_Smoothstep_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float);
            float _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float, _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float);
            float4 _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4, (_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float.xxxx), _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4);
            float4 _Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise02Color) : _Noise02Color;
            float2 _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2 = _Noise02Smooth;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[0];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[1];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_B_3_Float = 0;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_A_4_Float = 0;
            float _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float;
            Unity_Add_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float);
            UnityTexture2D _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4 = IN.uv0;
            float _Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[0];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[1];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_B_3_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[2];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_A_4_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[3];
            float2 _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2 = float2(_Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float, _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float);
            float2 _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2, _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2);
            float4 _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4 = _Noise02TilingOffset;
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2 = float2(_Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float, _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float);
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2 = float2(_Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float, _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float);
            float2 _Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2 = _Noise02Speed;
            float2 _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2);
            float2 _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2);
            float2 _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2;
            Unity_Add_float2(_Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2);
            float2 _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2, _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2, _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2);
            float4 _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.tex, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.samplerstate, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2) );
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_R_4_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.r;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.g;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_B_6_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.b;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_A_7_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.a;
            float _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float, _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float);
            float2 _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2 = _Noise02PowStr;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[0];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[1];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_B_3_Float = 0;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_A_4_Float = 0;
            float _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float;
            Unity_Power_float(_Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float, _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float);
            float _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float;
            Unity_Multiply_float_float(_Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float, _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float);
            float _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float;
            Unity_Saturate_float(_Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float);
            float _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float;
            Unity_Smoothstep_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float);
            float _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float);
            float4 _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4, (_Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float.xxxx), _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4);
            float4 _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4, _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4, _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4);
            float _Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[0];
            float _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[1];
            float _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[2];
            float _Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[3];
            float _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float);
            float _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float;
            Unity_Multiply_float_float(_Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float, _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float);
            float _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float = _NoiseAlpha;
            float _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float, _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float);
            float _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float;
            Unity_Add_float(_Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float, _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float);
            float _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float;
            Unity_Saturate_float(_Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float);
            float _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float;
            Unity_Add_float(_Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float, _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float);
            float2 _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2 = _MaskFoamWidthSmooth;
            float _Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[0];
            float _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[1];
            float _Split_0083280626cc439aa48131cbd3c99c0b_B_3_Float = 0;
            float _Split_0083280626cc439aa48131cbd3c99c0b_A_4_Float = 0;
            float _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float;
            Unity_Add_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float);
            float _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float;
            Unity_Smoothstep_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float);
            float _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float, _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float);
            float _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float;
            Unity_OneMinus_float(_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float);
            float _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float;
            Unity_Add_float(_Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float, _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float);
            float3 _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3 = _EdgeWidthSmoothStr;
            float _Split_455b73b72b364d559a420ef8b3344938_R_1_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[0];
            float _Split_455b73b72b364d559a420ef8b3344938_G_2_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[1];
            float _Split_455b73b72b364d559a420ef8b3344938_B_3_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[2];
            float _Split_455b73b72b364d559a420ef8b3344938_A_4_Float = 0;
            float _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float;
            Unity_Add_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Split_455b73b72b364d559a420ef8b3344938_G_2_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float);
            float _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float;
            Unity_Smoothstep_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float);
            float _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float, _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float);
            float _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float, _Split_455b73b72b364d559a420ef8b3344938_B_3_Float, _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float);
            float _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float;
            Unity_Saturate_float(_Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float, _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float);
            float _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float;
            Unity_OneMinus_float(_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float);
            float _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float;
            Unity_Add_float(_Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float, _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float);
            float _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float;
            Unity_Saturate_float(_Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float, _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float);
            float _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float);
            float _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float, _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float);
            float _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            Unity_Saturate_float(_Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float, _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float);
            surface.Alpha = _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
            // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull [_Cull]
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 WorldSpaceNormal;
             float3 WorldSpaceTangent;
             float3 WorldSpacePosition;
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 tangentWS : INTERP0;
             float4 texCoord0 : INTERP1;
             float3 positionWS : INTERP2;
             float3 normalWS : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _BottomFoamColor;
        float4 _MaskFoamColor;
        float _UseNormalRefrac;
        float _BottomFoamDistortStr;
        float _UseRemapRefrac;
        float _WaterColorDistortStr;
        float2 _NoiseMaskRange;
        float _TopFoamDistortStr;
        float2 _BottomFoamSmooth;
        float3 _EdgeWidthSmoothStr;
        float2 _MaskFoamWidthSmooth;
        float2 _FoamSmooth;
        float _SineWaveAmplitude;
        float4 _NoiseTex_TexelSize;
        float4 _FoamNoiseTilingOffset;
        float2 _FoamNoiseSpeed;
        float4 _WaterBaseColor;
        float _RefracNormalStr;
        float _RefracNormalOffset;
        float _SidesEdgeWidth;
        float _NoiseAlpha;
        float4 _Noise01Color;
        float4 _Noise02Color;
        float _RefracStr;
        float4 _RefractTilingOffset;
        float2 _RefractSpeed;
        float4 _WaterBottomColor;
        float4 _Noise02TilingOffset;
        float _DistortNormalOffset;
        float2 _WaterColorRange;
        float2 _Noise02Speed;
        float4 _WaterTopColor;
        float4 _DistortTex_TexelSize;
        float2 _Noise02PowStr;
        float4 _DistortTilingOffset;
        float2 _Noise02Smooth;
        float2 _Noise01Speed;
        float2 _DistortSpeed;
        float4 _Noise01TilingOffset;
        float2 _Noise01PowStr;
        float2 _Noise01Smooth;
        float _DistortStr;
        float _UseRemapDistort;
        float _WaterAlpha;
        float _UseNormalDistort;
        float _DistortNormalStr;
        float _WaterColorLerp;
        float4 _RefracTex_TexelSize;
        float _SineWaveFrequency;
        float _SineWaveSpeed;
        float _SineWaveDistortStr;
        float4 _TopFoamColor;
        float2 _MaskWidthSmooth;
        float3 _SidesWidthSmoothStr;
        float _TopFoamPosition;
        float _TopFoamWidth;
        float2 _TopFoamSmooth;
        float _FoamLerp;
        float _MaskFoamLerp;
        float2 _FoamPowStr;
        CBUFFER_END
        
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_DistortTex);
        SAMPLER(sampler_DistortTex);
        TEXTURE2D(_RefracTex);
        SAMPLER(sampler_RefracTex);
        float3 _ObjectPosition;
        float _ObjectRadius;
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_NormalFromTexture_float(TEXTURE2D_PARAM(Texture, Sampler), float2 UV, float Offset, float Strength, out float3 Out)
        {
            Offset = pow(Offset, 3) * 0.1;
            float2 offsetU = float2(UV.x + Offset, UV.y);
            float2 offsetV = float2(UV.x, UV.y + Offset);
            float normalSample = SAMPLE_TEXTURE2D(Texture, Sampler, UV).r;
            float uSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetU).r;
            float vSample = SAMPLE_TEXTURE2D(Texture, Sampler, offsetV).r;
            float3 va = float3(1, 0, (uSample - normalSample) * Strength);
            float3 vb = float3(0, 1, (vSample - normalSample) * Strength);
            Out = normalize(cross(va, vb));
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
        void Unity_Add_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_OneMinus_float(float In, out float Out)
        {
            Out = 1 - In;
        }
        
        void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Saturate_float(float In, out float Out)
        {
            Out = saturate(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Sine_float(float In, out float Out)
        {
            Out = sin(In);
        }
        
        void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A - B;
        }
        
        void Unity_Maximum_float(float A, float B, out float Out)
        {
            Out = max(A, B);
        }
        
        void Unity_Length_float3(float3 In, out float Out)
        {
            Out = length(In);
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float2 _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2 = _FoamSmooth;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[0];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float = _Property_91110a81c5b14113bcf0513c9f0278d4_Out_0_Vector2[1];
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_B_3_Float = 0;
            float _Split_910b8c9800ff4bc190149933c0fe7fa7_A_4_Float = 0;
            float _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float;
            Unity_Add_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Split_910b8c9800ff4bc190149933c0fe7fa7_G_2_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float);
            UnityTexture2D _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4 = IN.uv0;
            float _Split_81bacd708f454c8e8278db250d11f917_R_1_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[0];
            float _Split_81bacd708f454c8e8278db250d11f917_G_2_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[1];
            float _Split_81bacd708f454c8e8278db250d11f917_B_3_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[2];
            float _Split_81bacd708f454c8e8278db250d11f917_A_4_Float = _UV_e3d3f988466f441694a8f5646e9042e9_Out_0_Vector4[3];
            float2 _Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2 = float2(_Split_81bacd708f454c8e8278db250d11f917_R_1_Float, _Split_81bacd708f454c8e8278db250d11f917_G_2_Float);
            float4 _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4 = _FoamNoiseTilingOffset;
            float _Split_7fa49dda2d1849788afc51482698f959_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_7fa49dda2d1849788afc51482698f959_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_7fa49dda2d1849788afc51482698f959_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_7fa49dda2d1849788afc51482698f959_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2 = float2(_Split_7fa49dda2d1849788afc51482698f959_R_1_Float, _Split_7fa49dda2d1849788afc51482698f959_G_2_Float);
            float _Split_9cdfd6498d294ee08069eb99bef702b6_R_1_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[0];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_G_2_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[1];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[2];
            float _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float = _Property_50aa3cb756be487ba830ad25803292ee_Out_0_Vector4[3];
            float2 _Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2 = float2(_Split_9cdfd6498d294ee08069eb99bef702b6_B_3_Float, _Split_9cdfd6498d294ee08069eb99bef702b6_A_4_Float);
            float2 _Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2 = _FoamNoiseSpeed;
            float2 _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_e9a3912d0ebd45088096dd6c0d076eb3_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2);
            float2 _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_1674c0d3026349fd8edbbc0b79908f65_Out_2_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2);
            float2 _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2;
            Unity_Add_float2(_Vector2_d4c053b9d0c8476bbaf62e385b7c6a4e_Out_0_Vector2, _Fraction_437506d48ede410b8468e98c3f233a1a_Out_1_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2);
            float2 _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_d9900fdb1cd545aea801c2ffca7b0948_Out_0_Vector2, _Vector2_36dc161276db4f7f85c48a47068f6a70_Out_0_Vector2, _Add_58262042bae54899b6c062fc09a6022a_Out_2_Vector2, _TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2);
            float4 _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.tex, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.samplerstate, _Property_ec0a7a8bab8349f2b55385735a07867d_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8f134315244b47ad81e4812e1e898fa4_Out_3_Vector2) );
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_R_4_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.r;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_G_5_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.g;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.b;
            float _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_A_7_Float = _SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_RGBA_0_Vector4.a;
            float2 _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2 = _TopFoamSmooth;
            float _Split_211514ce3a224223a8f91e00d09582ca_R_1_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[0];
            float _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float = _Property_ad33ced41c2348ef85e01ec3ed92981b_Out_0_Vector2[1];
            float _Split_211514ce3a224223a8f91e00d09582ca_B_3_Float = 0;
            float _Split_211514ce3a224223a8f91e00d09582ca_A_4_Float = 0;
            float _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float;
            Unity_Add_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Split_211514ce3a224223a8f91e00d09582ca_G_2_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float);
            float4 _UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4 = IN.uv0;
            float _Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float = _TopFoamDistortStr;
            float _Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean = _UseNormalRefrac;
            float _Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float = _RefracStr;
            UnityTexture2D _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_RefracTex);
            float4 _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4 = IN.uv0;
            float _Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[0];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[1];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_B_3_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[2];
            float _Split_3d694a0b3ae646f39146392c53cabb9b_A_4_Float = _UV_950c75bf132b47f990dc687348eae6a2_Out_0_Vector4[3];
            float2 _Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2 = float2(_Split_3d694a0b3ae646f39146392c53cabb9b_R_1_Float, _Split_3d694a0b3ae646f39146392c53cabb9b_G_2_Float);
            float4 _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4 = _RefractTilingOffset;
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_30b54c3cc3ba4c628627e3aaca7d11d8_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2 = float2(_Split_30b54c3cc3ba4c628627e3aaca7d11d8_R_1_Float, _Split_30b54c3cc3ba4c628627e3aaca7d11d8_G_2_Float);
            float _Split_a2feec9f6a18404fa7513bc90ae72516_R_1_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[0];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_G_2_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[1];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[2];
            float _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float = _Property_0e460f606b8b4ee5b283809b4c037497_Out_0_Vector4[3];
            float2 _Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2 = float2(_Split_a2feec9f6a18404fa7513bc90ae72516_B_3_Float, _Split_a2feec9f6a18404fa7513bc90ae72516_A_4_Float);
            float2 _Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2 = _RefractSpeed;
            float2 _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_d5912a39ac424fc1be002fd517f63663_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2);
            float2 _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_4132d4ae8e024916ac9aa4d0522f50c3_Out_2_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2);
            float2 _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2;
            Unity_Add_float2(_Vector2_73ca40583510440aa7aa9b29ffa2e546_Out_0_Vector2, _Fraction_0d061849a6294ae8961783dde6af177a_Out_1_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2);
            float2 _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_561d02b234de4e1ba2dcff12a8e32fa9_Out_0_Vector2, _Vector2_b617074422ad4ea7967d50a86dd2f9cf_Out_0_Vector2, _Add_54e62ba94c1c402b8468e84d38dd9237_Out_2_Vector2, _TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2);
            float _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float = _RefracNormalOffset;
            float _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float = _RefracNormalStr;
            float3 _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate), _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2), _Property_22225d35a91e49b8916f6a1e19d17a7e_Out_0_Float, _Property_143cbc3921d14e80964edf0d2d605691_Out_0_Float, _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3);
            float3 _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxx), _NormalFromTexture_a02f7913e8c14101b5e0fc8fcbaae5b1_Out_5_Vector3, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3);
            float _Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean = _UseRemapRefrac;
            float _Float_a592706948e447c097be687bfae791c6_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.tex, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.samplerstate, _Property_f6e07c0233b34cee83a3bd793e3bf22a_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_8bdd51778a5945a3a3cd5cafe421189e_Out_3_Vector2) );
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_R_4_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.r;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_G_5_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.g;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_B_6_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.b;
            float _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_A_7_Float = _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4.a;
            float4 _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4;
            Unity_Add_float4((_Float_a592706948e447c097be687bfae791c6_Out_0_Float.xxxx), _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4);
            float _Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float = float(2);
            float4 _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_0c71fb51258846859422e20e9774591a_Out_2_Vector4, (_Float_42cd5fb07c35463782deab1499b5332e_Out_0_Float.xxxx), _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4);
            float4 _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4;
            Unity_Branch_float4(_Property_576412980ba44e08a621a8828cd81967_Out_0_Boolean, _Multiply_2d14f25554f14430b72f38c1b81ed0d4_Out_2_Vector4, _SampleTexture2D_b0a51e3abea7429eb54a4bfe0de12319_RGBA_0_Vector4, _Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4);
            float4 _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_78bf2a6e0b964740a704f87e87cc6d5d_Out_3_Vector4, (_Property_246f07862c314338bdc7e71a5e20083a_Out_0_Float.xxxx), _Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4);
            float3 _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3;
            Unity_Branch_float3(_Property_bf4c3898542c4538820e5401f3000070_Out_0_Boolean, _Multiply_0e68f12fe554456abcf2bbe2ef2810c5_Out_2_Vector3, (_Multiply_1f259d55990846d1bf1ca7619ec94b5d_Out_2_Vector4.xyz), _Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3);
            float _Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean = _UseNormalDistort;
            float _Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float = _DistortStr;
            UnityTexture2D _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_DistortTex);
            float4 _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4 = IN.uv0;
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[0];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[1];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_B_3_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[2];
            float _Split_bf00bbf5385142a2aa86fbe24c588fa8_A_4_Float = _UV_f126356e8a6d4cb5af1e78dee70dfb0b_Out_0_Vector4[3];
            float2 _Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2 = float2(_Split_bf00bbf5385142a2aa86fbe24c588fa8_R_1_Float, _Split_bf00bbf5385142a2aa86fbe24c588fa8_G_2_Float);
            float4 _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4 = _DistortTilingOffset;
            float _Split_3d3070089d0844319f72a05a13112d78_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_3d3070089d0844319f72a05a13112d78_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_3d3070089d0844319f72a05a13112d78_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_3d3070089d0844319f72a05a13112d78_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2 = float2(_Split_3d3070089d0844319f72a05a13112d78_R_1_Float, _Split_3d3070089d0844319f72a05a13112d78_G_2_Float);
            float _Split_c094edcf73d543cb919e5d7826a05b1e_R_1_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[0];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_G_2_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[1];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[2];
            float _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float = _Property_7be9218f449d48879ea95f8253790a90_Out_0_Vector4[3];
            float2 _Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2 = float2(_Split_c094edcf73d543cb919e5d7826a05b1e_B_3_Float, _Split_c094edcf73d543cb919e5d7826a05b1e_A_4_Float);
            float2 _Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2 = _DistortSpeed;
            float2 _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_346c76458589480d95f8f1201b3b1944_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2);
            float2 _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_cba7b1c491254a4883b9749d334e8618_Out_2_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2);
            float2 _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2;
            Unity_Add_float2(_Vector2_3e70559e0cb44472981fee18696031a0_Out_0_Vector2, _Fraction_881616d4c9a7425eb8966e81cfa97f30_Out_1_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2);
            float2 _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Vector2_0ddac0d2c9ea458ab178c304dd1da631_Out_0_Vector2, _Vector2_e690247497324586a99091d7ae132bc3_Out_0_Vector2, _Add_d873d48095b447329038ae3837d83281_Out_2_Vector2, _TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2);
            float _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float = _DistortNormalOffset;
            float _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float = _DistortNormalStr;
            float3 _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3;
            Unity_NormalFromTexture_float(TEXTURE2D_ARGS(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate), _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2), _Property_2ac3efad55e64f8196b4074547d3d1f6_Out_0_Float, _Property_89e50ac74dac4671b74c3eaffc39eda6_Out_0_Float, _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3);
            float3 _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxx), _NormalFromTexture_292c8d5236c444138607793d136677a2_Out_5_Vector3, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3);
            float _Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean = _UseRemapDistort;
            float _Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float = float(-0.5);
            float4 _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.tex, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.samplerstate, _Property_3cf1c320b08947a095405cc8f05624f1_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_c3e7a5d437cb4d6294233e4905f45ebb_Out_3_Vector2) );
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_R_4_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.r;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_G_5_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.g;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_B_6_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.b;
            float _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_A_7_Float = _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4.a;
            float4 _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4;
            Unity_Add_float4((_Float_7cda6b30545b403ebc2db5f4e99b6e44_Out_0_Float.xxxx), _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4);
            float _Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float = float(2);
            float4 _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Add_5169c3bf260c43a1b4244d806305e375_Out_2_Vector4, (_Float_77082ec410394d6da95bddfd71628d7c_Out_0_Float.xxxx), _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4);
            float4 _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4;
            Unity_Branch_float4(_Property_911d6823d61b4111ac227f3ec2fe5136_Out_0_Boolean, _Multiply_aa717f7ae90443189982663cff4f78e4_Out_2_Vector4, _SampleTexture2D_771200ab56a14c1f95d3821472ff8dc3_RGBA_0_Vector4, _Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4);
            float4 _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Branch_3fb40f6d63064cec87eba4079a344164_Out_3_Vector4, (_Property_a64ee729c9e040a083dd14eab1eca869_Out_0_Float.xxxx), _Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4);
            float3 _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3;
            Unity_Branch_float3(_Property_731637b4e5544002958ee4139cc97786_Out_0_Boolean, _Multiply_681779d5f473437f993ed96be785c806_Out_2_Vector3, (_Multiply_30f66701bfbf4338bf19568d85232a47_Out_2_Vector4.xyz), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3);
            float3 _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3;
            Unity_Add_float3(_Branch_a6feafe7ecee4b8aaeb31585aa8985b4_Out_3_Vector3, _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3);
            float3 _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_692124fbbffc4a58aa9e717b341239d1_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3);
            float3 _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_c993e2d4f1ea451dbd1a4fad0578031b_Out_2_Vector3, _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3);
            float _Split_c8e32710f8a04315886fca7a242e3c39_R_1_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[0];
            float _Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[1];
            float _Split_c8e32710f8a04315886fca7a242e3c39_B_3_Float = _Add_bc78583bc2de4e6fb830f67841bd4d08_Out_2_Vector3[2];
            float _Split_c8e32710f8a04315886fca7a242e3c39_A_4_Float = 0;
            float _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float = _TopFoamPosition;
            float _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float;
            Unity_Subtract_float(_Split_c8e32710f8a04315886fca7a242e3c39_G_2_Float, _Property_2687441f8e4245f9a81e175d77a71d33_Out_0_Float, _Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float);
            float _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float;
            Unity_Absolute_float(_Subtract_b51df311f1d44a76b58a7e3c0d1198af_Out_2_Float, _Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float);
            float _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float = _TopFoamWidth;
            float _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float;
            Unity_Subtract_float(_Absolute_e01c6b6d6cb14161821ad5a6beb236ab_Out_1_Float, _Property_4ff9487a40484c8d8f750cc3d9629c6f_Out_0_Float, _Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float);
            float _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float;
            Unity_OneMinus_float(_Subtract_297d24652ef84af3934e97391c2d2dc4_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float);
            float _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float;
            Unity_Smoothstep_float(_Split_211514ce3a224223a8f91e00d09582ca_R_1_Float, _Add_cdb76b9548c94ae79a793df52c39dea5_Out_2_Float, _OneMinus_625e8390830047dc87cb6781d63b5044_Out_1_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float);
            float _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float;
            Unity_Multiply_float_float(_SampleTexture2D_7f70f6ea9dcf4800b5d7d31e7092521e_B_6_Float, _Smoothstep_7d047702106543828093810b5dfbad93_Out_3_Float, _Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float);
            float2 _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2 = _BottomFoamSmooth;
            float _Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[0];
            float _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float = _Property_5473a212ff53449f9679ede4c12f3a3c_Out_0_Vector2[1];
            float _Split_bd10349e771946a2829f2a8d3408e00d_B_3_Float = 0;
            float _Split_bd10349e771946a2829f2a8d3408e00d_A_4_Float = 0;
            float _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float;
            Unity_Add_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Split_bd10349e771946a2829f2a8d3408e00d_G_2_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float);
            float _Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float = _BottomFoamDistortStr;
            float3 _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_71064849a9c8473ab1151c5f2b1b26ea_Out_0_Float.xxx), _Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3);
            float3 _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3;
            Unity_Add_float3((_UV_ac4c7301603a40429c84f5e5f16f5cd2_Out_0_Vector4.xyz), _Multiply_68258b4990cd43f2b7db73c484281f50_Out_2_Vector3, _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3);
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_R_1_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[0];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[1];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_B_3_Float = _Add_32d75d500742405aa92295faf88ffd32_Out_2_Vector3[2];
            float _Split_7f86bd7f940d4aa5bd747fa92cf925df_A_4_Float = 0;
            float _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float;
            Unity_OneMinus_float(_Split_7f86bd7f940d4aa5bd747fa92cf925df_G_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float);
            float _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float;
            Unity_Smoothstep_float(_Split_bd10349e771946a2829f2a8d3408e00d_R_1_Float, _Add_1f2fb779bcd2458ba60a3912ed415006_Out_2_Float, _OneMinus_93b165787a6b4a7d8b5e0e212bd91219_Out_1_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float);
            float _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float;
            Unity_Add_float(_Multiply_ff5f34f6e0c24fc388e807187d4f5706_Out_2_Float, _Smoothstep_909511e1ad8844fbadee272cfeada82d_Out_3_Float, _Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float);
            float _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float;
            Unity_Saturate_float(_Add_741576f895bb4d108334dada7ea3ec83_Out_2_Float, _Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float);
            float2 _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2 = _FoamPowStr;
            float _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[0];
            float _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float = _Property_75dc7d7d84ec4e85be97496e66fa77e7_Out_0_Vector2[1];
            float _Split_93b2b90a6f164ce18354940ff78c4947_B_3_Float = 0;
            float _Split_93b2b90a6f164ce18354940ff78c4947_A_4_Float = 0;
            float _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float;
            Unity_Power_float(_Saturate_41d05814f01d417d9416832aae03bb87_Out_1_Float, _Split_93b2b90a6f164ce18354940ff78c4947_R_1_Float, _Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float);
            float _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float;
            Unity_Multiply_float_float(_Power_6aae5ce132434920af0ae79e35e7d3e3_Out_2_Float, _Split_93b2b90a6f164ce18354940ff78c4947_G_2_Float, _Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float);
            float _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float;
            Unity_Saturate_float(_Multiply_573c59bbf07f417890a4879aa62104a0_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float);
            float _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float;
            Unity_Smoothstep_float(_Split_910b8c9800ff4bc190149933c0fe7fa7_R_1_Float, _Add_915363943fce476d9dbbaa1b5015b2d3_Out_2_Float, _Saturate_ce77e7bf2d7b4ac0964cf5c92a73269a_Out_1_Float, _Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float);
            float3 _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3 = _SidesWidthSmoothStr;
            float _Split_36856a9943d7412593732e444047eb04_R_1_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[0];
            float _Split_36856a9943d7412593732e444047eb04_G_2_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[1];
            float _Split_36856a9943d7412593732e444047eb04_B_3_Float = _Property_e4997832d50f4ee09b4c230ca5a437cb_Out_0_Vector3[2];
            float _Split_36856a9943d7412593732e444047eb04_A_4_Float = 0;
            float _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float;
            Unity_Add_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Split_36856a9943d7412593732e444047eb04_G_2_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float);
            float4 _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4 = IN.uv0;
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[0];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_G_2_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[1];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_B_3_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[2];
            float _Split_56c51d0d274b483a9ed8a994f03b74ad_A_4_Float = _UV_d4bb9645f47549feb711063ab1406848_Out_0_Vector4[3];
            float _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float;
            Unity_Subtract_float(_Split_56c51d0d274b483a9ed8a994f03b74ad_R_1_Float, float(0.5), _Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float);
            float _Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float = _SineWaveFrequency;
            float _Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float = _SineWaveDistortStr;
            float3 _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3;
            Unity_Multiply_float3_float3(_Add_c0fb8594f25f40348e49fce7c46e1793_Out_2_Vector3, (_Property_aa36f7978e1949228c1b8cb228380c0f_Out_0_Float.xxx), _Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3);
            float4 _UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4 = IN.uv0;
            float3 _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3;
            Unity_Add_float3(_Multiply_874736527bb04d6f928e4514565b04e2_Out_2_Vector3, (_UV_a95d0605516041d489998d69ffe7a5bf_Out_0_Vector4.xyz), _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3);
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_R_1_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[0];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[1];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_B_3_Float = _Add_925f596f567f45688fa25ac6e02ce022_Out_2_Vector3[2];
            float _Split_19d7afb52dfb48f59a76c4e19ca1c030_A_4_Float = 0;
            float _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float;
            Unity_Multiply_float_float(_Property_c48f1bba1c5344da92578475c56806bf_Out_0_Float, _Split_19d7afb52dfb48f59a76c4e19ca1c030_G_2_Float, _Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float);
            float _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float = _SineWaveSpeed;
            float _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_bb6b7da5ca5345919d28982ade0cc772_Out_0_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float);
            float _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float;
            Unity_Add_float(_Multiply_3c3fa09a79814515a6818e43e909a5b1_Out_2_Float, _Multiply_4e09df1ec5d34096bd3df1b528a713f1_Out_2_Float, _Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float);
            float _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float;
            Unity_Sine_float(_Add_038978ebd7744baf9f1abf2af176bc10_Out_2_Float, _Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float);
            float _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float;
            Unity_Multiply_float_float(_Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, 0.05, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float);
            float _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Multiply_9e25278d518a4b65b131f67eb6bab8f3_Out_2_Float, _Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float);
            float _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_30a475b34d224bbfabc3ba903adeeb52_Out_2_Float, 0.5, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float);
            float _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float;
            Unity_Subtract_float(_Subtract_43ad7d576e31481ca0fc9c04ee18544e_Out_2_Float, _Multiply_25a61b97a9bb44d0bab18a1d63368c37_Out_2_Float, _Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float);
            float _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float;
            Unity_Absolute_float(_Subtract_18a887d665ca45ff9108fde2b4c531b7_Out_2_Float, _Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float);
            float _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float;
            Unity_Subtract_float(_Absolute_a5163a5381c6455ea5c8ab62adf6e0ea_Out_1_Float, float(0.5), _Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float);
            float _Property_e192f06194264eccbd544e066faf25eb_Out_0_Float = _SineWaveAmplitude;
            float _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float;
            Unity_Multiply_float_float(_Property_e192f06194264eccbd544e066faf25eb_Out_0_Float, 0.5, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float);
            float _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float;
            Unity_Add_float(_Subtract_52a076ad180f4e429f1ac351d60ce990_Out_2_Float, _Multiply_bf12cd1c8db14e39b211ae3ecc0169a2_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float);
            float _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float;
            Unity_Smoothstep_float(_Split_36856a9943d7412593732e444047eb04_R_1_Float, _Add_2548f4b8a7bc4f0da2f77032e72ba6f1_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float);
            float _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_0ed1cd17acdd447da63e58d39beec27d_Out_3_Float, _OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float);
            float _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_7c8836283b574c2d99e3f6cc4526d20e_Out_1_Float, _Split_36856a9943d7412593732e444047eb04_B_3_Float, _Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float);
            float _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float;
            Unity_Saturate_float(_Multiply_bff5662f476f4c2c83b659d21198c361_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float);
            float _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_9d2d752f5d0f42f799a353fff3100493_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float);
            float4 _Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterBottomColor) : _WaterBottomColor;
            float4 _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_WaterTopColor) : _WaterTopColor;
            float2 _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2 = _WaterColorRange;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[0];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float = _Property_3579b0037ca04161998476686c983da9_Out_0_Vector2[1];
            float _Split_071b8ccf17b64663b193b70ab421c5b5_B_3_Float = 0;
            float _Split_071b8ccf17b64663b193b70ab421c5b5_A_4_Float = 0;
            float _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float;
            Unity_Add_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Split_071b8ccf17b64663b193b70ab421c5b5_G_2_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float);
            float4 _UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4 = IN.uv0;
            float _Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float = _WaterColorDistortStr;
            float3 _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3;
            Unity_Multiply_float3_float3((_Property_00b3cbae54434ae2ac69f0aac819bfd3_Out_0_Float.xxx), _Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3, _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3);
            float3 _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3;
            Unity_Add_float3((_UV_adb69f8ccf7949769677c3e0925a0eec_Out_0_Vector4.xyz), _Multiply_623fac0b79e949b5ab600ea5fa771343_Out_2_Vector3, _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3);
            float _Split_5904126eeadf4862b44d432382f26e43_R_1_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[0];
            float _Split_5904126eeadf4862b44d432382f26e43_G_2_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[1];
            float _Split_5904126eeadf4862b44d432382f26e43_B_3_Float = _Add_bf940897ccf24111a357bcf513a6f6ac_Out_2_Vector3[2];
            float _Split_5904126eeadf4862b44d432382f26e43_A_4_Float = 0;
            float _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float;
            Unity_Saturate_float(_Split_5904126eeadf4862b44d432382f26e43_G_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float);
            float _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float;
            Unity_Smoothstep_float(_Split_071b8ccf17b64663b193b70ab421c5b5_R_1_Float, _Add_cfbc0e45080a441abe26bdeea46a142e_Out_2_Float, _Saturate_0acf37945f3f4f0b9c8e65179922dc4c_Out_1_Float, _Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float);
            float _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f20a9c9baeb8441181e32bf6e5c62607_Out_3_Float, _Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float);
            float4 _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4;
            Unity_Lerp_float4(_Property_673591c3026e4e15a5841af93393bb8a_Out_0_Vector4, _Property_8c248241c9e34ff3816d5337ca2cd1e1_Out_0_Vector4, (_Saturate_959d01e0fc7a447c974caf3f7bcc3069_Out_1_Float.xxxx), _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4);
            float _Split_5c957c23ba944f22a3599824406a1f36_R_1_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[0];
            float _Split_5c957c23ba944f22a3599824406a1f36_G_2_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[1];
            float _Split_5c957c23ba944f22a3599824406a1f36_B_3_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[2];
            float _Split_5c957c23ba944f22a3599824406a1f36_A_4_Float = _Lerp_ff89e41bb3104c56b76c8a79e6509571_Out_3_Vector4[3];
            float2 _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2 = _MaskWidthSmooth;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[0];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float = _Property_ecb898f24748439289e4c34a01be15be_Out_0_Vector2[1];
            float _Split_3b52bf3756d5491bb0ed074f060f421b_B_3_Float = 0;
            float _Split_3b52bf3756d5491bb0ed074f060f421b_A_4_Float = 0;
            float _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float;
            Unity_Add_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Split_3b52bf3756d5491bb0ed074f060f421b_G_2_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float);
            float3 _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3 = _ObjectPosition;
            float3 _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3;
            Unity_Subtract_float3(IN.WorldSpacePosition, _Property_2e19eea8187e468d9752b65696c226f1_Out_0_Vector3, _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3);
            float _Split_ccebce80c1f44409877640fc36481eac_R_1_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[0];
            float _Split_ccebce80c1f44409877640fc36481eac_G_2_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[1];
            float _Split_ccebce80c1f44409877640fc36481eac_B_3_Float = _Subtract_96902746f15e4d679df7bd726919d7f6_Out_2_Vector3[2];
            float _Split_ccebce80c1f44409877640fc36481eac_A_4_Float = 0;
            float _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float;
            Unity_Maximum_float(_Split_ccebce80c1f44409877640fc36481eac_G_2_Float, float(0), _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float);
            float3 _Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3 = float3(_Split_ccebce80c1f44409877640fc36481eac_R_1_Float, _Maximum_862fb99c2a50462a9eb085e2cdfe3f50_Out_2_Float, _Split_ccebce80c1f44409877640fc36481eac_B_3_Float);
            float _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float;
            Unity_Multiply_float_float(_Sine_718b6dee389c4b3390565465cdce6742_Out_1_Float, _Property_0f35c5779bf24f9aacfe9ea0813df9f9_Out_0_Float, _Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float);
            float3 _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3;
            Unity_Multiply_float3_float3(IN.WorldSpaceTangent, (_Multiply_99981449c6754e11b022df6d05b25bb7_Out_2_Float.xxx), _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3);
            float3 _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3;
            Unity_Subtract_float3(_Vector3_28fa88cbfab840e1a4505bcfbd574ee8_Out_0_Vector3, _Multiply_7b311e2d402d40f7be55611b4ba98349_Out_2_Vector3, _Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3);
            float _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float;
            Unity_Length_float3(_Subtract_9a6a23becfc343f0910a5e23eddf76e7_Out_2_Vector3, _Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float);
            float _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float = _ObjectRadius;
            float _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float;
            Unity_Subtract_float(_Length_08f93d7ef72d4daf82298422a82f6930_Out_1_Float, _Property_bb7a4c8677d8406f99bda72d2a30fdd2_Out_0_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float);
            float _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float;
            Unity_Smoothstep_float(_Split_3b52bf3756d5491bb0ed074f060f421b_R_1_Float, _Add_8e22cb4b6a11469d88a25838305255c8_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float);
            float _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float;
            Unity_Multiply_float_float(_Split_5c957c23ba944f22a3599824406a1f36_A_4_Float, _Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float);
            float _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_aea4705af8774743ac39b7eef3b58015_Out_2_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float);
            float _Property_525f3b66b77449758f64a237743c6372_Out_0_Float = _WaterAlpha;
            float _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_b09fa1d6e8a14c26877174cb9d2092ef_Out_2_Float, _Property_525f3b66b77449758f64a237743c6372_Out_0_Float, _Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float);
            float4 _Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise01Color) : _Noise01Color;
            float2 _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2 = _NoiseMaskRange;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[0];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float = _Property_f39a326a39754be788af8c276b2192bc_Out_0_Vector2[1];
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_B_3_Float = 0;
            float _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_A_4_Float = 0;
            float _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float;
            Unity_Add_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_G_2_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float);
            float4 _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4 = IN.uv0;
            float _Split_2f15213772824def9fb752d1d60e12dc_R_1_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[0];
            float _Split_2f15213772824def9fb752d1d60e12dc_G_2_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[1];
            float _Split_2f15213772824def9fb752d1d60e12dc_B_3_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[2];
            float _Split_2f15213772824def9fb752d1d60e12dc_A_4_Float = _UV_2e969408f3b24204806c31fa6ec1a257_Out_0_Vector4[3];
            float _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float;
            Unity_Saturate_float(_Split_2f15213772824def9fb752d1d60e12dc_G_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float);
            float _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float;
            Unity_Smoothstep_float(_Split_5f1fb58d1d73472aa5fcf2edfd9a4e0a_R_1_Float, _Add_8e867869dbe44e96a8eb971b555531b7_Out_2_Float, _Saturate_5135859ba292499d9b53d3374295fc0a_Out_1_Float, _Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float);
            float _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_f1c9a21039994e8a91bc6e44c663bc6a_Out_3_Float, _Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float);
            float2 _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2 = _Noise01Smooth;
            float _Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[0];
            float _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float = _Property_3543a428c1ab40c4af1ce54f0328af06_Out_0_Vector2[1];
            float _Split_7edd56182b1b4da88913294d5de35a2d_B_3_Float = 0;
            float _Split_7edd56182b1b4da88913294d5de35a2d_A_4_Float = 0;
            float _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float;
            Unity_Add_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Split_7edd56182b1b4da88913294d5de35a2d_G_2_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float);
            UnityTexture2D _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4 = IN.uv0;
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[0];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[1];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_B_3_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[2];
            float _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_A_4_Float = _UV_eaab6f81eb514f55890021f27b3c5600_Out_0_Vector4[3];
            float2 _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2 = float2(_Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_R_1_Float, _Split_20456b7cf64c4bd3a1cf36ad43e0f9ae_G_2_Float);
            float2 _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_5b23d833138f45c3b56e32664edb37c8_Out_0_Vector2, _Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2);
            float4 _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4 = _Noise01TilingOffset;
            float _Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_76666e838777429a8e6d8162a0d7ab16_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_76666e838777429a8e6d8162a0d7ab16_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2 = float2(_Split_76666e838777429a8e6d8162a0d7ab16_R_1_Float, _Split_76666e838777429a8e6d8162a0d7ab16_G_2_Float);
            float _Split_e5bebaba69094dca8731554a45fb3b66_R_1_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[0];
            float _Split_e5bebaba69094dca8731554a45fb3b66_G_2_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[1];
            float _Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[2];
            float _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float = _Property_ef1447876286444a94fc554c93aa6689_Out_0_Vector4[3];
            float2 _Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2 = float2(_Split_e5bebaba69094dca8731554a45fb3b66_B_3_Float, _Split_e5bebaba69094dca8731554a45fb3b66_A_4_Float);
            float2 _Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2 = _Noise01Speed;
            float2 _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_4d96ff17a2744a99b88f011a73c42be7_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2);
            float2 _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_69a615be311a4ea7bca4f6c2343452f1_Out_2_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2);
            float2 _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2;
            Unity_Add_float2(_Vector2_f31b90752c7a44aeb64164ac030ca0d6_Out_0_Vector2, _Fraction_2255f571b42642b8b0d787e758445a9c_Out_1_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2);
            float2 _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_5242681089914833a8030bffe24c21e6_Out_2_Vector2, _Vector2_29d771dfb7cc4d42b46b4bc973a2a6f1_Out_0_Vector2, _Add_fff40c37e6974e7f81979d48a77dd3b7_Out_2_Vector2, _TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2);
            float4 _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.tex, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.samplerstate, _Property_b5f1aad2a4784d87a29782046ca18183_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_60af14e51713477da27620fa3a69b011_Out_3_Vector2) );
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.r;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_G_5_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.g;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_B_6_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.b;
            float _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_A_7_Float = _SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_RGBA_0_Vector4.a;
            float _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_14007ba9efa6466e85452459ce4cb2ad_R_4_Float, _Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float);
            float2 _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2 = _Noise01PowStr;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[0];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float = _Property_c0834f88aab9404ab096ee4f6c523d5e_Out_0_Vector2[1];
            float _Split_86c9b1fb50984315ab11dc2b432d1829_B_3_Float = 0;
            float _Split_86c9b1fb50984315ab11dc2b432d1829_A_4_Float = 0;
            float _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float;
            Unity_Power_float(_Saturate_42927df1878e4a67a58e1503877f607f_Out_1_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_R_1_Float, _Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float);
            float _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float;
            Unity_Multiply_float_float(_Power_54cf3b5b3ca2425ab0b01982b6689dde_Out_2_Float, _Split_86c9b1fb50984315ab11dc2b432d1829_G_2_Float, _Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float);
            float _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float;
            Unity_Saturate_float(_Multiply_8f7204dd4c43479cbd67f7c5964bb42e_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float);
            float _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float;
            Unity_Smoothstep_float(_Split_7edd56182b1b4da88913294d5de35a2d_R_1_Float, _Add_9743d4b6a19c4698a9b964c95c22029f_Out_2_Float, _Saturate_f91b4913e9604d73881dd83277248edd_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float);
            float _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_b20562f50b514fdc87469439ecc56763_Out_3_Float, _Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float);
            float4 _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_e39d6ddeb0534738b0f8476966f96653_Out_0_Vector4, (_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float.xxxx), _Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4);
            float4 _Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Noise02Color) : _Noise02Color;
            float2 _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2 = _Noise02Smooth;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[0];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float = _Property_d2836b93d969485eb9740f44abc17efe_Out_0_Vector2[1];
            float _Split_8352aa22d9094e888948d684fe7ff1c9_B_3_Float = 0;
            float _Split_8352aa22d9094e888948d684fe7ff1c9_A_4_Float = 0;
            float _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float;
            Unity_Add_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Split_8352aa22d9094e888948d684fe7ff1c9_G_2_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float);
            UnityTexture2D _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_NoiseTex);
            float4 _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4 = IN.uv0;
            float _Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[0];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[1];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_B_3_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[2];
            float _Split_c479a13b857d4e2db856c2bcd4190c50_A_4_Float = _UV_a3704bcfe6514e5392dd7a6da2bb943e_Out_0_Vector4[3];
            float2 _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2 = float2(_Split_c479a13b857d4e2db856c2bcd4190c50_R_1_Float, _Split_c479a13b857d4e2db856c2bcd4190c50_G_2_Float);
            float2 _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2;
            Unity_Add_float2((_Branch_5e8779e2edcc466b9e7cdc04dfcd20d7_Out_3_Vector3.xy), _Vector2_f9357f503ae24f8181231e33e548c68a_Out_0_Vector2, _Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2);
            float4 _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4 = _Noise02TilingOffset;
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_5b0ee1bb63a041bd9350ad0a657314c3_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2 = float2(_Split_5b0ee1bb63a041bd9350ad0a657314c3_R_1_Float, _Split_5b0ee1bb63a041bd9350ad0a657314c3_G_2_Float);
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_R_1_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[0];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_G_2_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[1];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[2];
            float _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float = _Property_1c230374a2594b00a3a5df40f24509af_Out_0_Vector4[3];
            float2 _Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2 = float2(_Split_3ed20b105a9b4f2a8cd1b8bc50148827_B_3_Float, _Split_3ed20b105a9b4f2a8cd1b8bc50148827_A_4_Float);
            float2 _Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2 = _Noise02Speed;
            float2 _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2;
            Unity_Multiply_float2_float2(_Property_da1ff009ed624e68b8b60c190ade9a21_Out_0_Vector2, (IN.TimeParameters.x.xx), _Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2);
            float2 _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2;
            Unity_Fraction_float2(_Multiply_8069c55ef8b24b35bd70c820bd4dabc9_Out_2_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2);
            float2 _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2;
            Unity_Add_float2(_Vector2_c316ccb0a38c441184ae5925c97adaeb_Out_0_Vector2, _Fraction_f3f288d6d0f140068e09834ffc598914_Out_1_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2);
            float2 _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2;
            Unity_TilingAndOffset_float(_Add_e496ed096b4444788363ed51a5d50a4b_Out_2_Vector2, _Vector2_f3e881576704489887bca51dfa8cf89a_Out_0_Vector2, _Add_9a08a35b0b1b46d9a1d7ea3e26004f15_Out_2_Vector2, _TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2);
            float4 _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.tex, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.samplerstate, _Property_f436058485a14c8c8852eaa2b7d734ea_Out_0_Texture2D.GetTransformedUV(_TilingAndOffset_0a61ac0b41c74e43ba21d671ed388cbe_Out_3_Vector2) );
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_R_4_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.r;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.g;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_B_6_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.b;
            float _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_A_7_Float = _SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_RGBA_0_Vector4.a;
            float _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float;
            Unity_Saturate_float(_SampleTexture2D_bd4ec10d29f34b918effbc2a59f7e47d_G_5_Float, _Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float);
            float2 _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2 = _Noise02PowStr;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[0];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float = _Property_14e6d4f0c33c4ca8bafed2c83f1a4c01_Out_0_Vector2[1];
            float _Split_fa5bbc433a4a42b090acef3708d56d20_B_3_Float = 0;
            float _Split_fa5bbc433a4a42b090acef3708d56d20_A_4_Float = 0;
            float _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float;
            Unity_Power_float(_Saturate_97fc58f417d7451c85e46872220c8555_Out_1_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_R_1_Float, _Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float);
            float _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float;
            Unity_Multiply_float_float(_Power_f849cb9493d347ab9d02119fb31627f8_Out_2_Float, _Split_fa5bbc433a4a42b090acef3708d56d20_G_2_Float, _Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float);
            float _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float;
            Unity_Saturate_float(_Multiply_1071086f72244d75bb3175df218cbd85_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float);
            float _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float;
            Unity_Smoothstep_float(_Split_8352aa22d9094e888948d684fe7ff1c9_R_1_Float, _Add_05c64cbd61d245679638115c4b4c7709_Out_2_Float, _Saturate_d012318c1d9a4bc1bbc657f55a36b31a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float);
            float _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_58ef340e2ac3428f9f2c7b5627a13d6a_Out_1_Float, _Smoothstep_9ad6018d85664661a8d2eea01aaba793_Out_3_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float);
            float4 _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Property_dfdee13d4b1746928237b3c9172e782b_Out_0_Vector4, (_Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float.xxxx), _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4);
            float4 _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4;
            Unity_Multiply_float4_float4(_Multiply_04f7bf48a911416c8dbb01e0692393e8_Out_2_Vector4, _Multiply_75ac5f2b8e784124aaa9cd617b3f624e_Out_2_Vector4, _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4);
            float _Split_4e177009864549f4a3575f0fce56b4c4_R_1_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[0];
            float _Split_4e177009864549f4a3575f0fce56b4c4_G_2_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[1];
            float _Split_4e177009864549f4a3575f0fce56b4c4_B_3_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[2];
            float _Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float = _Multiply_44e3ea9a9c9840c1b7d93279a0acc723_Out_2_Vector4[3];
            float _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_9075ef99ce124d2f9b3ba75d9dc7f0cb_Out_2_Float, _Multiply_0802a79ac7294fffb3e22b06d8367d8f_Out_2_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float);
            float _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float;
            Unity_Multiply_float_float(_Split_4e177009864549f4a3575f0fce56b4c4_A_4_Float, _Multiply_4c427a319e6b44c5bdcf8e36e09284ed_Out_2_Float, _Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float);
            float _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float = _NoiseAlpha;
            float _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float;
            Unity_Multiply_float_float(_Multiply_d0f1a610f359414f9b72a35a7c130edc_Out_2_Float, _Property_fa94556ad8ae4bd1a2bc5451d9124ec4_Out_0_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float);
            float _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float;
            Unity_Add_float(_Multiply_25f0f4a390fd4bbb9adfca112bc576ef_Out_2_Float, _Multiply_62706895a7b94e33a3d8f148fb7e46e9_Out_2_Float, _Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float);
            float _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float;
            Unity_Saturate_float(_Add_10e8b4904a4e4dfeba966623768e145a_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float);
            float _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float;
            Unity_Add_float(_Multiply_6c97d790ff464b7fbc920aa2d362183e_Out_2_Float, _Saturate_362466e249cb4bb9b7b2871ad3b582e3_Out_1_Float, _Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float);
            float2 _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2 = _MaskFoamWidthSmooth;
            float _Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[0];
            float _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float = _Property_5aa5642f154e4327a2a09e748572bcb9_Out_0_Vector2[1];
            float _Split_0083280626cc439aa48131cbd3c99c0b_B_3_Float = 0;
            float _Split_0083280626cc439aa48131cbd3c99c0b_A_4_Float = 0;
            float _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float;
            Unity_Add_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Split_0083280626cc439aa48131cbd3c99c0b_G_2_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float);
            float _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float;
            Unity_Smoothstep_float(_Split_0083280626cc439aa48131cbd3c99c0b_R_1_Float, _Add_ac4c84f5c6b9429c94bd0780717a598b_Out_2_Float, _Subtract_97a78ca550104dcd8fcd73119c6aedef_Out_2_Float, _Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float);
            float _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float;
            Unity_Saturate_float(_Smoothstep_d126116ef998407288c70999d5d49c4d_Out_3_Float, _Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float);
            float _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float;
            Unity_OneMinus_float(_Saturate_14fa1caf4b0c4ef4bb1726c49cfd4712_Out_1_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float);
            float _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float;
            Unity_Add_float(_Add_a48d2dbec83048b987f691902d31bc0d_Out_2_Float, _OneMinus_72d03cc7c61d472b8bdc267ec57bc3cb_Out_1_Float, _Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float);
            float3 _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3 = _EdgeWidthSmoothStr;
            float _Split_455b73b72b364d559a420ef8b3344938_R_1_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[0];
            float _Split_455b73b72b364d559a420ef8b3344938_G_2_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[1];
            float _Split_455b73b72b364d559a420ef8b3344938_B_3_Float = _Property_d4fc079fda6a4609ae5d24884ed83367_Out_0_Vector3[2];
            float _Split_455b73b72b364d559a420ef8b3344938_A_4_Float = 0;
            float _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float;
            Unity_Add_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Split_455b73b72b364d559a420ef8b3344938_G_2_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float);
            float _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float;
            Unity_Smoothstep_float(_Split_455b73b72b364d559a420ef8b3344938_R_1_Float, _Add_3235d0cc619d4837b74e919977e602a4_Out_2_Float, _Add_1cba0be94050468ca7919b0fb871a754_Out_2_Float, _Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float);
            float _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float;
            Unity_OneMinus_float(_Smoothstep_5966efa27ef649f6ae88f064ead9abe8_Out_3_Float, _OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float);
            float _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float;
            Unity_Multiply_float_float(_OneMinus_40186a361caa4deb9656c987d017715f_Out_1_Float, _Split_455b73b72b364d559a420ef8b3344938_B_3_Float, _Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float);
            float _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float;
            Unity_Saturate_float(_Multiply_505c0aea7bd64716821c72d67a7ec508_Out_2_Float, _Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float);
            float _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float;
            Unity_OneMinus_float(_Saturate_25b6af7a12c944dfb0d1fa66038913e7_Out_1_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float);
            float _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float;
            Unity_Add_float(_Add_65b89a59c02c48d3a04abd3677159f4f_Out_2_Float, _OneMinus_553fa913ed8a44d3b34ed2a6c7d48288_Out_1_Float, _Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float);
            float _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float;
            Unity_Saturate_float(_Add_5561db6e5b4544ffa4036345964cf47b_Out_2_Float, _Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float);
            float _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float;
            Unity_Multiply_float_float(_Smoothstep_204cfe29abc24865a4126db17f3da4c8_Out_3_Float, _Saturate_8d19a5c0d53f40dd935304333023f170_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float);
            float _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float;
            Unity_Multiply_float_float(_Saturate_25396e2e24d74f5f831d54458681ee02_Out_1_Float, _Multiply_1d07674708754076b852ab20c9e157f1_Out_2_Float, _Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float);
            float _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            Unity_Saturate_float(_Multiply_857312cb779e45dfa415c7c7ec57382b_Out_2_Float, _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float);
            surface.Alpha = _Saturate_61e9a9f7a12c462ea8c9de472aa5f2f8_Out_1_Float;
            surface.AlphaClipThreshold = float(0.5);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            
        
            // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            float3 unnormalizedNormalWS = input.normalWS;
            const float renormFactor = 1.0 / length(unnormalizedNormalWS);
        
        
            output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
        
            // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
            // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
            output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
        
            output.WorldSpacePosition = input.positionWS;
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphUnlitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}