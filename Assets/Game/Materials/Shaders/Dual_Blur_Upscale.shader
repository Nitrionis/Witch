Shader "Custom/Dual_Blur_Upscale"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
		ZWrite Off
		ZTest Always
		Blend  Off

        Pass
        {
            Name "Dual_Blur_Upscale"

            HLSLPROGRAM

            #pragma vertex upVert
            #pragma fragment upFrag
            #include "Dual_Blur.hlsl"

            ENDHLSL
        }
    }
}
