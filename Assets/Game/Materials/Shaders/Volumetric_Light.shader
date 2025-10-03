Shader "Custom/Volumetric_Light"
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
        //Cull Front
        Cull Back
		ZWrite Off
		ZTest LEqual
		//ZTest Always
		Blend One One
		//Blend SrcAlpha OneMinusSrcAlpha
        //BlendOp Max

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
			#pragma fragment frag
            #include "Volumetric_Light.hlsl"

			ENDHLSL
        }
    }
}
