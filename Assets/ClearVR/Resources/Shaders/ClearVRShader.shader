//
// A special thanks goes out to Joey from Stage for his invaluable contribution to this shader.
//

Shader "ClearVR/ClearVRShader" {
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
        Tags { "RenderType" = "Opaque" "Queue" = "Background" "IgnoreProjector" = "True"  }
		LOD 100
		Lighting Off
		Cull Off
        // These values are toggled from code.
		ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]
		Pass {
            Name "GLSL"
            GLSLPROGRAM
                //#version 310 es
                #include "UnityCG.glslinc"
#if defined(USE_UI)
               // #include "UnityUI.cginc"
#endif
                #define SHADERLAB_GLSL
                #include "GLSLSupport.glslinc"
                // Step 1. import generic and vertex-shader specific functions
                #include "ClearVR.cginc"
                #pragma only_renderers gles gles3
				#pragma multi_compile STEREO_CUSTOM_UV_OFF STEREO_CUSTOM_UV_ON
				#pragma multi_compile USE_OES_FAST_PATH_OFF USE_OES_FAST_PATH_ON
                #pragma multi_compile __ USE_NV12 USE_YUV420P
                #pragma multi_compile __ GAMMA_TO_LINEAR_CONVERSION
                #pragma multi_compile __ USE_UI
                
                #extension GL_OES_EGL_image_external       : enable
                // Full specification:
                // https://www.khronos.org/registry/OpenGL/extensions/OES/OES_EGL_image_external_essl3.txt
                // Note that #version, #extension, and some others are passed straight through (ignoring any preprocessor defines). See glcpp
                // Source: https://github.com/aras-p/glsl-optimizer/tree/master/src/glsl
                // This means that we cannot "require" this extension as it would break backwards compatibility with OpenGLES 2. For now, "enable" should suffice.
                #extension GL_OES_EGL_image_external_essl3 : enable

                // OVR_multiview + OVR_multiview2
                // Full specifications:
                // https://www.khronos.org/registry/OpenGL/extensions/OVR/OVR_multiview.txt
                // https://www.khronos.org/registry/OpenGL/extensions/OVR/OVR_multiview2.txt
                // Multiview2 relaxes the constraints of only gl_Position being able to be influenced by gl_ViewID_OVR
                // Multiview2 implicitly enabled multiview but here we are being explicit

				#extension GL_OVR_multiview                : enable
                #extension GL_OVR_multiview2               : enable
                // Force high precision on our shader to prevent rounding errors from creeping up
                precision highp float;

                #ifdef VERTEX
                    #if __VERSION__ >= 300
                        /// Unity Stereo uniforms
                        layout(std140) uniform UnityStereoGlobals {
                            mat4 unity_StereoMatrixP[2];
                            mat4 unity_StereoMatrixV[2];
                            mat4 unity_StereoMatrixInvV[2];
                            mat4 unity_StereoMatrixVP[2];
                            mat4 unity_StereoCameraProjection[2];
                            mat4 unity_StereoCameraInvProjection[2];
                            mat4 unity_StereoWorldToCamera[2];
                            mat4 unity_StereoCameraToWorld[2];
                            vec3 unity_StereoWorldSpaceCameraPos[2];
                            vec4 unity_StereoScaleOffset[2];
                        };
                        
                        layout(std140) uniform UnityStereoEyeIndices {
                            vec4 unity_StereoEyeIndices[2];
                        };
                        
                        #if defined(STEREO_MULTIVIEW_ON)
                            // For GL_OVR_multiview we use gl_ViewID_OVR to get the current view index
                            layout(num_views = 2) in; 
                        #endif
                    #endif

                    varying vec2 uvs;
                    uniform vec4 _MainTex_ST;
        			uniform vec3 _cameraPosition;
	                uniform mat4 _viewMatrix;

                    #if defined (STEREO_CUSTOM_UV_ON) 
                        varying vec2 texcoord[2];
                    #endif

                    vec2 transformTex(vec2 texCoord, vec4 texST) {
                        return (texCoord.xy * texST.xy + texST.zw);
                    }

                    void main() {
                        int eyeIndex = GetStereoEyeIndex(_cameraPosition, _viewMatrix[0].xyz);
                        #if defined (STEREO_MULTIVIEW_ON)
                            gl_Position = unity_StereoMatrixVP[eyeIndex] * unity_ObjectToWorld * gl_Vertex;
                        #else
                            // Remember that gl_ModelViewProjectionMatrix = unity_MatrixVP * unity_ObjectToWorld
                            gl_Position = unity_MatrixVP * unity_ObjectToWorld * gl_Vertex;
                        #endif

                        #if defined (STEREO_CUSTOM_UV_ON)
                            texcoord[0] = gl_MultiTexCoord0.xy;
                            texcoord[1] = gl_MultiTexCoord1.xy;
                            uvs = transformTex(texcoord[eyeIndex], _MainTex_ST);
                        #else
                            uvs = transformTex(gl_MultiTexCoord0.xy, _MainTex_ST);
                        #endif
                    }
                #endif  

                #ifdef FRAGMENT
                    varying vec2 uvs;
                    uniform SAMPLER2D _MainTex;
                    #if defined(USE_NV12)
                        uniform SAMPLER2D _ChromaTex;
                        uniform FLOAT4X4 _ColorSpaceTransformMatrix;
                    #elif defined(USE_YUV420P)
                        uniform SAMPLER2D _ChromaTex;
                        uniform SAMPLER2D _ChromaTex2;
                        uniform FLOAT4X4 _ColorSpaceTransformMatrix;
                    #endif
                    uniform FLOAT4X4 _TextureTransformMatrix;
                    uniform vec4 _Color;
                    
                    // Step 2. import fragment-shader specific functions. Must happen just before main() {}
                    #include "ClearVR.cginc"

                    void main() {
                           gl_FragColor = GetRGBAPixel(GET_RGBA_PIXEL_ARGS, uvs) * _Color;
                    }
                #endif
            ENDGLSL
        }
    }
	SubShader
	{
        Tags { "RenderType" = "Opaque" "Queue" = "Background" "IgnoreProjector" = "True"  }
		LOD 100
		Lighting Off
		Cull Off
        // These values are toggled from code.
		ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
		Pass
		{
            Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers metal d3d11 glcore
			#pragma multi_compile STEREO_CUSTOM_UV_OFF STEREO_CUSTOM_UV_ON
            #pragma multi_compile __ USE_NV12 USE_YUV420P
            #pragma multi_compile __ GAMMA_TO_LINEAR_CONVERSION
            #pragma multi_compile __ USE_UI

			#include "UnityCG.cginc"
#if defined(USE_UI)
            #include "UnityUI.cginc"
#endif
            // Step 1. import generic and vertex-shader specific functions
			#include "ClearVR.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct appdata {
				float4 vertex : POSITION;
				fixed4 color    : COLOR;
				float2 uv : TEXCOORD0;
// TODO: check if we can get rid of the STEREO_CUSTOM_UV_ON check. It appears as if we only need the STEREO_CUSTOM_UV (at least of D3D11)
#if defined(STEREO_CUSTOM_UV) || defined(STEREO_CUSTOM_UV_ON)
				float2 uv2 : TEXCOORD1;
#endif
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
				UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color    : COLOR;
				float2 uv : TEXCOORD0;
#if defined(UNITY_UI_CLIP_RECT) && defined(USE_UI)
                float4 worldPosition : TEXCOORD1;
#endif
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
				UNITY_VERTEX_OUTPUT_STEREO
#endif
			};

			uniform SAMPLER2D _MainTex;
            #if defined(USE_NV12)
                uniform SAMPLER2D _ChromaTex;
                uniform FLOAT4X4 _ColorSpaceTransformMatrix;
            #elif defined(USE_YUV420P)
                uniform SAMPLER2D _ChromaTex;
                uniform SAMPLER2D _ChromaTex2;
                uniform FLOAT4X4 _ColorSpaceTransformMatrix;
            #endif
            uniform FLOAT4X4 _TextureTransformMatrix;
			uniform float4 _MainTex_ST;
			uniform float3 _cameraPosition;
			uniform fixed4 _Color;
            float4 _ClipRect;

			v2f vert (appdata appData) {
				v2f output;
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
				// Source: https://docs.unity3d.com/Manual/SinglePassInstancing.html
				UNITY_SETUP_INSTANCE_ID(appData);					// Calculates and sets the built-in unity_StereoEyeIndex and unity_InstanceID Unity shader variables to the correct values based on which eye the GPU is currently rendering.
				UNITY_INITIALIZE_OUTPUT(v2f, output);				// Initializes all v2f values to 0.
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);		// Tells the GPU which eye in the texture array it should render to, based on the value of unity_StereoEyeIndex. This macro also transfers the value of unity_StereoEyeIndex from the vertex shader...
#endif

				output.vertex = UnityObjectToClipPos(appData.vertex);

#if defined(STEREO_CUSTOM_UV) || defined(STEREO_CUSTOM_UV_ON)
				if (GetStereoEyeIndex(_cameraPosition, UNITY_MATRIX_V[0].xyz) == 1) {
					output.uv.xy = TRANSFORM_TEX(appData.uv2, _MainTex);
				} else {
    				output.uv.xy = TRANSFORM_TEX(appData.uv, _MainTex);
                }
#else
                output.uv.xy = TRANSFORM_TEX(appData.uv, _MainTex);
#endif
                output.color = appData.color * _Color;
#if defined(UNITY_UI_CLIP_RECT) && defined(USE_UI)
                output.worldPosition = appData.vertex;
#endif
				return output;
			}

            // Step 2. import fragment-shader specific functions. Must happen just before the fragment shader.
            #include "ClearVR.cginc"

			fixed4 frag (v2f input) : SV_Target {
                fixed4 col = GetRGBAPixel(GET_RGBA_PIXEL_ARGS, input.uv.xy);
				col *= input.color;
                #if defined(UNITY_UI_CLIP_RECT) && defined(USE_UI)
                col.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #if defined(UNITY_UI_ALPHACLIP) && defined(USE_UI)
                clip (col.a - 0.001);
                #endif
				return col;
			}
			ENDCG
		}
	}    
}