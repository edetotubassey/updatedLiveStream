using System;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.tiledmedia.clearvr {
    class ClearVRLogger {
        static internal void LOGD(String format, params object[] args) {
            if (ClearVRPlayer.loggingConfig.sdkLogLevel >= LogLevels.Debug) {
                String msg = String.Format(format, args);
                ClearVRCoreLog(msg, LogLevels.Debug);
            }
        }
        static internal void LOGI(String format, params object[] args) {
            if (ClearVRPlayer.loggingConfig.sdkLogLevel >= LogLevels.Info) {
                String msg = String.Format(format, args);
                ClearVRCoreLog(msg, LogLevels.Info);
            }
        }

        static internal void LOGW(String format, params object[] args) {
            if (ClearVRPlayer.loggingConfig.sdkLogLevel >= LogLevels.Warn) {
                String msg = String.Format(format, args);
                ClearVRCoreLog(msg, LogLevels.Warn);
            }
        }

        static internal void LOGE(String format, params object[] args) {
            if (ClearVRPlayer.loggingConfig.sdkLogLevel >= LogLevels.Error) {
                String msg = String.Format(format, args);
                ClearVRCoreLog(msg, LogLevels.Error);
            }
        }

        static internal void LOGF(String format, params object[] args) {
            if (ClearVRPlayer.loggingConfig.sdkLogLevel >= LogLevels.Fatal) {
                String msg = String.Format(format, args);
                ClearVRCoreLog(msg, LogLevels.Fatal);
            }
        }

        private static void ClearVRCoreLog(String msg, LogLevels logLevel) {
#if UNITY_ANDROID && !UNITY_EDITOR
            JNIBridges.ClearVRCoreLog(msg, logLevel);
#elif UNITY_IOS && !UNITY_EDITOR
            MediaPlayerIOS._tm_clearVRCoreLog(msg, (int) LogComponents.Sdk, (int) logLevel);
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // TODO: Stan removes this elif once the MacOS MF-PC is enabled
            UnityEngine.Debug.Log(msg);
#elif UNITY_STANDALONE || UNITY_EDITOR
            MediaPlayerPC.MediaFlowPC._tm_log(msg, (byte) LogComponents.Sdk, (byte) logLevel);
#else
            throw new Exception("ClearVRCoreLog not implemented for this platform");
#endif
        }
    }
}