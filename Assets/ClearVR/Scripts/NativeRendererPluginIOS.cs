using System.Runtime.InteropServices;
using System;

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
namespace com.tiledmedia.clearvr {
    /* Methods exposed by NativeRendererPlugin that are IOS specific */
    internal class NativeRendererPluginIOS : NativeRendererPluginBase{

        /* Native Renderer Plugin imports */

        [DllImport(CVR_NRP_LIBRARY_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CVR_NRP_GetCbRendererFrameAsByteArrayAvailable();
    }
}
#endif