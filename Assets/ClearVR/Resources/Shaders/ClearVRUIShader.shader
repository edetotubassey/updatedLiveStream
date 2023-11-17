//
// A special thanks goes out to Joey from Stage for his invaluable contribution to this shader.
//

Shader "ClearVR/ClearVRUIShader" {
    Properties {
        _MainTex ("Texture", 2D) = "black" {}
        _ChromaTex ("ChromaTexture", 2D) = "black" {} // this contain UV for nv12/YpCbCr, nothing otherwise.
        _ChromaTex2 ("ChromaTexture2", 2D) = "black" {}
		_Color("Main Color", Color) = (1,1,1,1)
		[Toggle(STEREO_CUSTOM_UV)] Stereo("Stereo Mode", Float) = 0
		[Toggle(USE_OES_FAST_PATH)] Stereo("OES Fast Path", Float) = 0
		[Toggle(PICO_VR_EYE_INDEX)] Stereo("Pico VR Eye Index", Float) = 0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0

         // required for UI.Mask
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader {
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
            "DisableBatching"="True"
        }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]
		LOD 100
		Lighting Off
		Cull Off
        // These values are toggled from code.
		ZWrite [_ZWrite]
        // Blend [_SrcBlend] [_DstBlend]
		Blend SrcAlpha OneMinusSrcAlpha


		UsePass "ClearVR/ClearVRShader/GLSL"
    }
	SubShader
	{
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="False"
            "DisableBatching"="True"
        }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]
		LOD 100
		Lighting Off
		Cull Off
        // These values are toggled from code.
		// ZWrite [_ZWrite]
		ZWrite Off
        // Blend [_SrcBlend] [_DstBlend]
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha


		UsePass "ClearVR/ClearVRShader/Default"
		
	}    
}