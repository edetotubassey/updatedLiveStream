using System;

namespace com.tiledmedia.clearvr {
    public abstract class StatisticsBase : ClearVRCoreWrapperStatisticsInterface {
        public abstract class VideoStatisticsBase :  ClearVRCoreWrapperVideoStatisticsInterface {
            public virtual float interFrameDecoderLatencyMean {
                get {
                    return 0;
                }
            }
            
            public virtual float interFrameDecoderLatencyStandardDeviation {
                get {
                    return 0;
                }
            }

            public virtual float interFrameRenderLatencyMean {
                get {
                    return 0;
                }
            }

            public virtual float interFrameRenderLatencyStandardDeviation {
                get {
                    return 0;
                }
            }

            public virtual long framesRendered {
                get {
                    return 0;
                }
            }
            
            public virtual long framesDropped {
                get {
                    return 0;
                }
            }
            
            public virtual float vsyncQuality {
                get {
                    return 0;
                }
            }
            
            public virtual float frameReleaseQuality {
                get {
                    return 0;
                }
            }
            
            public virtual float averageDecoderInputQueueSize {
                get {
                    return 0;
                }
            }
            
            public virtual float averageDecoderOutputQueueSize {
                get {
                    return 0;
                }
            }
            
            public virtual float endToEndFrameLatencyMean {
                get {
                    return 0;
                }
            }
            
            public virtual float endToEndFrameLatencyStandardDeviation {
                get {
                    return 0;
                }
            }
            
            public virtual float interFrameApplicationLatencyMean {
                get {
                    return 0;
                }
            }

            public virtual float interFrameApplicationLatencyStandardDeviation {
                get {
                    return 0;
                }
            }

            public virtual String GetInterFrameRenderLatencyAsPrettyString() {
                return "";
            }

            public virtual String GetInterFrameDecoderLatencyAsPrettyString() {
                return "";
            }

            public virtual String GetEndToEndFrameLatencyAsPrettyString() {
                return "";
            }

            public virtual String GetInterFrameApplicationLatencyAsPrettyString() {
                return "";
            }

            public virtual float GetInterFrameRenderRateInFramesPerSecond() {
                return 0f;
            }

            public virtual float GetInterFrameDecoderRateInFramesPerSecond() {
                return 0f;
            }

            public virtual float GetInterFrameApplicationRateInFramesPerSecond() {
                return 0f;
            }

            public virtual void Destroy() {}
        }

        public abstract class AudioStatisticsBase : ClearVRCoreWrapperAudioStatisticsInterface {
            public virtual long framesRendered {
                get {
                    return 0;
                }
            }
            
            public virtual long framesDropped {
                get {
                    return 0;
                }
            }
            
            public virtual int playbackUnderrunCount {
                get {
                    return 0;
                }
            }

            public virtual void Destroy() {}
        }

        public VideoStatisticsBase videoStatistics = null;
        public AudioStatisticsBase audioStatistics = null;
        
        public virtual long pipelineLatencyInNanoseconds {
            get {
                return 0;
            }
        }

        protected bool isDestroyed = false;

        public virtual void Destroy() {}
    }
}