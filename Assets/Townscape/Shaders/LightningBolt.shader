// The glowing channel of a lightning bolt. Unlit, additive and deliberately free of fog: a real
// bolt cuts through rain and murk that hide everything else at the same distance, and the
// storm's fog is far too thick to let a bolt a kilometre away through otherwise.
Shader "Townscape/Lightning Bolt"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (6, 6.5, 9, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Bolt"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // Vertex alpha fades the tips; the colour carries the HDR brightness.
                return half4(_Color.rgb * input.color.rgb * input.color.a, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
