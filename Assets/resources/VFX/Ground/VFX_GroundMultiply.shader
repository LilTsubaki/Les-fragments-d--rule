﻿Shader "Custom/ VFX_GroundMultiply" {
	Properties {
		_MainTex ("Grayscale TilingTex", 2D) = "white" {}
		_Mask ("Grayscale Mask", 2D) = "white" {}
		_MaskContrast("Mask contrast", Range(0.1,5)) = 1 // the contrast is blown out below 1
		_Tint("Emissive tint", color) = (1,1,1) // half3 color value (do not need alpha)
		_Opacity("Opacity", Range(0,1)) = 0 
		_Strength("Emissive strength", Range (1,100)) = 1
		_Contrast("Contrast", Range(0.1,3)) = 1 // the contrast is blown out below 1

		_TilingTexMoveSpeedV("V Move Speed", Range(-5,5)) = 0.5
		_TilingTexMoveSpeedU("U Move Speed", Range(-5,5)) = 0.5

	}
	SubShader {
		Tags { "Queue" = "Transparent+99" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		ZWrite Off
		Cull Off
		Lighting On
		BindChannels{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord
			Bind "Color", color
		}
		Blend Zero SrcColor
		//Blend DstColor Zero
		//Blend DstColor SrcColor
		
		CGPROGRAM

		#pragma surface surf Lambert full

		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Mask;

		half3 _Tint;
		half _Opacity;
		half _Strength;
		half _Contrast;
		half _MaskContrast;

		float _TilingTexMoveSpeedU;
		float _TilingTexMoveSpeedV;

		struct Input {
			float2 uv_MainTex;
			float2 uv_Mask;
		};

		void surf (Input IN, inout SurfaceOutput o) {

			float2 TilingTexMoveScrolledUV = IN.uv_MainTex; // screen proection ==> replace IN.uv_TilingTex by screenUV
			
			float TilingTexMoveU = _TilingTexMoveSpeedU * _Time;
			float TilingTexMoveV = _TilingTexMoveSpeedV * _Time;
			TilingTexMoveScrolledUV += float2(TilingTexMoveU, TilingTexMoveV);

			half c = pow(tex2D (_MainTex, TilingTexMoveScrolledUV), _Contrast); 
			half d = pow(tex2D (_Mask, IN.uv_Mask), _MaskContrast);
			 
			o.Albedo = c * _Tint + (1 - d);
			o.Albedo = lerp(1, o.Albedo, _Opacity);
			o.Emission = lerp(0, c, _Opacity) * _Tint * _Strength;

		}
		ENDCG
	}
	FallBack "Diffuse"
}
