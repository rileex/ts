Shader "ALIyerEdon/Screen Rain" {
Properties {
	_intensity("Intensity",Range(1,10)) = 3
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 200

CGPROGRAM
#pragma surface surf Lambert alpha:fade

sampler2D _MainTex;


float _intensity;

struct Input {
	float2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.r*_intensity;
	o.Alpha = c.r;
}
ENDCG
}

Fallback "Legacy Shaders/Transparent/VertexLit"
}
