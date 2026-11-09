Shader "Custom/ColorMaskRGB"
{
    Properties
    {
        _RedMaskColor("Color Mask Red", Color) = (1,0,0,1)
        _GreenMaskColor("Color Mask Green", Color) = (0,1,0,1)
        _BlueMaskColor("Color Mask Blue", Color) = (0,0,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _MaskTex("Mask (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
    {
        Tags{ "RenderType" = "Opaque" }
        LOD 200
 
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard fullforwardshadows
        // Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0
    sampler2D _MainTex;
    sampler2D _MaskTex;
 
    struct Input {
        float2 uv_MainTex;
        float2 uv_MaskTex;
    };
 
    half _Glossiness;
    half _Metallic;
    fixed4 _RedMaskColor;
    fixed4 _GreenMaskColor;
    fixed4 _BlueMaskColor;
 
    void surf(Input IN, inout SurfaceOutputStandard o)
    {
        // Albedo comes from a texture tinted by color masked by a second texture
        fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
        // reuse the main texture's UVs for the mask texture
        fixed4 mask = tex2D(_MaskTex, IN.uv_MainTex);
 
        if (mask.r > 0)
        {
            o.Albedo = lerp(c, _RedMaskColor, pow(mask.r, 2));
        }
        else if (mask.g > 0)
        {
            o.Albedo = lerp(c, _GreenMaskColor, pow(mask.g, 2));
        }
        else if (mask.b > 0)
        {
            o.Albedo = lerp(c, _BlueMaskColor, pow(mask.b, 2));
        }
        else
        {
            o.Albedo = c;
        }
 
        // Metallic and smoothness come from slider variables
        o.Metallic = _Metallic;
        o.Smoothness = _Glossiness;
    }
    ENDCG
 
    }
        FallBack "Standard"
}