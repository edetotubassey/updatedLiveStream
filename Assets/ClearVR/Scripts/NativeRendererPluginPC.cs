// #define USE_NRP_BYPASS
using System.Runtime.InteropServices;
using System;
#if USE_NRP_BYPASS
using fts;
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
// Note to self: if you define USE_NRP_BYPASS, UnityPluginLoad() is NOT called!!!

namespace com.tiledmedia.clearvr {
    /* Methods exposed by NativeRendererPlugin that are PC specific */
#if USE_NRP_BYPASS
    [PluginAttr("libClearVRPC")]
#endif
    internal class NativeRendererPluginPC : NativeRendererPluginBase {

        /* Native Renderer Plugin imports */
#if !USE_NRP_BYPASS
        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CVR_NRP_GetCbRendererFrameAsByteArrayAvailable();
#else
        [PluginFunctionAttr("CVR_NRP_GetCbRendererFrameAsByteArrayAvailable")]
        public static CVR_NRP_GetCbRendererFrameAsByteArrayAvailableDelegate CVR_NRP_GetCbRendererFrameAsByteArrayAvailable = null;
        public delegate IntPtr CVR_NRP_GetCbRendererFrameAsByteArrayAvailableDelegate();
#endif
    }
}
#endif