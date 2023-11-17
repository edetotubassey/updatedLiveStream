// #define USE_NRP_BYPASS

using UnityEngine;
using System.Runtime.InteropServices;
using System;
#if USE_NRP_BYPASS
using fts;
#endif

namespace com.tiledmedia.clearvr {
    // the following struct mush be memory aligned with their counterparts in the NRP
    //Structure used by the NRP to send the proper pointers to the SDK
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public class SharedPointersWithSDK
    {
        public IntPtr displayObjectDescriptorHeaderPtr;
        public IntPtr dynamicDisplayObjectDescriptorPtr;
        public IntPtr staticDisplayObjectDescriptorPtr;
        public IntPtr debugDisplayObjectDescriptorPtr;
    }
    /* Methods exposed by NativeRendererPlugin that are cross-platform */
#if USE_NRP_BYPASS
    [PluginAttr("libClearVRPC")]
#endif
    internal class NativeRendererPluginBase {

#if !USE_NRP_BYPASS
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        protected const String CVR_NRP_LIBRARY_NAME = "__Internal";
#elif UNITY_ANDROID && !UNITY_EDITOR
        protected const String CVR_NRP_LIBRARY_NAME = "libClearVRNativeRendererPlugin";
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        protected const String CVR_NRP_LIBRARY_NAME = "ClearVRPC";
#else
        protected const String CVR_NRP_LIBRARY_NAME = "ClearVRNativeRendererPlugin";
#endif
        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CVR_NRP_GetRenderEventFunc();

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern Int32 CVR_NRP_Load(IntPtr argLoadParameters);
        
        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CVR_NRP_Unload(Int32 argID);

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CVR_NRP_SetMeshDataPointers(IntPtr argMeshDataPointersStruct);

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CVR_NRP_RegisterDisplayObject(Int32 argDisplayObjectID, MeshTextureModes argMeshTextureMode, String argDisplayObjectNamePtr);

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CVR_NRP_FreeSharedPointersWithSDK(IntPtr sharedPointersWithSDKPtr);

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CVR_NRP_UnregisterDisplayObject(Int32 argDisplayObjectID);

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern byte CVR_NRP_GetIsTextureBlitModeSupported(NRPBridgeTypes argNRPBridgeType,
            RenderAPITypes argRenderAPIType,
            TextureBlitModes argNRPTextureBlitMode,
            ContentProtectionRobustnessLevels argContentProtectionRobustnessLevel);
        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CVR_NRP_UpdateApplicationMeshState(Int32 argDisplayObjectID);

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CVR_NRP_RegisterCallbackToTheCore();
#else
        [PluginFunctionAttr("CVR_NRP_GetRenderEventFunc")]
        internal static CVR_NRP_GetRenderEventFuncDelegate CVR_NRP_GetRenderEventFunc = null;
        internal delegate IntPtr CVR_NRP_GetRenderEventFuncDelegate();

        [PluginFunctionAttr("CVR_NRP_Load")]
        internal static CVR_NRP_LoadDelegate CVR_NRP_Load = null;
        internal delegate Int32 CVR_NRP_LoadDelegate(IntPtr argLoadParameters);

        [PluginFunctionAttr("CVR_NRP_Unload")]
        internal static CVR_NRP_UnloadDelegate CVR_NRP_Unload = null;
        internal delegate void CVR_NRP_UnloadDelegate(Int32 argID);

        [PluginFunctionAttr("CVR_NRP_SetMeshDataPointers")]
        internal static CVR_NRP_SetMeshDataPointersDelegate CVR_NRP_SetMeshDataPointers = null;
        internal delegate void CVR_NRP_SetMeshDataPointersDelegate(IntPtr argMeshDataPointersStruct);

        [PluginFunctionAttr("CVR_NRP_GetIsTextureBlitModeSupported")]
        internal static CVR_NRP_GetIsTextureBlitModeSupportedDelegate CVR_NRP_GetIsTextureBlitModeSupported = null;
        internal delegate byte CVR_NRP_GetIsTextureBlitModeSupportedDelegate(NRPBridgeTypes argNRPBridgeType,
            RenderAPITypes argRenderAPIType,
            TextureBlitModes argNRPTextureBlitMode,
            ContentProtectionRobustnessLevels argContentProtectionRobustnessLevel);


        [PluginFunctionAttr("CVR_NRP_RegisterCallbackToTheCore")]
        internal static CVR_NRP_RegisterCallbackToTheCoreDelegate CVR_NRP_RegisterCallbackToTheCore = null;
        internal delegate void CVR_NRP_RegisterCallbackToTheCoreDelegate();
        //
        [PluginFunctionAttr("CVR_NRP_RegisterDisplayObject")]
        internal static CVR_NRP_RegisterDisplayObjectDelegate CVR_NRP_RegisterDisplayObject = null;
        private delegate IntPtr CVR_NRP_RegisterDisplayObjectDelegate(Int32 argDisplayObjectID, MeshTextureModes argMeshTextureMode, String argDisplayObjectName);
        [PluginFunctionAttr("CVR_NRP_FreeSharedPointersWithSDK")]
        internal static CVR_NRP_FreeSharedPointersWithSDKDelegate CVR_NRP_FreeSharedPointersWithSDK = null;
        private delegate void CVR_NRP_FreeSharedPointersWithSDKDelegate(IntPtr sharedPointersWithSDKPtr);
        [PluginFunctionAttr("CVR_NRP_UnregisterDisplayObject")]
        internal static CVR_NRP_UnregisterDisplayObjectDelegate CVR_NRP_UnregisterDisplayObject = null;
        internal delegate void CVR_NRP_UnregisterDisplayObjectDelegate(Int32 argDisplayObjectID);


        [PluginFunctionAttr("CVR_NRP_UpdateApplicationMeshState")]
        internal static CVR_NRP_UpdateApplicationMeshStateDelegate CVR_NRP_UpdateApplicationMeshState = null;
        internal delegate void CVR_NRP_UpdateApplicationMeshStateDelegate(Int32 argDisplayObjectID);



#endif

        internal static SharedPointersWithSDK CVR_NRP_RegisterDisplayObject_Wrapped(Int32 argDisplayObjectID, MeshTextureModes argMeshTextureMode, String argDisplayObjectNamePtr) {
            IntPtr sharedPointersWithSDKPtr = CVR_NRP_RegisterDisplayObject(argDisplayObjectID, argMeshTextureMode, argDisplayObjectNamePtr);
            SharedPointersWithSDK sharedPointersWithSDK = (SharedPointersWithSDK)Marshal.PtrToStructure(sharedPointersWithSDKPtr, typeof(SharedPointersWithSDK));
            CVR_NRP_FreeSharedPointersWithSDK(sharedPointersWithSDKPtr);
            return sharedPointersWithSDK;
        }


        private enum NRPEventTypes {
            Unknown = 0,
            Initialize = 1,
            VSync = 2,
            Destroy = 3
        }

        private static IntPtr cvrNRPRenderEventFunctionPointer = IntPtr.Zero;
        private static GCHandle cvrNRPRenderEventFunctionPointerGCHandle;

        internal static void InitializeAsync() {
            IssuePluginEvent(NRPEventTypes.Initialize);
        }

        internal static void VSyncAsync(int vsyncCounter) {
            int vsyncCounterShiffted = (vsyncCounter << 3);
            IssuePluginEvent(NRPEventTypes.VSync, vsyncCounterShiffted);
        }

        internal static void DestroyAsync() {
            IssuePluginEvent(NRPEventTypes.Destroy);
        }

        internal static void Setup() {
            if(cvrNRPRenderEventFunctionPointer == IntPtr.Zero) {
                cvrNRPRenderEventFunctionPointer = CVR_NRP_GetRenderEventFunc();
                cvrNRPRenderEventFunctionPointerGCHandle = GCHandle.Alloc(cvrNRPRenderEventFunctionPointer, GCHandleType.Pinned);
            }
        }

        private static void IssuePluginEvent(NRPEventTypes argType, long optionalOffset = 0) {
            GL.InvalidateState(); // Essential for proper operation in OpenGLES 3.x
            GL.IssuePluginEvent(cvrNRPRenderEventFunctionPointer, (int)argType + (int)optionalOffset);
        }

        internal static void Release() {
            if(cvrNRPRenderEventFunctionPointer != IntPtr.Zero) {
                cvrNRPRenderEventFunctionPointerGCHandle.Free();
            }
            cvrNRPRenderEventFunctionPointer = IntPtr.Zero;
        }


        //// DisplayObjectDescriptor

        // Unmanaged --> Managed wrapper structs
	internal class DisplayObjectDescriptorWrapper {
        // Public fields
		public DisplayObjectDescriptorFlags displayObjectDescriptorFlags {
            get {
                return isFirstRead ? DisplayObjectDescriptorFlags.Unknown : (DisplayObjectDescriptorFlags) dynamicDisplayObjectDescriptor.displayObjectDescriptorFlags;
            }
        }
		public IntPtr textureIDPlane0 {
            get {
                return isFirstRead ? IntPtr.Zero : dynamicDisplayObjectDescriptor.textureIDPlane0;
            }
        }
		public IntPtr textureIDPlane1 {
            get {
                return isFirstRead ? IntPtr.Zero : dynamicDisplayObjectDescriptor.textureIDPlane1;
            }
        }
		public IntPtr textureIDPlane2 {
            get {
                return isFirstRead ? IntPtr.Zero : dynamicDisplayObjectDescriptor.textureIDPlane2;
            }
        }
		public Int32 vertexCount {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.vertexCount;
            }
        }
		public Int32 indexCount {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.indexCount;
            }
        }
		public Int32 frameWidth {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.frameWidth;
            }
        }
		public Int32 frameHeight  {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.frameHeight;
            }
        }
		public Int32 signature  {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.meshSignature;
            }
        }
		public Int32 displayObjectID {
            get {
                return isFirstRead ? -1 : staticDisplayObjectDescriptor.displayObjectID;
            }
        }
		public Int32 feedIndex {
            get {
                return isFirstRead ? -1 : staticDisplayObjectDescriptor.feedIndex;
            }
        }
		public float boundsX {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.boundsX;
            }
        }
		public float boundsY {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.boundsY;
            }
        } 
		public float boundsZ  {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.boundsZ;
            }
        } 
		public ClearVRMeshTypes clearVRMeshType  {
            get {
                return isFirstRead ? ClearVRMeshTypes.Unknown : staticDisplayObjectDescriptor.clearVRMeshType;
            }
        } 
		public bool hasRightEye  {
            get {
                return isFirstRead ? false : dynamicDisplayObjectDescriptor.hasRightEye != 0;
            }
        } 
		public ClearVRFishEyeTypes clearVRFishEyeType  {
            get {
                return isFirstRead ? ClearVRFishEyeTypes.NotSet : staticDisplayObjectDescriptor.clearVRFishEyeType;
            }
        } 
		public float circularRadiusInRad {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.circularRadiusInRad;
            }
        } 
		public float sensorDensity {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.sensorDensity;
            }
        } 
		public float focalLength  {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.focalLength;
            }
        } 
		public Int32 referenceWidth  {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.referenceWidth;
            }
        } 
		public Int32 referenceHeight {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.referenceHeight;
            }
        } 
		public float centerU {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.centerU;
            }
        } 
		public float centerV {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.centerV;
            }
        }  
		public float affineParameterC {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.affineParameterC;
            }
        }  
		public float affineParameterD {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.affineParameterD;
            }
        }  
		public float affineParameterE  {
            get {
                return isFirstRead ? 0 : staticDisplayObjectDescriptor.affineParameterE;
            }
        }  
		public float[] sphereToPlanPolynome {
            get {
                return isFirstRead ? new float[16] : staticDisplayObjectDescriptor.sphereToPlanPolynome;
            }
        }  
		public long rts  {
            get {
                return isFirstRead ? 0 : debugDisplayObjectDescriptor.rts;
            }
        }  
		public ClearVRFishEyeStereoTypes clearVRFishEyeStereoType {
            get {
                return isFirstRead ? ClearVRFishEyeStereoTypes.StereoTypeMono : staticDisplayObjectDescriptor.clearVRFishEyeStereoType;
            }
        }  
		public TextureTypes textureType {
            get {
                return isFirstRead ? TextureTypes.RGBA : dynamicDisplayObjectDescriptor.textureType;
            }
        }  
		public ColorSpaceStandards colorSpace  {
            get {
                return isFirstRead ? ColorSpaceStandards.Unspecified : staticDisplayObjectDescriptor.colorSpace;
            }
        }  
		public Matrix4x4 textureTransformMatrix {
            get {
                Matrix4x4 textureTransformMatrix = new Matrix4x4();
                if(!isFirstRead) { // We need this check, because .textureTransformMatrix can be null when not fully initialized yet.
                    for (int j = 0; j < 4; j++) {
                        for (int i = 0; i < 4; i++) {
                            int index = i + j * 4;
                            textureTransformMatrix[j, i] = staticDisplayObjectDescriptor.textureTransformMatrix[index];
                        }
                    }
                }
                return textureTransformMatrix;
            }
        }  
		public bool isActive {
            get {
                return isFirstRead ? false : dynamicDisplayObjectDescriptor.isActive != 0;
            }
        }  
		public ProjectionTypes projectionType {
            get {
                return isFirstRead ? ProjectionTypes.Unknown : staticDisplayObjectDescriptor.projectionType;
            }
        }  
		public DisplayObjectClassTypes displayObjectClassType  {
            get {
                return isFirstRead ? DisplayObjectClassTypes.Unknown : staticDisplayObjectDescriptor.displayObjectClassType;
            }
        }  
		public bool isStereoscopicModeActive {
            get {
                return isFirstRead ? false : dynamicDisplayObjectDescriptor.isStereoscopicModeActive != 0;
            }
        }  
		public VideoStereoMode videoStereoMode {
            get {
                return isFirstRead ? VideoStereoMode.Unknown : staticDisplayObjectDescriptor.videoStereoMode;
            }
        }  
        public UInt32 vsyncCounter {
            get {
                return isFirstRead ? 0 : debugDisplayObjectDescriptor.vsyncCounter;
            }
        } 

        //Private fields

        // Pointer to the NRP C owned structures
        private IntPtr displayObjectDescriptorHeaderPtr = IntPtr.Zero;
        private IntPtr dynamicDisplayObjectDescriptorPtr = IntPtr.Zero;
        private IntPtr staticDisplayObjectDescriptorPtr = IntPtr.Zero;
        private IntPtr debugDisplayObjectDescriptorPtr = IntPtr.Zero;
        // Garbage collector handle to pin the pointers
        private GCHandle displayObjectDescriptorHeaderPtrHandle;
        private GCHandle dynamicDisplayObjectDescriptorPtrHandle;
        private GCHandle staticDisplayObjectDescriptorPtrHandle;
        private GCHandle debugDisplayObjectDescriptorPtrHandle;
        // Deserialized structures
        private DisplayObjectDescriptorHeader displayObjectDescriptorHeader = new DisplayObjectDescriptorHeader();
        private DynamicDisplayObjectDescriptor dynamicDisplayObjectDescriptor = new DynamicDisplayObjectDescriptor();
        private StaticDisplayObjectDescriptor staticDisplayObjectDescriptor = new StaticDisplayObjectDescriptor();
        private DebugDisplayObjectDescriptor debugDisplayObjectDescriptor = new DebugDisplayObjectDescriptor();
        // Internal state
        private bool isFirstRead = true;
        private int dynamicDisplayObjectDescriptorWriteCounter  {
            get {
                return displayObjectDescriptorHeader.dynamicNRPWriteCounter;
            }
        }
        private int dynamicDisplayObjectDescriptorReadCounter = 0;
        private int staticDisplayObjectDescriptorWriteCounter   {
            get {
                return displayObjectDescriptorHeader.staticNRPWriteCounter;
            }
        }
        private int staticDisplayObjectDescriptorReadCounter = 0;
        private int debugDisplayObjectDescriptorWriteCounter  {
            get {
                return displayObjectDescriptorHeader.debugNRPWriteCounter;
            }
        }
        private int debugDisplayObjectDescriptorReadCounter = 0;


        // Public methods
        internal void Initialize(SharedPointersWithSDK sharedPointerWithSDK) {
            displayObjectDescriptorHeaderPtr = sharedPointerWithSDK.displayObjectDescriptorHeaderPtr;
            displayObjectDescriptorHeaderPtrHandle = GCHandle.Alloc(displayObjectDescriptorHeaderPtr, GCHandleType.Pinned);
            dynamicDisplayObjectDescriptorPtr = sharedPointerWithSDK.dynamicDisplayObjectDescriptorPtr;
            dynamicDisplayObjectDescriptorPtrHandle = GCHandle.Alloc(dynamicDisplayObjectDescriptorPtr, GCHandleType.Pinned);
            staticDisplayObjectDescriptorPtr = sharedPointerWithSDK.staticDisplayObjectDescriptorPtr;
            staticDisplayObjectDescriptorPtrHandle = GCHandle.Alloc(staticDisplayObjectDescriptorPtr, GCHandleType.Pinned);
            debugDisplayObjectDescriptorPtr = sharedPointerWithSDK.debugDisplayObjectDescriptorPtr;
            debugDisplayObjectDescriptorPtrHandle = GCHandle.Alloc(debugDisplayObjectDescriptorPtr, GCHandleType.Pinned);
            UpdateStateWithoutUpdatingCounters();
        }
        internal void Reset() {
            displayObjectDescriptorHeaderPtrHandle.Free();
            dynamicDisplayObjectDescriptorPtrHandle.Free();
            staticDisplayObjectDescriptorPtrHandle.Free();
            debugDisplayObjectDescriptorPtrHandle.Free();
            displayObjectDescriptorHeaderPtr = IntPtr.Zero;
            dynamicDisplayObjectDescriptorPtr = IntPtr.Zero;
            staticDisplayObjectDescriptorPtr = IntPtr.Zero;
            debugDisplayObjectDescriptorPtr = IntPtr.Zero;
            isFirstRead = true;
        }
        public bool IsInitialized() {
            return displayObjectDescriptorHeaderPtr != IntPtr.Zero;
        }

        /// <summary>
        /// NOTE: There is no explicit IntPtr.Zero check here for performance reasons. The callee MUST make sure that we can call this safely.
        /// To check if safe to call, check IsInitialized() == true
        /// </summary>
        // return true if one of the structure got updated. WARNING: This function should only be called inside the UpdateMDS method of the DisplayObjectController
        public (bool somethingGotUpdated, bool wasLocked) UpdateState() {
            var (somethingGotUpdated, wasLocked) = UpdateStateWithoutUpdatingCounters();         
            // At this point all members of the DisplayObjectDescriptor are properly known, including the displayObjectID
            bool somethingNotDebugGotUpdate = false;
            if (somethingGotUpdated) {
                somethingNotDebugGotUpdate = dynamicDisplayObjectDescriptorReadCounter != dynamicDisplayObjectDescriptorWriteCounter;
                dynamicDisplayObjectDescriptorReadCounter = dynamicDisplayObjectDescriptorWriteCounter;
                somethingNotDebugGotUpdate = somethingNotDebugGotUpdate || staticDisplayObjectDescriptorReadCounter != staticDisplayObjectDescriptorWriteCounter;
                staticDisplayObjectDescriptorReadCounter = staticDisplayObjectDescriptorWriteCounter;
                debugDisplayObjectDescriptorReadCounter = debugDisplayObjectDescriptorWriteCounter;
                // Signal to the NRP that we are now up to date
                NativeRendererPluginBase.CVR_NRP_UpdateApplicationMeshState(displayObjectID);
            }
            return (somethingGotUpdated, wasLocked);
        }

        /// <summary>
        /// NOTE: There is no explicit IntPtr.Zero check here for performance reasons. The callee MUST make sure that we can call this safely.
        /// To check if safe to call, check IsInitialized() == true
        /// </summary>
        private (bool somethingGotUpdated, bool wasLocked) UpdateStateWithoutUpdatingCounters() {
            // We always need to update the header. We update the other structure based on what the header says
            UpdateDisplayObjectDescriptorHeader();
            bool somethingGotUpdated = false;
            bool wasLocked = false;
            if (!displayObjectDescriptorHeader.IsLocked()) {
                //Now all the Write counters are updated
                if (dynamicDisplayObjectDescriptorWriteCounter != dynamicDisplayObjectDescriptorReadCounter || isFirstRead) {
                    UpdateDynamicDisplayObjectDescriptor();
                    somethingGotUpdated = true;
                }
                if (staticDisplayObjectDescriptorWriteCounter != staticDisplayObjectDescriptorReadCounter || isFirstRead) {
                    UpdateStaticDisplayObjectDescriptor();
                    somethingGotUpdated = true;
                }
                if (debugDisplayObjectDescriptorWriteCounter != debugDisplayObjectDescriptorReadCounter || isFirstRead) {
                    UpdateDebugDisplayObjectDescriptor();
                    somethingGotUpdated = true;
                }
                isFirstRead = false;
            } else {
                wasLocked = true;
                ClearVRLogger.LOGW("Skip parsing the DisplayObjectDescriptor because it was still locked by the NRP");
            }
            // At this point all members of the DisplayObjectDescriptor are properly known, including the displayObjectID
            return (somethingGotUpdated, wasLocked);
        }


        /// <summary>
        /// NOTE: There is no explicit IntPtr.Zero check here for performance reasons. The callee MUST make sure that we can call this safely.
        /// To check if safe to call, check IsInitialized() == true
        /// </summary>
        private void UpdateDisplayObjectDescriptorHeader() {
            Marshal.PtrToStructure<DisplayObjectDescriptorHeader>(displayObjectDescriptorHeaderPtr, displayObjectDescriptorHeader);
        }

        /// <summary>
        /// NOTE: There is no explicit IntPtr.Zero check here for performance reasons. The callee MUST make sure that we can call this safely.
        /// To check if safe to call, check IsInitialized() == true
        /// </summary>
        private void UpdateDynamicDisplayObjectDescriptor() {
            Marshal.PtrToStructure<DynamicDisplayObjectDescriptor>(dynamicDisplayObjectDescriptorPtr, dynamicDisplayObjectDescriptor);
        }

        /// <summary>
        /// NOTE: There is no explicit IntPtr.Zero check here for performance reasons. The callee MUST make sure that we can call this safely.
        /// To check if safe to call, check IsInitialized() == true
        /// </summary>
        private void UpdateStaticDisplayObjectDescriptor() {
            Marshal.PtrToStructure<StaticDisplayObjectDescriptor>(staticDisplayObjectDescriptorPtr, staticDisplayObjectDescriptor);
        }
        /// <summary>
        /// NOTE: There is no explicit IntPtr.Zero check here for performance reasons. The callee MUST make sure that we can call this safely.
        /// To check if safe to call, check IsInitialized() == true
        /// </summary>
        private void UpdateDebugDisplayObjectDescriptor() {
            Marshal.PtrToStructure<DebugDisplayObjectDescriptor>(debugDisplayObjectDescriptorPtr, debugDisplayObjectDescriptor);
        }


        // the following struct mush be memory aligned with their counterparts in the NRP
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private class DisplayObjectDescriptorHeader
        {
            public Int32 dynamicNRPWriteCounter;
            public Int32 staticNRPWriteCounter;
            public Int32 debugNRPWriteCounter;
            public byte m_lock;

            public override String ToString() {
                return String.Format("DynamicWriteCounter: {0}, StaticWriteCounter: {1}, DebugWriteCounter: {2}, Locked: {3}",dynamicNRPWriteCounter, staticNRPWriteCounter, debugNRPWriteCounter, IsLocked());
            }

            public bool IsLocked() {
                return m_lock != 0;
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private class DynamicDisplayObjectDescriptor
        {
            public UInt32 displayObjectDescriptorFlags;
            public IntPtr textureIDPlane0;
            public IntPtr textureIDPlane1;
            public IntPtr textureIDPlane2;
            public TextureTypes textureType;
            public byte hasRightEye;
            public byte isActive;
            public byte isStereoscopicModeActive;

            public override String ToString() {
                return String.Format("Flags: {0}, ", DisplayObjectDescriptorFlagsMethods.GetAsPrettyString(displayObjectDescriptorFlags))+
                 String.Format("textureID: ({0}, {1},{2}), textureType: {3}, hasRightEye: {4}, isActive:{5}, isStereoscopicModeActive: {6}", textureIDPlane0, textureIDPlane1, textureIDPlane2,
                textureType, hasRightEye != 0, isActive != 0, isStereoscopicModeActive != 0);
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private class StaticDisplayObjectDescriptor
        {
            public Int32 vertexCount;
            public Int32 indexCount;
            public Int32 frameWidth;
            public Int32 frameHeight;
            public Int32 meshSignature;
            public Int32 displayObjectID;
            public Int32 feedIndex;
            public float boundsX;
            public float boundsY;
            public float boundsZ;
            public ClearVRMeshTypes clearVRMeshType;
            public ClearVRFishEyeTypes clearVRFishEyeType;
            public float circularRadiusInRad; // for all fish-eye models. If set (ie. if > 0), constains the radius in angular distance of the Fish-Eye circle
            public float sensorDensity; // for equi-distance or equi-solid fish-eye
            public float focalLength; // for equi-distance or equi-solid fish-eye
            public Int32 referenceWidth;
            public Int32 referenceHeight;
            public float centerU; // for polynomial fish-eye
            public float centerV; // for polynomial fish-eye
            public float affineParameterC; // for polynomial fish-eye
            public float affineParameterD; // for polynomial fish-eye
            public float affineParameterE;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.R4, SizeConst = 16)] // the size of this array is always 16
            public float[] sphereToPlanPolynome; // for polynomial fish-eye 
            public ClearVRFishEyeStereoTypes clearVRFishEyeStereoType;
            public ColorSpaceStandards colorSpace;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.R4, SizeConst = 16)] // the size of this array is always 16
            public float[] textureTransformMatrix; // for polynomial fish-eye 
            public ProjectionTypes projectionType;
            public DisplayObjectClassTypes displayObjectClassType;
            public VideoStereoMode videoStereoMode;
            public override String ToString() {
                return String.Format("vertexCount: {0}, indexCount: {1}, frameRes: {2}x{3}, meshSignature: {4}, displayObjectID: {5}, feedIndex: {6}, bounds: ({7},{8},{9}), clearVRMeshType: {10}, ",
                vertexCount, indexCount, frameWidth, frameHeight, meshSignature, displayObjectID, feedIndex, boundsX, boundsY, boundsZ, clearVRMeshType) +
                String.Format("clearVRFishEyeType: {0}, circularRadiusInRad: {1}, sensorDensity: {2}, focalLength: {3}, referenceRes: {4}x{5}, center: {6}x{7}, affineParameter: {8},{9},{10}, ",
                clearVRFishEyeType, circularRadiusInRad, sensorDensity, focalLength, referenceWidth, referenceHeight, centerU, centerV, affineParameterC, affineParameterD, affineParameterE) +
                String.Format("clearVRFishEyeStereoType: {0}, colorSpace: {1}, projectionType: {2}, displayObjectClassType: {3}, videoStereoMode: {4} ",clearVRFishEyeStereoType, colorSpace, projectionType, displayObjectClassType, videoStereoMode);
            }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private class DebugDisplayObjectDescriptor {
            public long rts;
            public UInt32 vsyncCounter;

            public override String ToString() {
                return String.Format("rts: {0}, vsyncCounter: {1}", rts, vsyncCounter);
            }
        }
    

		public override String ToString() {
			return String.Format("DOID: {0}, C: ({1},{2},{3}), RTS: {4}, Sig: {5}, active: {6}, DisplayObjectDescriptorFlags: {7}, vc: {8}, ic: {9}, dims: {7}x{8}, mesh type: {9}, textureType: {10}, colorSpace: {11}, texID per plane: {12},{13},{14}, FishEyeType: {15} (sd: {16}, fl: {17}, ref dims: {18}x{19}). Center U: {20}, V: {21}, affineParameters: {22}, {23}, {24}, projectionType: {25}, ClassType: {26}, isStereoscopicModeActive: {27}, TextureTransformMatrix: {28}, vsyncCounter: {29}",
				this.displayObjectID,
				this.dynamicDisplayObjectDescriptorReadCounter,
				this.staticDisplayObjectDescriptorReadCounter,
				this.debugDisplayObjectDescriptorReadCounter,
				this.rts,
				this.signature,
				this.isActive,
				DisplayObjectDescriptorFlagsMethods.GetAsPrettyString(this.displayObjectDescriptorFlags),
				this.vertexCount,
				this.indexCount,
				this.frameWidth,
				this.frameHeight,
				this.clearVRMeshType,
				this.textureType,
				this.colorSpace,
				this.textureIDPlane0,
				this.textureIDPlane1,
				this.textureIDPlane2,
				this.clearVRFishEyeType,
				this.sensorDensity,
				this.focalLength,
				this.referenceWidth,
				this.referenceHeight,
				this.centerU,
				this.centerV,
				this.affineParameterC,
				this.affineParameterD,
				this.affineParameterE,
				this.projectionType,
				this.displayObjectClassType,
				this.isStereoscopicModeActive,
				this.textureTransformMatrix != null ? this.textureTransformMatrix.ToString() : "not set",
                this.vsyncCounter
                );
		}

        public String ToStringFromRaw() {
            return String.Format("DOD: header: {0}, dynamic: {1}, static: {2}, debug: {3}", displayObjectDescriptorHeader, dynamicDisplayObjectDescriptor, staticDisplayObjectDescriptor, debugDisplayObjectDescriptor);
        }
    }
	
        //// End of DisplayObjectDescriptor
    }
}
