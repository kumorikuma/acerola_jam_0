Shader  "CustomURP/PostProcessOutline"
{

    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _CreaseAngleThreshold("Crease Angle Threshold", Range(0, 3.14)) = 0.1
        _DepthThreshold("Depth Threshold", Range(0, 100)) = 10
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    
    #pragma shader_feature_local USE_NORMAL_OUTLINE
    
    TEXTURE2D(_CameraColorTexture);
    SAMPLER(sampler_CameraColorTexture);
    uniform float4 _CameraColorTexture_TexelSize;
    uniform float4 _CameraDepthTexture_TexelSize;
    uniform float4 _CameraNormalsTexture_TexelSize;
    
    float3 _OutlineColor;
    float _CreaseAngleThreshold;
    float _DepthThreshold;

    void SampleSceneNormals(float2 uv, out float3 samples[9])
    {
        float2 Texel = (1.0) / float2(_CameraNormalsTexture_TexelSize.z, _CameraNormalsTexture_TexelSize.w);
        
        samples[0] = SampleSceneNormals(uv + float2(-Texel.x, Texel.y));
        samples[1] = SampleSceneNormals(uv + float2(0, Texel.y));
        samples[2] = SampleSceneNormals(uv + float2(Texel.x, Texel.y));
        samples[3] = SampleSceneNormals(uv + float2(-Texel.x, 0));
        samples[4] = SampleSceneNormals(uv + float2(0, 0));
        samples[5] = SampleSceneNormals(uv + float2(Texel.x, 0));
        samples[6] = SampleSceneNormals(uv + float2(-Texel.x, -Texel.y));
        samples[7] = SampleSceneNormals(uv + float2(0, -Texel.y));
        samples[8] = SampleSceneNormals(uv + float2(Texel.x, -Texel.y));
    }

    void SampleDepthTexture(float2 uv, out float samples[9])
    {
        float2 Texel = (1.0) / float2(_CameraDepthTexture_TexelSize.z, _CameraDepthTexture_TexelSize.w);
        
        samples[0] = SampleSceneDepth(uv + float2(-Texel.x, Texel.y));
        samples[1] = SampleSceneDepth(uv + float2(0, Texel.y));
        samples[2] = SampleSceneDepth(uv + float2(Texel.x, Texel.y));
        samples[3] = SampleSceneDepth(uv + float2(-Texel.x, 0));
        samples[4] = SampleSceneDepth(uv + float2(0, 0));
        samples[5] = SampleSceneDepth(uv + float2(Texel.x, 0));
        samples[6] = SampleSceneDepth(uv + float2(-Texel.x, -Texel.y));
        samples[7] = SampleSceneDepth(uv + float2(0, -Texel.y));
        samples[8] = SampleSceneDepth(uv + float2(Texel.x, -Texel.y));
    }
    
    half4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        float edgeResponse = 0;
        float3 originalColor = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uv);
        
        // Camera Normal Texture
        float3 normalSamples[9];
        SampleSceneNormals(uv, normalSamples);

        // Need the depth samples to tell apart the background
        float depthSamples[9];
        SampleDepthTexture(uv, depthSamples);
    
        // Compare the center normal vector against all neighboring normal vectors. If there's a difference greater than
        // a threshold, then this is an edge pixel.
        const float3 centerNormal = normalSamples[4];
        const float3 centerDepth = depthSamples[4];
        const bool isCenterBg = centerDepth <= _DepthThreshold;
        for (int i = 0; i < 4; i++)
        {
            if (i == 4)
            {
                continue;
            }
            
            const float3 neighborNormal = normalSamples[i];
            const float3 neighborDepth = depthSamples[i];
            const bool isNeighborhoodBg = neighborDepth <= _DepthThreshold;
            if (isCenterBg && isNeighborhoodBg)
            {
                // Special case of BG vs BG check
                continue;
            } else if (isNeighborhoodBg)
            {
                // Special case where center is not BG but neighbor is BG
                edgeResponse = 1.0f;
                break;
            }
            
            float angle = acos(dot(centerNormal, neighborNormal));
            if (angle > _CreaseAngleThreshold)
            {
                edgeResponse = 1.0f;
                break;
            }
        }
        
        // Multiply in the outline color
        float3 outlineColor = float3(edgeResponse, edgeResponse, edgeResponse) * _OutlineColor;

        // Debugging
        // float3 normalsColor = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv);
        // float3 depthColor = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv) * 10;
        
        // If there's no outline, render just what the color would've been.
        if (edgeResponse > 0.0f)
        {
            return half4(outlineColor, 1);
        } else
        {
            return half4(originalColor, 1);
        }
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
            LOD 100
            ZTest Always ZWrite Off Cull Off

            Pass
        {
            Name "PostProcess_Outline"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }

}
