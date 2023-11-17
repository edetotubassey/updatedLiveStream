//
//  GeneratedTypes.cs
//  UnitySDK
//
//  Created by Mees Kern on 22-08-2023.
//  Copyright 2023 Tiledmedia B.V. All rights reserved.
//
//  ClearVR generated file, do not edit.

using System;
using System.Reflection;
using cvri = com.tiledmedia.clearvr.cvrinterface;

namespace com.tiledmedia.clearvr {

	public enum AudioCodecTypes : Int32 {
		Unspecified = 0,
		AacLc = 1
	}

	static class AudioCodecTypesMethods {
		public static cvri.AudioCodecType ToCoreProtobuf(this AudioCodecTypes audioCodecType) {
			switch (audioCodecType) {
				case AudioCodecTypes.Unspecified:
					return cvri.AudioCodecType.Unspecified;
				case AudioCodecTypes.AacLc:
					return cvri.AudioCodecType.AacLc;
			}
			throw new System.ArgumentException("[ClearVR] AudioCodecTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static AudioCodecTypes FromCoreProtobuf(this cvri.AudioCodecType protoAudioCodecType) {
			switch (protoAudioCodecType) {
				case cvri.AudioCodecType.Unspecified:
					return AudioCodecTypes.Unspecified;
				case cvri.AudioCodecType.AacLc:
					return AudioCodecTypes.AacLc;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf AudioCodecTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static AudioCodecTypes FromInt(int val) {
			switch (val) {
				case 0:
					return AudioCodecTypes.Unspecified;
				case 1:
					return AudioCodecTypes.AacLc;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for AudioCodecTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum VideoCodecTypes : Int32 {
		Unspecified = 0,
		H264 = 1,
		H265 = 2,
		Av1 = 3
	}

	static class VideoCodecTypesMethods {
		public static cvri.VideoCodecType ToCoreProtobuf(this VideoCodecTypes videoCodecType) {
			switch (videoCodecType) {
				case VideoCodecTypes.Unspecified:
					return cvri.VideoCodecType.Unspecified;
				case VideoCodecTypes.H264:
					return cvri.VideoCodecType.H264;
				case VideoCodecTypes.H265:
					return cvri.VideoCodecType.H265;
				case VideoCodecTypes.Av1:
					return cvri.VideoCodecType.Av1;
			}
			throw new System.ArgumentException("[ClearVR] VideoCodecTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static VideoCodecTypes FromCoreProtobuf(this cvri.VideoCodecType protoVideoCodecType) {
			switch (protoVideoCodecType) {
				case cvri.VideoCodecType.Unspecified:
					return VideoCodecTypes.Unspecified;
				case cvri.VideoCodecType.H264:
					return VideoCodecTypes.H264;
				case cvri.VideoCodecType.H265:
					return VideoCodecTypes.H265;
				case cvri.VideoCodecType.Av1:
					return VideoCodecTypes.Av1;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf VideoCodecTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static VideoCodecTypes FromInt(int val) {
			switch (val) {
				case 0:
					return VideoCodecTypes.Unspecified;
				case 1:
					return VideoCodecTypes.H264;
				case 2:
					return VideoCodecTypes.H265;
				case 3:
					return VideoCodecTypes.Av1;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for VideoCodecTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum DRMTypes : Int32 {
		Fairplay = 7,
		Unspecified = 0,
		Tbd = 1,
		None = 2,
		HlsAes128 = 3,
		HlsSampleAes = 4,
		Playready = 5,
		Widevine = 6
	}

	static class DRMTypesMethods {
		public static cvri.DRMType ToCoreProtobuf(this DRMTypes drmType) {
			switch (drmType) {
				case DRMTypes.Fairplay:
					return cvri.DRMType.Fairplay;
				case DRMTypes.Unspecified:
					return cvri.DRMType.Unspecified;
				case DRMTypes.Tbd:
					return cvri.DRMType.Tbd;
				case DRMTypes.None:
					return cvri.DRMType.None;
				case DRMTypes.HlsAes128:
					return cvri.DRMType.HlsAes128;
				case DRMTypes.HlsSampleAes:
					return cvri.DRMType.HlsSampleAes;
				case DRMTypes.Playready:
					return cvri.DRMType.Playready;
				case DRMTypes.Widevine:
					return cvri.DRMType.Widevine;
			}
			throw new System.ArgumentException("[ClearVR] DRMTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static DRMTypes FromCoreProtobuf(this cvri.DRMType protoDRMType) {
			switch (protoDRMType) {
				case cvri.DRMType.Fairplay:
					return DRMTypes.Fairplay;
				case cvri.DRMType.Unspecified:
					return DRMTypes.Unspecified;
				case cvri.DRMType.Tbd:
					return DRMTypes.Tbd;
				case cvri.DRMType.None:
					return DRMTypes.None;
				case cvri.DRMType.HlsAes128:
					return DRMTypes.HlsAes128;
				case cvri.DRMType.HlsSampleAes:
					return DRMTypes.HlsSampleAes;
				case cvri.DRMType.Playready:
					return DRMTypes.Playready;
				case cvri.DRMType.Widevine:
					return DRMTypes.Widevine;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf DRMTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static DRMTypes FromInt(int val) {
			switch (val) {
				case 7:
					return DRMTypes.Fairplay;
				case 0:
					return DRMTypes.Unspecified;
				case 1:
					return DRMTypes.Tbd;
				case 2:
					return DRMTypes.None;
				case 3:
					return DRMTypes.HlsAes128;
				case 4:
					return DRMTypes.HlsSampleAes;
				case 5:
					return DRMTypes.Playready;
				case 6:
					return DRMTypes.Widevine;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for DRMTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum ProjectionTypes : Int32 {
		RectilinearMono = 0,
		Erp360StereoTopBottom = 3,
		FisheyeMono = 6,
		RectilinearStereoSideBySide = 8,
		MeshBoxMono = 9,
		Unknown = 99,
		RectilinearStereoTopBottom = 1,
		Erp360Mono = 2,
		Erp180Mono = 4,
		Erp180StereoSideBySide = 5,
		FisheyeStereoSideBySide = 7,
		MeshBoxStereo = 10
	}

	static class ProjectionTypesMethods {
		public static cvri.ProjectionType ToCoreProtobuf(this ProjectionTypes projectionType) {
			switch (projectionType) {
				case ProjectionTypes.RectilinearMono:
					return cvri.ProjectionType.RectilinearMono;
				case ProjectionTypes.Erp360StereoTopBottom:
					return cvri.ProjectionType.Erp360StereoTopBottom;
				case ProjectionTypes.FisheyeMono:
					return cvri.ProjectionType.FisheyeMono;
				case ProjectionTypes.RectilinearStereoSideBySide:
					return cvri.ProjectionType.RectilinearStereoSideBySide;
				case ProjectionTypes.MeshBoxMono:
					return cvri.ProjectionType.MeshBoxMono;
				case ProjectionTypes.Unknown:
					return cvri.ProjectionType.Unknown;
				case ProjectionTypes.RectilinearStereoTopBottom:
					return cvri.ProjectionType.RectilinearStereoTopBottom;
				case ProjectionTypes.Erp360Mono:
					return cvri.ProjectionType.Erp360Mono;
				case ProjectionTypes.Erp180Mono:
					return cvri.ProjectionType.Erp180Mono;
				case ProjectionTypes.Erp180StereoSideBySide:
					return cvri.ProjectionType.Erp180StereoSideBySide;
				case ProjectionTypes.FisheyeStereoSideBySide:
					return cvri.ProjectionType.FisheyeStereoSideBySide;
				case ProjectionTypes.MeshBoxStereo:
					return cvri.ProjectionType.MeshBoxStereo;
			}
			throw new System.ArgumentException("[ClearVR] ProjectionTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static ProjectionTypes FromCoreProtobuf(this cvri.ProjectionType protoProjectionType) {
			switch (protoProjectionType) {
				case cvri.ProjectionType.RectilinearMono:
					return ProjectionTypes.RectilinearMono;
				case cvri.ProjectionType.Erp360StereoTopBottom:
					return ProjectionTypes.Erp360StereoTopBottom;
				case cvri.ProjectionType.FisheyeMono:
					return ProjectionTypes.FisheyeMono;
				case cvri.ProjectionType.RectilinearStereoSideBySide:
					return ProjectionTypes.RectilinearStereoSideBySide;
				case cvri.ProjectionType.MeshBoxMono:
					return ProjectionTypes.MeshBoxMono;
				case cvri.ProjectionType.Unknown:
					return ProjectionTypes.Unknown;
				case cvri.ProjectionType.RectilinearStereoTopBottom:
					return ProjectionTypes.RectilinearStereoTopBottom;
				case cvri.ProjectionType.Erp360Mono:
					return ProjectionTypes.Erp360Mono;
				case cvri.ProjectionType.Erp180Mono:
					return ProjectionTypes.Erp180Mono;
				case cvri.ProjectionType.Erp180StereoSideBySide:
					return ProjectionTypes.Erp180StereoSideBySide;
				case cvri.ProjectionType.FisheyeStereoSideBySide:
					return ProjectionTypes.FisheyeStereoSideBySide;
				case cvri.ProjectionType.MeshBoxStereo:
					return ProjectionTypes.MeshBoxStereo;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf ProjectionTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static ProjectionTypes FromInt(int val) {
			switch (val) {
				case 0:
					return ProjectionTypes.RectilinearMono;
				case 3:
					return ProjectionTypes.Erp360StereoTopBottom;
				case 6:
					return ProjectionTypes.FisheyeMono;
				case 8:
					return ProjectionTypes.RectilinearStereoSideBySide;
				case 9:
					return ProjectionTypes.MeshBoxMono;
				case 99:
					return ProjectionTypes.Unknown;
				case 1:
					return ProjectionTypes.RectilinearStereoTopBottom;
				case 2:
					return ProjectionTypes.Erp360Mono;
				case 4:
					return ProjectionTypes.Erp180Mono;
				case 5:
					return ProjectionTypes.Erp180StereoSideBySide;
				case 7:
					return ProjectionTypes.FisheyeStereoSideBySide;
				case 10:
					return ProjectionTypes.MeshBoxStereo;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for ProjectionTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum DisplayObjectClassTypes : Int32 {
		SmallPanel = 3,
		Thumbnail = 4,
		Unknown = 0,
		FullScreen = 1,
		LargePanel = 2
	}

	static class DisplayObjectClassTypesMethods {
		public static cvri.DisplayObjectClass ToCoreProtobuf(this DisplayObjectClassTypes displayObjectClassType) {
			switch (displayObjectClassType) {
				case DisplayObjectClassTypes.SmallPanel:
					return cvri.DisplayObjectClass.SmallPanel;
				case DisplayObjectClassTypes.Thumbnail:
					return cvri.DisplayObjectClass.Thumbnail;
				case DisplayObjectClassTypes.Unknown:
					return cvri.DisplayObjectClass.Unknown;
				case DisplayObjectClassTypes.FullScreen:
					return cvri.DisplayObjectClass.FullScreen;
				case DisplayObjectClassTypes.LargePanel:
					return cvri.DisplayObjectClass.LargePanel;
			}
			throw new System.ArgumentException("[ClearVR] DisplayObjectClassTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static DisplayObjectClassTypes FromCoreProtobuf(this cvri.DisplayObjectClass protoDisplayObjectClassType) {
			switch (protoDisplayObjectClassType) {
				case cvri.DisplayObjectClass.SmallPanel:
					return DisplayObjectClassTypes.SmallPanel;
				case cvri.DisplayObjectClass.Thumbnail:
					return DisplayObjectClassTypes.Thumbnail;
				case cvri.DisplayObjectClass.Unknown:
					return DisplayObjectClassTypes.Unknown;
				case cvri.DisplayObjectClass.FullScreen:
					return DisplayObjectClassTypes.FullScreen;
				case cvri.DisplayObjectClass.LargePanel:
					return DisplayObjectClassTypes.LargePanel;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf DisplayObjectClassTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static DisplayObjectClassTypes FromInt(int val) {
			switch (val) {
				case 3:
					return DisplayObjectClassTypes.SmallPanel;
				case 4:
					return DisplayObjectClassTypes.Thumbnail;
				case 0:
					return DisplayObjectClassTypes.Unknown;
				case 1:
					return DisplayObjectClassTypes.FullScreen;
				case 2:
					return DisplayObjectClassTypes.LargePanel;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for DisplayObjectClassTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum EventTypes : Int32 {
		Vod = 0,
		Live = 1,
		FinishedLive = 2,
		Unknown = 3
	}

	static class EventTypesMethods {
		public static cvri.EventType ToCoreProtobuf(this EventTypes eventType) {
			switch (eventType) {
				case EventTypes.Vod:
					return cvri.EventType.Vod;
				case EventTypes.Live:
					return cvri.EventType.Live;
				case EventTypes.FinishedLive:
					return cvri.EventType.FinishedLive;
				case EventTypes.Unknown:
					return cvri.EventType.Unknown;
			}
			throw new System.ArgumentException("[ClearVR] EventTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static EventTypes FromCoreProtobuf(this cvri.EventType protoEventType) {
			switch (protoEventType) {
				case cvri.EventType.Vod:
					return EventTypes.Vod;
				case cvri.EventType.Live:
					return EventTypes.Live;
				case cvri.EventType.FinishedLive:
					return EventTypes.FinishedLive;
				case cvri.EventType.Unknown:
					return EventTypes.Unknown;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf EventTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static EventTypes FromInt(int val) {
			switch (val) {
				case 0:
					return EventTypes.Vod;
				case 1:
					return EventTypes.Live;
				case 2:
					return EventTypes.FinishedLive;
				case 3:
					return EventTypes.Unknown;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for EventTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum VideoCodecProfiles : Int32 {
		H265Main10Hdr10Plus = 6,
		H264ConstrainedBaseline = 10,
		H264High = 14,
		H264High444 = 17,
		H265Main = 1,
		H265Main10 = 2,
		H264High422 = 16,
		Unknown = 0,
		H264High10 = 15,
		H264Main = 13,
		H264ConstrainedHigh = 18,
		H264Baseline = 11,
		H264ExtendedProfile = 12,
		H265Main10Hdr10 = 5,
		Av1Main = 20,
		Av1High = 21,
		Av1Professional = 22,
		H265Main12 = 3,
		H265MainStill = 4
	}

	static class VideoCodecProfilesMethods {
		public static cvri.VideoCodecProfile ToCoreProtobuf(this VideoCodecProfiles videoCodecProfile) {
			switch (videoCodecProfile) {
				case VideoCodecProfiles.H265Main10Hdr10Plus:
					return cvri.VideoCodecProfile.H265Main10Hdr10Plus;
				case VideoCodecProfiles.H264ConstrainedBaseline:
					return cvri.VideoCodecProfile.H264ConstrainedBaseline;
				case VideoCodecProfiles.H264High:
					return cvri.VideoCodecProfile.H264High;
				case VideoCodecProfiles.H264High444:
					return cvri.VideoCodecProfile.H264High444;
				case VideoCodecProfiles.H265Main:
					return cvri.VideoCodecProfile.H265Main;
				case VideoCodecProfiles.H265Main10:
					return cvri.VideoCodecProfile.H265Main10;
				case VideoCodecProfiles.H264High422:
					return cvri.VideoCodecProfile.H264High422;
				case VideoCodecProfiles.Unknown:
					return cvri.VideoCodecProfile.Unknown;
				case VideoCodecProfiles.H264High10:
					return cvri.VideoCodecProfile.H264High10;
				case VideoCodecProfiles.H264Main:
					return cvri.VideoCodecProfile.H264Main;
				case VideoCodecProfiles.H264ConstrainedHigh:
					return cvri.VideoCodecProfile.H264ConstrainedHigh;
				case VideoCodecProfiles.H264Baseline:
					return cvri.VideoCodecProfile.H264Baseline;
				case VideoCodecProfiles.H264ExtendedProfile:
					return cvri.VideoCodecProfile.H264ExtendedProfile;
				case VideoCodecProfiles.H265Main10Hdr10:
					return cvri.VideoCodecProfile.H265Main10Hdr10;
				case VideoCodecProfiles.Av1Main:
					return cvri.VideoCodecProfile.Av1Main;
				case VideoCodecProfiles.Av1High:
					return cvri.VideoCodecProfile.Av1High;
				case VideoCodecProfiles.Av1Professional:
					return cvri.VideoCodecProfile.Av1Professional;
				case VideoCodecProfiles.H265Main12:
					return cvri.VideoCodecProfile.H265Main12;
				case VideoCodecProfiles.H265MainStill:
					return cvri.VideoCodecProfile.H265MainStill;
			}
			throw new System.ArgumentException("[ClearVR] VideoCodecProfiles cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static VideoCodecProfiles FromCoreProtobuf(this cvri.VideoCodecProfile protoVideoCodecProfile) {
			switch (protoVideoCodecProfile) {
				case cvri.VideoCodecProfile.H265Main10Hdr10Plus:
					return VideoCodecProfiles.H265Main10Hdr10Plus;
				case cvri.VideoCodecProfile.H264ConstrainedBaseline:
					return VideoCodecProfiles.H264ConstrainedBaseline;
				case cvri.VideoCodecProfile.H264High:
					return VideoCodecProfiles.H264High;
				case cvri.VideoCodecProfile.H264High444:
					return VideoCodecProfiles.H264High444;
				case cvri.VideoCodecProfile.H265Main:
					return VideoCodecProfiles.H265Main;
				case cvri.VideoCodecProfile.H265Main10:
					return VideoCodecProfiles.H265Main10;
				case cvri.VideoCodecProfile.H264High422:
					return VideoCodecProfiles.H264High422;
				case cvri.VideoCodecProfile.Unknown:
					return VideoCodecProfiles.Unknown;
				case cvri.VideoCodecProfile.H264High10:
					return VideoCodecProfiles.H264High10;
				case cvri.VideoCodecProfile.H264Main:
					return VideoCodecProfiles.H264Main;
				case cvri.VideoCodecProfile.H264ConstrainedHigh:
					return VideoCodecProfiles.H264ConstrainedHigh;
				case cvri.VideoCodecProfile.H264Baseline:
					return VideoCodecProfiles.H264Baseline;
				case cvri.VideoCodecProfile.H264ExtendedProfile:
					return VideoCodecProfiles.H264ExtendedProfile;
				case cvri.VideoCodecProfile.H265Main10Hdr10:
					return VideoCodecProfiles.H265Main10Hdr10;
				case cvri.VideoCodecProfile.Av1Main:
					return VideoCodecProfiles.Av1Main;
				case cvri.VideoCodecProfile.Av1High:
					return VideoCodecProfiles.Av1High;
				case cvri.VideoCodecProfile.Av1Professional:
					return VideoCodecProfiles.Av1Professional;
				case cvri.VideoCodecProfile.H265Main12:
					return VideoCodecProfiles.H265Main12;
				case cvri.VideoCodecProfile.H265MainStill:
					return VideoCodecProfiles.H265MainStill;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf VideoCodecProfiles cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static VideoCodecProfiles FromInt(int val) {
			switch (val) {
				case 6:
					return VideoCodecProfiles.H265Main10Hdr10Plus;
				case 10:
					return VideoCodecProfiles.H264ConstrainedBaseline;
				case 14:
					return VideoCodecProfiles.H264High;
				case 17:
					return VideoCodecProfiles.H264High444;
				case 1:
					return VideoCodecProfiles.H265Main;
				case 2:
					return VideoCodecProfiles.H265Main10;
				case 16:
					return VideoCodecProfiles.H264High422;
				case 0:
					return VideoCodecProfiles.Unknown;
				case 15:
					return VideoCodecProfiles.H264High10;
				case 13:
					return VideoCodecProfiles.H264Main;
				case 18:
					return VideoCodecProfiles.H264ConstrainedHigh;
				case 11:
					return VideoCodecProfiles.H264Baseline;
				case 12:
					return VideoCodecProfiles.H264ExtendedProfile;
				case 5:
					return VideoCodecProfiles.H265Main10Hdr10;
				case 20:
					return VideoCodecProfiles.Av1Main;
				case 21:
					return VideoCodecProfiles.Av1High;
				case 22:
					return VideoCodecProfiles.Av1Professional;
				case 3:
					return VideoCodecProfiles.H265Main12;
				case 4:
					return VideoCodecProfiles.H265MainStill;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for VideoCodecProfiles. Please report this crash to Tiledmedia.");
		}
	}


	public enum LogLevels : Int32 {
		Info = 1,
		Debug = 2,
		Error = -1,
		Fatal = -2,
		Warn = 0
	}

	static class LogLevelsMethods {
		public static cvri.LogLevel ToCoreProtobuf(this LogLevels logLevel) {
			switch (logLevel) {
				case LogLevels.Info:
					return cvri.LogLevel.Info;
				case LogLevels.Debug:
					return cvri.LogLevel.Debug;
				case LogLevels.Error:
					return cvri.LogLevel.Error;
				case LogLevels.Fatal:
					return cvri.LogLevel.Fatal;
				case LogLevels.Warn:
					return cvri.LogLevel.Warn;
			}
			throw new System.ArgumentException("[ClearVR] LogLevels cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static LogLevels FromCoreProtobuf(this cvri.LogLevel protoLogLevel) {
			switch (protoLogLevel) {
				case cvri.LogLevel.Info:
					return LogLevels.Info;
				case cvri.LogLevel.Debug:
					return LogLevels.Debug;
				case cvri.LogLevel.Error:
					return LogLevels.Error;
				case cvri.LogLevel.Fatal:
					return LogLevels.Fatal;
				case cvri.LogLevel.Warn:
					return LogLevels.Warn;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf LogLevels cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static LogLevels FromInt(int val) {
			switch (val) {
				case 1:
					return LogLevels.Info;
				case 2:
					return LogLevels.Debug;
				case -1:
					return LogLevels.Error;
				case -2:
					return LogLevels.Fatal;
				case 0:
					return LogLevels.Warn;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for LogLevels. Please report this crash to Tiledmedia.");
		}
	}


	public enum LogComponents : Int32 {
		Sdk = 4,
		SigmaAudio = 5,
		Unknown = 0,
		TmCore = 1,
		Nrp = 2,
		MediaFlow = 3
	}

	static class LogComponentsMethods {
		public static cvri.LogComponent ToCoreProtobuf(this LogComponents logComponent) {
			switch (logComponent) {
				case LogComponents.Sdk:
					return cvri.LogComponent.Sdk;
				case LogComponents.SigmaAudio:
					return cvri.LogComponent.SigmaAudio;
				case LogComponents.Unknown:
					return cvri.LogComponent.Unknown;
				case LogComponents.TmCore:
					return cvri.LogComponent.TmCore;
				case LogComponents.Nrp:
					return cvri.LogComponent.Nrp;
				case LogComponents.MediaFlow:
					return cvri.LogComponent.MediaFlow;
			}
			throw new System.ArgumentException("[ClearVR] LogComponents cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static LogComponents FromCoreProtobuf(this cvri.LogComponent protoLogComponent) {
			switch (protoLogComponent) {
				case cvri.LogComponent.Sdk:
					return LogComponents.Sdk;
				case cvri.LogComponent.SigmaAudio:
					return LogComponents.SigmaAudio;
				case cvri.LogComponent.Unknown:
					return LogComponents.Unknown;
				case cvri.LogComponent.TmCore:
					return LogComponents.TmCore;
				case cvri.LogComponent.Nrp:
					return LogComponents.Nrp;
				case cvri.LogComponent.MediaFlow:
					return LogComponents.MediaFlow;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf LogComponents cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static LogComponents FromInt(int val) {
			switch (val) {
				case 4:
					return LogComponents.Sdk;
				case 5:
					return LogComponents.SigmaAudio;
				case 0:
					return LogComponents.Unknown;
				case 1:
					return LogComponents.TmCore;
				case 2:
					return LogComponents.Nrp;
				case 3:
					return LogComponents.MediaFlow;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for LogComponents. Please report this crash to Tiledmedia.");
		}
	}


	public enum SDKTypes : Int32 {
		Unknown = 0,
		Unity = 1,
		Native = 2,
		Web = 3
	}

	static class SDKTypesMethods {
		public static cvri.SDKType ToCoreProtobuf(this SDKTypes sdkType) {
			switch (sdkType) {
				case SDKTypes.Unknown:
					return cvri.SDKType.Unknown;
				case SDKTypes.Unity:
					return cvri.SDKType.Unity;
				case SDKTypes.Native:
					return cvri.SDKType.Native;
				case SDKTypes.Web:
					return cvri.SDKType.Web;
			}
			throw new System.ArgumentException("[ClearVR] SDKTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static SDKTypes FromCoreProtobuf(this cvri.SDKType protoSDKType) {
			switch (protoSDKType) {
				case cvri.SDKType.Unknown:
					return SDKTypes.Unknown;
				case cvri.SDKType.Unity:
					return SDKTypes.Unity;
				case cvri.SDKType.Native:
					return SDKTypes.Native;
				case cvri.SDKType.Web:
					return SDKTypes.Web;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf SDKTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static SDKTypes FromInt(int val) {
			switch (val) {
				case 0:
					return SDKTypes.Unknown;
				case 1:
					return SDKTypes.Unity;
				case 2:
					return SDKTypes.Native;
				case 3:
					return SDKTypes.Web;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for SDKTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum TelemetryTargetTypes : Int32 {
		TelemetryTargetNewRelic = 0
	}

	static class TelemetryTargetTypesMethods {
		public static cvri.TelemetryTargetType ToCoreProtobuf(this TelemetryTargetTypes telemetryTargetType) {
			switch (telemetryTargetType) {
				case TelemetryTargetTypes.TelemetryTargetNewRelic:
					return cvri.TelemetryTargetType.TelemetryTargetNewRelic;
			}
			throw new System.ArgumentException("[ClearVR] TelemetryTargetTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static TelemetryTargetTypes FromCoreProtobuf(this cvri.TelemetryTargetType protoTelemetryTargetType) {
			switch (protoTelemetryTargetType) {
				case cvri.TelemetryTargetType.TelemetryTargetNewRelic:
					return TelemetryTargetTypes.TelemetryTargetNewRelic;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf TelemetryTargetTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static TelemetryTargetTypes FromInt(int val) {
			switch (val) {
				case 0:
					return TelemetryTargetTypes.TelemetryTargetNewRelic;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for TelemetryTargetTypes. Please report this crash to Tiledmedia.");
		}
	}


	public enum TelemetryIPSignallingTypes : Int32 {
		TelemetryIpSignallingDisabled = 0,
		TelemetryIpSignallingMasked = 1,
		TelemetryIpSignallingFull = 2
	}

	static class TelemetryIPSignallingTypesMethods {
		public static cvri.TelemetryIPSignallingType ToCoreProtobuf(this TelemetryIPSignallingTypes telemetryIpsignallingType) {
			switch (telemetryIpsignallingType) {
				case TelemetryIPSignallingTypes.TelemetryIpSignallingDisabled:
					return cvri.TelemetryIPSignallingType.TelemetryIpSignallingDisabled;
				case TelemetryIPSignallingTypes.TelemetryIpSignallingMasked:
					return cvri.TelemetryIPSignallingType.TelemetryIpSignallingMasked;
				case TelemetryIPSignallingTypes.TelemetryIpSignallingFull:
					return cvri.TelemetryIPSignallingType.TelemetryIpSignallingFull;
			}
			throw new System.ArgumentException("[ClearVR] TelemetryIPSignallingTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static TelemetryIPSignallingTypes FromCoreProtobuf(this cvri.TelemetryIPSignallingType protoTelemetryIPSignallingType) {
			switch (protoTelemetryIPSignallingType) {
				case cvri.TelemetryIPSignallingType.TelemetryIpSignallingDisabled:
					return TelemetryIPSignallingTypes.TelemetryIpSignallingDisabled;
				case cvri.TelemetryIPSignallingType.TelemetryIpSignallingMasked:
					return TelemetryIPSignallingTypes.TelemetryIpSignallingMasked;
				case cvri.TelemetryIPSignallingType.TelemetryIpSignallingFull:
					return TelemetryIPSignallingTypes.TelemetryIpSignallingFull;
			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf TelemetryIPSignallingTypes cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
		internal static TelemetryIPSignallingTypes FromInt(int val) {
			switch (val) {
				case 0:
					return TelemetryIPSignallingTypes.TelemetryIpSignallingDisabled;
				case 1:
					return TelemetryIPSignallingTypes.TelemetryIpSignallingMasked;
				case 2:
					return TelemetryIPSignallingTypes.TelemetryIpSignallingFull;
			}
			throw new System.ArgumentException("[ClearVR] Provided integer cannot be converted to internal equivalent for TelemetryIPSignallingTypes. Please report this crash to Tiledmedia.");
		}
	}

	/// <summary>
	/// Fish-eye settings presets for known camera types
	/// </summary>
	public enum FisheyePresets : Int32 {
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 8mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@8mm")]
		BlackmagicUrsaMiniCanon8_15_8Mm = 0,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 8.5mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@8.5mm")]
		BlackmagicUrsaMiniCanon8_15_8Dot5Mm = 1,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 9mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@9mm")]
		BlackmagicUrsaMiniCanon8_15_9Mm = 2,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 9.5mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@9.5mm")]
		BlackmagicUrsaMiniCanon8_15_9Dot5Mm = 3,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 10mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@10mm")]
		BlackmagicUrsaMiniCanon8_15_10Mm = 4,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 10.5mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@10.5mm")]
		BlackmagicUrsaMiniCanon8_15_10Dot5Mm = 5,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 11mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@11mm")]
		BlackmagicUrsaMiniCanon8_15_11Mm = 6,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 11.5mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@11.5mm")]
		BlackmagicUrsaMiniCanon8_15_11Dot5Mm = 7,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 12mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@12mm")]
		BlackmagicUrsaMiniCanon8_15_12Mm = 8,
		/// <summary>
		/// Use the preset for the blackmagic URSA Mini Canon 8-15 at 12.5mm.
		/// </summary>
		[StringValue("blackmagic-ursa-mini_canon8-15mmF4.0_@12.5mm")]
		BlackmagicUrsaMiniCanon8_15_12Dot5Mm = 9,
		/// <summary>
		/// Use the preset for the Blackmagic Ursa 12K Camera with Canon 815 lens at 8 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[StringValue("blackmagic-ursa-12k_canon8-15mmF4.0_@8mm_8K_16:9")]
		BlackmagicUrsa12Kcanon8_15_8Mm_8K_16_9 = 10,
		/// <summary>
		/// Use the preset for the Z cam K1 pro with Pro Izugar Mkx 22mft sensor.
		/// </summary>
		[StringValue("zcam-k1-pro_izugar-mkx22mft-3.25mm_@3.25mm")]
		Zcamk1ProIzugarMkx22Mft_3Dot25Mm = 11,
		/// <summary>
		/// Use the preset for the Z cam K2 pro with Pro Izugar Mkx 200 sensor.
		/// </summary>
		[StringValue("zcam-k2-pro_izugar-mkx200-3.8mm_@3.8mm")]
		Zcamk2ProIzugarMkx200_3Dot8Mm = 12,
		/// <summary>
		/// Use the preset for the Red Komodo 6K Camera with Canon 815 lens.
		/// </summary>
		[StringValue("red-komodo-6K-canon8_12mm_@8mm")]
		RedKomodo6Kcanon8_12_8Mm = 13,
		/// <summary>
		/// Use the preset for the Red V-Raptor 8K Camera with Canon 815 lens at 8 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[StringValue("red-v-raptor-8K-canon8_15mm_@8mm")]
		RedVraptor8Kcanon8_15_8Mm = 14,
		/// <summary>
		/// Use the preset for the Red V-Raptor 8K Camera with Canon 815 lens at 10 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[StringValue("red-v-raptor-8K-canon8_15mm_@10mm")]
		RedVraptor8Kcanon8_15_10Mm = 15,
		/// <summary>
		/// Use the preset for the Red V-Raptor 8K Camera with Canon 815 lens at 13 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[StringValue("red-v-raptor-8K-canon8_15mm_@13mm")]
		RedVraptor8Kcanon8_15_13Mm = 16,
		/// <summary>
		/// No preset used for the fish eye camera. Can be used if a custom camera and lens type combination is used not covered by any of the provided presets.
		/// </summary>
		[StringValue("custom")]
		Custom = 17
	}

	static class FisheyePresetsMethods {
		public static string GetStringValue(this FisheyePresets value) {
			Type type = value.GetType();

			FieldInfo fieldInfo = type.GetField(value.ToString());

			StringValueAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];

			return attribs.Length > 0 ? attribs[0].StringValue : null;
		}

		public static FisheyePresets FromStringValue(string value) {
			foreach (FisheyePresets _value in Enum.GetValues(typeof(FisheyePresets))) {
				if (_value.GetStringValue() == value) {
					return _value;
				}
			}
			throw new System.ArgumentException(string.Format("[ClearVR] string value of: {0} cannot be converted to enum equivalent. Please report this issue to Tiledmedia", value));
		}
	}
} // namespace com.tiledmedia.clearvr