﻿Shader "Custom/Transparent Diffuse (Mask cutoff)" {
	Properties{
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}

		/*_Cutoff("Mask cutoff", Range(0,1)) = 0
		_Blur("Blurring alpha", Range(0,50)) = 0*/
		_MaskTex("Mask", 2D) = "white" {}
		_MainTexMoveSpeedU("U Move Speed", Range(-50,50)) = 0.5
		_MainTexMoveSpeedV("V Move Speed", Range(-50,50)) = 0.5

	}

	SubShader{
		Tags{ "Queue" = "Transparent+99" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Cull Off
		ZWrite On
		Lighting Off

		CGPROGRAM
		#pragma surface surf Lambert
		 
		sampler2D _MainTex;
		sampler2D _MaskTex;
		/*half _Cutoff;
		half _Blur;*/
		fixed _MainTexMoveSpeedU;
		fixed _MainTexMoveSpeedV;

		struct Input {
			float2 uv_MainTex;
			float2 uv_MaskTex;
		};

		void surf(Input IN, inout SurfaceOutput o) {

			// Moving

			fixed2 MainTexMoveScrolledUV = IN.uv_MaskTex;

			fixed MainTexMoveU = _MainTexMoveSpeedU * _Time;
			fixed MainTexMoveV = _MainTexMoveSpeedV * _Time;

			MainTexMoveScrolledUV += fixed2(MainTexMoveU, MainTexMoveV);

			// Alpha cutoff


			half mask = tex2D(_MaskTex, IN.uv_MaskTex);
			fixed4 c = tex2D(_MainTex, MainTexMoveScrolledUV);
			
			/*clip(mask.a - _Cutoff * 1.004);*/		//*1.004); 1.004 = 1+(1/255) to make sure also white is clipped

			o.Albedo = c.rgb;
			o.Alpha = mask; //.r * (1 - c.a)

		}

		ENDCG
	}

	Fallback "Transparent/Diffuse"
}