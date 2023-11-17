#if UNITY_ANDROID
using System;
using UnityEngine;

namespace com.tiledmedia.clearvr {
    public class StatisticsAndroid : StatisticsBase {
        protected const String clearVRCoreWrapperStatisticsClassName = "com/tiledmedia/clearvrcorewrapper/ClearVRCoreWrapperStatistics";
        IntPtr videoStatisticsFieldID = IntPtr.Zero;
        IntPtr audioStatisticsFieldID = IntPtr.Zero;
        IntPtr clearVRCoreWrapperGlobalRef = IntPtr.Zero; // Do not release!
        IntPtr getClearVRCoreWrapperStatisticsMethodId = IntPtr.Zero;
        IntPtr clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef = IntPtr.Zero;
        IntPtr pipelineLatencyInNanosecondsFieldID = IntPtr.Zero;
        private class VideoStatisticsAndroid : VideoStatisticsBase {
            public static String videoStatisticsClassName = "com/tiledmedia/clearvrcorewrapper/ClearVRCoreWrapperStatistics$VideoStatistics";

            public IntPtr interFrameDecoderLatencyMeanFieldID = IntPtr.Zero;
            public IntPtr interFrameDecoderLatencyStandardDeviationFieldID = IntPtr.Zero;
            public IntPtr interFrameRenderLatencyMeanFieldID = IntPtr.Zero;
            public IntPtr interFrameRenderLatencyStandardDeviationFieldID = IntPtr.Zero;

            public IntPtr framesRenderedFieldID = IntPtr.Zero;
            public IntPtr framesDroppedFieldID = IntPtr.Zero;

            public IntPtr vsyncQualityFieldID = IntPtr.Zero;
            public IntPtr frameReleaseQualityFieldID = IntPtr.Zero;
            public IntPtr averageDecoderInputQueueSizeFieldID = IntPtr.Zero;
            public IntPtr averageDecoderOutputQueueSizeFieldID = IntPtr.Zero;
            public IntPtr endToEndFrameLatencyMeanFieldID = IntPtr.Zero;
            public IntPtr endToEndFrameLatencyStandardDeviationFieldID = IntPtr.Zero;
            public IntPtr interFrameApplicationLatencyMeanFieldID = IntPtr.Zero;
            public IntPtr interFrameApplicationLatencyStandardDeviationFieldID = IntPtr.Zero;

            public IntPtr getInterFrameRenderLatencyAsPrettyStringMethodID = IntPtr.Zero;
            public IntPtr getInterFrameDecoderLatencyAsPrettyStringMethodID = IntPtr.Zero;
            public IntPtr getEndToEndFrameLatencyAsPrettyStringMethodID = IntPtr.Zero;
            public IntPtr getInterFrameApplicationLatencyAsPrettyStringMethodID = IntPtr.Zero;
            public IntPtr getInterFrameRenderRateInFramesPerSecondMethodID = IntPtr.Zero;
            public IntPtr getInterFrameDecoderRateInFramesPerSecondMethodID = IntPtr.Zero;
            public IntPtr getInterFrameApplicationRateInFramesPerSecondMethodID = IntPtr.Zero;

            protected IntPtr videoStatisticsObjectFieldGlobalRef = IntPtr.Zero;

            public override float interFrameDecoderLatencyMean {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, interFrameDecoderLatencyMeanFieldID);
                }
            }

            public override float interFrameDecoderLatencyStandardDeviation {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, interFrameDecoderLatencyStandardDeviationFieldID);
                }
            }

            public override float interFrameRenderLatencyMean {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, interFrameRenderLatencyMeanFieldID);
                }
            }

            public override float interFrameRenderLatencyStandardDeviation {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, interFrameRenderLatencyStandardDeviationFieldID);
                }
            }

            public override long framesRendered {
                get {
                    return AndroidJNI.GetLongField(videoStatisticsObjectFieldGlobalRef, framesRenderedFieldID);
                }
            }

            public override long framesDropped {
                get {
                    return AndroidJNI.GetLongField(videoStatisticsObjectFieldGlobalRef, framesDroppedFieldID);
                }
            }

            public override float vsyncQuality {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, vsyncQualityFieldID);
                }
            }

            public override float frameReleaseQuality {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, frameReleaseQualityFieldID);
                }
            }

            public override float averageDecoderInputQueueSize {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, averageDecoderInputQueueSizeFieldID);
                }
            }

            public override float averageDecoderOutputQueueSize {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, averageDecoderOutputQueueSizeFieldID);
                }
            }

            public override float endToEndFrameLatencyMean {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, endToEndFrameLatencyMeanFieldID);
                }
            }

            public override float endToEndFrameLatencyStandardDeviation {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, endToEndFrameLatencyStandardDeviationFieldID);
                }
            }
            
            public override float interFrameApplicationLatencyMean {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, interFrameApplicationLatencyMeanFieldID);
                }
            }
            
            public override float interFrameApplicationLatencyStandardDeviation {
                get {
                    return AndroidJNI.GetFloatField(videoStatisticsObjectFieldGlobalRef, interFrameApplicationLatencyStandardDeviationFieldID);
                }
            }

            public VideoStatisticsAndroid(IntPtr argVideoStatisticsObjectField) {
                videoStatisticsObjectFieldGlobalRef = AndroidJNI.NewGlobalRef(argVideoStatisticsObjectField);

                IntPtr videoStatisticsClass = AndroidJNI.FindClass(VideoStatisticsAndroid.videoStatisticsClassName);

    			getInterFrameRenderLatencyAsPrettyStringMethodID = AndroidJNI.GetMethodID(videoStatisticsClass, "getInterFrameRenderLatencyAsPrettyString", "()Ljava/lang/String;");
                getInterFrameDecoderLatencyAsPrettyStringMethodID = AndroidJNI.GetMethodID(videoStatisticsClass, "getInterFrameDecoderLatencyAsPrettyString", "()Ljava/lang/String;");
                getEndToEndFrameLatencyAsPrettyStringMethodID = AndroidJNI.GetMethodID(videoStatisticsClass, "getEndToEndFrameLatencyAsPrettyString", "()Ljava/lang/String;");
                getInterFrameApplicationLatencyAsPrettyStringMethodID = AndroidJNI.GetMethodID(videoStatisticsClass, "getInterFrameApplicationLatencyAsPrettyString", "()Ljava/lang/String;");
                getInterFrameRenderRateInFramesPerSecondMethodID = AndroidJNI.GetMethodID(videoStatisticsClass, "getInterFrameRenderRateInFramesPerSecond", "()F");
                getInterFrameDecoderRateInFramesPerSecondMethodID = AndroidJNI.GetMethodID(videoStatisticsClass, "getInterFrameDecoderRateInFramesPerSecond", "()F");
                getInterFrameApplicationRateInFramesPerSecondMethodID = AndroidJNI.GetMethodID(videoStatisticsClass, "getInterFrameApplicationRateInFramesPerSecond", "()F");

                interFrameDecoderLatencyMeanFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "interFrameDecoderLatencyMean", "F");
                interFrameDecoderLatencyStandardDeviationFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "interFrameDecoderLatencyStandardDeviation", "F");
                interFrameRenderLatencyMeanFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "interFrameRenderLatencyMean", "F");
                interFrameRenderLatencyStandardDeviationFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "interFrameRenderLatencyStandardDeviation", "F");

                framesRenderedFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "framesRendered", "J");
                framesDroppedFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "framesDropped", "J");

                vsyncQualityFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "vsyncQuality", "F");
                frameReleaseQualityFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "frameReleaseQuality", "F");
                averageDecoderInputQueueSizeFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "averageDecoderInputQueueSize", "F");
                averageDecoderOutputQueueSizeFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "averageDecoderOutputQueueSize", "F");
                endToEndFrameLatencyMeanFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "endToEndFrameLatencyMean", "F");
                endToEndFrameLatencyStandardDeviationFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "endToEndFrameLatencyStandardDeviation", "F");
                interFrameApplicationLatencyMeanFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "interFrameApplicationLatencyMean", "F");
                interFrameApplicationLatencyStandardDeviationFieldID = AndroidJNI.GetFieldID(videoStatisticsClass, "interFrameApplicationLatencyStandardDeviation", "F");
                AndroidJNI.DeleteLocalRef(videoStatisticsClass);
            }

            public override String GetInterFrameRenderLatencyAsPrettyString() {
                return AndroidJNI.CallStringMethod(videoStatisticsObjectFieldGlobalRef, getInterFrameRenderLatencyAsPrettyStringMethodID, new jvalue[0]);
            }

            public override String GetInterFrameDecoderLatencyAsPrettyString() {
                return AndroidJNI.CallStringMethod(videoStatisticsObjectFieldGlobalRef, getInterFrameDecoderLatencyAsPrettyStringMethodID, new jvalue[0]);
            }

            public override String GetEndToEndFrameLatencyAsPrettyString() {
                return AndroidJNI.CallStringMethod(videoStatisticsObjectFieldGlobalRef, getEndToEndFrameLatencyAsPrettyStringMethodID, new jvalue[0]);
            }
            
            public override float GetInterFrameRenderRateInFramesPerSecond() {
                return AndroidJNI.CallFloatMethod(videoStatisticsObjectFieldGlobalRef, getInterFrameRenderRateInFramesPerSecondMethodID, new jvalue[0]);
            }

            public override float GetInterFrameDecoderRateInFramesPerSecond() {
                return AndroidJNI.CallFloatMethod(videoStatisticsObjectFieldGlobalRef, getInterFrameDecoderRateInFramesPerSecondMethodID, new jvalue[0]);
            }

            public override void Destroy() {
                if(!videoStatisticsObjectFieldGlobalRef.Equals(IntPtr.Zero)) {
                    AndroidJNI.DeleteGlobalRef(videoStatisticsObjectFieldGlobalRef);
                }
                videoStatisticsObjectFieldGlobalRef = IntPtr.Zero;
            }
        }

        public class AudioStatisticsAndroid : AudioStatisticsBase {
            public static String audioStatisticsClassName = "com/tiledmedia/clearvrcorewrapper/ClearVRCoreWrapperStatistics$AudioStatistics";
            public IntPtr framesRenderedFieldID = IntPtr.Zero;
            public IntPtr framesDroppedFieldID = IntPtr.Zero;
            public IntPtr playbackUnderrunCountFieldID = IntPtr.Zero;
            protected IntPtr audioStatisticsObjectFieldGlobalRef = IntPtr.Zero;

            public AudioStatisticsAndroid(IntPtr argAudioStatisticsObjectField) {
                audioStatisticsObjectFieldGlobalRef = AndroidJNI.NewGlobalRef(argAudioStatisticsObjectField);

                IntPtr audioStatisticsClass = AndroidJNI.FindClass(AudioStatisticsAndroid.audioStatisticsClassName);
                framesRenderedFieldID = AndroidJNI.GetFieldID(audioStatisticsClass, "framesRendered", "J");
                framesDroppedFieldID = AndroidJNI.GetFieldID(audioStatisticsClass, "framesDropped", "J");
                playbackUnderrunCountFieldID = AndroidJNI.GetFieldID(audioStatisticsClass, "playbackUnderrunCount", "I");
                AndroidJNI.DeleteLocalRef(audioStatisticsClass);
            }

            public override long framesRendered {
                get {
                    return AndroidJNI.GetLongField(audioStatisticsObjectFieldGlobalRef, framesRenderedFieldID);
                }
            }

            public override long framesDropped {
                get {
                    return AndroidJNI.GetLongField(audioStatisticsObjectFieldGlobalRef, framesDroppedFieldID);
                }
            }

            public override int playbackUnderrunCount {
                get {
                    return AndroidJNI.GetIntField(audioStatisticsObjectFieldGlobalRef, playbackUnderrunCountFieldID);
                }
            }

            public override void Destroy() {
                if(!audioStatisticsObjectFieldGlobalRef.Equals(IntPtr.Zero)) {
                    AndroidJNI.DeleteGlobalRef(audioStatisticsObjectFieldGlobalRef);
                }
                audioStatisticsObjectFieldGlobalRef = IntPtr.Zero;
            }
        }

        public StatisticsAndroid(IntPtr argClearVRCoreWrapperRawClassGlobalRef, IntPtr argClearVRCoreWrapperGlobalRef) {
            clearVRCoreWrapperGlobalRef = argClearVRCoreWrapperGlobalRef;
			getClearVRCoreWrapperStatisticsMethodId = AndroidJNI.GetMethodID(argClearVRCoreWrapperRawClassGlobalRef, "getClearVRCoreWrapperStatistics", String.Format("()L{0};", clearVRCoreWrapperStatisticsClassName));

            /* Find pointers to the stats classes */
            IntPtr clearVRCoreWrapperStatisticsClass = (IntPtr)AndroidJNI.FindClass(clearVRCoreWrapperStatisticsClassName);
            /* find videoStatistics and audioStatistics fields */
            videoStatisticsFieldID = AndroidJNI.GetFieldID(clearVRCoreWrapperStatisticsClass, "videoStatistics", String.Format("L{0};", VideoStatisticsAndroid.videoStatisticsClassName));
            audioStatisticsFieldID = AndroidJNI.GetFieldID(clearVRCoreWrapperStatisticsClass, "audioStatistics", String.Format("L{0};", AudioStatisticsAndroid.audioStatisticsClassName));

            clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef = AndroidJNI.NewGlobalRef(AndroidJNI.CallObjectMethod(clearVRCoreWrapperGlobalRef, getClearVRCoreWrapperStatisticsMethodId, new jvalue[0]));

            /* Get field IDs */
            pipelineLatencyInNanosecondsFieldID = AndroidJNI.GetFieldID(clearVRCoreWrapperStatisticsClass, "pipelineLatencyInNanoseconds", "J");
            AndroidJNI.DeleteLocalRef(clearVRCoreWrapperStatisticsClass);
            // Get audioStatistics and videoStatistics object from our clearVRCoreWrapperStatistics object.
            IntPtr videoStatisticsObjectField = AndroidJNI.GetObjectField(clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef, videoStatisticsFieldID);
            IntPtr audioStatisticsObjectField = AndroidJNI.GetObjectField(clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef, audioStatisticsFieldID);

            videoStatistics = new VideoStatisticsAndroid(videoStatisticsObjectField);
            audioStatistics = new AudioStatisticsAndroid(audioStatisticsObjectField);
        }


        public override long pipelineLatencyInNanoseconds {
            get {
                return AndroidJNI.GetLongField(clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef, pipelineLatencyInNanosecondsFieldID);
            }
        }
        public override void Destroy() {
            if(isDestroyed) {
                return;
            }
            isDestroyed = true;
            if(!clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef.Equals(IntPtr.Zero)) {
                AndroidJNI.DeleteGlobalRef(clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef);
                clearVRCoreWrapperStatisticsObjectObjectFieldGlobalRef = IntPtr.Zero;
            }
            videoStatistics.Destroy();
            audioStatistics.Destroy();
        }
    }
}
#endif