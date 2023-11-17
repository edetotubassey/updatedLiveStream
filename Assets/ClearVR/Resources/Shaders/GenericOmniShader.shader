// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//
// A special thanks goes out to Joey from Stage for his invaluable contribution to this shader.
//

Shader "ClearVR/GenericNonClearVROmni" {
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
        [HideInInspector] _SensorDensity ("__sensorDensity", Float) = 75.346033292
        [HideInInspector] _FocalLength ("__focalLength", Float) = 3.25
        [HideInInspector] _CircularRadiusInRad ("__circularRadiusInRad", Float) = 0
        [HideInInspector] _CenterU ("__centerU", Float) = 958.923715
        [HideInInspector] _CenterV ("__centerV", Float) = 649.093850
        [HideInInspector] _AffineParameterC ("__affineParameterC", Float) = 1.000096
        [HideInInspector] _AffineParameterD ("__affineParameterD", Float) = 0.000048
        [HideInInspector] _AffineParameterE ("__affineParameterE", Float) = 0.000068
        [HideInInspector] _ReferenceWidth ("__referenceWidth", Float) = 807.0
        [HideInInspector] _ReferenceHeight ("__referenceHeight", Float) = 807.0
        [HideInInspector] _StereoUOffset ("__stereoUOffset", Float) = 0.0 //shall be 0.5 if stereo side-by-side else 0. Note: stereo not implemented in ClearVRMeshBase!
        [HideInInspector] _StereoUConstantOffset ("__stereoUConstantOffset", Float) = 0.0 //shall be 0.25 if stereo side-by-side else 0. Note: stereo not implemented in ClearVRMeshBase!
        [HideInInspector] _StereoVOffset ("__stereoVOffset", Float) = 0.0 //shall be 0.5 if stereo top-bottom else 0. Note: stereo not implemented in ClearVRMeshBase!
        [HideInInspector] _StereoVConstantOffset ("__stereoVConstantOffset", Float) = 0.0 //shall be 0.25 if stereo top-bottom else 0. Note: stereo not implemented in ClearVRMeshBase!
        [HideInInspector] _MonoUFactor ("__monoUFactor", Float) = 1.0 //shall be set to 1.0 
        [HideInInspector] _MonoVFactor ("__monoVFactor", Float) = 1.0 //shall be set to 1.0 (currently ignored)
        [HideInInspector] _LongitudeOffsetInRad ("__LongitudeOffsetInRad", Float) = 0.0 //shall be set to 0.0 except for 360 ERP where it shall be set to -PI/2.0
    }
	SubShader {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" "IgnoreProjector" = "True"  }
		LOD 100
		Lighting Off
		Cull Off
        // These values are toggled from code.
		ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]
		Pass {
            GLSLPROGRAM
                //#version 310 es
                #include "UnityCG.glslinc"
                #define SHADERLAB_GLSL
                // Step 1. import generic and vertex-shader specific functions
                #include "ClearVR.cginc"
                #pragma only_renderers gles gles3
				#pragma multi_compile STEREO_CUSTOM_UV_OFF STEREO_CUSTOM_UV_ON
				#pragma multi_compile USE_OES_FAST_PATH_OFF USE_OES_FAST_PATH_ON
				#pragma multi_compile FISH_EYE_EQUISOLID FISH_EYE_EQUIDISTANT FISH_EYE_POLYNOME ERP
                #pragma multi_compile __ USE_NV12 USE_YUV420P
                #pragma multi_compile __ GAMMA_TO_LINEAR_CONVERSION
                
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
                    varying vec2 uvsStereoOffset;
                    varying vec3 normals;
                    uniform vec4 _MainTex_ST;
        			uniform vec3 _cameraPosition;
	                uniform mat4 _viewMatrix;
                    uniform float _StereoUOffset;
                    uniform float _StereoUConstantOffset;
                    uniform float _StereoVOffset;
                    uniform float _StereoVConstantOffset;
                    uniform float _MonoUFactor;
#if defined(FISH_EYE_EQUISOLID) || defined(FISH_EYE_EQUIDISTANT)
                    uniform float _SensorDensity;
			        uniform float _FocalLength;
                    uniform float _ReferenceWidth;
                    uniform float _ReferenceHeight;
                    uniform float _CircularRadiusInRad;
#elif defined(FISH_EYE_POLYNOME)
                    uniform float _ReferenceWidth;
                    uniform float _ReferenceHeight;
                    uniform float _CircularRadiusInRad;
                    uniform float _CenterU;
                    uniform float _CenterV;
                    uniform float _AffineParameterC;
                    uniform float _AffineParameterD;
                    uniform float _AffineParameterE;
                    uniform mat4 _SphereToPlanPolynome;
#endif


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
                            gl_Position = unity_MatrixVP * unity_ObjectToWorld * gl_Vertex;
                        #endif
						
                        normals = gl_Normal;

                        #if defined (STEREO_CUSTOM_UV_ON)
                            texcoord[0] = gl_MultiTexCoord0.xy;
                            texcoord[1] = gl_MultiTexCoord1.xy;
                            uvs = transformTex(texcoord[eyeIndex], _MainTex_ST);
                            uvsStereoOffset = vec2(_StereoUOffset * float(eyeIndex) - _StereoUConstantOffset, _StereoVOffset * float(eyeIndex) - _StereoVConstantOffset);
                        #else
                            uvs = transformTex(gl_MultiTexCoord0.xy, _MainTex_ST);
                            uvsStereoOffset = vec2(-_StereoUConstantOffset, -_StereoVConstantOffset);
                        #endif
                    }
                #endif  

                #ifdef FRAGMENT
                    varying vec2 uvs;
                    varying vec2 uvsStereoOffset;
                    varying vec3 normals;
                    uniform float _MonoUFactor;
                    uniform float _MonoVFactor;
#if defined(ERP)
                    uniform float _LongitudeOffsetInRad;
#endif
#if defined(FISH_EYE_EQUISOLID) || defined(FISH_EYE_EQUIDISTANT)
                    uniform float _SensorDensity;
			        uniform float _FocalLength;
                    uniform float _ReferenceWidth;
                    uniform float _ReferenceHeight;
                    uniform float _CircularRadiusInRad;
#elif defined(FISH_EYE_POLYNOME)
                    uniform float _ReferenceWidth;
                    uniform float _ReferenceHeight;
                    uniform float _CircularRadiusInRad;
                    uniform float _CenterU;
                    uniform float _CenterV;
                    uniform float _AffineParameterC;
                    uniform float _AffineParameterD;
                    uniform float _AffineParameterE;
                    uniform mat4 _SphereToPlanPolynome;
#endif

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

                    // Step 2. import fragment-shader specific functions. Must happen just before its main() { }.
                    #include "ClearVR.cginc"


                    void main() {
#if defined(FISH_EYE_EQUISOLID)
    vec2 equiUV = RadialCoordsFishEyeEquiSolid(normals, _ReferenceWidth, _ReferenceHeight, _CircularRadiusInRad, _FocalLength, _SensorDensity);
#elif defined(FISH_EYE_EQUIDISTANT)
    vec2 equiUV = RadialCoordsFishEyeEquidistant(normals, _ReferenceWidth, _ReferenceHeight, _CircularRadiusInRad, _FocalLength, _SensorDensity);
#elif defined(FISH_EYE_POLYNOME)
    vec2 equiUV = RadialCoordsFishEyePolynomial(normals, _ReferenceWidth, _ReferenceHeight, _CircularRadiusInRad, _CenterU, _CenterV, _AffineParameterC, _AffineParameterD, _AffineParameterE, _SphereToPlanPolynome);
#elif defined(ERP)
    vec2 equiUV = RadialCoordsERP(normals, _LongitudeOffsetInRad);
#else
    //else compilation will fail
#endif
                        equiUV[0] *= _MonoUFactor;
                        equiUV[1] *= _MonoVFactor;
                        equiUV += uvsStereoOffset;
                        if ( equiUV[0] < uvsStereoOffset.x || equiUV[0] >= _MonoUFactor + uvsStereoOffset.x || equiUV[1] < uvsStereoOffset.y || equiUV[1] >= _MonoVFactor + uvsStereoOffset.y) {
                            discard;
                        }
                        gl_FragColor = GetRGBAPixel(GET_RGBA_PIXEL_ARGS, equiUV) * _Color;
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
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers metal d3d11 glcore
			#pragma multi_compile STEREO_CUSTOM_UV_OFF STEREO_CUSTOM_UV_ON
			#pragma multi_compile FISH_EYE_EQUISOLID FISH_EYE_EQUIDISTANT FISH_EYE_POLYNOME ERP
            #pragma multi_compile __ USE_NV12 USE_YUV420P
            #pragma multi_compile __ GAMMA_TO_LINEAR_CONVERSION

			#include "UnityCG.cginc"
            // Step 1. import generic and vertex-shader specific functions
			#include "ClearVR.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
// TODO: check if we can get rid of the STEREO_CUSTOM_UV_ON check. It appears as if we only need the STEREO_CUSTOM_UV (at least of D3D11)
#if defined(STEREO_CUSTOM_UV) || defined(STEREO_CUSTOM_UV_ON)
				float2 uv2 : TEXCOORD1;
#endif
				float3 normal : NORMAL;
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
				UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
			};

			struct v2f {
				float4    pos : SV_POSITION;
                float3    normal : NORMAL;
                float2    stereoUVShift : TEXCOORD2;
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
                UNITY_VERTEX_OUTPUT_STEREO
#endif
			}; 

			uniform sampler2D _MainTex;
#if defined(USE_NV12)
			uniform sampler2D _ChromaTex;
            uniform FLOAT4X4 _ColorSpaceTransformMatrix;
#elif defined(USE_YUV420P)
			uniform sampler2D _ChromaTex;
			uniform sampler2D _ChromaTex2;
            uniform FLOAT4X4 _ColorSpaceTransformMatrix;
#endif
            uniform FLOAT4X4 _TextureTransformMatrix;
			uniform float4 _MainTex_ST;
			uniform float4 _MainTex_TexelSize;
			uniform float3 _cameraPosition;
			uniform fixed4 _Color;
#if defined(ERP)
            uniform float _LongitudeOffsetInRad;
#endif
#if defined(FISH_EYE_EQUISOLID) || defined(FISH_EYE_EQUIDISTANT)
			uniform float _SensorDensity;
			uniform float _FocalLength;
            uniform float _ReferenceWidth;
            uniform float _ReferenceHeight;
            uniform float _CircularRadiusInRad;
#elif defined(FISH_EYE_POLYNOME)
            uniform float _ReferenceWidth;
            uniform float _ReferenceHeight;
            uniform float _CircularRadiusInRad;
            uniform float _CenterU;
            uniform float _CenterV;
            uniform float _AffineParameterC;
            uniform float _AffineParameterD;
            uniform float _AffineParameterE;
            uniform float4x4 _SphereToPlanPolynome;
#endif
			uniform float _StereoUOffset;
            uniform float _StereoUConstantOffset;
			uniform float _StereoVOffset;
			uniform float _StereoVConstantOffset;
            uniform float _MonoUFactor;
            uniform float _MonoVFactor;

            v2f vert (appdata v) {
                v2f o;
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
                // Source: https://docs.unity3d.com/Manual/SinglePassInstancing.html
                UNITY_SETUP_INSTANCE_ID(v);					// Calculates and sets the built-in unity_StereoEyeIndex and unity_InstanceID Unity shader variables to the correct values based on which eye the GPU is currently rendering.
                UNITY_INITIALIZE_OUTPUT(v2f, o);			// Initializes all v2f values to 0.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);	// Tells the GPU which eye in the texture array it should render to, based on the value of unity_StereoEyeIndex. This macro also transfers the value of unity_StereoEyeIndex from the vertex shader...
#endif
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
#if STEREO_CUSTOM_UV || defined(STEREO_CUSTOM_UV_ON)
                int stereoIndex = GetStereoEyeIndex(_cameraPosition, UNITY_MATRIX_V[0].xyz);
                o.stereoUVShift = float2(_StereoUOffset * stereoIndex + _StereoUConstantOffset, _StereoVOffset * stereoIndex + _StereoVConstantOffset);
#else
                o.stereoUVShift = float2(-_StereoUConstantOffset, -_StereoVConstantOffset);
#endif
                return o;
            }
            // Step 2. import fragment-shader specific functions. Must happen just before the fragment shader.
            #include "ClearVR.cginc"

            fixed4 frag(v2f IN) : SV_Target
            {
#if defined(FISH_EYE_EQUISOLID)
    float2 equiUV = RadialCoordsFishEyeEquiSolid(IN.normal, _ReferenceWidth, _ReferenceHeight, _CircularRadiusInRad, _FocalLength, _SensorDensity);
#elif defined(FISH_EYE_EQUIDISTANT)
    float2 equiUV = RadialCoordsFishEyeEquidistant(IN.normal, _ReferenceWidth, _ReferenceHeight, _CircularRadiusInRad, _FocalLength, _SensorDensity);
#elif defined(FISH_EYE_POLYNOME)
    float2 equiUV = RadialCoordsFishEyePolynomial(IN.normal, _ReferenceWidth, _ReferenceHeight, _CircularRadiusInRad, _CenterU, _CenterV, _AffineParameterC, _AffineParameterD, _AffineParameterE, _SphereToPlanPolynome);
#elif defined(ERP)
    float2 equiUV = RadialCoordsERP(IN.normal, _LongitudeOffsetInRad);
#else
    //else compilation will fail
#endif
                equiUV[0] *= _MonoUFactor;
                equiUV[1] *= _MonoVFactor;
                equiUV += IN.stereoUVShift;
				fixed4 col = GetRGBAPixel(GET_RGBA_PIXEL_ARGS, equiUV);
				if (equiUV.x <= IN.stereoUVShift.x || equiUV.x >= _MonoUFactor + IN.stereoUVShift.x || equiUV.y <= IN.stereoUVShift.y || equiUV.y >= _MonoVFactor + IN.stereoUVShift.y) {
					float _CutoutThresh = 1.5;
					col.a = 0;
					clip(col.r - _CutoutThresh);
				}
				return col * _Color;
            }
			ENDCG
		}
	}    
}
