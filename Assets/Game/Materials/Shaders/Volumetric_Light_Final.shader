Shader "Custom/Volumetric_Light_Final"
{
    Properties
    {
      _FarFogColor("_FarFogColor", Color) = (1, 1, 1, 1)
      _NearFogColor("_NearFogColor", Color) = (0, 0, 0, 1)
      _FarFogColorTop("_FarFogColorTop", Color) = (1, 1, 1, 1)
      _NearFogColorTop("_NearFogColorTop", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags {
        	"LightMode" = "Custom_Volumetric_Light"
        }
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "Volumetric_Light_Final.hlsl"

            ENDHLSL
        }
    }
}
