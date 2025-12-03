Shader "Custom/Copy_Depth_For_Blur"
{
    Properties
    {
    }
    SubShader
    {
        Tags {
          "LightMode" = "Custom_Volumetric_Light"
        }
        LOD 100
        Cull Off
		ZTest Always
		ZWrite On
		Blend One Zero

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
			#pragma fragment frag
            #include "Copy_Depth_For_Blur.hlsl"

			ENDHLSL
        }
    }
}
