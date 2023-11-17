#if UNITY_ANDROID && !UNITY_EDITOR
using System.Runtime.InteropServices;
using System;
using UnityEngine;
namespace com.tiledmedia.clearvr {
    /* Methods exposed by NativeRendererPlugin that are Android specific */
    internal class NativeRendererPluginAndroid : NativeRendererPluginBase{

        /* Native Renderer Plugin imports */
        [DllImport(CVR_NRP_LIBRARY_NAME)]
        public static extern IntPtr CVR_NRP_GetSurfaceObject();

        [DllImport(CVR_NRP_LIBRARY_NAME)]
        public static extern void CVR_NRP_SetClearVRCoreWrapperObject(IntPtr argObject);

#if CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS
        [DllImport(CVR_NRP_LIBRARY_NAME)]
        public static extern void CVR_NRP_UpdateApplicationMeshStateWithOVR(Int32 argDisplayObjectID,
            OVRPlugin.Bool argHeadLocked,
            OVRPlugin.Posef argPose, // Note that we use the C ovrPosef rather than the C++ ovrpPosef
            OVRPlugin.Vector3f argScale, // Note that we use the C ovrVector3f rather than the C++ ovrpVector3f
            OVRPlugin.Bool argOverridePerLayerColorScaleAndOffset,
            Vector4 argColorScale,
            Vector4 argColorOffset,
            OVRPlugin.Bool argForceMonoscopicRendering,
            OVRPlugin.Bool argIsOverlay);
#endif
    }
}
#endif