// This file is imported twice in our shaders.
// Step 1. Generic and vertex shader specific functions are defined.
// Step 2. Fragment shader specific functions are defined.
// Note that Step 2 relies on the samplers and textures to be defined.
#if !defined(FIRST_INCLUDE_DONE)
#define FIRST_INCLUDE_DONE

#define PI 3.141592653589793

#if defined (SHADERLAB_GLSL)
	#define CONST_FLOAT float
	#define FLOAT4 vec4
	#define FLOAT3 vec3
	#define FLOAT2 vec2
	#define FLOAT4X4 mat4
	#define FLOAT3X3 mat3
	#define ATAN atan
	#define INLINE 
	#define VAR_A j
	#define VAR_B i
	#define FIXED4 vec4
	#define FIXED3 vec3
	#if defined(USE_OES_FAST_PATH_ON)
		#define SAMPLER2D samplerExternalOES
	#else
		#define SAMPLER2D sampler2D
	#endif
	#if __VERSION__ >= 300
		#define TEX2D  texture
	#else
		#define TEX2D  texture2D
	#endif
#else
	#define CONST_FLOAT const float
	#define FLOAT4 float4
	#define FLOAT3 float3
	#define FLOAT2 float2
	#define FLOAT4X4 float4x4
	#define FLOAT3X3 float3x3
	#define INLINE inline
	#define ATAN atan2
	#define VAR_A i
	#define VAR_B j
	#define FIXED4 fixed4
	#define FIXED3 fixed3
	#define TEX2D tex2D
	#define SAMPLER2D sampler2D
#endif
	#if defined(USE_NV12) || defined(USE_YUV420P)
		#define GET_RGBA_PIXEL_ARGS _ColorSpaceTransformMatrix, _TextureTransformMatrix
	#else
		#define GET_RGBA_PIXEL_ARGS _TextureTransformMatrix
	#endif


INLINE int GetStereoEyeIndexLegacy(FLOAT3 argWorldNosePosition, FLOAT3 argWorldCameraRight) {
	float distanceRight = distance(argWorldNosePosition + argWorldCameraRight, _WorldSpaceCameraPos);
	float distanceLeft = distance(argWorldNosePosition - argWorldCameraRight, _WorldSpaceCameraPos);
	return int(distanceRight < distanceLeft);
}

INLINE int GetStereoEyeIndex(FLOAT3 argWorldNosePosition, FLOAT3 argWorldCameraRight) {
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED)
	return int(unity_StereoEyeIndex);
#elif defined (STEREO_MULTIVIEW_ON)
	return int(gl_ViewID_OVR);
#elif defined(UNITY_DECLARE_MULTIVIEW)
	// OVR_multiview extension
	return int(UNITY_VIEWID);
#else 
	return GetStereoEyeIndexLegacy(argWorldNosePosition, argWorldCameraRight);
#endif
}

// ********************
// END OF FIRST INCLUDE
// ********************
#else
// ***********************
// START OF SECOND INCLUDE
// ***********************


#if defined (SHADERLAB_GLSL)
	#define A_COORDS_N_X a_coords_n[0]
	#define A_COORDS_N_Y a_coords_n[1]
	#define A_COORDS_N_Z a_coords_n[2]
#else
	#define A_COORDS_N_X a_coords_n.x
	#define A_COORDS_N_Y a_coords_n.y
	#define A_COORDS_N_Z a_coords_n.z
#endif

INLINE FLOAT2 RadialCoordsFishEyeEquidistant(FLOAT3 a_coords, float referenceWidth, float referenceHeight, float circularRadiusInRad, float focalLength, float sensorDensity)
{
	float normalized_width = 1.0/referenceWidth;
	float normalized_height = 1.0/ referenceHeight;

	FLOAT3 a_coords_n = normalize(a_coords);

	float x = A_COORDS_N_Z;
	float y = A_COORDS_N_X;
	float z = A_COORDS_N_Y;

	if (circularRadiusInRad > 0.0 && cos(circularRadiusInRad) > x) {
		return FLOAT2(-1, -1); //we crop the pixel
	}


	float theta = acos(x);
	float radialNorm = sqrt(y*y+z*z);

	float h_rNorm = focalLength * sensorDensity * normalized_width * theta;
	float v_rNorm =  focalLength * sensorDensity * normalized_height * theta;

	return FLOAT2(1.0 + h_rNorm * y / radialNorm - 0.5, 0.5 - v_rNorm * z / radialNorm);
}

INLINE FLOAT2 RadialCoordsFishEyeEquiSolid(FLOAT3 a_coords, float referenceWidth, float referenceHeight, float circularRadiusInRad, float focalLength, float sensorDensity)
{
	CONST_FLOAT inv_width = 1.0 / referenceWidth;
	CONST_FLOAT inv_height = 1.0/ referenceHeight;

	FLOAT3 a_coords_n = normalize(a_coords);

	float x = A_COORDS_N_Z;
	float y = A_COORDS_N_X;
	float z = A_COORDS_N_Y;

	if (circularRadiusInRad > 0.0 && cos(circularRadiusInRad) > x) {
		return FLOAT2(-1, -1); //we crop the pixel
	}

	float theta = acos(x);
	float sinTwo = sin(theta/2.0);
	float radialNorm = sqrt(y*y+z*z);

	float h_rNorm = 2.0 * focalLength * sensorDensity *   inv_width * sinTwo;
	float v_rNorm = 2.0 * focalLength * sensorDensity * inv_height * sinTwo;

	return FLOAT2(1.0 + h_rNorm * y / radialNorm - 0.5, 0.5 - v_rNorm * z / radialNorm);
}

INLINE FLOAT2 RadialCoordsFishEyePolynomial(FLOAT3 a_coords, float referenceWidth, float referenceHeight, float circularRadiusInRad, float centerU, float centerV, float affineParameterC, float affineParameterD, float affineParameterE, FLOAT4X4 sphereToPlanPolynome)
{
	FLOAT3 a_coords_n = normalize(a_coords);

	float xCore = A_COORDS_N_Z;
	float yCore = A_COORDS_N_X;
	float zCore = A_COORDS_N_Y;

	float x = -yCore;
	float y = zCore;
	float z = -xCore;

	if (circularRadiusInRad > 0.0 && cos(circularRadiusInRad) > xCore) {
		return FLOAT2(-100, -100); //we crop the pixel
	}

	float norm = sqrt(x * x + y * y);

	const int m_s2pOrderPlusOne = 16;

	if (norm != 0.0) {
		float theta = ATAN(z, norm) + PI / 2.0;
		float rho = 0.0;
		for (int k = m_s2pOrderPlusOne-1; k >= 0; --k) {
			int VAR_A = (k / 4);
			int VAR_B = k - VAR_A*4;
			rho = sphereToPlanPolynome[i][j]+theta*rho;
		}
		float xx = x * rho / norm;
		float yy = y * rho / norm;

		float u = xx * affineParameterC + yy * affineParameterD + centerU;
		float v = xx * affineParameterE + yy + centerV;

		return FLOAT2(
			1.0 - u / referenceWidth,
			1.0 - v / referenceHeight
		);

	} else {
		return FLOAT2(
			1.0 - centerU / referenceWidth,
			1.0 - centerV / referenceHeight
		);
	}
}

INLINE float modulo(float a, float b) {
#if defined (SHADERLAB_GLSL)
	return mod(a,  b);
#else
	return a % b;
#endif
}

INLINE FLOAT2 RadialCoordsERP(FLOAT3 a_coords, float longitudeOffsetInRad)
{
	FLOAT3 a_coords_n = normalize(a_coords);
#if defined (SHADERLAB_GLSL)
	float lon = modulo(((ATAN(A_COORDS_N_Z, A_COORDS_N_X) + longitudeOffsetInRad) + PI), (2.0*PI)) - PI;
#else
	float lon = modulo(((ATAN(A_COORDS_N_Z, A_COORDS_N_X) + longitudeOffsetInRad) - PI), (2.0*PI)) + PI;
#endif
	float lat = acos(A_COORDS_N_Y);
	FLOAT2 sphereCoords = FLOAT2(lon, lat) * (1.0 / PI);
	return FLOAT2(0.5 - sphereCoords.x * 0.5, sphereCoords.y);
}


INLINE FLOAT3 ConvertYpCbCrToRGB(FLOAT3 yuv, FLOAT4X4 colorSpaceTextureTransformMatrix)
{
#if defined(SHADERLAB_GLSL)
	return clamp(FLOAT3X3(colorSpaceTextureTransformMatrix) * (yuv + colorSpaceTextureTransformMatrix[3].xyz), 0.0, 1.0);
#else
	return saturate(mul((FLOAT3X3)colorSpaceTextureTransformMatrix, yuv + colorSpaceTextureTransformMatrix[3].xyz));
#endif
}

#if defined(GAMMA_TO_LINEAR_CONVERSION)
INLINE FIXED4 GammaCorrection(FIXED4 inRgba) {
	FIXED3 col = inRgba.rgb;
#if defined(SHADERLAB_GLSL)
	//Cheap approximation
	col = pow(col, FLOAT3(2.2, 2.2, 2.2));
#else 
	//Slow exact conversion
	if (col.r <= 0.04045) {
		col.r = col.r / 12.92;
	} else {
		col.r = pow((col.r + 0.055)/1.055, 2.4);
	}
	if (col.g <= 0.04045) {
		col.g = col.g / 12.92;
	} else {
		col.g = pow((col.g + 0.055)/1.055, 2.4);
	}
	if (col.b <= 0.04045) {
		col.b = col.b / 12.92;
	} else {
		col.b = pow((col.b + 0.055)/1.055, 2.4);
	}
#endif
	return FIXED4(col, inRgba.a);
}
#else
INLINE FIXED4  GammaCorrection(FIXED4 x) {return x;}
#endif

INLINE FLOAT2 ApplyTextureTransformMatrix(FLOAT2 uv, FLOAT4X4 textureTransformMatrix) {
#if defined(SHADERLAB_GLSL)
	FLOAT4 newHomogeneousUV = textureTransformMatrix * FLOAT4(uv.x, 1.0-uv.y, 0.0, 1.0); // 1-y because vertical zero is not at the same spot as for non GLSL shaders
#else
	FLOAT4 newHomogeneousUV = mul(textureTransformMatrix, FLOAT4(uv, 0.0, 1.0));
#endif
	return newHomogeneousUV.xy / newHomogeneousUV.w;
}


#if defined(USE_NV12) || defined(USE_YUV420P)
INLINE FIXED4 GetRGBAPixel(FLOAT4X4 colorSpaceTransformMatrix, FLOAT4X4 textureTransformMatrix, FLOAT2 uv) {
#else
INLINE FIXED4 GetRGBAPixel(FLOAT4X4 textureTransformMatrix, FLOAT2 uv) {
#endif

	FLOAT2 transformedUV = ApplyTextureTransformMatrix(uv, textureTransformMatrix);

#if defined(USE_NV12) 
	FLOAT3 yuv = FLOAT3(TEX2D(_MainTex, transformedUV).r, TEX2D(_ChromaTex, transformedUV).rg);
	FIXED4 rgba = FIXED4(ConvertYpCbCrToRGB(yuv, colorSpaceTransformMatrix), 1.0);
	return GammaCorrection(rgba);
#elif defined(USE_YUV420P)
	FLOAT3 yuv = FLOAT3(TEX2D(_MainTex, transformedUV).r, TEX2D(_ChromaTex, transformedUV).r, TEX2D(_ChromaTex2, transformedUV).r);
	FIXED4 rgba = FIXED4(ConvertYpCbCrToRGB(yuv, colorSpaceTransformMatrix), 1.0);
	return GammaCorrection(rgba);
#else
	return GammaCorrection(TEX2D(_MainTex, transformedUV));
#endif
}
// *********************
// END OF SECOND INCLUDE
// *********************
#endif
