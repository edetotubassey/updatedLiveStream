using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Text;
using cvri = com.tiledmedia.clearvr.cvrinterface;

namespace com.tiledmedia.clearvr {
	/// <summary>
	/// The ClearVREvents listener prototype.
	/// </summary>
	public class ClearVREvents : UnityEngine.Events.UnityEvent<ClearVRPlayer, ClearVREvent> { }
	/// <summary>
	/// The ClearVRDisplayObjects Events listener prototype.
	/// </summary>
	[Serializable]
	public class ClearVRDisplayObjectEvents : UnityEngine.Events.UnityEvent<ClearVRPlayer, ClearVRDisplayObjectControllerBase, ClearVRDisplayObjectEvent> { }

	[Serializable]
	public class ClearVRDisplayObjectEventsInternal : UnityEngine.Events.UnityEvent<ClearVRDisplayObjectControllerBase, ClearVRDisplayObjectEvent> { }
	public class ClearVRConstants {
		/// <summary>
		/// Use this constant to indicate that you want to maintain the same gain.
		/// This is usefull in conjunction with the [AudioTrackAndPlaybackParameters](xref:com.tiledmedia.clearvr.AudioTrackAndPlaybackParameters) 
		/// </summary>
		internal const float AUDIO_MAINTAIN_GAIN = -1F; // internal until we can publicly exposed it, as this is still WIP.

		internal const String CLEARVR_LOG_GLOBAL_FILE_NAME = "clearvr.tmlog"; // If you change this, don't forget to update documentation
		internal const String CLEARVR_EVENT_RECORDER_PROTO_FILE_NAME = "recorder.tmerp"; // If you change this, don't forget to update documentation
		internal const String CLEARVR_EVENT_RECORDER_JSON_FILE_NAME = "recorder.tmerj"; // If you change this, don't forget to update documentation
	}

	/// <summary>
	/// The primary interface for handling events from the ClearVRPlayer in your own application. As soon as you have created your CLearVRPlayer object, one shuuld subscribe an EventListener through `clearVRPlayer.clearVREvents.AddListener(this.02);`
	/// </summary>
	public class ClearVREvent {
		private ClearVREventTypes _type;
		private ClearVRMessage _message = null;
		private ClearVRAsyncRequestResponse _clearVRAsyncRequestResponse = null;

		public ClearVREventTypes type {
			get {
				return _type;
			}
			set {
				_type = value;
			}
		}

		/// <summary>
		/// The message that this event holds.
		/// </summary>
		/// <value>The ClearVRMessage</value>
		public ClearVRMessage message {
			get {
				return _message;
			}
			set {
				_message = value;
			}
		}

		[Obsolete("Public access to the clearVRAsyncRequestResponse field has been removed. Please query the optionalArguments field instead.", true)]
		public System.Object clearVRAsyncRequestResponse {
			get {
				return null;
			}
		}

		internal ClearVRAsyncRequestResponse __clearVRAsyncRequestResponse {
			get {
				return _clearVRAsyncRequestResponse;
			}
		}

		public object[] optionalArguments {
			get {
				return _clearVRAsyncRequestResponse.optionalArguments;
			}
		}

		internal static ClearVREvent GetGenericOKEvent(ClearVREventTypes argType) {
			return new ClearVREvent(argType, ClearVRMessage.GetGenericOKMessage());
		}

		internal static ClearVREvent GetGenericWarningEvent(ClearVREventTypes argType) {
			return new ClearVREvent(argType, ClearVRMessage.GetGenericWarningMessage());
		}

		internal static ClearVREvent GetGenericFatalErrorEvent(ClearVREventTypes argType) {
			return new ClearVREvent(argType, ClearVRMessage.GetGenericFatalErrorMessage());
		}

		internal ClearVREvent(ClearVREventTypes argEventType, ClearVRMessageTypes argMessageType, ClearVRMessageCodes argCode, String argMessage, ClearVRResult argClearVRResult) {
			_type = argEventType;
			_message = new ClearVRMessage(argMessageType, argCode, argMessage, argClearVRResult);
		}

		internal ClearVREvent(ClearVREventTypes argEventType, int argMessageType, int argCode, String argMessage, ClearVRResult argClearVRResult) {
			_type = argEventType;
			_message = new ClearVRMessage(argMessageType, argCode, argMessage, argClearVRResult);
		}

		internal ClearVREvent(ClearVREventTypes argEventType, ClearVRMessage argMessage) {
			_type = argEventType;
			_message = argMessage;
		}

		internal ClearVREvent(ClearVREventTypes argEventType, ClearVRAsyncRequestResponse argClearVRAsyncRequestResponse, ClearVRMessage argClearVRMessage) {
			_type = argEventType;
			_message = argClearVRMessage;
			_clearVRAsyncRequestResponse = argClearVRAsyncRequestResponse;
		}

		/// <summary>
		/// Convenience method to distinguish between Warning/FatalError events and  Info events.
		/// </summary>
		/// <returns>True if the event is a Warning or FatalError, false otherwise.</returns>
		public bool HasWarningOrFatalErrorMessage() {
			return !(message.GetIsInfo());
		}

		/// <summary>
		/// Produces the state name of the ClearVREvent IF it is a state changed event.
		/// 
		/// > Warning: The returned String will be null if the ClearVREventType is NOT a state.
		/// 
		/// </summary>
		/// <returns>The name of the state if the event is a state as a String. Otherwise null.</returns>
		public String GetStateName() {
			String stateName = null;
			switch (_type)
			{
				case ClearVREventTypes.StateChangedUninitialized:
					stateName = "uninitialized";
					break;
				case ClearVREventTypes.StateChangedInitializing:
					stateName = "initializing";
					break;
				case ClearVREventTypes.StateChangedInitialized:
					stateName = "initialized";
					break;
				case ClearVREventTypes.StateChangedPreparingCore:
					stateName = "preparing-core";
					break;
				case ClearVREventTypes.StateChangedCorePrepared:
					stateName = "core-prepared";
					break;
				case ClearVREventTypes.StateChangedPreparingContentForPlayout:
					stateName = "preparing-content-for-playout";
					break;
				case ClearVREventTypes.StateChangedContentPreparedForPlayout:
					stateName = "content-prepared-for-playout";
					break;
				case ClearVREventTypes.StateChangedBuffering:
					stateName = "buffering";
					break;
				case ClearVREventTypes.StateChangedPlaying:
					stateName = "running";
					break;
				case ClearVREventTypes.StateChangedPausing:
					stateName = "pausing";
					break;
				case ClearVREventTypes.StateChangedPaused:
					stateName = "paused";
					break;
				case ClearVREventTypes.StateChangedSeeking:
					stateName = "seeking";
					break;
				case ClearVREventTypes.StateChangedSwitchingContent:
					stateName = "switching-content";
					break;
				case ClearVREventTypes.StateChangedFinished:
					stateName = "finished";
					break;
				case ClearVREventTypes.StateChangedStopping:
					stateName = "stopping";
					break;
				case ClearVREventTypes.StateChangedStopped:
					stateName = "stopped";
					break;
				default:
					break;
			}
			return stateName;
		}

		/// <summary>
		/// Convenience method that allows you to quickly distinguish between a StateChanged event or any other type of event.
		/// </summary>
		/// <returns>True if this is a StateChanged event, false otherwise.</returns>
		[Obsolete("This API has been deprecated and will be removed 2023/09/31. Please use GetStateName() instead & check if the result of this is null or not.", false)]
		public bool GetIsStateChangedEvent() {
			return true ? (_type.ToString().IndexOf("StateChanged") == 0) : false;
		}

		/// <summary>
		/// Convenience method that prints this object as a pretty string to the Unity console as a debug message.
		/// </summary>
		public void Print() {
			UnityEngine.Debug.Log(this.ToString());
		}

		/// <summary>
		/// Convenience method that prints this object as a pretty string to the Unity console as a debug message.
		/// </summary>
		public void PrintShort() {
#if UNITY_2019_1_OR_NEWER
			UnityEngine.Debug.LogFormat(LogType.Log, LogOption.NoStacktrace /* only available on 2019_1_OR_NEWER */, null, this.ToString());
#else
			UnityEngine.Debug.Log(this.ToString());
#endif
		}

		/// <summary>
		/// Returns the fields of this object as a properly formatted string.
		/// </summary>
		/// <returns>A properly formatted string.</returns>
		public override String ToString() {
			return String.Format("Event type: {0}, message type: {1}, message code: {2}, result: {3}, message string: {4}", _type, message.type, message.code, message.result, message.message);
		}
	}

	/// <summary>
	/// Since v9.0
	/// The event as emitted by a [ClearVRDisplayObject](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase).
	/// </summary>
	public class ClearVRDisplayObjectEvent {
		/// <summary>
		/// The event type.
		/// </summary>
		public ClearVRDisplayObjectEventTypes clearVRDisplayObjectEventType {
			get;
			private set;
		}

		/// <summary>
		/// The message payload.
		/// </summary>
		public ClearVRMessage clearVRMessage {
			get;
			private set;
		}
		
		/// <summary>
		/// A convenience alias getter for [clearVRDisplayObjectEventType](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectEvent.clearVRDisplayObjectEventType).
		/// </summary>
		public ClearVRDisplayObjectEventTypes type {
			get {
				return clearVRDisplayObjectEventType;
			}
		}

		/// <summary>
		/// A convenience alias getter for the message that this event holds.
		/// </summary>
		/// <value>The ClearVRMessage</value>
		public ClearVRMessage message {
			get {
				return clearVRMessage;
			}
		}

		internal ClearVRDisplayObjectEvent(ClearVRDisplayObjectEventTypes argClearVRDisplayObjectEventType, ClearVRMessage argClearVRMessage) {
			clearVRDisplayObjectEventType = argClearVRDisplayObjectEventType;
			clearVRMessage = argClearVRMessage;
		}

		internal static ClearVRDisplayObjectEvent GetGenericOKEvent(ClearVRDisplayObjectEventTypes argType) {
			return new ClearVRDisplayObjectEvent(argType, ClearVRMessage.GetGenericOKMessage());
		}

		/// <summary>
		/// Returns the fields of this object as a properly formatted string.
		/// </summary>
		/// <returns>A properly formatted string.</returns>
		public override String ToString() {
			return String.Format("Event type: {0}, message: {1}", clearVRDisplayObjectEventType, message);
		}
	}

	/// <summary>
	/// Each ClearVREvent contains a ClearVRMessage. The ClearVRMessage contains valuable information about the event. Please refer to [ClearVRMessageCodes](xref:com.tiledmedia.clearvr.ClearVRMessageCodes) for details on how to interpret the various codes.
	/// The [code](xref:com.tiledmedia.clearvr.ClearVRMessage.code) field can hold a value of either [](xref:com.tiledmedia.clearvr.ClearVRMessageCodes) or [ClearVRCoreErrorCodes](xref:com.tiledmedia.clearvr.ClearVRCoreErrorCodes)
	/// Use the [GetIsClearVRCoreErrorCode()](xref:com.tiledmedia.clearvr.ClearVRMessage.GetIsClearVRCoreErrorCode) API to figure our whether the code is a ClearVRCoreErrorCode or ClearVRMessageCode.
	/// Please refer to the [GetClearVRMessageCode()](xref:com.tiledmedia.clearvr.ClearVRMessage.GetClearVRMessageCode) API to convert the Integer into its `ClearVRMessageCode` and [GetClearVRCoreErrorCode()](xref:com.tiledmedia.clearvr.ClearVRMessage.GetClearVRCoreErrorCode) API to convert the Integer into its `ClearVRCoreErrorCode` respectively.
	/// </summary>
	public class ClearVRMessage {
		private ClearVRMessageTypes _type;
		private int _code;
		private String _message;
		private ClearVRResult _result;

		/// <summary>
		/// Get the type of this message. The type is either FatalError, Warning or Info.
		/// </summary>
		/// <value>The type of the message.</value>
		public ClearVRMessageTypes type {
			get {
				return _type;
			}
			set {
				_type = value;
			}
		}

		/// <summary>
		/// Check whether the [](xref:com.tiledmedia.clearvr.ClearVRMessage.code) is a ClearVRCoreErrorCode or not.
		/// </summary>
		/// <returns>True if the code holds a ClearVRCoreErrorCode, false if it holds a ClearVRMessageCode.</returns>
		public bool GetIsClearVRCoreErrorCode() {
			return Enum.IsDefined(typeof(ClearVRCoreErrorCodes), _code);
		}

		/// <summary>
		/// Converts [](xref:com.tiledmedia.clearvr.ClearVRMessage.code) into ClearVRCoreErrorCode.
		/// This API will return [](xref:com.tiledmedia.clearvr.ClearVRCoreErrorCodes.UnknownError) in case the code field value cannot be converted. If this were to be the case, the [](xref:com.tiledmedia.clearvr.ClearVRMessage.code) field was actually a [](xref:com.tiledmedia.clearvr.ClearVRMessageCodes) instead.
		/// </summary>
		/// <returns>The ClearVRCoreErrorCode equivalent of the code field value.</returns>
		public ClearVRCoreErrorCodes GetClearVRCoreErrorCode() {
			if (GetIsClearVRCoreErrorCode()) {
				return (ClearVRCoreErrorCodes)_code;
			}
			return ClearVRCoreErrorCodes.UnknownError;
		}

		/// <summary>
		/// Converts [](xref:com.tiledmedia.clearvr.ClearVRMessage.code) into ClearVRMessageCodes.
		/// This API will return [](xref:com.tiledmedia.clearvr.ClearVRMessageCodes.Unknown) in case the code field value cannot be converted. If this were to be the case, the [](xref:com.tiledmedia.clearvr.ClearVRMessage.code) field was actually a [](xref:com.tiledmedia.clearvr.ClearVRCoreErrorCodes) instead.
		/// </summary>
		/// <returns>The ClearVRMessageCodes equivalent of the code field value.</returns>
		public ClearVRMessageCodes GetClearVRMessageCode() {
			if (!GetIsClearVRCoreErrorCode()) {
				return (ClearVRMessageCodes)_code;
			}
			return ClearVRMessageCodes.Unknown;
		}

		/// <summary>
		/// Helper method that returns the code field as the String equivalent from the matching enum.
		/// This can be called on any [](xref:com.tiledmedia.clearvr.ClearVRMessage.code) value.
		/// </summary>
		/// <returns>String representation of the enum that matches the code.</returns>
		public String GetCode() {
			if (GetIsClearVRCoreErrorCode()) {
				return ((ClearVRCoreErrorCodes)_code).ToString();
			}
			return ((ClearVRMessageCodes)_code).ToString();
		}
		/// <summary>
		/// The message code as an integer. The integer can refer to both a [](xref:com.tiledmedia.clearvr.ClearVRCoreErrorCodes) as well as a [](xref:com.tiledmedia.clearvr.ClearVRMessageCodes).
		/// </summary>
		/// <value>The code as an integer.</value>
		public int code {
			get {
				return _code;
			}
			set {
				_code = value;
			}
		}

		/// <summary>
		/// The message as a String, possibly containing additional information about the event this ClearVRMessage was attached to.
		/// Note that this field is typically empty in case of a [](xref:com.tiledmedia.clearvr.ClearVRMessageCodes.ClearVRCoreWrapperGenericOK) message code
		/// </summary>
		/// <value>The message as a string.</value>
		public String message {
			get {
				return _message;
			}
			set {
				_message = value;
			}
		}

		internal ClearVRResult result {
			get {
				return _result;
			}
			set {
				_result = value;
			}
		}

		internal static ClearVRMessage GetGenericOKMessage() {
			return new ClearVRMessage(ClearVRMessageTypes.Info, ClearVRMessageCodes.ClearVRCoreWrapperGenericOK, "", ClearVRResult.Success);
		}

		internal static ClearVRMessage GetGenericWarningMessage(String argOverrideMessage = null) {
			return new ClearVRMessage(ClearVRMessageTypes.Warning, ClearVRMessageCodes.GenericWarning, String.IsNullOrEmpty(argOverrideMessage) ? "An unexpected warning occured." : argOverrideMessage, ClearVRResult.Failure);
		}

		internal static ClearVRMessage GetGenericFatalErrorMessage(String argOverrideMessage = null) {
			return new ClearVRMessage(ClearVRMessageTypes.FatalError, ClearVRMessageCodes.GenericWarning, String.IsNullOrEmpty(argOverrideMessage) ? "An unexpected fatal error occured." : argOverrideMessage, ClearVRResult.Failure);
		}

		internal ClearVRMessage(ClearVRMessageTypes argType, ClearVRMessageCodes argCode, String argMessage, ClearVRResult argClearVRResult) {
			_type = argType;
			_code = (int)argCode;
			_message = argMessage;
			_result = argClearVRResult;
		}

		internal ClearVRMessage(int argType, int argCode, String argMessage, ClearVRResult argClearVRResult) {
			_type = (ClearVRMessageTypes)argType;
			_code = argCode;
			_message = argMessage;
			_result = argClearVRResult;
		}

		internal void Update(ClearVRMessageTypes argType, ClearVRMessageCodes argCode, String argMessage, ClearVRResult argClearVRResult) {
			_type = argType;
			_code = (int)argCode;
			_message = argMessage;
			_result = argClearVRResult;
		}

		internal void Update(int argType, int argCode, String argMessage, ClearVRResult argClearVRResult) {
			_type = (ClearVRMessageTypes)argType;
			_code = argCode;
			_message = argMessage;
			_result = argClearVRResult;
		}

		/// <summary>
		/// Convenience method to determine whether this message contains a FatalError
		/// </summary>
		/// <returns>True if this is a FatalError, false otherwise.</returns>
		public bool GetIsFatalError() {
			return _type == ClearVRMessageTypes.FatalError;
		}

		/// <summary>
		/// Convenience method to determine whether this message contains a Warning
		/// </summary>
		/// <returns>True if this is a Warning, false otherwise</returns>
		public bool GetIsWarning() {
			return _type == ClearVRMessageTypes.Warning;
		}

		/// <summary>
		/// Convenience method to determine whether this message contains an informational message
		/// </summary>
		/// <returns>True if this is an informational messgae, false otherwise</returns>
		public bool GetIsInfo() {
			return _type == ClearVRMessageTypes.Info;
		}

		public bool GetIsSuccess() {
			return (_result == ClearVRResult.Success);
		}

		public String GetFullMessage() {
			return String.Format("Type: {0}, code: {1} ({2}), message: {3}. Result {4}", this._type, this.GetCode(), this._code, this._message.Trim('.'), this._result);
		}

		public override String ToString() {
			return GetFullMessage();
		}
		/// <summary>
		/// Convenience method that prints the message to the Unity console as a debug message.
		/// </summary>
		public void PrintFullMessage() {
			UnityEngine.Debug.Log(this.ToString());
		}

		internal static ClearVRResult ConvertBooleanToClearVRResult(bool argValue) {
			return argValue ? ClearVRResult.Success : ClearVRResult.Failure;
		}

		/// <summary>
		/// If [code](xref:com.tiledmedia.clearvr.ClearVRMessage.code) == [ClearVRCoreWrapperAudioTrackChanged](xref:com.tiledmedia.clearvr.ClearVRMessageCodes.ClearVRCoreWrapperAudioTrackChanged) this will parse the serialized payload in the `message` field.
		/// After successful parsing, the [AudioTrackAndPlaybackParameters](xref:com.tiledmedia.clearvr.AudioTrackAndPlaybackParameters) will be set to a non-null value. In case of failure, it will be set to null.
		/// </summary>
		/// <param name="argAudioTrackAndPlaybackParameters">The parameters of the currently active audio track.</param>
		/// <returns>True if message was successfully parsed, false otherwise.</returns>
		public bool ParseAudioTrackChanged(out AudioTrackAndPlaybackParameters argAudioTrackAndPlaybackParameters) {
			if (GetClearVRMessageCode() == ClearVRMessageCodes.ClearVRCoreWrapperAudioTrackChanged) {
				try {
					var raw = System.Convert.FromBase64String(message);
					argAudioTrackAndPlaybackParameters = AudioTrackAndPlaybackParameters.FromCoreProtobuf(cvri.AudioTrackAndPlaybackParametersMediaFlow.Parser.ParseFrom(raw));
					return true;
				} catch {
					argAudioTrackAndPlaybackParameters = null;
					return false;
				}
			}
			argAudioTrackAndPlaybackParameters = null;
			return false;
		}

		/// <summary>
		/// If [code](xref:com.tiledmedia.clearvr.ClearVRMessage.code) == [ClearVRCoreWrapperSyncStateChanged](xref:com.tiledmedia.clearvr.ClearVRMessageCodes.ClearVRCoreWrapperSyncStateChanged) this will parse the serialized payload in the `message` field.
		/// After successful parsing, the [SyncStateChanged](xref:com.tiledmedia.clearvr.SyncStateChanged) will be set to a non-null value. In case of failure, it will be set to null.
		/// </summary>
		/// <param name="argSyncStatus">The sync status return parameter</param>
		/// <returns>True if message was successfully parsed, false otherwise.</returns>
		public bool ParseSyncStateChanged(out SyncStateChanged argSyncStateChanged) {
			return SyncStateChanged.ParseSyncStatusMessageToSyncStateChanged(this.message, out argSyncStateChanged);
		}

		/// <summary>
		/// Convenience method that parses reported VideoDecoderCapabilities.
		/// </summary>
		/// <param name="argVideoDecoderCapabilities">out argument that will contain the VideoDecoderCapabilities or null if parsing was unsuccessful.</param>
		/// <returns>True if message was successfully parsed, false otherwise.</returns>
		public bool ParseClearVRCoreWrapperVideoDecoderCapabilities(out VideoDecoderCapabilities argVideoDecoderCapabilities) {
			if (GetClearVRMessageCode() == ClearVRMessageCodes.ClearVRCoreWrapperVideoDecoderCapabilities) {
				try {
					var raw = System.Convert.FromBase64String(message);
					argVideoDecoderCapabilities = VideoDecoderCapabilities.FromCoreProtobuf(cvri.VideoDecoderCapabilitiesMediaFlow.Parser.ParseFrom(raw));
					return argVideoDecoderCapabilities != null;
				} catch {
					argVideoDecoderCapabilities = null;
					return false;
				}
			}
			argVideoDecoderCapabilities = null;
			return false;
		}

		/// <summary>
		/// Parses the payload of the ClearVRCoreWrapperABRLevelActivated ClearVRMessage.
		/// </summary>
		/// <param name="argABRLevel">The ABR level</param>
		/// <returns>True if the message could be successfully parsed, false otherwise. The value of the out parameter is undefined if false is returned. If true is returned, it will never be null.</returns>
		[Obsolete("Please use ActiveTracksChanged to be notified about video, audio and subtitle tracks being changed & then use ParseClearVRCoreWrapperActiveTracksChanged() to parse the message.", true)]
		public bool ParseClearVRCoreWrapperABRLevelActivated(out ABRLevel argABRLevel) {
			if (GetClearVRMessageCode() == ClearVRMessageCodes.ClearVRCoreWrapperABRLevelActivated) {
				return ABRLevel.Deserialize(this.message, out argABRLevel);
			}
			argABRLevel = null;
			return false;
		}

		/// <summary>
		/// Parses the payload of the ClearVRCoreWrapperActiveTracksChanged ClearVRMessage.
		/// </summary>
		/// <param name="contentInfo">The ContentInfo object that will hold information about the current ContentItem.</param>
		/// <returns>True if the message could be successfully parsed, false otherwise. The value of the out parameter is undefined if false is returned. If true is returned, it will never be null.</returns>
		public bool ParseClearVRCoreWrapperActiveTracksChanged(out ContentInfo contentInfo) {
			if (GetClearVRMessageCode() == ClearVRMessageCodes.ClearVRCoreWrapperActiveTracksChanged) {
				contentInfo = new ContentInfo(this.message);
				return contentInfo != null;
			}
			contentInfo = null;
			return false;
		}

		/// <summary>
		/// Convenience method that parses a ContentSupportedTesterInternalReport message.
		/// </summary>
		/// <param name="argContentSupportedTesterInternalReport">The out object containing the report. It will be null if the ClearVRMessageCode != ClearVRCoreWrapperTestContentSupportedInternalReport (i.e. in case this message held an error instead of a report)</param>
		internal void ParseContentSupportedTesterInternalReport(out ContentSupportedTesterInternalReport argContentSupportedTesterInternalReport) {
			if (GetClearVRMessageCode() == ClearVRMessageCodes.ClearVRCoreWrapperTestContentSupportedInternalReport) {
				ContentSupportedTesterInternalReport.Deserialize(this.message, out argContentSupportedTesterInternalReport);
			} else {
				argContentSupportedTesterInternalReport = null;
			}
		}

		/// <summary>
		/// Parses the payload of the ClearVRCoreWrapperSubtitle ClearVRMessage.
		/// </summary>
		/// <param name="clearVRSubtitle">The ClearVRSubtitle object that will hold information about the current subtitle.</param>
		/// <returns>True if the message could be successfully parsed, false otherwise. The value of the out parameter is undefined if false is returned. If true is returned, it will never be null.</returns>
		public bool ParseClearVRSubtitle(out ClearVRSubtitle clearVRSubtitle) {
			if (GetClearVRMessageCode() == ClearVRMessageCodes.ClearVRCoreWrapperSubtitle) {
				clearVRSubtitle = new ClearVRSubtitle(this.message);
				return clearVRSubtitle != null;
			}
			clearVRSubtitle = null;
			return false;
		}

	}

	/// <summary>
	/// Helper object that is of little use to the integrator. Allows one to parse the ClearVREventTypes.GenericMessage with ClearVRMessageCodes.ClearVRCoreWrapperVideoDecoderCapabilities.
	/// This cotntains some information about the reported video decoder capabilities. Notably, it can be used to query the video decoder level
	/// </summary>
	public class VideoDecoderCapabilities {
		String _videoDecoderName;
		String _mimetype;
		String _decoderMaximumCodecLevelSupported;
		String _clearVRSdkMaximumCodecLevelSupported;
		String _maximumCodecLevelInUse;
		/// <summary>
		/// Reported video decoder name.
		/// On Android, this will return a String that holds the MediaCodec decoder name, e.g. OMX.dec.hevc
		/// </summary>
		/// <value>The name as a canonical string.</value>
		public String videoDecoderName {
			get {
				return _videoDecoderName;
			}
		}

		/// <summary>
		/// The mimetype of the video that is currently being decoded by this codec.
		/// Examples: video/hevc, video/avc
		/// </summary>
		/// <value>The mimetype</value>
		public String mimetype {
			get {
				return _mimetype;
			}
		}

		/// <summary>
		/// The maximum codec level supported by the decoder, e.g. 5.2
		/// </summary>
		/// <value>The level</value>
		public String decoderMaximumCodecLevelSupported {
			get {
				return _decoderMaximumCodecLevelSupported;
			}
		}

		/// <summary>
		/// The maximum codec level supported by the SDK, e.g. 5.2
		/// > [!NOTE]
		/// > In the past, the Android SDK supported HEVC decoders up to level 5.2. This limitation has since been lifted.
		/// </summary>
		/// <value>The level</value>
		public String clearVRSdkMaximumCodecLevelSupported {
			get {
				return _clearVRSdkMaximumCodecLevelSupported;
			}
		}

		/// <summary>
		/// The codec level in use (= `Math.min(maxSupportedByDecoder, maxSupportedBySDK)`)
		/// </summary>
		/// <value>The level</value>
		public String maximumCodecLevelInUse {
			get {
				return _maximumCodecLevelInUse;
			}
		}

		protected VideoDecoderCapabilities(String argVideoDecoderName, String argMimetype, String argDecoderMaximumCodecLevelSupported, String argClearVRSdkMaximumCodecLevelSupported, String argMaximumCodecLevelInUse) {
			_videoDecoderName = argVideoDecoderName;
			_mimetype = argMimetype;
			_decoderMaximumCodecLevelSupported = argDecoderMaximumCodecLevelSupported;
			_clearVRSdkMaximumCodecLevelSupported = argClearVRSdkMaximumCodecLevelSupported;
			_maximumCodecLevelInUse = argMaximumCodecLevelInUse;
		}

		internal static VideoDecoderCapabilities FromCoreProtobuf(cvri.VideoDecoderCapabilitiesMediaFlow coreVideoDecoderCapabilities) {
			if(coreVideoDecoderCapabilities == null) {
				return null;
			}
			return new VideoDecoderCapabilities(coreVideoDecoderCapabilities.DecoderName, 
				coreVideoDecoderCapabilities.Mimetype, 
				coreVideoDecoderCapabilities.MaximumVideoLevelSupportedByDecoder,
				coreVideoDecoderCapabilities.MaximumVideoLevelSupportedBySdk,
				coreVideoDecoderCapabilities.MaximumVideoLevelSupportedByDecoderAndSdk);
		}
	}

	/// <summary>
	/// Internal helper enum, do not use.
	/// </summary>
	internal enum ClearVRResult {
		Unspecified,
		Success,
		Failure
	}


	/// <summary>
	/// A ClearVRMessage is of certain type. See also ClearVRMessage class for details.
	/// </summary>
	public enum ClearVRMessageTypes {
		///<summary>A fatal error is reported. PLayback will halt after receiving a message of this kind.</summary>
		FatalError,
		///<summary>A warning is reported. This might indicate trouble. Please refer to the message's code for details on how to interpret the message.</summary>
		Warning,
		///<summary>Informational message, the payload can be inferred by checking the message's code.</summary>
		Info
	}

	/// <summary>
	/// The ClearVRPlayer object has various modes on how it can handle app pause/unpause and suspend/resume events.
	/// Under typical conditions, one is strongly recommended to use Recommended mode.
	/// </summary>
	public enum ApplicationFocusAndPauseHandlingTypes {
		/// <summary>
		/// Always put player to pause when app looses focus or is paused: drains battery as the player will remain active and resources will not be freed.
		/// > [!WARNING]
		/// > Legacy mode is not available on iOS. It will default to `Recommended` instead on this platform.
		/// </summary>
		Legacy,
		/// <summary>
		/// Player is killed when app is paused (e.g. pushed to background); player is paused when app looses focus (e.g. in case of a pop-up or when the user puts down the headset).
		/// ClearVREventTypes.StateChangedStopped below demonstrates how one could resume playback when the app resumes after it was paused. Note that this behaviour might not suit your requirements, so please check it thoroughly.
		/// </summary>
		Recommended,
		/// <summary>
		/// Handling app focus/pause is left to you as an integrator. Use with care!
		/// </summary>
		Disabled
	}

	/// <summary>
	/// How the loss and gain of audio focus is being handled by the player.
	/// </summary>
	public enum AudioFocusChangedHandlingTypes {
		/// <summary>The video playback will pause on loss of audio focus (ex: receiving a call) and will resume on gaining audio focus again.</summary>
		Recommended
	}

	/// <summary>
	/// Since v9.0
	/// Throughout the lifecycle of a [ClearVRDisplayObject](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase), it emits various events. One can subscribe to these events by using the `AddEventListener` method on [clearVRPlayer.clearVRDisplayObjectEvents](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectEvents).
	/// This enum describes all the events that can be emitted.
	/// </summary>
	public enum ClearVRDisplayObjectEventTypes {
		/// <summary>
		/// This is an internal event and should not be used. It will never be triggered.
		/// </summary>
		None,
		/// <summary>
		/// The RenderMode of the associated DisplayObject has changed. Please refer to [RenderModes](xref:com.tiledmedia.clearvr.RenderModes) for details.
		/// </summary>
		RenderModeChanged,
		/// <summary>
		/// The ContentFormat of the associated DisplayObject has changed. Please refer to [ContentFormat](xref:com.tiledmedia.clearvr.ContentFormat) for details.
		/// </summary>
		ContentFormatChanged,
		/// <summary>
		/// This event is emitted every time the mesh has changed. For example, after the content is loaded and started playing, but also after an ABR event or after a SwitchContent or SetLayout API call has completed.
		/// </summary>
		FirstFrameRendered,
		/// <summary>
		/// Emitted when the associated DisplayObject has changed its Active state. Note that this is NOT the same as the gameObject.activeSelf property.
		/// </summary>
		ActiveStateChanged,
		/// <summary>
		/// Emitted when the [ClearVRDisplayObjectClassType](xref:com.tiledmedia.clearvr.DisplayObjectClassTypes) on the associated DisplayObject has changed.
		/// </summary>
		ClassTypeChanged,
		/// <summary>
		/// Emitted when the associated DisplayObject receive subtitle information.
		/// </summary>
		Subtitle
	}

	/// <summary>
	/// Throughout the life-cycle of a ClearVRPlayer object, several events are generated. Attach a listener using `clearVRPlayer.clearVREvents.AddListener(CbClearVREvent);` to be informed of these events in your own class.
	/// The signature of this event handler is: `void CbClearVREvent(ClearVRPlayer argClearVRPlayer, ClearVREvent argClearVREvent)`
	/// </summary>
	public enum ClearVREventTypes {
		/// <summary>
		/// Used internally, do not use
		/// </summary>
		None,
		/// <summary>
		/// Initial state of the player
		/// </summary>
		StateChangedUninitialized,
		/// <summary>
		/// Basic initialization of the ClearVRPlayer, do not interfer.
		/// </summary>
		StateChangedInitializing,
		/// <summary>
		/// Transient state while initialization the ClearVRPlayer. Ready to load content.
		/// </summary>
		StateChangedInitialized,
		/// <summary>
		/// Transient state while loading content. The next state will be StateChangedCorePrepared
		/// </summary>
		StateChangedPreparingCore,
		/// <summary>
		/// By now, the ClearVRPlayer is ready to load content.
		/// </summary>
		StateChangedCorePrepared,
		/// <summary>
		/// The ClearVRPlayer is loading the selected clip.
		/// </summary>
		StateChangedPreparingContentForPlayout,
		/// <summary>
		/// The content is buffered and prepared for playout.
		/// </summary>
		StateChangedContentPreparedForPlayout,
		/// <summary>
		/// The player's internal buffers have depleted and buffering is required to resume playback.
		/// </summary>
		StateChangedBuffering,
		/// <summary>
		/// In this state, the player stack is rendering audio and video.
		/// </summary>
		StateChangedPlaying,
		/// <summary>
		/// Transient state while playback is pausing. It can take a couple of frames before playback has paused.
		/// </summary>
		StateChangedPausing,
		/// <summary>
		/// Video playback has paused.
		/// </summary>
		StateChangedPaused,
		/// <summary>
		/// Transient state while seeking.
		/// </summary>
		StateChangedSeeking,
		/// <summary>
		/// Transient state while switching content. When done, you will receive the ContentSwitched event. Playback has only commenced when you have received the StateChangedPlaying event.
		/// </summary>
		StateChangedSwitchingContent,
		/// <summary>
		/// The current clip has finished playback (e.g. the end of the clip was reached). In this state, no new frames are generated until you seek to a new position in the content.
		/// </summary>
		StateChangedFinished, /* end of clip reached */
		/// <summary>
		/// The ClearVRPlayer is in the process of destruction. It is essential for this to complete, so wait for StateChangedStopped until you Destroy() the ClearVRPlayer object!
		/// </summary>
		StateChangedStopping,
		/// <summary>
		/// The ClearVRPlayer object is completely cleaned-up, has released all its resources and is ready for destruction.
		/// You will receive no further events after this state change.
		/// </summary>
		StateChangedStopped,
		/// <summary>
		/// Transient state, the ClearVRPlayer is parsing the media info of the selected clip.
		/// </summary>
		ParsingMediaInfo,
		/// <summary>
		/// The media info of the selected clip has been parsed and you can now query its parameters.
		/// </summary>
		MediaInfoParsed,
		/// <summary>
		/// This event is triggered when the way the video is being displayed to the user has changed. This is specifically applicable to stereoscopic content playback.
		/// > [!WARNING]
		/// > Since v9.x, this event is not emitted anymore, except when the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) is running in legacy mode for backwards compatibility.
		/// > This event is now available per [DisplayObject](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase). Refer to [ClearVRDisplayObjectEvent](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectEvent) for more information.
		/// </summary>
		[Obsolete("This enum case is deprecated and will be removed after 2023-01-01. It has been replaced by ClearVRDisplayObjectEventTypes.RenderModeChanged. It will only fire in backwards compatibility mode. See the inline documentation for important (upgrade) details.", false)]
		RenderModeChanged,
		/// <summary>
		/// The first frame of the video has been rendered, or the first frame of the *new* video after SwitchContent() has been rendered.
		/// Note that by the time you receive this callback, the first frame is actually already rendered. Any action taken will only be effectuated when rendering the *second* video frame.
		/// > [!WARNING]
		/// > Since v9.x, this event is not emitted anymore, except when the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) is running in legacy mode for backwards compatibility.
		/// > This event is now available per [DisplayObject](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase). Refer to [ClearVRDisplayObjectEvent](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectEvent) for more information.
		/// </summary>
		[Obsolete("This enum case is deprecated and will be removed after 2023-01-01. It has been replaced by ClearVRDisplayObjectEventTypes.FirstFrameRendered. It will only fire in backwards compatibility mode. See the inline documentation for important (upgrade) details.", false)]
		FirstFrameRendered,
		/// <summary>
		/// The Audio track has changed. You can use clearVREvent.message.ParseAudioTrackChanged() API to figure out which audio track index is currently selected.
		/// </summary>
		AudioTrackSwitched,
		/// <summary>
		/// A SwitchContent() request has been completed.
		/// </summary>
		ContentSwitched,
		/// <summary>
		/// Triggered when the ClearVRPlayer object was unable to initialize. Typically the result of a faulty URL or missing license file data.
		/// </summary>
		UnableToInitializePlayer,
		
		// Note: you cannot xref obsolete members as we filter them out in our documentation generator.

		/// <summary>
		/// Added in v9.0
		/// Renamed in v9.1
		/// This event has been renamed to SuspendingPlaybackAfterApplicationLostFocus in v9.1.
		/// </summary>
		[Obsolete("This event has been renamed to SuspendingPlaybackAfterApplicationLostFocus.", true)]
		SuspendingPlaybackBeforeApplicationPaused,
		/// <summary>
		/// Renamed in v9.1.
		/// This event has been renamed to [ResumingPlaybackAfterApplicationRegainedFocus](xref:com.tiledmedia.clearvr.ClearVREventTypes.ResumingPlaybackAfterApplicationRegainedFocus) in v9.1.
		/// </summary>
		[Obsolete("This event has been renamed to ResumingPlaybackAfterApplicationRegainedFocus.", true)]
		ResumingPlaybackAfterApplicationPaused,

		/// <summary>
		/// Since v9.1 (was known as ResumingPlaybackAfterApplicationPaused in older versions).
		/// 
		/// Triggered when the player is resuming after the application regained focus again after it was has previously lost focus (e.g. because it was pushed to the background).
		/// [SuspendingPlaybackAfterApplicationLostFocus](xref:com.tiledmedia.clearvr.ClearVREventTypes.SuspendingPlaybackAfterApplicationLostFocus) is triggered when the application has lost focus.
		/// > [!NOTE]
		/// > This event is only emitted when [platformOptions.applicationFocusAndPauseHandling](xref:com.tiledmedia.clearvr.PlatformOptionsBase.applicationFocusAndPauseHandling) is set to its default value [Recommended](xref:com.tiledmedia.clearvr.ApplicationFocusAndPauseHandlingTypes.Recommended).
		/// </summary>
		ResumingPlaybackAfterApplicationRegainedFocus,
		/// <summary>
		/// Since v9.1
		/// Triggered when the player is suspending after the application has lost focus (e.g. was pushed to the background). 
		/// [ResumingPlaybackAfterApplicationRegainedFocus](xref:com.tiledmedia.clearvr.ClearVREventTypes.ResumingPlaybackAfterApplicationRegainedFocus) is triggered after the application has regained focus again.
		/// > [!NOTE]
		/// > This event is only emitted when [platformOptions.applicationFocusAndPauseHandling](xref:com.tiledmedia.clearvr.PlatformOptionsBase.applicationFocusAndPauseHandling) is set to its default value [Recommended](xref:com.tiledmedia.clearvr.ApplicationFocusAndPauseHandlingTypes.Recommended).
		/// </summary>
		SuspendingPlaybackAfterApplicationLostFocus,
		/// <summary>
		/// This event is triggered when there is a change in which eye(s) of the video are retrieved. For example, the video for the right eye is no longer fetched due to poor network conditions or, vice-versa, the right eye video is being fetched because of improved network conditions.
		/// </summary>
		StereoModeSwitched,
		/// <summary>
		/// Since v4.0
		/// The format of the content has changed. This event is triggered after loading the first clip and when the content format changed after a SwitchContent() API call or an ABRLevelChanged event (for example if there are stereoscopic and monoscopic renditions available in an HLS ladder).
		/// You can use the `clearVRPlayer.mediaInfo.GetContentFormat()` to know what the new ContentFormat is.
		/// > [!WARNING]
		/// > Since v9.x, this event is not emitted anymore, except when the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) is running in legacy mode for backwards compatibility.
		/// > This event is now available per [DisplayObject](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase). Refer to [ClearVRDisplayObjectEvent](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectEvent) for more information.
		/// </summary>
		[Obsolete("This enum case is deprecated and will be removed after 2023-01-01. It has been replaced by ClearVRDisplayObjectEventTypes.ContentFormatChanged. It will only fire in backwards compatibility mode. See the inline documentation for important (upgrade) details.", false)]
		ContentFormatChanged,
		/// <summary>
		/// The adaptive bitrate mechanism has trigged a switch between representation (quality) layers available in this clip.
		/// Since v8.0, this has been renamed to ABRLevelActivated
		/// </summary>
		[Obsolete("This event has been renamed to ABRLevelActivated.", true)]
		ABRSwitch,
		/// <summary>
		/// The adaptive bitrate mechanism has trigged a switch between representation (quality) layers available in this clip.
		/// </summary>
		[Obsolete("This event has been obseleted. Please use ActiveTracksChanged to be notified about video, audio and subtitle tracks being changed.", true)]
		ABRLevelActivated,
		/// <summary>
		/// A video, audio or subtitle track has switched to a different one.
		/// </summary>
		ActiveTracksChanged,
		/// <summary>
		/// The app has lost or gained audio focus. Ex: a call is coming in.
		/// </summary>
		AudioFocusChanged,
		/// <summary>
		/// Cache prewarm has completed.
		/// </summary>
		[Obsolete("Since cache prewarming is obsolete, this event is obsolete as well. You can safely remove it from your code", true)]
		PrewarmCacheCompleted,
		/// <summary>
		/// Debug call core has completed.
		/// </summary>
		CallCoreCompleted,
		/// <summary>
		/// Set feed layout call has completed.
		/// </summary>
		SetLayoutCompleted,
		/// <summary>
		/// A generic message can contain information information, a warning or a fatal error. You can check the embedded ClearVRMessage for details.
		/// </summary>
		GenericMessage
	}


	/// <summary>
	/// This enum specifies how the video is *rendered* to the display.
	/// </summary>
	public enum RenderModes {
		/// <summary>
		/// Rendering will follow the content's format. This is the default behaviour.
		/// <list type="bullet">
		/// <item>
		/// <term></term>
		/// <description>Stereoscopic clips will be rendered as stereoscopic on headsets and monoscopic on non-HMD devices.</description>
		/// </item>
		/// <item>
		/// <term></term>
		/// <description>Monoscopic clips will be rendered as monoscopic on all devices.</description>
		/// </item>
		/// </list>
		/// </summary>
		Native,
		///<summary>
		/// Rendering will be forced to monoscopic:
		/// <list type="bullet">
		/// <item>
		/// <term></term>
		/// <description>Stereoscopic clips will be rendered as monoscopic on all devices.</description>
		/// </item>
		/// <item>
		/// <term></term>
		/// <description>Monoscopic clips will be rendered as monoscopic on all devices.</description>
		/// </item>
		/// </list>
		/// > [!WARNING]
		/// > The RenderMode will reset to the behaviour as described under `Native` when calling SwitchContent()! 
		/// > This means that if you used the [SetRenderMode()](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerInterface.SetRenderMode(com.tiledmedia.clearvr.RenderModes)) API to switch to monoscopic rendering during playback, rendering will switch to Stereoscopic again when switching to a stereoscopic clip.
		/// > If you want to enforce monoscopic rendering even after a SwitchContent, refer to [ForcedMonoscopic](xref:com.tiledmedia.clearvr.RenderModes.ForcedMonoscopic) instead.
		/// </summary>
		Monoscopic,
		/// <summary>
		/// Rendering will be forced to Stereoscopic on HMD if the current playing content is stereoscopic. This has no effect on non-HMD devices.
		/// > [!NOTE]
		/// > The RenderMode will reset to the behaviour as described under `Native` when calling SwitchContent()! 
		/// </summary>
		Stereoscopic,
		///<summary>
		/// Since v8.1
		/// Force monoscopic playback, even if content is stereoscopic.
		/// When set to this mode, content playback will always stick to monoscopic whatever the clip you SwitchContent() to.
		/// </summary>
		// Note: ForcedMonoscopic may NEVER be set internally!
		ForcedMonoscopic
	}

	/// <summary>
	/// Every ClearVREvent that you receive contains a ClearVRMessage. Each ClearVRMessage holds a code, that can either be:
	/// 1. a number from the ClearVRMessageCodes enum
	/// 2. a number from the ClearVRCoreErrorCodes enum
	/// This enum describes the ClearVRMessageCodes.
	/// </summary>
	public enum ClearVRMessageCodes {
		///<summary>An unknown message code. You will never receive this code.</summary>
		Unknown = 0,
		/* Message codes reserved for Unity */
		///<summary>Generic OK. This is always an informational message that might hold more information in the ClearVRMessage's message field (it can, however, be empty).</summary>
		[Obsolete("The GenericOK field has been deprecated and can no longer be used. Please use ClearVRCoreWrapperGenericOK instead.", true)] // Marked as no longer usable on 2020-06-01
		GenericOK = -2000,
		///<summary>A generic warning. The message body will contain details about the warning.</summary>
		GenericWarning = -2001,
		///<summary>A generic fatal error. This indicates an unexpected fatal error has happened for which no dedicated ClearVRMessageCode has been defined. As this is fatla, playback will be terminated automatically.</summary>
		GenericFatalError = -2002,
		///<summary>Unable to set requested render mode. This can happen if you try to force stereoscopic rendering on a monoscopic clip, or if no headset is present.</summary>
		SetRenderModeFailed = -2003,
		///<summary>The API is not implemented on this current platform.</summary>
		APINotSupportedOnThisPlatform = -2004,
		///<summary>The API is deprecated and cannot be used anymore. Please upgrade, check the docs for details.</summary>
		APIObsolete = -2005,

		/* ClearVRCoreWrapper related errors */

		/* -1000 .. -1099 are fatal ERROR messages */
		// -1000 - -1009 reserved for ClearVRCore initialization errors
		///<summary>Fatal. An unspecified error occurred in the platform specific MediaFlow library. Unable to continue.</summary>
		ClearVRCoreWrapperUnspecifiedFatalError = -1000,
		///<summary>Fatal. The core library is already initialized. This typically indicates an application logic problem (e.g. trying to initialize a ClearVRPlayer object twice)</summary>
		ClearVRCoreWrapperAlreadyInitialized = -1001,
		///<summary>Fatal. A fatal exception occurred in the ClearVRCore library.</summary>
		ClearVRCoreWrapperFatalException = -1002, // Note to self: aka FatalExceptionFromClearVRCore
		///<summary>Fatal. Unable to correctly initialize libraries.</summary>
		ClearVRCoreWrapperNotProperlyInitialized = -1003,
		///<summary>Fatal. Unable to parse media info, cannot access host? See message for details.</summary>
		ClearVRCoreWrapperUnableToParseMediaInfo = -1004,
		///<summary>Fatal. Unable to generate anonymous device app id.</summary>
		ClearVRCoreWrapperCannotGenerateDeviceAppId = -1005,
		///<summary>Fatal. A timeout was triggered while loading the content. Network connection issues?</summary>
		ClearVRCoreWrapperContentLoadingTimeout = -1006,
		///<summary>Fatal. Playout was unable to succesfully initialize. Device not supported? See message for details.</summary>
		ClearVRCoreWrapperInitializationTimeout = -1007,
		///<summary>Fatal. The provided proxy settings are either incomplete, invalid or not support.</summary>
		ClearVRCoreWrapperInvalidOrUnsupportedProxySettings = -1008,
		///<summary>This device is not supported. Typically, this means that it has been blacklisted. Details on why the device is not supported can be found in the attached ClearVRMessage (String) message field.</summary>
		ClearVRCoreWrapperDeviceNotSupported = -1009,

		// 1010 - 1019 reserved for video decoder related errors
		///<summary>Fatal. Unable to initialize video decoder. Mimetype not supported or decoder limitation reached?</summary>
		ClearVRCoreWrapperVideoDecoderNotInitialized = -1010,
		///<summary>Fatal. The video dcoder threw a fatal error during runtime.</summary>
		ClearVRCoreWrapperVideoDecoderDecodingFailure = -1011,
		///<summary>Fatal. No hardware video decoder found. On mobile platforms, hardware decoder support is mandatory for HEVC and AVC playback.</summary>
		ClearVRCoreWrapperNoHardwareVideoDecoderAvailable = -1012,
		///<summary>Fatal. The video decoder was unable to decode a frame.</summary>
		ClearVRCoreWrapperCannotDecodeFrame = -1013,
		///<summary>Fatal. The video decoder does not support decoding the requested mimetype/profile/level combination. For example, it reports support for decoding up to 1080p while the content is 2160p</summary>
		ClearVRCoreWrapperVideoDecoderDoesNotSupportProfileOrLevel = -1014,
		///<summary>Fatal. You tried to play back a video mimetype that is not supported by this SDK.</summary>
		ClearVRCoreWrapperVideoMimetypeNotSupportBySDK = -1015,
		///<summary>Fatal. Unable to configure the video decoder.</summary>
		ClearVRCoreWrapperVideoDecoderCannotConfigureDecoder = -1016,

		/* Audio decoder related fatal errors */
		///<summary>Fatal. Audio decoder fatiled to initialize.</summary>
		ClearVRCoreWrapperAudioDecoderNotInitialized = -1020,
		///<summary>Fatal. The aduioo decoder threw a generic error during runtime.</summary>
		ClearVRCoreWrapperAudioDecoderDecodingFailure = -1021,
		///<summary>Fatal. Audio decoder was unable to decoder audio sample.</summary>
		ClearVRCoreWrapperAudioDecoderCannotDecodeSample = -1022,
		///<summary>Fatal. No audio decoder was found that supports the requested format.</summary>
		ClearVRCoreWrapperAudioDecoderFormatNotSupported = -1023,

		/* -1050 .. -1059 DRM related messages */
		///<summary>Fatal. A generic DRM-related error occurred.</summary>
		ClearVRCoreWrapperVideoDRMGenericError = -1050,
		///<summary>Fatal. The selected video DRM scheme is not supported by the platform.</summary>
		ClearVRCoreWrapperVideoDRMSchemeUnsupported = -1051,
		///<summary>Fatal. The DRM license server denied this DRM session. Not licensed?</summary>
		ClearVRCoreWrapperVideoDRMSessionDeniedByServer = -1052,
		///<summary>Fatal. Unable to provision DRM session. Cannot play protected content. Required protention level not supported?</summary>
		ClearVRCoreWrapperVideoDRMUnableToProvision = -1053,

		/* 1080 - 1099 reserved for generic errors */

		/* -1100 .. -1199 are non-fatal WARNING _messages */
		///<summary>Warning. An unexpected, unspecified warning was received. Please check the message payload for details.</summary>
		ClearVRCoreWrapperUnspecifiedWarning = -1100,
		///<summary>Warning. A non-fatal exception (warning) was thrown by the ClearVRCore library.</summary>
		ClearVRCoreWrapperNonFatalClearVRCoreException = -1101,
		///<summary>Warning. Spatial audio track selected, but not supported by the current platform.</summary>
		ClearVRCoreWrapperSpatialAudioNotSupported = -1102,
		///<summary>Warning. The video decoder input is overflowing. This indicates that video frames are produced faster than the video decoder can consume.  This can result in a lagging view-port. See also ClearVRMessageCodes.ClearVRCoreWrapperVideoDecoderTooSlow below. </summary>
		ClearVRCoreWrapperVideoDecoderInputOverflow = -1103,
		///<summary>Warning. The video decoder output is overflowing. This indicates that the application cannot consume video frames fast enough. This typically indicates severe performance issues in the application (e.g. low application framerate). See also ClearVRMessageCodes.ClearVRCoreWrapperVideoDecoderTooSlow below. </summary>
		ClearVRCoreWrapperVideoDecoderOutputOverflow = -1104,
		///<summary>Warning. Attempting to call an API that cannot be serviced in the current state.</summary>
		ClearVRCoreWrapperInvalidState = -1105,
		///<summary>Warning. Attempting to seek in a clip that does not support seeking.</summary>
		ClearVRCoreWrapperContentDoesNotSupportSeek = -1106,
		///<summary>Warning. Unable to switch audio track.</summary>
		ClearVRCoreWrapperCannotSwitchAudioTrack = -1107,
		///<summary>Warning. Unable to switch content.</summary>
		ClearVRCoreWrapperCannotSwitchContent = -1108,
		///<summary>Warning. The video decoder dropped a frame, this might indicate corrupt video data.</summary>
		ClearVRCoreWrapperVideoDecoderFrameDropped = -1109,
		///<summary>Warning. An asynchronous request has been canceled.</summary>
		ClearVRCoreWrapperRequestCancelled = -1110,
		/// <summary>
		/// A message with this code indicates that the video decoder cannot keep up with the video framerate. For example, you are trying to play back a 60 fps clip on a device that is only rated for 30 fps at the given resolution.
		/// As it is virtually impossible to know what a decoder is truely capable off a priori (especially on the Android ecosystem), and as the video resolution we feed into the decoder varies from clip to clip, we will try to
		/// playback video as good as possible. If the decoder struggles or is failing miserably to keep up, you will receive this warning message and it is up to you to decide what to do with it.
		///
		/// Note that this is a "simplified" version of the "ClearVRCoreWrapperVideoDecoderInputOverflow" message which gives you a slightly more detailed message (its message value contains how much the decoder input queue overflowed during the last 10 seconds).
		/// </summary>
		ClearVRCoreWrapperVideoDecoderTooSlow = -1111,
		/// <summary>
		/// There is the possibility that the video decoder threw an error during configure-state. We will retry this a couple of times (in varying configurations) before we give up.
		/// Note that when you receive this message, you should treat it as a warning. There is still the chance that the configure will be succesful at the next attempt.
		/// </summary>
		ClearVRCoreWrapperVideoDecoderConfigureDecoderThrewWarning = -1112,
		/// <summary>
		/// There is the remote possibility that a decoded frame has no metadata associated with it. In that case, the video frame must be dropped.
		/// Notes:
		/// 1. It is extremely unlikely that this event is triggered. Please contact us if you see this event passing by.
		/// </summary>
		ClearVRCoreWrapperVideoDecoderFrameWithoutMetadataDropped = -1113,
		/// <summary>
		/// Priming the video decoder took unexpectedly long. Performance issues might arrise (but not necessarily)
		/// Notes:
		/// 1. It is extremely unlikely that this event is triggered. Please contact us if you see this event passing by.
		/// </summary>
		ClearVRCoreWrapperVideoDecoderSlowPriming = -1114,
		/// <summary>
		/// The player was asked (by the application) or forced (because of an error) to stop while it was preparing playback for the first clip.
		/// Since: v7.3.4
		/// </summary>
		ClearVRCoreWrapperPrepareContentForPlayoutCancelled = -1115,
		/* -1200 .. -1299 are INFO messages */
		///<summary>Info. Generic OK message. You will receive this message on a response to a request that was serviced successfully. The ClearVRMessage's message field will typically be empty.</summary>
		ClearVRCoreWrapperGenericOK = -1200,
		///<summary>Info. Generic informational message.</summary>
		ClearVRCoreWrapperGenericInfo = -1201,
		///<summary>Info. Legacy message code that should be ignored. Contains the reported OpenGL version formatted as 2.x or 3.x on Android only.</summary>
		ClearVRCorWrapperOpenGLVersionInfo = -1202,
		///<summary>Info. VIdeo decoder capabilities reported. Refer to ClearVRmessage.ParseClearVRCoreWrapperVideoDecoderCapabilities() for details on how to parse the ClearVRMessage's message.</summary>
		ClearVRCoreWrapperVideoDecoderCapabilities = -1203,
		///<summary>Info. Audio track changed successfully. Refer to ClearVRMessage.ParseAudioTrackChanged() for details on how to parse the ClearVRMessage's message field.</summary>
		ClearVRCoreWrapperAudioTrackChanged = -1204,
		///<summary>Info. Stereoscopic mode changed. This refer to how the video is retrieved. Remember that ClearVR content allows one to only retrieve the left eye on stereoscopic content, significantly reducing bandwidth usage when trying to playback stereoscopic content on devices that do not support stereoscopic rendering.  This is NOT the same as the RenderMode.</summary>
		ClearVRCoreWrapperStereoscopicModeChanged = -1205,
		///<summary>Info. Video DRM sessions was established successfully.</summary>
		ClearVRCoreWrapperVideoDRMSessionEstablished = -1206,
		///<summary>Info. An ABR level has been activated. The payload can be parsed by calling clearVRMessage.ParseClearVRCoreWrapperABRLevelActivated().</summary>
		[Obsolete("The ClearVRCoreWrapperABRLevelActivated field has been deprecated and can no longer be used. Please use ClearVRCoreWrapperActiveTracksChanged instead.", true)]
		ClearVRCoreWrapperABRLevelActivated = -1207,
		///<summary>Info. The sync state changed.</summary>
		ClearVRCoreWrapperSyncStateChanged = -1208,
		///<summary>
		/// Info.
		/// This event indicates that the active audio/video/subtitle track has changed.
		/// This event will be triggered after content playback has started, for both the first clip and after each SwitchContent.
		/// Furthermore, this is triggered when an ABR event happened.
		/// The payload can be parsed by calling `bool clearVRMessage.ParseClearVRCoreWrapperActiveTracksChanged(out ContentInfo).`
		///</summary>
		ClearVRCoreWrapperActiveTracksChanged = -1209,
		///<Summary>
		/// Info.
		/// This event indicates that the audio focus has been regained. 
		/// See [ClearVRCoreWrapperAudioFocusLost](xref:com.tiledmedia.clearvr.ClearVRCoreWrapperAudioFocusLost).
		/// </Summary>
		ClearVRCoreWrapperAudioFocusGained = -1210,
		///<Summary>
		/// Info.
		/// This event indicates that the audio focus has been lost.
		/// This can happen when the phone gets a call or another app hijacks the audio.
		/// </Summary>
		ClearVRCoreWrapperAudioFocusLost = -1211,
		ClearVRCoreWrapperSubtitle = -1212,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperCallApp = -1299,
		/* -1300 .. -1399 are state change messages */
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateUninitialized = -1300,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateInitializing = -1301,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateInitialized = -1302,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateRunning = -1303,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStatePausing = -1304,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStatePaused = -1305,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateBuffering = -1306,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateSeeking = -1307,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateSwitchingContent = -1308,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateFinished = -1309,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperClearVRCoreStateStopped = -1310,
		///<summary>Internal. You will never receive this message code.</summary>
		ClearVRCoreWrapperTestContentSupportedInternalReport = -1400,


	}

	/// <summary>
	/// This is an internal enum and not for public use.
	/// </summary>
	internal enum RequestTypes {
		/* A list of requests types that can be performed on the ClearVRCoreWrapper */
		Unknown = 0,
		Initialize = 1,
		ParseMediaInfo = 2,
		PrepareContentForPlayout = 3,
		Start = 4,
		Pause = 5,
		Unpause = 6,
		Seek = 7,
		Stop = 8,
		SwitchAudioTrack = 9,
		SwitchContent = 10,
		ChangeStereoMode = 11,
		// PrewarmCache = 12, // Cache prewarming has been removed from the SDK. Do not re-use 12 in the future.
		CallCore = 13,
		ContentSupportedTest = 14,
		CallCoreStatic = 15,
		ClearVRPlayerInitialize = 999 /* This is a magic RequestType that codes for a request to initialize a (Unity) ClearVRPlayer class */
	}

	/// <summary>
	/// Flags specific to initializing the ClearVRPlayer.
	/// </summary>
	public enum InitializeFlags {
		/// <summary>
		/// Default, no flags raised
		/// </summary>
		None = 0x0000000000000000,
		/// <summary>
		/// Disable cache prewarming, reduces initialization time.
		/// </summary>
		NoCachePrewarming = 0x0000000000000001,
		/// <summary>
		/// Force long buffer for more stable playout (not recommended).
		/// </summary>
		[Obsolete("The LongBuffer flag has been removed and is no longer supported.", true)] // Marked as no longer usable on 2020-06-01
		LongBuffer = 0x0000000000000002
	}

	/// <summary>
	/// Flags related to prewarming cache. See ClearVRPlayer.PrewarmCache() for details.
	/// </summary>
	[Obsolete("Prewarm cache API is obsolete and no longer exists. Please remove any reference to it from your code.", true)]
	public enum PrewarmCacheFlags {
		/// <summary>
		/// Default, no flags raised.
		/// </summary>
		None = 0x0000000000000000,
	}

	/// <summary>
	/// Allows one to specify how the new content position in a Seek() or SwitchContent() call (specified in milliseconds) should be interpreted.
	/// </summary>
	[Obsolete("SeekFlags have been replaced by TimingTypes.", true)]
	public enum SeekFlags {
		///<summary>
		/// Default, no flags raised.
		///</summary>
		[Obsolete("SeekFlags.None has been replaced by TimingTypes.None.", true)] // Marked as no longer usable on 2020-12-01
		None = 0x0000000000000000,
		///<summary>
		/// When specified, seek to live edge of the currently playing ContentItem. This flag is only valid when playing Live content.
		///</summary>
		[Obsolete("SeekFlags.LiveEdge has been replaced by TimingTypes.LiveEdge.", true)] // Marked as no longer usable on 2020-12-01
		LiveEdge = 0x0000000000000004,
		/// <summary>
		/// When specified, seek to specified wallclock time. Normally, one would seek based on content time. This flag is typically used when playing back synchronized live broadcast with multiple cameras.
		/// </summary>
		[Obsolete("SeekFlags.WallclockTime has been replaced by TimingTypes.WallclockTime.", true)] // Marked as no longer usable on 2020-12-01
		WallclockTime = 0x0000000000000008,
		/// <summary>
		/// Use the RelativeTime flag to seek based on the current content position. This saves you from grabbing the current content position and adding the required offset yourself.
		/// </summary>
		[Obsolete("SeekFlags.RelativeTime has been replaced by TimingTypes.RelativeTime.", true)] // Marked as no longer usable on 2020-12-01
		RelativeTime = 0x0000000000000010,
		/// <summary>
		/// Use the Seamless flag to seamlessly switch between two content items. This is typically used when switching between cameras in a synchronized multi-camera event. This does not apply to seeking, only switch content.
		/// </summary>
		[Obsolete("SeekFlags.Seamless has been replaced by TimingTypes.Seamless.", true)] // Marked as no longer usable on 2020-12-01
		Seamless = 0x0000000000000020
	}

	/// <summary>
	/// Allows one to specify how the new content position in TimingParameters() should be interpreted.
	/// </summary>
	public enum TimingTypes : Int32 {
		///<summary>
		/// Default, interpret time as a content time.
		///</summary>
		[Obsolete("TimingTypes.None has been renamed to TimingTypes.ContentTime.", true)] // This is added to guide customers in their upgrade from SeekFlags to the new TimingType enum.
		None = 0,
		///<summary>
		/// Default, interpret time as a content time.
		///</summary>
		ContentTime = (Int32) cvrinterface.TimingType.ContentTime,
		///<summary>
		/// Only applicable to Live content. Indicates that the actual start position will be ignored and playback will start at the live edge.
		///</summary>
		LiveEdge = (Int32) cvrinterface.TimingType.LiveEdge,
		/// <summary>
		/// Interpret the specified time position as Wallclock time. This is applicable to live broadcast streams.
		/// </summary>
		WallclockTime = (Int32) cvrinterface.TimingType.WallclockTime,
		/// <summary>
		/// Use RelativeTime to seek based on the current content position. This saves you from grabbing the current content position and adding the required offset yourself.
		/// </summary>
		RelativeTime = (Int32) cvrinterface.TimingType.RelativeTime,
		/// <summary>
		/// Use Seamless to seamlessly switch between two content items. This is typically used when switching between cameras in a synchronized multi-camera event. This does not apply to seeking, only switch content.
		/// </summary>
		Seamless = (Int32) cvrinterface.TimingType.Seamless,
		/// <summary>
		/// Interpret the specified time position as the start time of a scheduled on demand content item. This applies to PrepareContentParameters and SwitchContentParameters, but cannot be used in conjunction with SeekParameters.
		/// Refer to [ClearVRPlayer](~/readme/clearvrplayer.md) section `Switching cameras / content` for more details and an example on playing a VOD asset as ScheduledOnDemand.
		/// </summary>
		ScheduledOnDemand = (Int32) cvrinterface.TimingType.ScheduledOnDemand
	}

	static class TimingTypesMethods {
		/// <summary>Return a protobuf representation of the timing type</summary>
		public static cvrinterface.TimingType ToCoreProtobuf(this TimingTypes argTimingType) {
			switch (argTimingType) {
				case TimingTypes.ContentTime:
					return cvrinterface.TimingType.ContentTime;
				case TimingTypes.LiveEdge:
					return cvrinterface.TimingType.LiveEdge;
				case TimingTypes.WallclockTime:
					return cvrinterface.TimingType.WallclockTime;
				case TimingTypes.RelativeTime:
					return cvrinterface.TimingType.RelativeTime;
				case TimingTypes.Seamless:
					return cvrinterface.TimingType.Seamless;
				case TimingTypes.ScheduledOnDemand:
					return cvrinterface.TimingType.ScheduledOnDemand;
				default:
					UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to convert TimingTypes: {0} to cvrinterface.TimingType equivalent. Please report this issue to Tiledmedia.", argTimingType));
					break;
			}
			// We default to content time just in case.
			return cvrinterface.TimingType.ContentTime;
		}

		/// <summary>
		/// Convert core protobuf TimingType to SDK equivalent.
		/// </summary>
		/// <param name="coreTimingType"></param>
		/// <returns>TimingTypes</returns>
		public static TimingTypes FromCoreProtobuf(cvri.TimingType coreTimingType) {
			switch (coreTimingType) {
				case cvri.TimingType.ContentTime:
					return TimingTypes.ContentTime;
				case cvri.TimingType.WallclockTime:
					return TimingTypes.WallclockTime;
				case cvri.TimingType.RelativeTime:
					return TimingTypes.RelativeTime;
				case cvri.TimingType.Seamless:
					return TimingTypes.Seamless;
				case cvri.TimingType.ScheduledOnDemand:
					return TimingTypes.ScheduledOnDemand;
				case cvri.TimingType.LiveEdge:
					return TimingTypes.LiveEdge;
				default:
					UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to convert core TimingType: {0} to SDK TimingTypes equivalent. Please report this issue to Tiledmedia.", coreTimingType));
					break;
			}
			// We default to content time just in case.
			return TimingTypes.ContentTime;
		}
		public static TimingTypes GetAsTimingType(int argValue) {
			if (argValue == 0) {
				return TimingTypes.ContentTime; // Special care is taken to always return ContentTime, not the deprecated None value.
			}
			return (TimingTypes)argValue;
		}
		/// <summary>
		/// Custom ToString() that properly converts TimingTypes.ContentTime to "ContentTime" instead of "None".
		/// </summary>
		/// <param name="argTimingType">The object on which the method is called.</param>
		/// <returns>A formatted string representation of the enum.</returns>
		// Unfortunately, you cannot override String ToString() on an enum :(
		public static String ToString2(this TimingTypes argTimingType) {
			if ((int)argTimingType == 0) {
				return "ContentTime";
			}
			return argTimingType.ToString();
		}
	}
	/// <summary>
	/// When seeking or switching content, one can specify how this should be effectuated.
	/// </summary>
	public enum TransitionTypes {
		/// <summary>
		/// In case of a continuous transition, the current content will keep playing until the transition to the next position or content item has completed (e.g. until the internal buffers have been filled sufficiently to start playback at the new position (and new clip in case of switch content)l.
		/// This has been renamed to Continuous since v8.2.1.
		/// </summary>
		[Obsolete("The `Continous` enum case has been renamed to `Continuous`.", true)]
		/// <summary>
		/// In case of a continuous transition, the current content will keep playing until the transition to the next position or content item has completed (e.g. until the internal buffers have been filled sufficiently to start playback at the new position (and new clip in case of switch content)l.
		/// </summary>
		Continous = -1,
		Continuous = 0,
		/// <summary>
		/// When specifying Fast as transition type, playback will immediately halt and will only continue once the buffers at the requested content position (and in case of switch content in the new content item) have been sufficiently filled.
		/// </summary>
		Fast = 1
	}

	static class TransitionTypesMethods {
		/// <summary>Return a protobuf representation of the transition type</summary>
		public static cvrinterface.TransitionType ToCoreProtobuf(this TransitionTypes argTransitionType) {
			switch (argTransitionType) {
				case TransitionTypes.Continuous:
					return cvrinterface.TransitionType.Smooth;
				case TransitionTypes.Fast:
					return cvrinterface.TransitionType.Fast;
				default:
					UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to convert TransitionTypes: {0} to cvrinterface.TransitionType equivalent. Please report this issue to Tiledmedia.", argTransitionType));
					break;
			}
			// We default to the Fast Transition Type
			return cvrinterface.TransitionType.Fast;
		}
		public static TransitionTypes FromCoreProtobuf(cvrinterface.TransitionType argTransitionTypeCore) {
			switch (argTransitionTypeCore) {
				case cvrinterface.TransitionType.Smooth:
					return TransitionTypes.Continuous;
				case cvrinterface.TransitionType.Fast:
					return TransitionTypes.Fast;
				default:
					UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to convert cvrinterface.TransitionType {0} to the TransitionTypes equivalent. Please report this issue to Tiledmedia.", argTransitionTypeCore));
					break;
			}
			// We default to the Fast Transition Type
			return TransitionTypes.Fast;
		}
	}

	/// <summary>
	/// SwitchContent specific flags.
	/// </summary>
	public enum SwitchContentFlags {
		/// <summary>
		/// Default, no flags raised.
		/// </summary>
		None = 0x0000000000000000,
	}

	/// <summary>
	/// A list of the various supported devices andd headsets.
	/// These are automatically detected. You can use `Utils.GetDeviceType()` to know which device type was detected.
	/// </summary>
	public enum DeviceTypes {
		// This list must be in sync with the list in core/External.go. As the core does not specify an Unknown/default value, we start at -1 in our case.
		Unknown = -001,
		AndroidFlat = 100,
		IOSFlat = 150,
		AppleTV = 160,
		PCFlat = 200,
		AndroidGenericHMD = 1000,
		AndroidGenericCardboard = 1001,
		AndroidGenericDaydream = 1002,
		AndroidMobfishCardboard = 1003,
		AndroidOculusGeneric = 1050,
		AndroidOculusGo = 1051,
		AndroidOculusGearVR = 1052,
		AndroidOculusQuest = 1053,
		AndroidOculusQuest2 = 1054,
		AndroidWaveVRGeneric = 1100,
		AndroidPicoVRGeneric = 1150,
		AndroidSkyworthVRGeneric = 1200,
		AndroidGSXRGeneric = 1250,
		IOSGenericHMD = 2000,
		IOSGenericCardboard = 2001,
		IOSMobfishCardboard = 2002,

		PCGenericHMD = 3000,
		PCOculusGeneric = 3050,
		PCOculusRiftDK1 = 3051,
		PCOculusRiftDK2 = 3052,
		PCOculusRiftCV1 = 3053,
		PCOculusRiftS = 3054,
		PCOculusLinkQuest = 3055,
		PCHTCGeneric = 3100,
		PCHTCVive = 3101,
		PCHTCVivePro = 3102,
		PCHTCViveCosmos = 3103,
		PCValveGeneric = 3150,
		PCValveIndex = 3151,
		Tester = 10000
	};

	/// <summary>
	/// This class is used to signal the audio track index that one wants to play. Typically used in conjunction with SwitchContentParameters.
	/// Notes.
	///  * It is strongly recommended to keep the AudioDecoder and AudioPlaybackEngine arguments at null. This will allow the SDK to select the optional audio decoder and playback engine respectively for the current platform.
	/// </summary>
	public class AudioTrackAndPlaybackParameters {
		public AudioDecoder audioDecoder;
		public AudioPlaybackEngine audioPlaybackEngine;
		public int audioTrackIndex;
		/// <summary>
		/// The contentID to which these AudioTrackAndPlaybackParameters belong.
		/// </summary>
		internal int contentID; // internal until publicly exposed.
		/// <summary>
		/// The preferred audio gain bound between [0, 1]. Set to ClearVRConstants.AUDIO_MAINTAIN_GAIN to maintain the current gain (default value)
		/// </summary>
		internal float audioGain; // internal until publicly exposed.
		/// <summary>
		/// The indicative minimum playback buffer duration, as reported by the host device, in milliseconds. This value might be inaccurate, and actual end-to-end latency might differ from this value.
		/// When unknown, this value will be 0.
		/// Read-only
		/// </summary>
		public long estimatedPlaybackLatencyInNanoseconds {
			get {
				return _estimatedPlaybackLatencyInNanoseconds;
			}
		}
		private long _estimatedPlaybackLatencyInNanoseconds;

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="argAudioTrackIndex">The audio track index to play. Default value: 0.</param>
		/// <param name="argAudioDecoder">The AudioDecoder to use. If set to `null`, the default AudioDecoder for the current platform will be used.</param>
		/// <param name="argAudioPlaybackEngine">The AudioPlaybackEngine to use. If set to `null`, the default AudioPlaybackEngine for the current platform will be used.</param>
		public AudioTrackAndPlaybackParameters(int argAudioTrackIndex = 0, AudioDecoder argAudioDecoder = null, AudioPlaybackEngine argAudioPlaybackEngine = null) : this(argAudioTrackIndex, argAudioDecoder, argAudioPlaybackEngine, ClearVRConstants.AUDIO_MAINTAIN_GAIN) {
			// This implementation is empty on purpose. 
			// It can be removed (and replaced) by the below one, once tuning audio gain through AudioTrackAndPlaybackParameters goes public.
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="argAudioTrackIndex">The audio track index to play. Default value: 0.</param>
		/// <param name="argAudioDecoder">The AudioDecoder to use. If set to `null`, the default AudioDecoder for the current platform will be used.</param>
		/// <param name="argAudioPlaybackEngine">The AudioPlaybackEngine to use. If set to `null`, the default AudioPlaybackEngine for the current platform will be used.</param>
		/// <param name="argAudioGain">The audio gain to apply. If set to ClearVRConstants.AUDIO_MAINTAIN_GAIN (-1), the current gain is maintained.</param>
		// This is internal until the dayy we properly implement audio track control.
		// We should re-instate the default argument values once the above API has been removed. Currently, both API signatures would clash if they both would've all default argument values.
		internal AudioTrackAndPlaybackParameters(int argAudioTrackIndex, AudioDecoder argAudioDecoder, AudioPlaybackEngine argAudioPlaybackEngine, float argAudioGain) : this(argAudioTrackIndex, argAudioDecoder, argAudioPlaybackEngine, argAudioGain, -1 /* meaning: unknown contentID */, 0 /* unknown */) {
			// Empty implementation
		}

		internal AudioTrackAndPlaybackParameters(int argAudioTrackIndex, AudioDecoder argAudioDecoder, AudioPlaybackEngine argAudioPlaybackEngine, float argAudioGain, int argContentID, long argEstimatedPlaybackLatencyInNanoseconds) {
			if (argAudioDecoder == null) {
				argAudioDecoder = AudioDecoder.GetDefaultAudioDecoderForPlatform(Application.platform);
			}
			if (argAudioPlaybackEngine == null) {
				argAudioPlaybackEngine = AudioPlaybackEngine.GetDefaultAudioPlaybackEngineForPlatform(Application.platform);
			}
			audioTrackIndex = argAudioTrackIndex;
			audioDecoder = argAudioDecoder;
			audioPlaybackEngine = argAudioPlaybackEngine;
			audioGain = argAudioGain;
			contentID = argContentID;
			_estimatedPlaybackLatencyInNanoseconds = argEstimatedPlaybackLatencyInNanoseconds;
		}


		/// <summary>
		/// Returns an AudioTrackAndPlaybackParameters with all it's fields set to the current platform's default values.
		/// </summary>
		/// <returns>Default AudioTrackAndPlaybackParameters for the current platform (=Application.platform).</returns>
		public static AudioTrackAndPlaybackParameters GetDefault() {
			return GetDefault(Application.platform);
		}

		/// <summary>
		/// Returns an AudioTrackAndPlaybackParameters with all it's fields set to the current platform's default values.
		/// </summary>
		/// <param name="argRuntimePlatform">The runtime platform for which to retrieve the default parameters.</param>
		/// <returns></returns>
		public static AudioTrackAndPlaybackParameters GetDefault(RuntimePlatform argRuntimePlatform) {
			return new AudioTrackAndPlaybackParameters(0, AudioDecoder.GetDefaultAudioDecoderForPlatform(argRuntimePlatform), AudioPlaybackEngine.GetDefaultAudioPlaybackEngineForPlatform(argRuntimePlatform), ClearVRConstants.AUDIO_MAINTAIN_GAIN);
		}

		internal AudioDecoderTypes GetAudioDecoderTypeEvenIfNull() {
			return audioDecoder != null ? audioDecoder.audioDecoderType : AudioDecoder.GetDefaultAudioDecoderForPlatform(Application.platform).audioDecoderType;
		}

		internal AudioPlaybackEngineTypes GetAudioPlaybackEngineTypeEvenIfNull() {
			return audioPlaybackEngine != null ? audioPlaybackEngine.audioPlaybackEngineType : AudioPlaybackEngine.GetDefaultAudioPlaybackEngineForPlatform(Application.platform).audioPlaybackEngineType;
		}

		/// <summary>Return a protobuf representation of the audio track and playback parameters</summary>
		internal cvri.AudioTrackAndPlaybackParametersMediaFlow ToCoreProtobuf() {
			return new cvri.AudioTrackAndPlaybackParametersMediaFlow() {
				AudioTrackIndex = audioTrackIndex,
				AudioDecoderType = (int)audioDecoder.audioDecoderType,
				AudioPlaybackEngineType = (int)audioPlaybackEngine.audioPlaybackEngineType,
				AudioGain = audioGain,
				ContentID = contentID,
				EstimatedPlaybackLatencyInNanoseconds = _estimatedPlaybackLatencyInNanoseconds,
			};
		}

		internal static AudioTrackAndPlaybackParameters FromCoreProtobuf(cvri.AudioTrackAndPlaybackParametersMediaFlow coreAudioTrackAndPlaybackParameters) {
			if(coreAudioTrackAndPlaybackParameters == null) {
				return null;
			}
			return new AudioTrackAndPlaybackParameters() {
				audioTrackIndex = coreAudioTrackAndPlaybackParameters.AudioTrackIndex,
				audioDecoder = new AudioDecoder((AudioDecoderTypes) coreAudioTrackAndPlaybackParameters.AudioDecoderType),
				audioPlaybackEngine = new AudioPlaybackEngine((AudioPlaybackEngineTypes) coreAudioTrackAndPlaybackParameters.AudioPlaybackEngineType),
				audioGain = coreAudioTrackAndPlaybackParameters.AudioGain,
				contentID = coreAudioTrackAndPlaybackParameters.ContentID,
				_estimatedPlaybackLatencyInNanoseconds = coreAudioTrackAndPlaybackParameters.EstimatedPlaybackLatencyInNanoseconds,
			};
		}

		internal AudioTrackAndPlaybackParameters Clone(float argNewGain) {
			return new AudioTrackAndPlaybackParameters(this.audioTrackIndex,
				this.audioDecoder,
				this.audioPlaybackEngine,
				argNewGain,
				this.contentID,
				this.estimatedPlaybackLatencyInNanoseconds);
		}

		/// <summary>
		/// Create a deep copy of the object.
		/// </summary>
		/// <returns></returns>
		public AudioTrackAndPlaybackParameters Clone() {
			return this.Clone(this.audioGain);
		}

		public override string ToString() {
			return String.Format("AudioTrackIndex: {0}, AudioDecoder: {1}, AudioPlaybackEngine: {2}, AudioGain: {3}, ContentID: {4}, EstimatedPlaybackLatencyInNanoseconds: {5}", audioTrackIndex, audioDecoder, audioPlaybackEngine, audioGain, contentID, estimatedPlaybackLatencyInNanoseconds);
		}
	}

	static class DeviceTypesMethods {
		public static bool GetIsAndroidOculusDevice(this DeviceTypes argDeviceType) {
			return (argDeviceType == DeviceTypes.AndroidOculusGearVR ||
				argDeviceType == DeviceTypes.AndroidOculusGo ||
				argDeviceType == DeviceTypes.AndroidOculusQuest ||
				argDeviceType == DeviceTypes.AndroidOculusQuest2);
		}
		public static bool GetIsOculusDevice(this DeviceTypes argDeviceType) {
			return GetIsAndroidOculusDevice(argDeviceType) ||
				(argDeviceType == DeviceTypes.PCOculusRiftDK1 ||
				argDeviceType == DeviceTypes.PCOculusRiftDK2 ||
				argDeviceType == DeviceTypes.PCOculusRiftCV1 ||
				argDeviceType == DeviceTypes.PCOculusRiftS ||
				argDeviceType == DeviceTypes.PCOculusLinkQuest);
		}
#if UNITY_ANDROID && !UNITY_EDITOR
		public static bool GetIsPicoVRDevice(this DeviceTypes argDeviceType) {
			return (argDeviceType == DeviceTypes.AndroidPicoVRGeneric);
		}
#endif

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		public static bool GetIsMobfishDevice(this DeviceTypes argDeviceType) {
			return (argDeviceType == DeviceTypes.AndroidMobfishCardboard ||
				argDeviceType == DeviceTypes.IOSMobfishCardboard);
		}
#endif

		public static bool GetIsFlatDeviceType(this DeviceTypes argDeviceType) {
			return argDeviceType == DeviceTypes.AndroidFlat ||
			argDeviceType == DeviceTypes.IOSFlat ||
			argDeviceType == DeviceTypes.PCFlat;
		}

		public static bool GetIsCardboardLikeDeviceType(this DeviceTypes argDeviceType) {
			return argDeviceType == DeviceTypes.AndroidGenericCardboard ||
			argDeviceType == DeviceTypes.AndroidMobfishCardboard ||
			argDeviceType == DeviceTypes.IOSGenericCardboard ||
			argDeviceType == DeviceTypes.IOSMobfishCardboard;
		}

		public static bool GetIsVRDeviceThatCanRenderStereoscopicContent(this DeviceTypes argDeviceType) {
			return (int)argDeviceType >= 1000 && argDeviceType != DeviceTypes.Tester;
		}

		/// <summary>Return a protobuf representation of the device type/summary>
		internal static cvri.DeviceType ToCoreProtobuf(this DeviceTypes argDeviceType) {
			switch (argDeviceType) {
				// This list must be in sync with the list in core/External.go. As the core does not specify an Unknown/default value, we start at -1 in our case.
				case DeviceTypes.Unknown:
					return cvri.DeviceType.UnknownDeviceType;
				case DeviceTypes.AndroidFlat:
					return cvri.DeviceType.AndroidFlat;
				case DeviceTypes.IOSFlat:
					return cvri.DeviceType.IosFlat;
				case DeviceTypes.AppleTV:
					return cvri.DeviceType.AppleTv;
				case DeviceTypes.PCFlat:
					return cvri.DeviceType.PcFlat;
				case DeviceTypes.AndroidGenericHMD:
					return cvri.DeviceType.AndroidGenericHmd;
				case DeviceTypes.AndroidGenericCardboard:
					return cvri.DeviceType.AndroidGenericCardboard;
				case DeviceTypes.AndroidGenericDaydream:
					return cvri.DeviceType.AndroidGenericDaydream;
				case DeviceTypes.AndroidMobfishCardboard:
					return cvri.DeviceType.AndroidMobfishCardboard;
				case DeviceTypes.AndroidOculusGeneric:
					return cvri.DeviceType.AndroidOculusGeneric;
				case DeviceTypes.AndroidOculusGo:
					return cvri.DeviceType.AndroidOculusGo;
				case DeviceTypes.AndroidOculusGearVR:
					return cvri.DeviceType.AndroidOculusGearvr;
				case DeviceTypes.AndroidOculusQuest:
					return cvri.DeviceType.AndroidOculusQuest;
				case DeviceTypes.AndroidOculusQuest2:
					return cvri.DeviceType.AndroidOculusQuest2;
				case DeviceTypes.AndroidWaveVRGeneric:
					return cvri.DeviceType.AndroidWavevrGeneric;
				case DeviceTypes.AndroidPicoVRGeneric:
					return cvri.DeviceType.AndroidPicovrGeneric;
				case DeviceTypes.AndroidSkyworthVRGeneric:
					return cvri.DeviceType.AndroidSkyworthvrGeneric;
				case DeviceTypes.AndroidGSXRGeneric:
					return cvri.DeviceType.AndroidGsxrGeneric;
				case DeviceTypes.IOSGenericHMD:
					return cvri.DeviceType.IosGenericHmd;
				case DeviceTypes.IOSGenericCardboard:
					return cvri.DeviceType.IosGenericCardboard;
				case DeviceTypes.IOSMobfishCardboard:
					return cvri.DeviceType.IosMobfishCardboard;
				case DeviceTypes.PCGenericHMD:
					return cvri.DeviceType.PcGenericHmd;
				case DeviceTypes.PCOculusGeneric:
					return cvri.DeviceType.PcOculusGeneric;
				case DeviceTypes.PCOculusRiftDK1:
					return cvri.DeviceType.PcOculusRiftDk1;
				case DeviceTypes.PCOculusRiftDK2:
					return cvri.DeviceType.PcOculusRiftDk2;
				case DeviceTypes.PCOculusRiftCV1:
					return cvri.DeviceType.PcOculusRiftCv1;
				case DeviceTypes.PCOculusRiftS:
					return cvri.DeviceType.PcOculusRiftS;
				case DeviceTypes.PCOculusLinkQuest:
					return cvri.DeviceType.PcOculusLinkQuest;
				case DeviceTypes.PCHTCGeneric:
					return cvri.DeviceType.PcHtcGeneric;
				case DeviceTypes.PCHTCVive:
					return cvri.DeviceType.PcHtcVive;
				case DeviceTypes.PCHTCVivePro:
					return cvri.DeviceType.PcHtcVivePro;
				case DeviceTypes.PCHTCViveCosmos:
					return cvri.DeviceType.PcHtcViveCosmos;
				case DeviceTypes.PCValveGeneric:
					return cvri.DeviceType.PcValveGeneric;
				case DeviceTypes.PCValveIndex:
					return cvri.DeviceType.PcValveIndex;
				case DeviceTypes.Tester:
					return cvri.DeviceType.Tester;
				default:
					UnityEngine.Debug.LogError(String.Format("[ClearVR] The following device type cannot be converted into its protobuf equivalent. Falling back to default device type `PCFlat`, which might be unoptimal. Please report this issue to Tiledmedia to obtain an updated SDK."));
					return cvri.DeviceType.PcFlat;
			}

		}
	}

	/// <summary>
	/// Enum that distinguishes between different supported proxy types.
	/// </summary>
	public enum ProxyTypes {
		/// <summary>
		/// Unknown, not used.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// HTTP proxy
		/// </summary>
		Http = 1,
		/// <summary>
		/// HTTPS proxy
		/// </summary>
		Https = 2
	}

	static class ProxyTypesMethods {
		public static cvri.ProxyType ToCoreProtobuf(this ProxyTypes proxyType) {
			switch (proxyType) {
				case ProxyTypes.Unknown:
					return cvri.ProxyType.Unknown;
				case ProxyTypes.Http:
					return cvri.ProxyType.Http;
				case ProxyTypes.Https:
					return cvri.ProxyType.Https;
			}
			throw new System.ArgumentException("[ClearVR] ProxyTypes cannot be converted to Core Protobuf equivalent. Please report this crash to Tiledmedia.");
		}

		internal static ProxyTypes FromCoreProtobuf(this cvri.ProxyType protoProxyType) {
			switch (protoProxyType) {
				case cvri.ProxyType.Unknown:
					return ProxyTypes.Unknown;
				case cvri.ProxyType.Http:
					return ProxyTypes.Http;
				case cvri.ProxyType.Https:
					return ProxyTypes.Https;

			}
			throw new System.ArgumentException("[ClearVR] Core Protobuf ProxyType cannot be converted to internal equivalent. Please report this crash to Tiledmedia.");
		}
	}

	/// <summary>
	/// Helper class to define HTTP/HTTPS proxy parameters.
	/// </summary>
	public class ProxyParameters {
		private ProxyTypes _proxyType = ProxyTypes.Unknown;
		private String _host = "<auto>";
		private int _port = -1;
		private String _username = "";
		private String _password = "";

		/// <summary>
		/// Get proxy type
		/// </summary>
		/// <value>The type of this ProxyParameters class.</value>
		public ProxyTypes proxyType {
			get {
				return _proxyType;
			}
		}
		/// <summary>
		/// Get the host as a string
		/// </summary>
		/// <value>The host</value>
		public String host {
			get {
				return _host;
			}
		}
		/// <summary>
		/// Get the port as an integer.
		/// </summary>
		/// <value>The port</value>
		public int port {
			get {
				return _port;
			}
		}
		/// <summary>
		/// Get the username as a string.
		/// </summary>
		/// <value>The username</value>
		public String username {
			get {
				return _username;
			}
		}
		/// <summary>
		/// Get the password as a string.
		/// </summary>
		/// <value>The password</value>
		public String password {
			get {
				return _password;
			}
		}
		/// <summary>
		/// Default constructor. Note that you first construct a ProxyParameters class, and subsequently call SetProxyParameters() to configure it accordingly.
		/// </summary>
		/// <param name="argProxyType">The proxy type.</param>
		public ProxyParameters(ProxyTypes argProxyType) {
			_proxyType = argProxyType;
		}

		/// <summary>Return a protobuf representation of the proxy parameters</summary>
		internal cvri.ProxyParamsMediaFlow ToCoreProtobuf() {
			cvri.ProxyParamsMediaFlow proxyParamsMediaFlow = new cvri.ProxyParamsMediaFlow();
			proxyParamsMediaFlow.Host = host;
			proxyParamsMediaFlow.Port = port;
			proxyParamsMediaFlow.Username = username;
			proxyParamsMediaFlow.Password = password;
			proxyParamsMediaFlow.ProxyType = proxyType.ToCoreProtobuf();
			return proxyParamsMediaFlow;
		}

		/// <summary>
		/// Set the proxy settings. At minimal, host and port need to specified if a proxy is active. You can either opt for auto-detection (setting them to "&lt;auto&gt;" and -1 respectively or disable proxy detection completely (by setting them to "" and 0 respectively). Optionally a username and password can be set.
		/// Leave username and password empty (e.g. an empty string) in case an anonymous proxy is used.
		/// <param name="argHost">The proxy host. Set to its default value "&lt;auto&gt;" for auto-detection. Set to an empty string to display the proxy.</param>
		/// <param name="argPort">The proxy port. Set to its default value -1 for auto-detection, set to 0 for disable the proxy.</param>
		/// <param name="argUsername">The proxy username. Default value: "&lt;auto&gt;". Note that we can ONLY auto-detect the username on iOS, so you have to provide it on any other platform. Set this value in case the proxy does not allow anonymous access.</param>
		/// <param name="argPassword">The proxy password. Default value: "". Note that we cannot auto-detect the password, it must always be provided in case the proxy does not allow anonymous access.</param>
		/// </summary>
		public void SetProxyParameters(String argHost, int argPort, String argUsername = "<auto>", String argPassword = "") {
#if !UNITY_IOS && !UNITY_TVOS && !UNITY_EDITOR
			if(argUsername == "<auto>") {
				argUsername = ""; // We only support <auto> username resolve on iOS. On any other platform we should put it to an empty string by default.
			}
#endif
			_host = argHost;
			_port = argPort;
			_username = argUsername;
			_password = argPassword;
		}

		public override String ToString() {
			return String.Format("Type: {0}, host: '{1}', port: {2}, username: '{3}', password: '{4}'", _proxyType, _host, _port, Utils.MaskString(_username), Utils.MaskString(_password));
		}
	}

	/// <summary>
	/// The ClearVRPlayer can take care of automatically placing camera and/or content based on the content type. Various modes are supported.
	/// </summary>
	public enum CameraAndContentPlacementModes {
		/// <summary> The ClearVRPlayer object will NOT automatically move the camera and the display object. The application is responsible for the positioning of the camera and the display object when the FirstFrameRendered ClearVREvent is triggered.</summary>
		Disabled,
		/// <summary> The ClearVRPlayer object will automatically move the camera and the display object. For planar or rectilinear content, the display object will be positioned such that if the camera is at position (0,0,0) with default rotation, full content is visible; for omnidirectional content the display object will be moved at the center of the scene (0, 0, 0) will be positionned and its rotation will be reset. </summary>
		Default,
		/// <summary> Default behaviors for headset. The camera pose is not touched. </summary>
		MoveDisplayObjectAndIgnoreCamera,
		/// <summary> Default behaviors for flat display. The camera pose is reset when needed. </summary>
		MoveDisplayObjectResetCamera,
	}

	/// <summary>
	/// Configuring your ClearVRPlayer object can be achieved using the platformOptions class.
	/// > [!WARNING]
	/// > Reading or changing any field _after_ calling ClearVRPlayer.Initialize() is not allowed and can result in undefined behaviour. The only exception to this rule is when using the [applicationRegainedFocusDelegate](xref:com.tiledmedia.clearvr.ClearVRPlayer.applicationRegainedFocusDelegate) delegate to customize player behaviour during an application suspend/resume cycle.
	/// </summary>
	public class PlatformOptionsBase : PlatformOptionsInterface {
		/// <summary>
		/// Automatically inferred, do not overwrite.
		/// </summary>
		public readonly RuntimePlatform platform = Application.platform;
		/// <summary>
		/// The ClearVRPlayer requires a license file to function. Provide the binary data here.
		/// </summary>
		public byte[] licenseFileBytes;
		/// <summary>
		/// Holds whether a VR headset is present or not.
		/// Note that this value is ALWAYS overwritten based on the deviceType field. Setting this value yourself does not have any effect.
		/// </summary>
		[Obsolete("This field is no longer used. Please remove any reference to it from your code. Refer to Utils.GetIsVRDevicePresent() instead.", true)]
		public bool isVRDevicePresent = false;
		/// <summary>
		/// The parent gameobject of the ClearVRPlayer. If set to null (default value) it will be set to the GameObject the ClearVRPlayer script is attached to. This is preferred default behaviour.
		/// </summary>
		[Obsolete("This field is no longer used. Please remove any reference to it from your code.", true)]
		public GameObject parentGameObject = null;
		/// <summary>
		/// The preferred RenderMode of the content. Default value: [RenderModes.Native](xref:com.tiledmedia.clearvr.RenderModes.Native).
		/// > [!NOTE]
		/// > The value of this field will be ignored if [enableAutomaticRenderModeSwitching](xref:com.tiledmedia.clearvr.PlatformOptionsBase.enableAutomaticRenderModeSwitching) = false.
		/// </summary>
		public RenderModes preferredRenderMode = RenderModes.Native;
		/// <summary>
		/// Whether content playback should automatically start after content has been loaded. Default value: true
		/// </summary>
		public bool autoPlay = true;
		/// <summary>
		/// Whether content should loop when it has reached the end of the clip. Default value: true.
		/// Note that live content does not support looping.
		/// </summary>
		public bool loopContent = true;
		/// <summary>
		/// If specified, this ContentItem will be automatically loaded after the ClearVRPlayer has completed initialization.
		/// By default, we highly recommend you to set this to your ContentItem of choice.
		/// </summary>
		[Obsolete("This field has been replaced by the platformOptions.prepareContentParameters field.", true)]
		/// </summary>
		public ContentItem autoPrepareContentItem = null;
		/// <summary>
		/// The AudioPlaybackEngine to use. Must be set to its default value null.
		/// </summary>
		[Obsolete("audioPlaybackEngine field has been deprecated and is no longer functional. You can safely remove any reference to it in your code.", true)]
		public AudioPlaybackEngine audioPlaybackEngine = null;
		/// <summary>
		/// The AudioDecoder to use. Must be set to its default value null.
		/// </summary>
		[Obsolete("audioDecoder field has been deprecated and is no longer functional. You can safely remove any reference to it in your code.", true)]
		public AudioDecoder audioDecoder = null;
		/// <summary>
		/// How to handle application pause/unpause and suspend/resume. See ApplicationFocusAndPauseHandlingTypes for details.
		/// </summary>
		public ApplicationFocusAndPauseHandlingTypes applicationFocusAndPauseHandling = ApplicationFocusAndPauseHandlingTypes.Recommended;
		/// <summary>
		/// How to handle the loss and gain of the audio focus. See [AudioFocusChangedHandlingTypes](xref:com.tiledmedia.clearvr.AudioFocusChangedHandlingTypes) for details.
		/// </summary>
		public AudioFocusChangedHandlingTypes audioFocusChangedHandlingType = AudioFocusChangedHandlingTypes.Recommended;
		/// <summary>
		/// ClearVR's SwitchContent() API allows you to switching between monoscopic and stereoscopic clips. This feature requires careful management of the active [RenderMode](xref:com.tiledmedia.clearvr.RenderModes).
		/// This option applies only when targeting HMDs, as playback will always be monoscopic if no HMD is detected.
		/// When set to true (default value), the ClearVRPlayer will automatically configure the RenderMode (monoscopic/stereoscopic) based on the active content item.
		/// > [!WARNING]
		/// > Setting this value to false will require the developer to implement all RenderMode switching logic himself. This can be a daunting task given the many permutations possible.
		/// > You are discouraged to do so.
		/// </summary>
		public bool enableAutomaticRenderModeSwitching = true;
		/// <summary>
		/// The width of the screen in pixels. Keep at the default value 0 for auto-detection.
		/// </summary>
		[Obsolete("PlatformOptions.screenWidth field has been replaced by the PlatformOptions.deviceParameters field.", true)]
		public short screenWidth = 0;
		/// The height of the screen in pixels. Keep at the default value 0 for auto-detection.
		[Obsolete("PlatformOptions.screenHeight field has been replaced by the PlatformOptions.deviceParameters field.", true)]
		public short screenHeight = 0;
		/// <summary>
		/// The device type. For optimal performance it is essential to set the correct device type. Set/keep at its default value DeviceTypes.Unknown for auto-detection. If you fail to set the correct value, poor performance, poor visual quality or stereoscopic playback may break.
		/// </summary>
		[Obsolete("PlatformOptions.deviceType field has been replaced by the PlatformOptions.deviceParameters field.", true)]
		public DeviceTypes deviceType = DeviceTypes.Unknown;
		/// <summary>
		/// Allows you to manually override the device type and screen dimensions.
		/// This is an advanced API, and under normal circumstances there should never be the need to change this from its default value: null.
		/// Since v8.0. 
		/// > [!NOTE]
		/// > This field replaces the screenWidth/screenHeight and deviceType fields that were available in v7 and older.
		/// > If you are targeting a custom headset that is not correctly auto-detected by the SDK, you can use this field to deviceType and screen dimensions.
		/// </summary>
		public DeviceParameters deviceParameters = null; // The default value will be null, allowing us to know whether it was explicitly set or not. Changing it to a non-null default value impacts PlatformOptions.Verify(), so be careful when pondering on doing that (free tip for future self: don't do that).
		/// <summary>
		/// ClearVRCore verbosity level. Please keep at 0 at all times.
		/// </summary>
		[Obsolete("This field has been replaced by the static int ClearVRPlayer.coreLogLevel field.", true)]
		public int clearVRCoreVerbosity = 0;
		/// <summary>
		/// Instead of writing to stdout, the core ClearVRCore log will be written to the specified file.
		/// </summary>
		[Obsolete("This field has been replaced by the static String ClearVRPlayer.coreLogFile field.", true)]
		public String clearVRCoreLogToFile = "";
		[Obsolete("enableAutomaticRenderModeSwitchingOnContentFormatChanged has been deprecated and is no longer useful. Please remove any reference to it from your code", true)]
		public bool enableAutomaticRenderModeSwitchingOnContentFormatChanged = true;
		public long initializeFlags = (long)InitializeFlags.None;
		/// <summary>
		/// Any value &lt;= 0 will result in the default timeout to be used. This default value is currently 30000 milliseconds.
		/// This timeout will only be triggered if loading a specific content item took longer than the specified amount of time. In this case, one will receive a ClearVRCoreWrapperContentLoadingTimeout message.
		/// </summary>
		[Obsolete("This field is obsolete and has moved to platformOptions.prepareContentParameters(..., argTimeoutInMilliseconds, ...).", true)]
		public int prepareContentForPlayoutTimeoutInMilliseconds = 0;
		/// <summary>
		/// Setting this to true (default: false) allows the implementor to change the transparency of the sphere by using the mediaPlayer.SetMainColor() api.
		/// For common performance reasons not specifically related to ClearVR streaming, you should carefully assess the potential negative performance impact of using transparency-enabled shaders on mobile devices.
		/// Please note that you need to manually include the required shaders into your project (via Edit -> Project Settings -> Graphics -> Always Included Shaders).
		/// </summary>
		[Obsolete("You can no longer explicitly enable or disable support for transparency. Setting an alpha component != 1 using the SetMainColor() API now automatically enables transparency support.", true)]
		public bool isTransparencySupportEnabled = false;
		/// <summary>
		/// Since v4.1.2
		/// Supersedes the deprecated camera field. This transform is used to track the highquality viewport. Typically, one would set this field to the activeCamera.transform field.
		/// One could also use the transform of for example a random gameobject and let the viewport track the orientation of it.
		/// </summary>
		public Transform trackingTransform = null;
		/// <summary>
		/// Since v5.1
		/// Specify the HTTP proxy settings. Note that the lower-level SDK will attempt to detect proxy host and port automatically if host and port are at their default values ("&lt;auto&gt;" and -1 respectively).
		/// Due to platform security constraints, we cannot detect username and password automatically. We must rely on the application to provide those. See [SetProxyParameters](xref:com.tiledmedia.clearvr.ProxyParameters.SetProxyParameters(String,System.Int32,String,String)) for details.
		/// </summary>
		public readonly ProxyParameters httpProxyParameters = new ProxyParameters(ProxyTypes.Http);
		/// <summary>
		/// Since v5.1
		/// Specify the HTTPS proxy settings. Note that the lower-level SDK will attempt to detect proxy host and port automatically if host and port are at their default values ("&lt;auto&gt;" and -1 respectively).
		/// Due to platform security constraints, we cannot detect username and password automatically. We must rely on the application to provide those. See [SetProxyParameters](xref:com.tiledmedia.clearvr.ProxyParameters.SetProxyParameters(String,System.Int32,String,String)) for details.
		/// </summary>
		public readonly ProxyParameters httpsProxyParameters = new ProxyParameters(ProxyTypes.Https);
		/// <summary>
		/// Since v6.0
		/// Specifies the active camera rendering to the screen.
		/// Default value: Camera.main
		/// </summary>
		public Camera renderCamera = Camera.main;
		/// <summary>
		/// Since v6.0
		/// Specifies how the video texture should be rendered.
		/// <list type="bullet">
		/// <item>
		/// <description>Android (OpenGLES 2): TextureBlitModes.UVShufflingZeroCopy (default) and TextureBlitModes.UVShufflingCopy are allowed. The latter should ONLY be used if targetting Picture in Picture mode using another third party video player at the same time as when using ClearVR.</description>
		/// </item>
		/// <item>
		/// <description>Android (OpenGLES 3): TextureBlitModes.UVShufflingZeroCopy (default), TextureBlitModes.UVShufflingCopy and TextureBlitModes.OVROverlayZeroCopy are allowed. 
		/// `TextureBlitModes.UVShufflingCopy` should ONLY be used if targetting Picture in Picture mode using another third party video player at the same time as when using ClearVR. 
		/// `TextureBlitModes.OVROverlayZeroCopy` implies the use of OVROverlay on Oculus hardware and is ONLY enabled if the ClearVR Oculus VR Extensions have been enabled in the ClearVR drop down menu.</description>
		/// </item>
		/// <item>
		/// <description>iOS (OpenGLES 2 and 3): only TextureBlitModes.UVShufflingZeroCopy (default) is allowed. OpenGLES 2 and 3 are only supported on Unity versions 2020.2 and older.</description>
		/// </item>
		/// <item>
		/// <description>iOS (Metal): only TextureBlitModes.UVShufflingCopy (default) is allowed.</description>
		/// </item>
		/// <item>
		/// <description>Win64 (OpenGLCore): only TextureBlitModes.UVShufflingZeroCopy (default) is allowed.</description>
		/// </item>
		/// <item>
		/// <description>Win64 (Direct3D11): only TextureBlitModes.UVShufflingZeroCopy (default) is allowed.</description>
		/// </item>
		/// <item>
		/// <description>Linux (OpenGLCore): only TextureBlitModes.UVShufflingZeroCopy (default) is allowed.</description>
		/// </item>
		/// </list>
		/// > [!NOTE]
		/// > `TextureBlitModes.OVROverlayZeroCopy` support is ONLY supported on Oculus Android hardware (e.g. the Quest 1 and 2). 
		/// > It supports playback of non-ClearVR encoded content like rectilinear ("16x9") feeds, and traditional ERP360, ERP180 and Cubemap content.
		/// </summary>
		public TextureBlitModes textureBlitMode = TextureBlitModes.Default;
		/// <summary>
		/// Specifies the active color space. Default value is based on QualitySettings.activeColorSpace
		/// You can use ColorSpacesMethods.ConvertUnityColorSpace() to convert a Unity ColorSpace enum, into a ClearVR ColorSpaces enum.
		/// </summary>
		public ColorSpaces overrideColorSpace = ColorSpacesMethods.ConvertActiveUnityColorSpace();
		/// <summary>
		/// Specifies the currently active VR API. By default this will be auto-detected, but you can override it my manually specifying.
		/// The latter is, however, not recommended.
		/// Notes.
		/// 1. Auto detection is currently limited to Oculus VR and OpenVR only. Any other VRApi is detected as Unknown.
		/// 2. One is not encouraged to rely on this value for your own application logic.
		/// </summary>
		public VRAPITypes vrAPIType = Utils.GetVRAPIType();
		/// <summary>
		/// You can tweak OVROverlay specific settings to your preference on this struct. See [OVROverlayOptions](xref:com.tiledmedia.clearvr.OVROverlayOptions) for details.<br/>
		/// This applies to Android only currently.
		/// </summary>
		public OVROverlayOptions ovrOverlayOptions = new OVROverlayOptions();
		/// <summary>
		/// Set the required robustness of playback of protected content. Default value: ContentProtectionRobustnessLevels.Unprotected
		/// Note that this toggle is effective throughout the lifetime of your ClearVRPlayer object.
		/// </summary>
		[Obsolete("Content protection level does not need to be set anymore, it will be configured automatically. It will be removed after 29/01/2024.", false)]
#pragma warning disable 0618 // Silence deprecated API usage warning
		public ContentProtectionRobustnessLevels contentProtectionRobustnessLevel = ContentProtectionRobustnessLevels.Unprotected;
#pragma warning restore 0618
		/// <summary>
		/// Specify the behavior of the ClearVRPlayer object regarding the camera and display object automatic placement after a content switch. Default value: CameraAndContentPlacementModes.Default
		/// You can set this to Disabled to not move the Camera and/or mesh to their default/recommended position.
		/// </summary>
		[Obsolete("Automatic mesh and camera placement has been deprecated in v9.x and will be removed after 2023-03-31. Please use the LayoutManager instead.", false)]
		public CameraAndContentPlacementModes cameraAndContentPlacementMode = CameraAndContentPlacementModes.Default;
		/// <summary>
		/// This field allows you to override the user agent field in each video-streaming related HTTP request.
		/// As viewport-adaptive streaming results in many HTTP requests, one is STRONGLY discouraged to set this field as it will increase network overhead.. 
		/// If needed, be sure to keep this String as short as possible. Also, this string cannot be changed after the ClearVRPlayer has initialized.
		/// Default value: "" (an empty string)
		/// </summary>
		public String overrideUserAgent = "";
		/// <summary>
		/// Allows one to override the initial audio gain [0, 1].
		/// Since: v8.0
		/// </summary>
		public float initialAudioGain = ClearVRConstants.AUDIO_MAINTAIN_GAIN;
		/// <summary>
		/// Used internally, never make public. Used to signal mute state when resuming playback after app suspend/resume cycle.
		/// Since: v8.0
		/// </summary>
		internal float muteState = 0; // 0 = default value. Do not change, logic depends on it in Verify().
		/// <summary>
		/// Enable or disable ABR. Default value: true
		/// </summary>
		public bool enableABR = true;
		/// <summary>
		/// Define the start mode of the ABR algorithm. See [ABRStartModes](xref:com.tiledmedia.clearvr.ABRStartModes) for details.
		/// Default value: Default
		/// </summary>
		public ABRStartModes abrStartMode = ABRStartModes.Default;

		/// <summary>
		/// In some cases, a stereoscopic clip cannot be rendered stereoscopic due to insufficient decoder capabilities. For example, playing a 12K stereoscopic clip requires a hardware decoder that can decode at least 6K. If you would've tried to play this clip on a 4K decoder, playback would simply fail.
		/// If you enable this option (e.g. set it to true), the player will attempt to render the clip in monoscopic in case insufficient decoder capacity is detected for stereoscopic playback, permitting that the decoder has sufficient capacity to do so.
		/// Default value: false
		/// > [!WARNING]
		/// > Note that this can only be configured once, either when calling the ClearVRPlayer.TestIsContentSupported() API or clearVRPlayer.Initialize(), whichever comes first. From that point onward, it cannot be changed until you completely stop the ClearVRPlayer.
		/// </summary>
		public bool allowDecoderContraintsInducedStereoToMono = false;
		/// <summary>
		/// Specifies what content should be loaded and at what position playout should start. Cannot be null.
		/// This replaces the deprecated autoPrepareContentItem field.
		/// 
		/// Since: v8.0
		/// 
		/// > [!NOTE]
		/// > Before v8.0, one could set autoPrepareContentItem to null to only bootstrap the player. This feature has been removed in v8.0 because speed gain was very minimal (less then 50 msec)
		/// </summary>
		public PrepareContentParameters prepareContentParameters = null;

		private bool _isVerifyCalled = false;
		/// <summary>
		/// Configure telemetry services reporting. See [TelemetryConfiguration](xref:com.tiledmedia.clearvr.TelemetryConfiguration) for details.
		/// Default value: null.
		/// </summary>
		public TelemetryConfiguration telemetryConfiguration = null;

		/// <summary>
		/// Used internally to verify whether the specified options are supported by the current platform. Do not use yourself!
		/// In case of failure, an error message will be logged to the console. The callee should stop execution if false is returned from this method, as library behaviour is undefined otherwise.
		/// </summary>
		/// <returns>True in case of success, false otherwise.</returns>
		public virtual bool Verify(ClearVRLayoutManager argClearVRLayoutManager) { // Will be called multiple times!
			_isVerifyCalled = true;
			// Each platform specific options class should call this base.Verify() method, see e.g. PlatformOptionsAndroid.Verify()
			// Since v4.0, we need to set the screen width and screen height.
			// Since v8.0, we use of the DeviceParameters struct instead of the screenWidth/screenHeight/deviceType fields.
			if (deviceParameters == null) {
				deviceParameters = new DeviceParameters(); // Takes care of auto-detecting screen dimensions and deviceType if not explicitly set.
			} // else: deviceParameters are already set, we can continue to verify them.
			if (!deviceParameters.Verify()) {
				return false;
			}
			if (textureBlitMode == TextureBlitModes.Default) {
				// This is ClearVRPlayer layer dependent, so we implement the logic here instead of in the NRP
				// Verify() will query the NRP if the chosen combination is supported or not by the NRP.
				if (platform == RuntimePlatform.Android) {
					// Default option is to use ZeroCopy on Android
					textureBlitMode = TextureBlitModes.UVShufflingZeroCopy;
				} else if (platform == RuntimePlatform.IPhonePlayer) {
					switch (SystemInfo.graphicsDeviceType) {
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2: {
								UnityEngine.Debug.LogError("[ClearVR] OpenGLES 2 is not supported on iOS.");
								return false;
							}
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3: {
								UnityEngine.Debug.LogError("[ClearVR] OpenGL ES is not supported anymore on iOS. OpenGL ES support has been dropped in Unity 2020.2.0f1 and newer. Unity's changelog (https://unity3d.com/unity/whats-new/2020.2.0) states: 'iOS: Removed OpenGL ES support on iOS/tvOS.'. Switch to Metal instead.");
								return false;
							}
						case UnityEngine.Rendering.GraphicsDeviceType.Metal: {
								// We use Copy mode, zerocopy is not supported as that results in synchronisation issues with shuffled tiles as a result.
								textureBlitMode = TextureBlitModes.UVShufflingCopy;
								break;
							}
						default:
							UnityEngine.Debug.LogError(String.Format("[ClearVR] Graphics API {0} not supported on this platform.", SystemInfo.graphicsDeviceType));
							return false;
					}

				} else if (platform == RuntimePlatform.WindowsEditor || platform == RuntimePlatform.WindowsPlayer) {
					switch (SystemInfo.graphicsDeviceType) {
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore: {
								textureBlitMode = TextureBlitModes.UVShufflingZeroCopy;
								break;
							}
						case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11: {
								textureBlitMode = TextureBlitModes.UVShufflingZeroCopy;
								break;
							}
						default:
							break;
					}
				} else if (platform == RuntimePlatform.LinuxEditor || platform == RuntimePlatform.LinuxPlayer) {
					switch (SystemInfo.graphicsDeviceType) {
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
						case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore: {
								textureBlitMode = TextureBlitModes.UVShufflingZeroCopy;
								break;
							}
						default:
							break;
					}
				}
			} else {
				if (textureBlitMode.GetIsOVROverlayMode()) {
					if (!deviceParameters.deviceType.GetIsAndroidOculusDevice()) {
						UnityEngine.Debug.LogError(String.Format("[ClearVR] TextureBlitMode {0} is only supported on Android Oculus hardware. No such headset detected. Cannot continue.", textureBlitMode));
						return false;
					}
				}
			}

#pragma warning disable 0618 // Silence deprecated API usage warning
			if (cameraAndContentPlacementMode == CameraAndContentPlacementModes.Default) {
				if (Utils.GetIsVrDevicePresent()) {
					cameraAndContentPlacementMode = CameraAndContentPlacementModes.MoveDisplayObjectAndIgnoreCamera;
				} else {
					cameraAndContentPlacementMode = CameraAndContentPlacementModes.MoveDisplayObjectResetCamera;
				}
			}
#pragma warning restore 0618 // Silence deprecated API usage warning
#if UNITY_ANDROID && !UNITY_EDITOR // PicoVR supports Android only (despite having a bit of code in the SDK referring to iOS)
			if(deviceParameters.deviceType.GetIsPicoVRDevice()) {
				// On Pico, an explicit check is required to make sure that the Camera is selected with the Pvr_UnitySDKHeadTrack component attached to it (== Head GameObject in the PVr_UnitySDK prefab).
				// This is a fix for #3631 and #3839
				String componentName = "Pvr_UnitySDKHeadTrack";
				String component2Name = "Pvr_UnitySDKEyeManager"; // Check for a second component that is known to be attached to the Head object in case the first is null
				Type pvr_UnitySDKHeadTrackType = Utils.GetType(componentName);
				Type pvr_UnitySDKEyeManagerType = Utils.GetType(component2Name);
				if(renderCamera != null) {
					// Check if correct renderCamera was selected.
					bool isComponentFound = pvr_UnitySDKHeadTrackType != null ? renderCamera.GetComponent(pvr_UnitySDKHeadTrackType) != null : false;
					bool isComponent2Found = pvr_UnitySDKEyeManagerType != null ? renderCamera.GetComponent(pvr_UnitySDKEyeManagerType) != null : false;
					
					if(!isComponentFound && !isComponent2Found) {
						// Render camera is specified, but does not contain the required component.
						// Most likely the user selected the incorrect renderCamera OR multiple cameras were tagged as MainCamera.
						UnityEngine.Debug.LogWarning(String.Format("[ClearVR] PicoVR headset detected, but configured platformOptions.renderCamera does not contain the '{0}' and/or '{1}' component. Selecting a Camera without this component wil break stereoscopic video rendering. The ClearVR SDK will attempt to automatically find the correct Camera object. To silence this warning, please specify the correct platformOptions.renderCamera manually and make sure that there is only one GameObject tagged as MainCamera in your scene(s).", componentName, component2Name));
						renderCamera = null; // Force auto-detect.
					}
				}
				if(renderCamera == null) {
					if (pvr_UnitySDKHeadTrackType != null) {
						Camera[] allCams = GameObject.FindObjectsOfType<Camera>();
						foreach (Camera cam in allCams) {
							var picoAttempt = cam.gameObject.GetComponent(pvr_UnitySDKHeadTrackType);
							if (picoAttempt != null) {
								renderCamera = cam;
								break;
							}
						}
					}
					// If still not found, check for second component
					if (renderCamera == null && pvr_UnitySDKEyeManagerType != null) {
						Camera[] allCams = GameObject.FindObjectsOfType<Camera>();
						foreach (Camera cam in allCams) {
							var picoAttempt = cam.gameObject.GetComponent(pvr_UnitySDKEyeManagerType);
							if (picoAttempt != null) {
								renderCamera = cam;
								break;
							}
						}
					}
				}
			} // else: textureBlitMode is explicitly set to something other than Default.
#endif
			if (renderCamera == null) {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR // Mobfish supports Android and iOS
				if(deviceParameters.deviceType.GetIsMobfishDevice()) {
					Type cardboardPostCameraType = Utils.GetType("MobfishCardboard.CardboardPostCamera");
					if (cardboardPostCameraType != null) {
						Camera[] allCams = GameObject.FindObjectsOfType<Camera>();
						foreach (Camera cam in allCams) {
							var mobfishAttempt = cam.gameObject.GetComponent(cardboardPostCameraType);
							if (mobfishAttempt != null) {
								renderCamera = cam;
							}
						}
					}
				}
#endif
			}
#if UNITY_2019_3_OR_NEWER
			if(renderCamera == null) {
				// Perhaps an XRRig is in use?
				Type xrRigType = Utils.GetType("UnityEngine.XR.Interaction.Toolkit.XRRig"); // This is a monobehaviour
				if(xrRigType != null) { // Can't pass null in FindObjectOfType()
					var xrRigObject = GameObject.FindObjectOfType(xrRigType);
					if(xrRigObject != null) {
						if(xrRigObject is MonoBehaviour) {
							MonoBehaviour xrRigMB = (MonoBehaviour)xrRigObject;
							var camera = xrRigMB.GetComponentInChildren<Camera>();
							if(camera != null) {
								renderCamera = camera;
							}
						}
					}
				}
			}
#endif
			if(renderCamera == null) {
				// Perhaps the main camera was not yet active when the PlatformOptions were constructed? We do one more final attempt at finding it.
				renderCamera = Camera.main; // Might still be null.
			}
			if (renderCamera == null) {
				// Our shaders rely on the camera position nowadays, so we need the "main" camera.
				UnityEngine.Debug.LogError("[ClearVR] No renderCamera specified. By default, platformOptions.renderCamera is set to Camera.main, but no camera tagged as such was found in the scene. Please override platformOptions.renderCamera manually.");
				return false;
			}

			if (trackingTransform == null) {
				// Now that we always enforce renderCamera to be != null, we can use it as an auto-detect mechanism for the trackingTransform if it is not set.
				trackingTransform = renderCamera.transform;
			}
			if (licenseFileBytes == null) {
				UnityEngine.Debug.LogError("[ClearVR] No license file data specified.");
				return false;
			} else if (licenseFileBytes.Length == 0) {
				UnityEngine.Debug.LogError("[ClearVR] License file data is zero bytes.");
				return false;
			}
			if (prepareContentParameters == null) {
				UnityEngine.Debug.LogError("[ClearVR] platformOptions.prepareContentParameters == null, which is not allowed. Please see this field's documentation on how to configure it.");
				return false;
			}
			if (prepareContentParameters.audioTrackAndPlaybackParameters == null) {
				// This should never happen as we fill a default value in the PrepareContentParameters constructor.
				UnityEngine.Debug.LogError("[ClearVR] platformOptions.prepareContentParameters.audioTrackAndPlaybackParameters == null, which is not allowed. Please see this field's documentation for the correct default value.");
				return false;
			}
			if(prepareContentParameters.contentItem == null) {
				UnityEngine.Debug.LogError("[ClearVR] platformOptions.prepareContentParameters.contentItem == null, which is not allowed. Please see this field's documentation for the correct default value.");
				return false;
			}
			if (initialAudioGain != ClearVRConstants.AUDIO_MAINTAIN_GAIN) {
				// We override the default gain on audioTrackAndPlaybackParameters
				// This can be removed once we have proper audio track control.
				PrepareContentParameters _prepareContentParameters = new PrepareContentParameters(prepareContentParameters.contentItem,
					prepareContentParameters.timingParameters,
					prepareContentParameters.layoutParameters);
				_prepareContentParameters.audioTrackAndPlaybackParameters = prepareContentParameters.audioTrackAndPlaybackParameters.Clone(initialAudioGain); /* This value is ignored, we use the muteState value instead, see also below */
				_prepareContentParameters.timeoutInMilliseconds = prepareContentParameters.timeoutInMilliseconds;
				_prepareContentParameters.approximateDistanceFromLiveEdgeInMilliseconds = prepareContentParameters.approximateDistanceFromLiveEdgeInMilliseconds;
				_prepareContentParameters.syncSettings = prepareContentParameters.syncSettings;
				prepareContentParameters = _prepareContentParameters;
			}
			if(muteState != 0) {
				// else: muteState is explicitly set.
				// We check if the muteState matches the initialAudioGain.
				// If not, we assume that the initialAudioGain was intentionally changed and override the muteState based on it.
				// As these values cross language bridges, there might be slight rounding errors we want to ignore.
				if(muteState >= -2 && muteState <= -1) {
					// muted
					if(muteState + 2 - initialAudioGain > 0.01) {
						// we were muted, but the initialAudioGain was changed. This means that we are going to honor the initialAudioGain AND unmute.
						muteState = 1 + initialAudioGain;
					} // else: values match sufficiently, no change
				} else if(muteState >= 1 && muteState <= 2) {
					if(muteState - 1 - initialAudioGain > 0.01) {
						// we were NOT muted, and the initialAudioGain was changed. This means that we are going to honor the initialAudioGain while remaning NOT muted.
						muteState = 1 + initialAudioGain;
					} // else: values match sufficiently, no change
				}
			} // else; no muteState set. We rely on the MediaFLow to exhibit default behaviour.
				
#if UNITY_ANDROID && !UNITY_EDITOR && UNITY_2018_2_OR_NEWER
			// useScriptableRenderPipelineBatching was introduced in Unity 2018
			if(Utils.GetRenderPipelineType() == RenderPipelineTypes.UniversalPipeline) {
				if(UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching) {
					UnityEngine.Debug.LogWarning("[ClearVR] SRP batching is not fully compatible with GLSL shaders in Unity 2019+. Be sure to disable SRP batching yourself, it will now be disabled automatically.");
					UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching = false;
				}
			}
#endif
#pragma warning disable 0618 // Silence deprecated API usage warning
			if (cameraAndContentPlacementMode == CameraAndContentPlacementModes.Default) {
				// Remember that the correct value is set above.
				UnityEngine.Debug.LogError("[ClearVR] CameraAndContentPlacementMode == CameraAndContentPlacementModes.Default which is not allowed. This indicates a bug in the ClearVR SDK and should be reported.");
				return false;
			}
			if (/* return bool as byte */ NativeRendererPluginBase.CVR_NRP_GetIsTextureBlitModeSupported(NRPBridgeTypesMethods.GetNRPBridgeType(), RenderAPITypesMethods.GetNRPGraphicsRendererType(), this.textureBlitMode, this.contentProtectionRobustnessLevel) != 1) {
				UnityEngine.Debug.LogError(String.Format("[ClearVR] graphics API {0} with TextureBlitMode {1} on platform {2} and content protection robustness level {3} is not supported.", RenderAPITypesMethods.GetNRPGraphicsRendererType(), this.textureBlitMode, this.platform, this.contentProtectionRobustnessLevel));
				return false;
			}
			if(contentProtectionRobustnessLevel == ContentProtectionRobustnessLevels.HWSecureAll) {
#if UNITY_ANDROID && !UNITY_EDITOR
				if(textureBlitMode != TextureBlitModes.OVROverlayZeroCopy) {
					UnityEngine.Debug.LogError(String.Format("[ClearVR] contentProtectionRobustnessLevel = .HWSecureAll is supported on Android, but *only* on Oculus Android headsets and when setting textureBlitMode = .OVROverlayZeroCopy. It is currently set to: '{0}', which is not supported.", textureBlitMode));
				} // else: supported!
#else
				UnityEngine.Debug.LogError("[ClearVR] contentProtectionRobustnessLevel = .HWSecureAll is not supported on this platform, it is *only* on Oculus Android headsets and when setting textureBlitMode = .OVROverlayZeroCopy.");
#endif
				return false;
			}
#pragma warning restore 0618 // Unsilence deprecated API usage warning
			return true;
		}

		/// <summary>Return a protobuf representation of the initialize parameters</summary>
		internal cvri.InitializeParametersMediaFlow ToCoreProtobuf() {
#pragma warning disable 0618
			cvri.InitializeParametersMediaFlow coreProtobuf = new cvri.InitializeParametersMediaFlow() {
				CreateContextParams = new cvri.CreateContextParams() {
					PersistenceFolderPath = "",
					DeviceParams = deviceParameters.ToCoreProtobuf(),
                    SDKType = ClearVRPlayer.SDK_TYPE.ToCoreProtobuf()
				},
				HttpProxyParamsMediaFlow = httpProxyParameters.ToCoreProtobuf(),
				HttpsProxyParamsMediaFlow = httpsProxyParameters.ToCoreProtobuf(),
				MuteState = muteState,
				AllowDecoderContraintsInducedStereoToMono = allowDecoderContraintsInducedStereoToMono,
				OverrideUserAgent = overrideUserAgent,
				ContentProtectionRobustnessLevel = contentProtectionRobustnessLevel.ToCoreProtobuf()
			};
#pragma warning restore 0618
			if(telemetryConfiguration != null) {
				coreProtobuf.CreateContextParams.TelemetryConfig = telemetryConfiguration.ToCoreProtobuf();
			}
			return coreProtobuf;
		}

		public void Print() {
			Utils.LongLog(ToString());
		}

		public override String ToString() {
#pragma warning disable 0618
			String returnValue = " \n" + String.Format(@"
************ ClearVR Platform Options ************
Is verify called:                                {0}
Runtime Platform:                                {1}
License file bytes (length):                     {2}
Preferred RenderMode:                            {3} 
Auto play:                                       {4}
Loop content:                                    {5}
Application focus and pause handling:            {6}
Enable automatic render mode switching:          {7}
Device parameters:                               {8}
Initialize Flags:                                {9}
Tracking transform:                              {10}
HTTP proxy parameters:                           {11}
HTTPS proxy parameters:                          {12}
Render camera:                                   {13}
Texture blit mode:                               {14}
Colorspace:                                      {15}
VR API Type:                                     {16}
OVR overlay options:                             {17}
Content protection robustness level:             {18}
Camera  and content placement mode:              {19}
Override user agent:                             '{20}'
Initial audio gain:                              {21}
Enable ABR:                                      {22}
ABR start mode:                                  {23}
Allow decoder contraints induced stereo to mono: {24}
Prepare content parameters:                      {25}
Render pipeline type                             {26}
LoggingConfiguration:							 {27}",
					_isVerifyCalled,
					platform,
					licenseFileBytes != null ? licenseFileBytes.Length : 0,
					preferredRenderMode,
					autoPlay,
					loopContent,
					applicationFocusAndPauseHandling,
					enableAutomaticRenderModeSwitching,
					deviceParameters,
					initializeFlags,
					trackingTransform,
					httpProxyParameters,
					httpsProxyParameters,
					renderCamera,
					textureBlitMode,
					overrideColorSpace,
					vrAPIType,
					ovrOverlayOptions,
					contentProtectionRobustnessLevel,
					cameraAndContentPlacementMode,
					overrideUserAgent,
					initialAudioGain,
					enableABR,
					abrStartMode,
					allowDecoderContraintsInducedStereoToMono,
					prepareContentParameters,
					Utils.GetRenderPipelineType(),
					ClearVRPlayer.loggingConfig.ToString());
#pragma warning restore 0618
			return returnValue;
		}
	}
	/// <summary>
	/// Android platform specific platform options.
	/// Currently, there are NO additional options/fields to set.
	/// </summary>
	public class PlatformOptionsAndroid : PlatformOptionsBase {
#if CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS
		/// <summary>
		/// The minimum OVR Plugin version we officially support. Older versions might work but milage may vary.
		/// </summary>
		private static System.Version MINIMUM_OVR_PLUGIN_VERSION_SUPPORTED = new System.Version(1, 59, 0);
#endif
		public new RuntimePlatform platform = RuntimePlatform.Android;
		/// <summary>
		/// Enables OES Fast Path mode. Video frames are directly streamed from the decoder onto the mesh, skipping one texture copy. An important limitation is that this mode does not support picture-in-picture.
		/// </summary>
		[Obsolete("This field is obsolete and will be removed no later than 2019-12-31. Please see platformOptions.textureBlitMode for details.", false)]
		public bool isUseOESFastPathEnabled = false;
		/// <summary>
		/// Verifies if Android platform specific options are valid or not.
		/// </summary>
		/// <returns>true if everything is OK, false otherwise.</returns>
		public override bool Verify(ClearVRLayoutManager argClearVRLayoutManager) {
#if UNITY_ANDROID && !UNITY_EDITOR
			// We need this check BEFORE calling the base verifyas sprite meshes are only supported in TextureBlitModes.UVShufflingCopy on Android
			if(prepareContentParameters != null && prepareContentParameters.layoutParameters != null) {
				if(prepareContentParameters.layoutParameters.GetIsAnyDisplayObjectControllerSprite()) {
					if(textureBlitMode == TextureBlitModes.Default) {
						textureBlitMode = TextureBlitModes.UVShufflingCopy;
					} else if(textureBlitMode != TextureBlitModes.UVShufflingCopy) {
						UnityEngine.Debug.LogError(String.Format("[ClearVR] Android only: platformOptions.textureBlitMode = `{0}`, which does not support sprite meshes as required by the configured LayoutParameters with name: '{1}'. Please set this option to TextureBlitModes.Default (or TextureBlitModes.UVShufflingCopy explicitly for UNITY_ANDROID only) or do not use sprite meshes.", textureBlitMode, prepareContentParameters.layoutParameters.name));
						return false;
					}
				}
			}
#endif
			bool isSuccess = base.Verify(argClearVRLayoutManager);
			// Do extra checks if necessary.
#if CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS
			if(textureBlitMode == TextureBlitModes.OVROverlayCopy) {
					if(vrAPIType != VRAPITypes.OculusVR) {
						UnityEngine.Debug.LogError(String.Format("[ClearVR] TextureBlitModes.OVROverlayCopy is only supported when using the Oculus runtime. VR API {0} is not supported.", vrAPIType));
						return false;
					}
			}
			if(textureBlitMode.GetIsOVROverlayMode()) {
				if(OVRPlugin.version < MINIMUM_OVR_PLUGIN_VERSION_SUPPORTED) {
					// We warn but reluctantly allow older versions of the OVR runtime.
					UnityEngine.Debug.LogWarning(String.Format("[ClearVR] OVRPlugin version {0} detected, but version {1} or newer required. Consider upgrading your OVRPlugin.", OVRPlugin.version, MINIMUM_OVR_PLUGIN_VERSION_SUPPORTED));
				}
			}
#else
			if(textureBlitMode.GetIsOVROverlayMode()) {
				UnityEngine.Debug.LogError(String.Format("[ClearVR] {0} requires the ClearVR Oculus VR Runtime extensions to be enabled. Please do so through the ClearVR menu in the Unity Editor.", textureBlitMode));
				return false;
			}
#endif
			return isSuccess;
		}
	}

	/// <summary>
	/// PC (Linux/Windows) platform specific platform options.
	/// Currently, there are NO additional options/fields to set.
	/// </summary>
	public class PlatformOptionsPC : PlatformOptionsBase {
		public new RuntimePlatform platform = Application.platform;
		public override bool Verify(ClearVRLayoutManager argClearVRLayoutManager) {
			bool isSuccess = base.Verify(argClearVRLayoutManager);
			if (!isSuccess) {
				return isSuccess;
			} // else: base was successfully verified
			  // Do extra checks if necessary.
			return isSuccess;
		}
	}
	/// <summary>
	/// iOS platform specific platform options.
	/// Currently, there are NO additional options/fields to set.
	/// </summary>
	public class PlatformOptionsIOS : PlatformOptionsBase {
		public new RuntimePlatform platform = RuntimePlatform.IPhonePlayer;
		[Obsolete("This field is obsolete and will be removed no later than 2019-12-31. Please see platformOptions.textureBlitMode for details.", false)]
		public bool isUseOESFastPathEnabled = true; // default true, do not change as non OES Fast Path mode is not supported.

        /// <summary>
        /// Verifies if IOs platform specific options are valid or not.
        /// </summary>
        /// <returns>true if everything is OK, false otherwise.</returns>
        public override bool Verify(ClearVRLayoutManager argClearVRLayoutManager) {
            bool isSuccess = base.Verify(argClearVRLayoutManager);
            // FIX for #4862. Legacy mode is fundamentally incompatible with iOS.
            if(applicationFocusAndPauseHandling == ApplicationFocusAndPauseHandlingTypes.Legacy) {
                applicationFocusAndPauseHandling = ApplicationFocusAndPauseHandlingTypes.Recommended;
            }
            return isSuccess;
        }
    }

	/// <summary>
	/// Holds OVROverlay playback specific settings.
	/// </summary>
	public class OVROverlayOptions {
		/// <summary>
		/// Specifies the composition depth of the video texture.<br/>
		/// Default value: 0.
		/// </summary>
		public int videoCompositionDepth = 0;
		/// <summary>
		/// Specify the preferred reserved OVROverlay indices. By default, the last two allowed indices (index 13 and 14 (0-based)) are selected.
		/// If you want to modify the composition depth, one should set videoCompositionDepth.<br/>
		/// Notes:
		/// 1. You MUST make sure that your application will NEVER use these two OVROverlay layer indices. In practice, this means that you are limited to using up to 12 OVROverlays yourself.
		/// </summary>
		public Int32[] reservedIndices = new Int32[2] { 13, 14 };
		/// <summary>
		/// Disable depth buffer testing on video OVROverlay. Default value: false <br/>
		/// Notes:
		/// 1. it is strongly recommended to not change this default value.
		/// </summary>
		public bool noDepthBufferTesting = false;
		/// <summary>
		/// Configure whether the video should be rendered as an Overlay (true) or Underlay (false). As Underlay mode has a severe performance penalty, Overlay is strongly recommended. <br/>
		/// Please refer to the documentation at https://developer.oculus.com/documentation/unity/unity-ovroverlay/#understanding-ovroverlay-script-configurations for details. <br/>
		/// Default value: true
		/// > [!NOTE]
		/// > You can change this value on the fly by setting [ClearVRDisplayObjectControllerOVROverlay.isOverlay](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerOVROverlay)
		/// </summary>
		public bool isOverlay = true;
	}

	/// <summary>
	/// An enum describing the format of the currently playing content.
	/// </summary>
	public enum ContentFormat : Int32 {
		// The order of the StringValues is shorthand description (sbs), longhand description (side-by-side), if applicable as the core defaults to the former, not the latter.
		/// <summary> The format is unknown and not supported.</summary>
		[StringValues(new String[] {"unknown"})]
		Unknown = 0,
		/// <summary> Monosopic, 180 degree ClearVR content. </summary>
		[StringValues(new String[] {"180-cubemap-mono"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use MonoscopicOmnidirectional instead.", true)]
		Monoscopic180 = 1,
		/// <summary> Monosopic, 180 degree tranditional equirectangular content. </summary>
		[StringValues(new String[] {"180-erp-mono"})]
		MonoscopicERP180 = 2,
		/// <summary> Monosopic, 180 degree equisolid fishEye content. </summary>
		[StringValues(new String[] {"fish-eye-equisolid-mono"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use MonoscopicFishEye instead.", true)]
		MonoscopicFishEyeEquiSolid180 = 3,
		/// <summary> Stereoscopic side-by-side, 180 degree equisolid fishEye content. </summary>
		[StringValues(new String[] {"fish-eye-equisolid-stereo-sbs", "fish-eye-equisolid-stereo-side-by-side"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use StereoscopicFishEyeSBS instead.", true)]
		StereoscopicFishEyeEquiSolid180SBS = 4,
		/// <summary> Monosopic, 180 degree equidistant fishEye content. </summary>
		[StringValues(new String[] {"fish-eye-equidistant-mono"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use MonoscopicFishEye instead.", true)]
		MonoscopicFishEyeEquidistant180 = 5,
		/// <summary> Stereoscopic side-by-side, 180 degree equidistant fishEye content. </summary>
		[StringValues(new String[] {"fish-eye-equidistant-stereo-sbs", "fish-eye-equidistant-stereo-side-by-side"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use StereoscopicFishEyeSBS instead.", true)]
		StereoscopicFishEyeEquidistant180SBS = 6,
		/// <summary> Monosopic, 360 degree ClearVR content. </summary>
		[StringValues(new String[] {"360-cubemap-mono"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use MonoscopicOmnidirectional instead.", true)]
		Monoscopic360 = 7,
		/// <summary> Monosopic, 360 degree tranditional equirectangular content. </summary>
		[StringValues(new String[] {"360-erp-mono"})]
		MonoscopicERP360 = 8,
		/// <summary> Stereoscopic, 180 degree ClearVR content.</summary>
		[StringValues(new String[] {"180-cubemap-stereo"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use StereoscopicOmnidirectional instead.", true)]
		Stereoscopic180 = 9,
		/// <summary> Stereoscopic side-by-side, 180 degree tranditional equirectangular content. </summary>
		[StringValues(new String[] {"180-erp-stereo-sbs", "180-erp-stereo-side-by-side"})]
		StereoscopicERP180SBS = 10,
		/// <summary> Stereoscopic, 360 degree ClearVR content.</summary>
		[StringValues(new String[] {"360-cubemap-stereo"})]
		[Obsolete("This enum case has been deprecated and can no longer be used. Use StereoscopicOmnidirectional instead.", true)]
		Stereoscopic360 = 11,
		/// <summary> Stereoscopic top-bottom, 360 degree tranditional equirectangular content. </summary>
		[StringValues(new string[] {"360-erp-stereo-tb", "360-erp-stereo-top-bottom"})]
		StereoscopicERP360TB = 12,
		/// <summary> Monosopic, ultra-wide and ultra-high resolution "flat" content. </summary>
		[StringValues(new string[] {"planar"})]
		Planar = 13,
		/// <summary> Monosopic, traditional broadcast content. </summary>
		[StringValues(new string[] {"rectilinear"})]
		MonoscopicRectilinear = 14,
		/// <summary> Stereoscopic top-bottom, traditional broadcast content. </summary>
		[StringValues(new string[] {"rectilinear-stereo-tb", "rectilinear-stereo-top-bottom"})]
		StereoscopicRectilinearTB = 15,
		/// <summary> Monosopic, fishEye content. </summary>
		[StringValues(new string[] {"fish-eye-mono"})]
		MonoscopicFishEye = 16,
		/// <summary> Stereoscopic side-by-side, fishEye content. </summary>
		[StringValues(new String[] {"fish-eye-stereo-sbs", "fish-eye-stereo-side-by-side"})]
		StereoscopicFishEyeSBS = 17,
		/// <summary> Stereoscopic sibe-by-side, traditional broadcast content. </summary>
		[StringValues(new String[] {"rectilinear-stereo-sbs", "rectilinear-stereo-side-by-side"})]
		StereoscopicRectilinearSBS = 18,
		/// <summary> MOnoscopic ClearVR content.</summary>
		[StringValues(new String[] {"omnidirectional-mono"})]
		MonoscopicOmnidirectional = 19,
		/// <summary> Stereoscopic ClearVR content.</summary>
		[StringValues(new String[] {"omnidirectional-stereo"})]
		StereoscopicOmnidirectional = 20
	}

	static class ContentFormatMethods {
		public static string[] GetStringValues(this ContentFormat value) {
			Type type = value.GetType();
			FieldInfo fieldInfo = type.GetField(value.ToString());

			StringValuesAttribute[] attribs = fieldInfo.GetCustomAttributes(typeof(StringValuesAttribute), false) as StringValuesAttribute[];

			return attribs.Length > 0 ? attribs[0].StringValues : null;
		}

		public static ContentFormat FromStringValue(string value) {
			foreach (ContentFormat _value in Enum.GetValues(typeof(ContentFormat))) {
				String[] stringValues = _value.GetStringValues();
				if(stringValues != null) {
					foreach(String stringValue in stringValues) {
						if (stringValue == value) {
							return _value;
						}
					}
				} else {
					throw new Exception(String.Format("[ClearVR] ContentFormat %s does not have a StringValues attribute. Please contact Tiledmedia to report this issue.", value));
				}
			}
			return ContentFormat.Unknown;
		}

		/// <summary>
		/// Converts the ContentFormat in a matching MeshType.
		/// This is a non-public method used in conjunction with the OVROVerlayOptions.ovrOverlayZeroCopyMeshType field.
		/// Note that Unknown is a perfectly valid return value in this context as we do not support ClearVR content playback in OVROverlayZeroCopy mode.
		/// </summary>
		/// <param name="argContentFormat">The ContentFormat to convert.</param>
		/// <returns>The ClearVRMeshType equivalent.</returns>
		internal static ClearVRMeshTypes GetAsClearVRMeshType(this ContentFormat argContentFormat) {
			switch (argContentFormat) {
				case ContentFormat.Unknown:
				case ContentFormat.MonoscopicOmnidirectional:
				case ContentFormat.StereoscopicOmnidirectional:
					return ClearVRMeshTypes.Unknown;
				case ContentFormat.Planar:
					return ClearVRMeshTypes.Planar;
				case ContentFormat.MonoscopicRectilinear:
				case ContentFormat.StereoscopicRectilinearTB:
				case ContentFormat.StereoscopicRectilinearSBS:
					return ClearVRMeshTypes.Rectilinear;
				case ContentFormat.MonoscopicERP180:
				case ContentFormat.StereoscopicERP180SBS:
					return ClearVRMeshTypes.ERP180;
				case ContentFormat.MonoscopicERP360:
				case ContentFormat.StereoscopicERP360TB:
					return ClearVRMeshTypes.ERP;
				case ContentFormat.MonoscopicFishEye:
				case ContentFormat.StereoscopicFishEyeSBS:
					return ClearVRMeshTypes.FishEye;
				default:
					UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to interpret content format {0} as MeshType. Not implemented, assuming Rectilinear. Please report this issue to Tiledmedia.", argContentFormat));
					return ClearVRMeshTypes.Rectilinear;
			}
		}

		public static bool GetIsStereoscopic(this ContentFormat argContentFormat) {
			return (argContentFormat == ContentFormat.StereoscopicERP180SBS ||
				argContentFormat == ContentFormat.StereoscopicOmnidirectional ||
				argContentFormat == ContentFormat.StereoscopicERP360TB ||
				argContentFormat == ContentFormat.StereoscopicRectilinearTB ||
				argContentFormat == ContentFormat.StereoscopicRectilinearSBS ||
				argContentFormat == ContentFormat.StereoscopicFishEyeSBS);
		}
	}

	/// <summary>
	/// The audio playback engine is the device that routes decoded audio to an output device.
	/// Note that this class is not of any use right now, and is merely provided as a boiler-plate concept for the future.
	/// > [!IMPORTANT]
	/// > One is encouraged NOT to use any of these APIs.
	/// </summary>
	public class AudioPlaybackEngine {
		private AudioPlaybackEngineTypes _audioPlaybackEngineType;
		/// <summary>
		/// Returns the type of the audio playback engine
		/// </summary>
		/// <value>The AudioPlaybackEngineType value of the object.</value>
		public AudioPlaybackEngineTypes audioPlaybackEngineType {
			get {
				return _audioPlaybackEngineType;
			}
		}
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="argType">The type of the audio playback engine to create.</param>
		public AudioPlaybackEngine(AudioPlaybackEngineTypes argType) {
			_audioPlaybackEngineType = argType;
		}

		/// <summary>
		/// Queries the object whether spatial audio playback is support or note.
		/// </summary>
		/// <value>True if spatial audio playback is supported, false otherwise.</value>
		public bool supportsSpatialAudio {
			get {
				// Since sigma audio, we support spatial audio on all platforms.
				switch (_audioPlaybackEngineType) {
					case AudioPlaybackEngineTypes.AndroidDefault:
						return true;
					case AudioPlaybackEngineTypes.IOSDefault:
						return true;
					default:
						return true;
				}
			}
		}
		/// <summary>
		/// A convenience method that returns a default AudioPlaybackEngine object, based on the provided RuntimePlatform.
		/// </summary>
		/// <param name="argPlatform">The RuntimePlatform for which to construct the default AudioPlaybackEngine.</param>
		/// <returns>The default AudioPlaybackEngine for the specified platform, or an AudioPlaybackEngine with type Unknown if platform is not supported.</returns>
		public static AudioPlaybackEngine GetDefaultAudioPlaybackEngineForPlatform(RuntimePlatform argPlatform) {
			switch (argPlatform) {
				case RuntimePlatform.Android:
					return new AudioPlaybackEngine(AudioPlaybackEngineTypes.AndroidDefault);
				case RuntimePlatform.IPhonePlayer:
					return new AudioPlaybackEngine(AudioPlaybackEngineTypes.IOSDefault);
				case RuntimePlatform.WindowsEditor:
				case RuntimePlatform.WindowsPlayer:
					return new AudioPlaybackEngine(AudioPlaybackEngineTypes.WindowsDefault);
				default:
					return new AudioPlaybackEngine(AudioPlaybackEngineTypes.Unknown);
			}
		}
		public static AudioPlaybackEngine GetDefaultAudioPlaybackEngineForPlatform() {
			return GetDefaultAudioPlaybackEngineForPlatform(Application.platform);
		}
		public override String ToString() {
			return String.Format("AudioPlaybackEngineType: {0}, supportsSpatialAudio: {1}", audioPlaybackEngineType, supportsSpatialAudio);
		}
	}

	/// <summary>
	/// Various Audio Playback Engines are defined, yet none of them are in use yet.
	/// One is encouraged to refrain from using this enum.
	/// </summary>
	public enum AudioPlaybackEngineTypes {
		/// <summary>Unknown/unsupported</summary>
		Unknown = -1,
		/// <summary>The default Audio Playback Engine on Android.</summary>
		AndroidDefault = 0,
		/// <summary>The AudioTrack-based Audio Playback Engine on Android.</summary>
		AndroidAudioTrack = 1,
		/// <summary>The OpenSL-based Audio Playback Engine on Android.</summary>
		AndroidOpenSL = 2,
		/// <summary>The default Audio Playback Engine on iOS.</summary>
		IOSDefault = 100,
		/// <summary>The default Audio Playback Engine on Windows.</summary>
		WindowsDefault = 200,
	}

	/// <summary>
	/// The audio decoder is the device that decodes audio samples before playback.
	/// Note that this class is not of any use right now, and is merely provided as a boiler-plate concept for the future.
	/// > [!IMPORTANT]
	/// > One is encouraged NOT to use any of these APIs.
	/// </summary>
	public class AudioDecoder {
		private AudioDecoderTypes _audioDecoderType;
		/// <summary>
		/// Get the audio decoder type of this object.
		/// </summary>
		/// <value>The type</value>
		public AudioDecoderTypes audioDecoderType {
			get {
				return _audioDecoderType;
			}
		}
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="argType">The type to construct.</param>
		public AudioDecoder(AudioDecoderTypes argType) {
			_audioDecoderType = argType;
		}

		/// <summary>
		/// A convenience method that returns a default AudioDecoder object, based on the provided RuntimePlatform.
		/// </summary>
		/// <param name="argPlatform">The RuntimePlatform for which to construct the default AudioDecoder.</param>
		/// <returns>The default AudioDecoder for the specified platform, or an AudioDecoder with type Unknown if platform is not supported.</returns>
		public static AudioDecoder GetDefaultAudioDecoderForPlatform(RuntimePlatform argPlatform) {
			switch (argPlatform) {
				case RuntimePlatform.Android:
					return new AudioDecoder(AudioDecoderTypes.AndroidDefault);
				case RuntimePlatform.IPhonePlayer:
					return new AudioDecoder(AudioDecoderTypes.IOSDefault);
				case RuntimePlatform.WindowsEditor:
				case RuntimePlatform.WindowsPlayer:
					return new AudioDecoder(AudioDecoderTypes.WindowsDefault);
				default:
					return new AudioDecoder(AudioDecoderTypes.Unknown);
			}
		}

		public static AudioDecoder GetDefaultAudioDecoderForPlatform() {
			return GetDefaultAudioDecoderForPlatform(Application.platform);
		}

		public override String ToString() {
			return String.Format("AudioDecoderType: {0}", audioDecoderType);
		}
	}

	/// <summary>
	/// > [!IMPORTANT]
	/// > This is an internal class that should not be used. Public access will be removed after 2020/12/31
	/// </summary>
	public class ClearVRAsyncRequest {
		internal static int CLEAR_VR_REQUEST_ID_NOT_SET = 1;
		static System.Random randomNumberGenerator = new System.Random();
		internal RequestTypes requestType;
		internal int requestId;
		internal object[] optionalArguments;
		internal Action<ClearVREvent, ClearVRPlayer> onSuccess;
		internal Action<ClearVREvent, ClearVRPlayer> onFailure;


		internal ClearVRAsyncRequest(int requestType, int requestId, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			this.requestType = (RequestTypes) requestType;
			this.requestId = requestId;
			this.optionalArguments = optionalArguments;
			this.onSuccess = onSuccess;
			this.onFailure = onFailure;
		}
		internal ClearVRAsyncRequest(int argRequestType, int argRequestId) : this((RequestTypes)argRequestType, argRequestId) {
		}

		internal ClearVRAsyncRequest(RequestTypes argRequestType, int argRequestId) {
			requestType = argRequestType;
			requestId = argRequestId;
		}
		internal void UpdateActionAndOptionalArguments(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments) {
			this.onSuccess = onSuccess;
			this.onFailure = onFailure;
			this.optionalArguments = optionalArguments;
		}

		internal ClearVRAsyncRequest(RequestTypes argRequestType) : this(argRequestType, randomNumberGenerator.Next(1000, Int32.MaxValue)) {
		}

		public override String ToString() {
			return String.Format("Request id: {0}, request type: {1}, optional argument count: {2}", requestId, requestType, (optionalArguments != null) ? optionalArguments.Length.ToString() : 0.ToString());
		}
	}

	internal class ClearVRAsyncRequestResponse {
		public RequestTypes requestType;
		public int requestId;
		public object[] optionalArguments;
		public ClearVRAsyncRequestResponse(RequestTypes argRequestType, int argRequestId, params object[] argOptionalArguments) {
			requestType = argRequestType;
			requestId = argRequestId;
			optionalArguments = argOptionalArguments;
		}

		public override String ToString() {
			return String.Format("Request id: {0}, request type: {1}, optional argument count: {2}", requestId, requestType, (optionalArguments != null) ? optionalArguments.Length.ToString() : 0.ToString());
		}
	}
	/// <summary>
	/// Various AudioDecoders are defined, yet none of them are in use yet.
	/// One is encouraged to refrain from using this enum.
	/// </summary>
	public enum AudioDecoderTypes {
		Unknown = -1,
		AndroidDefault = 0,
		AndroidMediaCodec = 1,
		IOSDefault = 0,
		WindowsDefault = 0,
	}

	/// <summary>
	/// An internal enum that is not of any use to the integrator.
	/// </summary>
    public enum DisplayObjectDescriptorFlags : UInt32 {
		/// <summary>
		/// Unknown is a valid value, returned when ClearVRDisplayObjectControllerBase::UpdateMesh() cannot complete because the mesh is (no longer) initialized.
		/// </summary>
        Unknown = 0,
        Created = 1 << 0, // 1
        MeshUpdated = 1 << 1, // 2
        MeshChanged = 1 << 2, // 4
		TextureLatched = 1 << 3, // 8
		TextureUpdated = 1 << 4, // 16
		TextureChanged = 1 << 5, // 32
		ActiveStateChanged = 1 << 6, // 64
		FeedIndexChanged = 1 << 7, // 128
		ClassTypeChanged = 1 << 8, // 256
		ShaderParameterChanged = 1 << 9, // 512
		LateVertexUpload = 1 << 10, // 1024
		LateTextureLatch = 1 << 11 // 2048
    };

	static class DisplayObjectDescriptorFlagsMethods {
		public static String GetAsPrettyString(UInt32 argFlags) {
			StringBuilder sb = new StringBuilder();
			var values = Enum.GetValues(typeof(DisplayObjectDescriptorFlags));
			foreach(var value in values) {
				if((argFlags & (UInt32)value) != 0) {
					sb.Append(value);
					sb.Append(",");
				}
			}
			return sb.ToString().TrimEnd(',');
		}

		public static String GetAsPrettyString(this DisplayObjectDescriptorFlags argFlags) {
			StringBuilder sb = new StringBuilder();
			var values = Enum.GetValues(typeof(DisplayObjectDescriptorFlags));
			foreach(var value in values) {
				if(((UInt32) argFlags & (UInt32)value) != 0) {
					sb.Append(value);
					sb.Append(",");
				}
			}
			return sb.ToString().TrimEnd(',');
		}

		public static bool IsUnknown(this DisplayObjectDescriptorFlags flag) {
			return flag ==  DisplayObjectDescriptorFlags.Unknown;
		}
		public static bool HasFlag(this DisplayObjectDescriptorFlags mdFlag, DisplayObjectDescriptorFlags flagToTest) {
			return ((UInt32)mdFlag & (UInt32) flagToTest) != 0;
		}
		public static bool HasCreated(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.Created);
		}
		public static bool HasTextureChanged(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.TextureChanged);
		}
		public static bool HasTextureUpdated(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.TextureUpdated);
		}
		public static bool HasTextureLatched(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.TextureLatched);
		}
		public static bool HasMeshChanged(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.MeshChanged);
		}
		public static bool HasMeshUpdated(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.MeshUpdated);
		}
		public static bool HasActiveStateChanged(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.ActiveStateChanged);
		}
		public static bool HasFeedIndexChanged(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.FeedIndexChanged);
		}
		public static bool HasClassTypeChanged(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.ClassTypeChanged);
		}
		public static bool HasShaderParameterChanged(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.ShaderParameterChanged);
		}
		public static bool HasLateVertexUpload(this DisplayObjectDescriptorFlags flag) {
			return flag.HasFlag(DisplayObjectDescriptorFlags.LateVertexUpload);
		}
	}


	/// <summary>
	/// An internal enum that is not of any use to the integrator.
	/// </summary>
    public enum MeshTextureModes : Int32 {
		/// <summary>
		/// Unknown is a forbidden value
		/// </summary>
        Unknown = 0,
		/// <summary>
		/// Normal texture rendering mode. The ClearVRPlayer is in full control over the mesh and its updates.
		/// </summary>
        UVShuffling = 1,
		/// <summary>
		/// OVR overlay texture rendering mode. Only available on Android
		/// </summary>
        OVROverlay = 2,
		/// <summary>
		/// Sprite texture rendering mode.
		/// </summary>
        Sprite = 3,
		/// <summary>
		/// Unmanaged mesh rendering mode. The customer has full control over the mesh. This mode does not support ClearVR content playback. 
		/// </summary>
        UnmanagedMesh = 4
    };

	
	
	[Obsolete("This enum has been deprecated in v9.0. There is no equivalent available. Please remove any references to it from your code base.", true)]
	public enum FallbackCubefaceIdentifier : Int32 {
		Unknown = 0,
		LeftEyeLeft = 1,
		LeftEyeRight = 2,
		LeftEyeTop = 3,
		LeftEyeBottom = 4,
		LeftEyeFront = 5,
		LeftEyeBack = 6,
		/* right eye is currently not signalled */
		RightEyeLeft = 101,
		RightEyeRight = 102,
		RightEyeTop = 103,
		RightEyeBottom = 104,
		RightEyeFront = 105,
		RightEyeBack = 106
	};

	/// <summary>
	/// TextureType is used to indicate how to use the planeIds (what color format is used and how the color planes are arraged)
	/// </summary>
	public enum TextureTypes : Int32 {
		RGBA = 0, //Default texture type. Only plane0 is used and contains interlace RGBA 8 bits colors with resolution frameWidth x frameHeight
		NV12 = 1, //plane0 contains a the luma plane: 8 bits Y of resolution frameWidth x frameHeight. plane1 contains the interlace chromaPlane uv 8 bits with resolution frameWidth/2 x frameHeight/2
		YUV420P = 2 //plane0 contains a the luma plane: 8 bits Y of resolution frameWidth x frameHeight. plane1 contains the chromaPlane u 8 bits with resolution frameWidth/2 x frameHeight/2 plane2 contains the chromaPlane v 8 bits with resolution frameWidth/2 x frameHeight/2
	}


	/// <summary>
	/// ColorSpace is used to indicate the color space used to generate the YUV coordinates. It is used to convert the color back to RGBA. ColorSpace.Unspecified may default to BT.709
	/// </summary>
    public enum ColorSpaceStandards : Int32 {
		Unspecified = 0,
		BT709 = 1,
		BT601 = 2,
		BT2020_NCL = 3,
		BT2020_CL = 4
    };

	/// <summary>
	/// Advanced API.
	/// </summary>
	// [StructLayout(LayoutKind.Sequential)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	[Obsolete("This struct has been deprecated in v9.0. There is no equivalent available. Please remove any references to it from your code base.", true)]
	public struct FallbackCubefaceInfoStruct {
		public FallbackCubefaceIdentifier fallbackCubefaceIdentifier;
		public Int32 topLeftX;
		public Int32 topLeftY;
		public Int32 width;
		public Int32 height;
		public override String ToString() {
			return String.Format("Face ID: {0}, x: {1}, y: {2}, w: {3}, h: {4}", fallbackCubefaceIdentifier, topLeftX, topLeftY, width, height);
		}
	};

	/// <summary>
	/// Advanced enum, do not use.
	/// </summary>
	[Serializable]
	[Obsolete("This enum has been deprecated in v9.0. There is no equivalent available. Please remove any references to it from your code base.", true)]
	public enum FallbackLayoutIdentifier : Int32 {
		Unknown,
		CubemapTwoByOne,
		CubemapThreeByTwo
	}

	/// <summary>
	/// Advanced API, do not use.
	/// </summary>
	[Obsolete("This class has been deprecated in v9.0. There is no equivalent available. Please remove any references to it from your code base.", true)]
	public class FallbackLayout {
		public System.Object  /* FallbackLayoutIdentifier */ fallbackLayoutIdentifier;
		public Int32 overlapX;
		public Int32 overlapY;
		#pragma warning disable 0618,0619
		public List<System.Object /* FallbackCubefaceInfoStruct */ > fallbackCubefaceInfoStructs = new List<System.Object /* FallbackCubefaceInfoStruct */ >();
		#pragma warning restore 0618,0619
		public override String ToString() {
			return String.Format("Fallback Layout Type: {0}, overlap x: {1}, overlap y: {2}, faces: {3}", fallbackLayoutIdentifier, overlapX, overlapY, string.Join( ",", fallbackCubefaceInfoStructs.Select(c=>c.ToString()).ToArray<string>()));
		}
	}

	/// <summary>
	/// Devices the type of a mesh. This allows you to infer the actual shape of the mesh
	/// </summary>
    public enum ClearVRMeshTypes : Int32 {
		/// <summary>Unknown mesh type, not allowed.</summary>
        Unknown = 0,
		/// <summary>360 degree cubemap.</summary>
        Cubemap = 1,
		/// <summary>a flat, large quad.</summary>
		Planar = 2,
		/// <summary>180 degree cubemap (half a cube).</summary>
		Cubemap180 = 3,
		/// <summary>360 degree ERP sphere.</summary>
		ERP = 4,
		/// <summary>180 degree ERP semi-sphere.</summary>
        ERP180 = 5,
		/// <summary>Simple rectangular quad.</summary>
        Rectilinear = 6,
		/// <summary>Custom, fish eye compatible mesh.</summary>
		FishEye = 7
    }

	/// <summary>
	/// Devices the type of a mesh. This allows you to infer the actual shape of the mesh
	/// </summary>
    public enum VideoStereoMode : Int32 {
		/// <summary>Unknown stereo type, not allowed.</summary>
        Unknown = 0,
		/// <summary>Monoscipic content</summary>
        Mono = 1,
		/// <summary>Stereo left eye top, right eye bottom.</summary>
		StereoTopBottom = 2,
		/// <summary>Stereo right eye top, left eye bottom.</summary>
		StereoBottomTop = 3,
		/// <summary>Stereo left eye left, right eye right.</summary>
		StereoSideBySide = 4,
		/// <summary>Stereo with a complex UV mapping</summary>
        StereoComplexMapping = 5
    }

	/// <summary>
	/// Various Fish Eye Mesh Types are supported.
	/// </summary>
	public enum ClearVRFishEyeTypes : Int32 {
		/// <summary> Not set: indicate that the Fish Eye parameters were not set. </summary>
		NotSet = 0,
		/// <summary> Equisolid </summary>
		EquiSolid = 1,
		/// <summary> Equidistant </summary>
		EquiDistant = 2,
		/// <summary> Polynomial model </summary>
		Polynomial = 3
    }

	public enum ClearVRFishEyeStereoTypes: Int32 {
  		StereoTypeMono = 0,
		StereoTypeStereoSideBySide = 1,
		StereoTypeStereoTopBottom = 2
	}

	static class ClearVRMeshTypesMethods {
		public static bool GetIsOmnidirectional(this ClearVRMeshTypes argClearVRMeshType) {
			return !(argClearVRMeshType == ClearVRMeshTypes.Planar || argClearVRMeshType == ClearVRMeshTypes.Rectilinear);
		}
		public static bool GetIsPlanar(this ClearVRMeshTypes argClearVRMeshType) {
			return argClearVRMeshType == ClearVRMeshTypes.Planar;
		}
		public static bool GetIsRectilinear(this ClearVRMeshTypes argClearVRMeshType) {
			return argClearVRMeshType == ClearVRMeshTypes.Rectilinear;
		}

		public static InteractionModes GetAsInteractionMode(this ClearVRMeshTypes argClearVRMeshType) {
			if(argClearVRMeshType.GetIsOmnidirectional()) {
				return InteractionModes.OmniDirectional;
			} else if(argClearVRMeshType.GetIsPlanar()) {
				return InteractionModes.Planar;
			} else if(argClearVRMeshType.GetIsRectilinear()) {
				return InteractionModes.Rectilinear;
			}
			return InteractionModes.Unknown;
		}
	}


	/// <summary>
	/// This enum is used to help distinguish between different interaction modes depending on the content type.
	/// </summary>
	public enum InteractionModes {
		Unknown,
		OmniDirectional,
		Planar,
		Rectilinear
	}


	internal enum NRPBridgeTypes : Int32 {
        Unknown = 0,
        // 1 ... 99 reserved for Unity platforms
        UnityAndroid = 1,
        UnityIOs = 2,
        UnityPC = 3,

        // 100 ... 199 reserved for native Android platforms
        NativeAndroid = 100,
        //NativeAndroidGearVRf = 101, // GearVRf support was removed in v8.1

        // 200 ... 299 reserved for native iOS platforms
        NativeIOs = 200

        // 300 ... 399 reserved for native Windows platforms

        // 400 ... 499 reserved for native MacOS-x platforms

        // 500 ... 599 reserved for native Unix platforms
	}
	static class NRPBridgeTypesMethods {
		internal static NRPBridgeTypes GetNRPBridgeType() {
			NRPBridgeTypes nrpBridgeType;
#if UNITY_ANDROID && !UNITY_EDITOR
			nrpBridgeType = NRPBridgeTypes.UnityAndroid;
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            nrpBridgeType = NRPBridgeTypes.UnityIOs;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            nrpBridgeType = NRPBridgeTypes.UnityPC;
#else
            nrpBridgeType = NRPBridgeTypes.Unknown;
#endif
            return nrpBridgeType;
		}
	}


	/// <summary>
	/// This enum specifies the texture blit mode, e.g. how the decoded video frame will be rendered to the screen.
	/// > [!WARNING]
	/// > W.r.t. TextureBlitModes.OVROverlayZeoCopy and when playing Widevine L1 protected content only: <br/>
	/// > Oculus provides two types of plugins: the traditional OVR plugin and the new OVR OpenXR plugin. Both are supported, but some versions of the traditional OVR plugin will spam logcat with the following message:
	/// > `D/OVRPlugin: ovrpLayerFlag_ProtectedContent requested without a protected front-buffer.`. This is a bug in this plugin as your content is being played back in a protected context (you can confirm this by taking a screenshot, your video will show up as black or green).
	/// </summary>
	public enum TextureBlitModes : Int32 {
		Unknown = 0,
		/// <summary>
		/// Pick default option based on platform.
		/// </summary>
		Default = 1,
		/// <summary>
		/// Classic, UV based, shuffling without texture copy ("Fast OES" on Android).
		/// </summary>
		UVShufflingZeroCopy = 2,
		/// <summary>
		/// Classic, UV based, shuffling with texture copy.
		/// Specifically for the Android platform, the following holds:
		/// 1. This mode is also known as "non-fast OES" mode. By default, UVShufflingZeroCopy is used on Android (see also point 3. below).
		/// 2. This mode must be selected when using a third party player at the same time as the ClearVRPlayer.
		/// 3. This mode must be selected when one or multiple [Sprite](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerSprite) meshes are found in the scene.
		/// </summary>
		UVShufflingCopy = 3,
		/// <summary>
		/// Android only. Render the video onto an OVROverlay. This mode is currently NOT supported. 
		/// </summary>
		OVROverlayCopy = 4,
		/// <summary>
		/// Android only. Render the video onto an OVROverlay. This mode only supports traditional (non-ClearVR) video playback like rectilinear, ERP360, ERP180 and cubemap. It does allow you to play Widevine L1 protected content on Oculus VR devices like the Quest 1 and Quest 2.
		/// </summary>
		OVROverlayZeroCopy = 5
	}

	static class TextureBlitModesMethods {
		public static bool GetIsOVROverlayMode(this TextureBlitModes argTextureBlitMode) {
			return argTextureBlitMode == TextureBlitModes.OVROverlayCopy || argTextureBlitMode == TextureBlitModes.OVROverlayZeroCopy;
		}
	}

	/// <summary>
	/// Specifies the Content Protection Robustness Level required to play encrypted content.
	/// </summary>
	[Obsolete("Content protection level does not need to be set anymore, it will be configured automatically. It will be removed after 29/01/2024.", false)]
	public enum ContentProtectionRobustnessLevels : Int32 {
		/// <summary>
		/// No content protection required.
		/// </summary>
		Unprotected = cvri.ContentProtectionRobustnessLevel.Unprotected,
		/// <summary>
		/// On Android: do not use. See SWSecureDecode if targeting Widevine L3
		/// </summary>
		SWSecureCrypto = cvri.ContentProtectionRobustnessLevel.SwSecureCrypto,
		/// <summary>
		/// On Android, this would equal to Widevine L3
		/// </summary>
		SWSecureDecode = cvri.ContentProtectionRobustnessLevel.SwSecureDecode,
		/// <summary>
		/// On Android, this is equal to Widevine L2 which is NOT supported.
		/// </summary>
		HWSecureCrypto = cvri.ContentProtectionRobustnessLevel.HwSecureCrypto,
		/// <summary>
		/// On Android, do not use. See HWSecureAll instead.
		/// </summary>
		HWSecureDecode = cvri.ContentProtectionRobustnessLevel.HwSecureDecode,
		/// <summary>
		/// On Android, thsi would equal to Widevine L1.
		/// </summary>
		HWSecureAll = cvri.ContentProtectionRobustnessLevel.HwSecureAll
	}

	static class ContentProtectionRobustnessLevelsMethods {
#pragma warning disable 0618 // Unsilence deprecated API usage warning
		public static cvri.ContentProtectionRobustnessLevel ToCoreProtobuf(this ContentProtectionRobustnessLevels argContentProtectionRobustnessLevel) {
			// This cast is allowed
			return (cvri.ContentProtectionRobustnessLevel) argContentProtectionRobustnessLevel;
		}
#pragma warning restore 0618 // Unsilence deprecated API usage warning
	}
	/// <summary>
	/// Enum specifying the various RenderAPIs that are supported.
	/// </summary>
	internal enum RenderAPITypes : Int32 {
		///<summary>unknown or not yet detected.</summary>
		Unknown         = 1000,
		///<summary>Direct3D 11.</summary>
        Direct3D11      = 1001,
		///<summary>OpenGL ES 2.0.</summary>
        OpenGLES20      = 1002,
		///<summary>OpenGL ES 3.x.</summary>
        OpenGLES30      = 1003,
        //	PS4               = 1004, // PlayStation 4
        //	XboxOne           = 1005, // Xbox One
		///<summary>Metal.</summary>
        Metal           = 1006,
		///<summary>OpenGL Core.</summary>
        OpenGLCore      = 1007,
		///<summary>Direct3D 12.</summary>
		Direct3D12      = 1008,
        //	Vulkan            = 1009, // Vulkan
        //	Nvn               = 1010, // Nintendo Switch NVN API
        //	XboxOneD3D12      = 1011  // MS XboxOne Direct3D 12
	}

	/// <summary>
	/// Supported color spaces. Currently, only Gamma is supported.
	/// </summary>
	public enum ColorSpaces : Int32 {
		/// <summary> Not yet specified and/or auto-detected.</summary>
		Uninitialized = 0,
		/// <summary> Regular Gamma.</summary>
		Gamma   = 1,
		/// <summary> Linear (not supported).</summary>
		Linear  = 2,
	}

	static class ColorSpacesMethods {
		public static ColorSpaces ConvertUnityColorSpace(ColorSpace argColorSpace) {
			switch(argColorSpace) {
				case ColorSpace.Uninitialized:
					return ColorSpaces.Uninitialized;
				case ColorSpace.Gamma:
					return ColorSpaces.Gamma;
				case ColorSpace.Linear:
					return ColorSpaces.Linear;
				default:
					throw new Exception(String.Format("[ClearVR] Unity color space {0} is not supported. Please report this issue to Tiledmedia.", QualitySettings.activeColorSpace));
			}
#pragma warning disable 0162
			return ColorSpaces.Gamma; // Assume Gamma by default.
#pragma warning restore 0162
		}

		public static ColorSpaces ConvertActiveUnityColorSpace() {
			return ConvertUnityColorSpace(QualitySettings.activeColorSpace);
		}
	}

	/// <summary>
	/// VRAPITypes enum, currently, only OculusVR is supported.
	/// > [!WARNING]
	/// > This enum is of no use to the integrator and might repurposed, changed and or removed without notice. Do not use.
	/// </summary>
	public enum VRAPITypes : Int32 {
		Unknown = 0,
		OculusVR = 1,
		OpenVR = 2
	}

	static class RenderAPITypesMethods {
		internal static RenderAPITypes GetNRPGraphicsRendererType() {
			RenderAPITypes renderAPIType = RenderAPITypes.Unknown;
			#pragma warning disable 0108 // Ignore deprecated UnityEngine.Rendering.GraphicsDeviceType.*
			switch(SystemInfo.graphicsDeviceType) {
		        case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
					renderAPIType = RenderAPITypes.OpenGLES20;
					break;
		        case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
					renderAPIType = RenderAPITypes.OpenGLES30;
					break;
		        case UnityEngine.Rendering.GraphicsDeviceType.Metal:
					renderAPIType = RenderAPITypes.Metal;
					break;
		        case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
					renderAPIType = RenderAPITypes.OpenGLCore;
					break;
		        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                    renderAPIType = RenderAPITypes.Direct3D11;
                    break;
                case UnityEngine.Rendering.GraphicsDeviceType.Null:
		        case UnityEngine.Rendering.GraphicsDeviceType.PlayStation4:
		        case UnityEngine.Rendering.GraphicsDeviceType.XboxOne:
		        case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                    renderAPIType = RenderAPITypes.Direct3D12;
                    break;
                case UnityEngine.Rendering.GraphicsDeviceType.Vulkan:
		        case UnityEngine.Rendering.GraphicsDeviceType.Switch:
		        case UnityEngine.Rendering.GraphicsDeviceType.XboxOneD3D12:
					break;
				/* These are deprecated and in general not supported by ClearVR */
        		// case UnityEngine.Rendering.GraphicsDeviceType.OpenGL2:
		        // case UnityEngine.Rendering.GraphicsDeviceType.Direct3D9:
		        // case UnityEngine.Rendering.GraphicsDeviceType.PlayStation3:
        		// case UnityEngine.Rendering.GraphicsDeviceType.Xbox360:
        		// case UnityEngine.Rendering.GraphicsDeviceType.PlayStationVita:
		        // case UnityEngine.Rendering.GraphicsDeviceType.PlayStationMobile:
		        // case UnityEngine.Rendering.GraphicsDeviceType.N3DS:
			}
			#pragma warning restore 0108

			return renderAPIType;
		}
	}
	[Serializable]// Do not use Pack = 1, it will break marshalling of the struct between C++ and C#
	internal struct CVRNRPLoadParametersStruct {
#pragma warning disable 414
        NRPBridgeTypes nrpBridgeType;
        TextureBlitModes nrpTextureRenderType;
        IntPtr overridePlane0; // Unused in the Unity SDK
		RenderAPITypes renderAPIType;
		IntPtr graphicsContext; // Unused in the Unity SDK
		ColorSpaces colorSpace;
		VRAPITypes vrAPIType;
		Int32 ovrOverlayVideoCompositionDepth;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] // the size of this array is always 2
		Int32[] ovrOverlayReservedIndices;
		byte ovrOverlayNoDepthBufferTesting;
		Int32 ovrOverlayZeroCopyMeshType;
#pragma warning disable 0618
		ContentProtectionRobustnessLevels contentProtectionRobustnessLevel;
#pragma warning restore 0618 // Unsilence deprecated API usage warning
		LogLevels nrpLogLevel;
#pragma warning restore 414
        public CVRNRPLoadParametersStruct(NRPBridgeTypes argNRPBridgeType,IntPtr argOverridePlaneID, PlatformOptionsBase argPlatformOptions) {
			nrpBridgeType = argNRPBridgeType;
			nrpTextureRenderType = argPlatformOptions.textureBlitMode;
			overridePlane0 = argOverridePlaneID;
			renderAPIType = RenderAPITypesMethods.GetNRPGraphicsRendererType();
			graphicsContext = IntPtr.Zero; // Auto detect. Do not change!
			colorSpace = argPlatformOptions.overrideColorSpace; // Note: NRP uses the same colorspace enum as the one from Unity.
			vrAPIType = argPlatformOptions.vrAPIType;
			ovrOverlayVideoCompositionDepth = argPlatformOptions.ovrOverlayOptions.videoCompositionDepth;
			ovrOverlayReservedIndices = argPlatformOptions.ovrOverlayOptions.reservedIndices;
			ovrOverlayNoDepthBufferTesting = (byte) (argPlatformOptions.ovrOverlayOptions.noDepthBufferTesting ? 1 : 0);
			ClearVRMeshTypes clearVRMeshType = argPlatformOptions.prepareContentParameters.contentItem != null ? 
				argPlatformOptions.prepareContentParameters.contentItem.overrideContentFormat.GetAsClearVRMeshType() :
				ClearVRMeshTypes.Rectilinear;
			nrpLogLevel = ClearVRPlayer.loggingConfig.nrpLogLevel;
			if(clearVRMeshType == ClearVRMeshTypes.Unknown) {
				if(argPlatformOptions.textureBlitMode == TextureBlitModes.OVROverlayZeroCopy) {
					UnityEngine.Debug.LogWarning("[ClearVR] TextureBlitModes.OVROverlayZeroCopy mode activated but no content format specified. Assuming that this clip has ContentFormat.Rectilinear. Explicitly set the ContentFormat when constructing your ContentItem to silence this warning. Note that you cannot play ClearVR content in this mode.");
				} // else: we only care for the ovrOverlayZeroCopyMeshType when in OVROverlayZeroCopy mode. The NRP cannot handle ClearVRMeshTypes.Unknown while constructing a zero-copy OVROverlay. See 
				clearVRMeshType = ClearVRMeshTypes.Rectilinear;
			}
			ovrOverlayZeroCopyMeshType = (int) clearVRMeshType;
#pragma warning disable 0618
			contentProtectionRobustnessLevel = argPlatformOptions.contentProtectionRobustnessLevel;
#pragma warning restore 0618 // Unsilence deprecated API usage warning
		}
	}

	[Serializable] // Do not use Pack = 1, it will break marshalling of the struct between C++ and C#
	internal struct CVRNRPMeshDataPointersStruct {
#pragma warning disable 414
        IntPtr vertexBuffer;
        IntPtr indexBuffer;
        Int32 displayObjectID;
#pragma warning restore 414
        public CVRNRPMeshDataPointersStruct(Mesh mesh, Int32 argDisplayObjectID) {
			vertexBuffer = mesh.GetNativeVertexBufferPtr(0);
			indexBuffer = mesh.GetNativeIndexBufferPtr();
			displayObjectID = argDisplayObjectID;
        }
    }

	/// <summary>
	/// Used to describe position and orientation of an object.
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal class _ClearVRPose {
		/// <summary>Position X coordinate.</summary>
		public double posX;
		/// <summary>Position Y coordinate.</summary>
		public double posY;
		/// <summary>Position Z coordinate.</summary>
		public double posZ;
		/// <summary>Orientation w quaternion component.</summary>
		public double w;
		/// <summary>Orientation x quaternion component.</summary>
		public double x;
		/// <summary>Orientation y quaternion component.</summary>
		public double y;
		/// <summary>Orientation z quaternion component.</summary>
		public double z;
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <returns>A default object, at position (0, 0, 0) and orientation (w=1, x=0, y=0, z=0).</returns>
		public _ClearVRPose() : this(0, 0, 0, 1, 0, 0, 0) {
		}
		/// <summary>
		/// The _ClearVRPose describes the position and orientation of an object.
		/// Note that ClearVR adopts the (W, X, Y, Z) attibute order for orientation in Quaternions.
		/// </summary>
		/// <param name="argPosX">Position X</param>
		/// <param name="argPosY">Position Y</param>
		/// <param name="argPosZ">Position Z</param>
		/// <param name="argW">Orientation W</param>
		/// <param name="argX">Orientation X</param>
		/// <param name="argY">Orientation Y</param>
		/// <param name="argZ">Orientation Z</param>
		public _ClearVRPose(double argPosX, double argPosY, double argPosZ, double argW, double argX, double argY, double argZ) {
			posX = argPosX;
			posY = argPosY;
			posZ = argPosZ;
			w = argW;
			x = argX;
			y = argY;
			z = argZ;
		}

		/// <summary>
		/// Convenience constructor
		/// </summary>
		/// <param name="argPosition">The position</param>
		/// <param name="argRotation">The orientation</param>
		public _ClearVRPose(Vector3 argPosition, Quaternion argRotation) {
			posX = argPosition.x;
			posY = argPosition.y;
			posZ = argPosition.z;
			w = argRotation.w;
			x = argRotation.x;
			y = argRotation.y;
			z = argRotation.z;
		}

		/// <summary>
		/// Convenience position setter taking a Vector3
		/// </summary>
		/// <param name="argPosition">The position</param>
		public void SetPosition(Vector3 argPosition) {
			posX = argPosition.x;
			posY = argPosition.y;
			posZ = argPosition.z;
		}
		
		/// <summary>
		/// Convenience orientation setter taking a Quaternion
		/// </summary>
		/// <param name="argRotation">The orientation.</param>
		public void SetOrientation(Quaternion argRotation) {
			w = argRotation.w;
			x = argRotation.x;
			y = argRotation.y;
			z = argRotation.z;
		}
		
		/// <summary>
		/// Converts the position component to a Unity Vector3 object.
		/// </summary>
		/// <returns>The Vector3 equivalent.</returns>
		public Vector3 PositionToVector3() {
			return new Vector3((float)this.posX, (float)this.posY, (float)this.posZ);
		}

		/// <summary>
		/// Converts the orientation component to a Unity Quaternion object.
		/// </summary>
		/// <returns>The Quaternion equivalent.</returns>
		public Quaternion OrientationToQuaternion() {
			return new Quaternion((float) this.x, (float) this.y, (float) this.z, (float) this.w);
		}

		/// <summary>
		/// Creates a deep-copy of itself.
		/// </summary>
		/// <returns>A deep-copy of itself.</returns>
		public _ClearVRPose Copy() {
			return new _ClearVRPose(this.posX, this.posY, this.posZ, this.w, this.x, this.y, this.z);
		}

		/// <summary>
		/// Convenience method that returns an informational string listing all fields and their values.
		/// </summary>
		/// <returns>A string</returns>
		public override String ToString() {
			return String.Format("Position: x: {0}, y: {1}, z: {2}, orientation: w: {3}, x: {4}, y: {5}, z: {6}", this.posX, this.posY, this.posZ, this.w, this.x, this.y, this.z);
		}

		public override bool Equals(object argOther) {
			_ClearVRPose otherObject = argOther as _ClearVRPose;
			if (otherObject == null) {
				return false;
			}
			return (this.posX == otherObject.posX &&
				this.posY == otherObject.posY &&
				this.posZ == otherObject.posZ &&
				this.w == otherObject.w &&
				this.x == otherObject.x &&
				this.y == otherObject.y &&
				this.z == otherObject.z);
		}

		public override int GetHashCode() {
			int hash = 12;
			hash = (hash * 6) + posX.GetHashCode();
			hash = (hash * 6) + posY.GetHashCode();
			hash = (hash * 6) + posZ.GetHashCode();
			hash = (hash * 6) + w.GetHashCode();
			hash = (hash * 6) + x.GetHashCode();
			hash = (hash * 6) + y.GetHashCode();
			hash = (hash * 6) + z.GetHashCode();
			return hash;
		}

#if CVR_SUPPORT_OCULUS_RUNTIME_EXTENSIONS
		public OVRPlugin.Posef ToPosef() {
			OVRPlugin.Posef posef = new OVRPlugin.Posef();
			posef.Orientation = this.OrientationToQuatf();
			posef.Position = this.PositionToVector3f();
			return posef;
		}

		public OVRPlugin.Quatf OrientationToQuatf() {
			OVRPlugin.Quatf quatf = new OVRPlugin.Quatf();
			quatf.w = (float) this.w;
			quatf.x = (float) this.x;
			quatf.y = (float) this.y;
			quatf.z = (float) this.z;
			return quatf;
		}

		public OVRPlugin.Vector3f PositionToVector3f() {
			OVRPlugin.Vector3f vec3f = new OVRPlugin.Vector3f();
			vec3f.x = (float) this.posX;
			vec3f.y = (float) this.posY;
			vec3f.z = (float) this.posZ;
			return vec3f;
		}
#endif
	}


	/// <summary>
	/// Object describing the scale of an object.
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 8)]
	public class ClearVRScale {
		/// <summary>Scale in the X local coordinate system of the object.</summary>
		public double x;
		/// <summary>Scale in the Y local coordinate system of the object.</summary>
		public double y;
		/// <summary>Scale in the Z local coordinate system of the object.</summary>
		public double z;
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <returns>A ClearVRScale object with identity scale (1, 1, 1).</returns>
		public ClearVRScale() : this (1, 1, 1) {
		}

		/// <summary>
		/// Constructor taking three arguments
		/// </summary>
		/// <param name="argX">The x scale</param>
		/// <param name="argY">The y scale</param>
		/// <param name="argZ">The z scale</param>
		public ClearVRScale(double argX, double argY, double argZ) {
			x = argX;
			y = argY;
			z = argZ;
		}

		/// <summary>
		/// Converts this object into a Unity Vector3 object
		/// </summary>
		/// <returns>The Vector3 equivalent</returns>
		public Vector3 ScaleToVector3() {
			return new Vector3((float) x, (float) y, (float) z);
		}

		/// <summary>
		/// Creates a deep-copy of this object
		/// </summary>
		/// <returns>A deep-copy</returns>
		public ClearVRScale Copy() {
			return new ClearVRScale(this.x, this.y, this.z);
		}

		/// <summary>
		/// Convenience method that returns an informational string listing all fields and their values.
		/// </summary>
		/// <returns>A string</returns>
		public override String ToString() {
			return String.Format("Scale: x: {0}, y: {1}, z: {2}", this.x, this.y, this.z);
		}

		public override bool Equals(object argOther) {
			ClearVRScale otherObject = argOther as ClearVRScale;
			if (otherObject == null) {
				return false;
			}
			return (this.x == otherObject.x &&
				this.y == otherObject.y &&
				this.z == otherObject.z);
		}

		public override int GetHashCode() {
			int hash = 11;
			hash = (hash * 6) + x.GetHashCode();
			hash = (hash * 6) + y.GetHashCode();
			hash = (hash * 6) + z.GetHashCode();
			return hash;
		}
	}
	/// <summary>
	/// The _ClearVRDisplayObject describes the Pose and Scale of the mesh that is rendering the video.
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 8)]
	internal class _ClearVRDisplayObject {
		/// <summary>
		/// The Pose
		/// </summary>
		public _ClearVRPose pose;
		/// <summary>
		/// The scale
		/// </summary>
		public ClearVRScale scale;
		/// <summary>
		/// Default constructor, setting Pose to (0, 0, 0) and Scale to (1, 1, 1).
		/// </summary>
		/// <returns></returns>
		public _ClearVRDisplayObject() : this(new _ClearVRPose(), new ClearVRScale()) { }
		/// <summary>
		/// Constructor allowing you to specify Pose and Scale.
		/// </summary>
		/// <param name="argPose">The pose.</param>
		/// <param name="argScale">The scale.</param>
		public _ClearVRDisplayObject(_ClearVRPose argPose, ClearVRScale argScale) {
			this.pose = argPose;
			this.scale = argScale;
		}
		/// <summary>
		/// Creates a deep-copy of this object
		/// </summary>
		/// <returns>A deep-copy</returns>
		public _ClearVRDisplayObject Copy() {
			return new _ClearVRDisplayObject(this.pose.Copy(), this.scale.Copy());
		}
		/// <summary>
		/// Convenience method that returns an informational string listing all fields and their values.
		/// </summary>
		/// <returns>A string</returns>
		public override String ToString() {
			return String.Format("Pose: {0}, scale: {1}", this.pose.ToString(), this.scale.ToString());
		}

		public override bool Equals(object argOther) {
			_ClearVRDisplayObject otherObject = argOther as _ClearVRDisplayObject;
			if (otherObject == null) {
				return false;
			}
			return this.pose.Equals(otherObject.pose) && this.scale.Equals(otherObject.scale);
		}

		public override int GetHashCode() {
			int hash = 12;
			hash = (hash * 6) + pose.GetHashCode();
			hash = (hash * 6) + scale.GetHashCode();
			return hash;
		}
	}

	// DO NOT CHANGE THE LAYOUT OF FIELDS WITHOUT CHANGING THE OFFSET
	// PLEASE REFER TO "pose.h" IN TMPOSE
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
	[Serializable, StructLayout(LayoutKind.Explicit, Pack = 8)]
#else
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
	/// <summary>
	/// Describes the viewport Pose and display object Pose and Scale. This is used to describe the positional relationship between camera and mesh on which the video is rendered.
	/// </summary>
	internal class _ClearVRViewportAndObjectPose {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		[FieldOffset(0)]
#endif
		/// <summary>
		/// Viewport Pose
		/// </summary>
		internal _ClearVRPose viewportPose = new _ClearVRPose();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		[FieldOffset(56)]
#endif
		/// <summary>
		/// Describes display object (mesh on which the video is rendered) Pose and Scale.
		/// </summary>
		internal _ClearVRDisplayObject displayObject = new _ClearVRDisplayObject();

		/// <summary>
		/// Default constructor.
		/// For legacy reasons, the default display object scale is set to 15 units. Viewport and DisplayObject are located at (0, 0, 0) and have orientation (1, 0, 0,0).
		/// </summary>
		/// <param name="defaultObjectScale">The display object scale, default value: 15</param>
		/// <returns>The object</returns>
		internal _ClearVRViewportAndObjectPose(double defaultObjectScale = 15f /* DO NOT CHANGE, USED IN DIFFERENT SPOTS IN THE CODE AS WELL */) : this(new _ClearVRPose(), new _ClearVRDisplayObject()) {
			displayObject.scale.x = defaultObjectScale;
			displayObject.scale.y = defaultObjectScale;
			displayObject.scale.z = defaultObjectScale;
		}
		/// <summary>
		/// Advanced constructor allowing you to specify Viewport and DisplayObject position, scale and orientation.
		/// </summary>
		/// <param name="argClearVRViewportPose">The viewport Pose.</param>
		/// <param name="argClearVRDisplayObject">The DisplayObject Pose and Scale.</param>
		internal _ClearVRViewportAndObjectPose(_ClearVRPose argClearVRViewportPose, _ClearVRDisplayObject argClearVRDisplayObject) {
			viewportPose = argClearVRViewportPose;
			displayObject = argClearVRDisplayObject;
		}

		internal _ClearVRViewportAndObjectPose Copy() {
			return new _ClearVRViewportAndObjectPose(this.viewportPose.Copy(), this.displayObject.Copy());
		}

		public override String ToString() {
			return String.Format("Viewport pose: {0}, display object: {1}", viewportPose.ToString(), displayObject.ToString());
		}

		public override bool Equals(object argOther) {
			_ClearVRViewportAndObjectPose vop = argOther as _ClearVRViewportAndObjectPose;
			if (vop == null) {
				return false;
			}
			return this.displayObject.Equals(vop.displayObject) && this.viewportPose.Equals(vop.viewportPose);
		}

		public override int GetHashCode() {
			int hash = 13;
			hash = (hash * 7) + displayObject.GetHashCode();
			hash = (hash * 7) + viewportPose.GetHashCode();
			return hash;
		}

		/// <summary>Return a protobuf representation of the viewport and display object pose</summary>
		internal cvri.ViewportAndDisplayObjectPose ToCoreProtobuf() {
			return new cvri.ViewportAndDisplayObjectPose() {
				ViewportPosition = new cvri.Vec3 {
					X = viewportPose.posX,
					Y = viewportPose.posY,
					Z = viewportPose.posZ,
				},
				ViewportOrientation = new cvri.Quaternion {
					W = viewportPose.w,
					X = viewportPose.x,
					Y = viewportPose.y,
					Z = viewportPose.z,
				},
				DisplayObjectPosition = new cvri.Vec3 {
					X = displayObject.pose.posX,
					Y = displayObject.pose.posY,
					Z = displayObject.pose.posZ,
				},
				DisplayObjectOrientation = new cvri.Quaternion {
					W = displayObject.pose.w,
					X = displayObject.pose.x,
					Y = displayObject.pose.y,
					Z = displayObject.pose.z,
				},
				DisplayObjectScale = new cvri.Vec3 {
					X = displayObject.scale.x,
					Y = displayObject.scale.y,
					Z = displayObject.scale.z,
				},
			};
		}

		internal static _ClearVRViewportAndObjectPose FromCoreProtobuf(cvri.ViewportAndDisplayObjectPose coreViewportAndDisplayObjectPose) {
			if(coreViewportAndDisplayObjectPose == null) {
				return null;
			}
			return new _ClearVRViewportAndObjectPose(
				new _ClearVRPose() {
					posX = coreViewportAndDisplayObjectPose.ViewportPosition.X,
					posY = coreViewportAndDisplayObjectPose.ViewportPosition.Y,
					posZ = coreViewportAndDisplayObjectPose.ViewportPosition.Z,
					w = coreViewportAndDisplayObjectPose.ViewportOrientation.W,
					x = coreViewportAndDisplayObjectPose.ViewportOrientation.X,
					y = coreViewportAndDisplayObjectPose.ViewportOrientation.Y,
					z = coreViewportAndDisplayObjectPose.ViewportOrientation.Z,
				},
				new _ClearVRDisplayObject(
					new _ClearVRPose() {
						posX = coreViewportAndDisplayObjectPose.DisplayObjectPosition.X,
						posY = coreViewportAndDisplayObjectPose.DisplayObjectPosition.Y,
						posZ = coreViewportAndDisplayObjectPose.DisplayObjectPosition.Z,
						w = coreViewportAndDisplayObjectPose.DisplayObjectOrientation.W,
						x = coreViewportAndDisplayObjectPose.DisplayObjectOrientation.X,
						y = coreViewportAndDisplayObjectPose.DisplayObjectOrientation.Y,
						z = coreViewportAndDisplayObjectPose.DisplayObjectOrientation.Z,
					},
					new ClearVRScale() {
						x = coreViewportAndDisplayObjectPose.DisplayObjectScale.X,
						y = coreViewportAndDisplayObjectPose.DisplayObjectScale.Y,
						z = coreViewportAndDisplayObjectPose.DisplayObjectScale.Z,
					}
				)
			);
        }
	}

	/// Source: https://weblogs.asp.net/stefansedich/enum-with-string-values-in-c
	/// <summary>
    /// This attribute is used to represent a string value
    /// for a value in an enum.
    /// </summary>
    internal class StringValueAttribute : Attribute {
        /// <summary>
        /// Holds the stringvalue for a value in an enum.
        /// </summary>
        public string StringValue { get; protected set; }

        /// <summary>
        /// Constructor used to init a StringValue Attribute
        /// </summary>
        /// <param name="value"></param>
        public StringValueAttribute(string value) {
            this.StringValue = value;
        }
    }

	/// Source: https://weblogs.asp.net/stefansedich/enum-with-string-values-in-c
	/// <summary>
    /// This attribute is used to represent a string value
    /// for a value in an enum.
    /// </summary>
    internal class StringValuesAttribute : Attribute {
        /// <summary>
        /// Holds the stringvalue for a value in an enum.
        /// </summary>
        public string[] StringValues { get; protected set; }

        /// <summary>
        /// Constructor used to init a StringValue Attribute
        /// </summary>
        /// <param name="value"></param>
        public StringValuesAttribute(string[] value) {
            this.StringValues = value;
        }
    }
	
	// Note: the literal variable names AND the String attribute of this enum must be identical to the ones used in the ClearVRCore. Note that the Cvrinterface does not specify a DRMLicenseServerTypes enum.

	/// <summary>
	/// Enum listing the various DRM license server types.
	/// </summary>
	public enum ClearVRDRMLicenseServerTypes : Int32 {
		/// <summary>
		/// The license server type is unspecified. This is typically selected when doing header-based encryption.
		/// </summary>
		[StringValue("unspecified")]
		Unspecified = 0,
		/// <summary>
		/// Viaccess-orca token based license server type.
		/// </summary>
		[StringValue("viaccess-orca-token-based")]
    	DRMLicenseServerViaccessOrcaTokenBased = 1,
		/// <summary>
		/// Generic Widevine license server type.
		/// </summary>
		[StringValue("widevine-generic")]
    	DRMLicenseServerWidevineGeneric = 2

	}

	internal static class ClearVRDRMLicenseServerTypesMethods {
		/// <summary>
		/// Will get the string value for a given enums value, this will
		/// only work if you assign the StringValue attribute to
		/// the items in your enum.
		/// </summary>
		/// <param name="argValue"></param>
		/// <returns></returns>
		public static String GetStringValue(this ClearVRDRMLicenseServerTypes argValue) {
			// Get the type
			Type type = argValue.GetType();

			// Get fieldinfo for this type
			FieldInfo fieldInfo = type.GetField(argValue.ToString());

			// Get the stringvalue attributes
			StringValueAttribute[] attribs = fieldInfo.GetCustomAttributes(
				typeof(StringValueAttribute), false) as StringValueAttribute[];

			// Return the first if there was a match.
			return attribs.Length > 0 ? attribs[0].StringValue : null;
		}

		public static ClearVRDRMLicenseServerTypes GetFromStringValue(String argValue) {
			foreach(ClearVRDRMLicenseServerTypes value in Enum.GetValues(typeof(ClearVRDRMLicenseServerTypes))) {
				if(value.GetStringValue() == argValue) {
					return value;
				}
			}
			return ClearVRDRMLicenseServerTypes.Unspecified;
		}
	}

	/// <summary>
	/// This helper class specifies information on a clip's DRM protection.
	/// This class supports token and certificat-based decryption, but DRM protected content playback is only support on Android.
	/// </summary>
	public class DRMInfo {
		private ClearVRDRMLicenseServerTypes _clearVRDRMLicenseServerType = ClearVRDRMLicenseServerTypes.Unspecified;
		private string _url;
		private string _token;
		private byte[] _certificate;
		private byte[] _key;
		private byte[] _caChain;
		private string _password;
		private KeyValuePair<String, String>[] _licenseAuthenticationHeaders;
		private KeyValuePair<String, String>[] _licenseAuthenticationQueryStrings;
		private KeyValuePair<String, String>[] _tokenizationHeaders;
		private KeyValuePair<String, String>[] _tokenizationQueryStrings;
		private String _keyOverrideBase64;
		private String _ivOverrideBase64;
		/// <summary>
		/// The DRM License Server type. Note that Unspecified is a valid value.
		/// </summary>
		public ClearVRDRMLicenseServerTypes clearVRDRMLicenseServerType {
			get {
				return _clearVRDRMLicenseServerType;
			}
			// No setter, this is a read-only property.
		}
		/// <summary>
		/// The DRM server URL
		/// </summary>
		public String url {
			get {
				return _url;
			}
			set {
				_url = value;
			}
		}
		/// <summary>
		/// The token used for DRM decryption.
		/// </summary>
		public String token {
			get {
				return _token;
			}
			set {
				_token = value;
			}
		}
		/// <summary>
		/// Specifies the certificate.
		/// </summary>
		public byte[] certificate {
			get {
				return _certificate;
			}
			set {
				_certificate = value;
			}
		}
		/// <summary>
		/// Specifies the key
		/// </summary>
		public byte[] key {
			get {
				return _key;
			}
			set {
				_key = value;
			}
		}
		/// <summary>
		/// Specifies the CA certificate if a non-standard CA authority is used.
		/// </summary>
		public byte[] caChain {
			get {
				return _caChain;
			}
			set {
				_caChain = value;
			}
		}
		/// <summary>
		/// THe password, use to decrypt the crypto keys if applicable.
		/// </summary>
		public String password {
			get {
				return _password;
			}
			set {
				_password = value;
			}
		}

		/// <summary>
		/// An array of license authentication header key/value pairs.
		/// </summary>
		/// <value>The license authentication header key/value pairs, as set during object construction. Can be empty, but cannot be null.</value>
		public KeyValuePair<String, String>[] licenseAuthenticationHeaders {
			get {
				return _licenseAuthenticationHeaders;
			}
			set {
				_licenseAuthenticationHeaders = value;
			}
		}

		/// <summary>
		/// An array of license authentication query string key/value pairs.
		/// </summary>
		/// <value>The license authentication query string key/value pairs, as set during object construction. Can be empty, but cannot be null.</value>
		public KeyValuePair<String, String>[] licenseAuthenticationQueryStrings {
			get {
				return _licenseAuthenticationQueryStrings;
			}
			set {
				_licenseAuthenticationQueryStrings = value;
			}
		}

		/// <summary>
		/// An array of tokenisation header key/value pairs.
		/// </summary>
		/// <value>The tokenisation header key/value pairs, as set during object construction. Can be empty, but cannot be null.</value>
		[Obsolete("[ClearVR] This field will be removed after 31-01-2023. Please use tokenizationHeaders instead.")]
		public KeyValuePair<String, String>[] tokenisationHeaders {
			get {
				return tokenizationHeaders;
			}
			set {
				tokenizationHeaders = value;
			}
		}

		/// <summary>
		/// An array of tokenization header key/value pairs.
		/// </summary>
		/// <value>The tokenization header key/value pairs, as set during object construction. Can be empty, but cannot be null.</value>
		public KeyValuePair<String, String>[] tokenizationHeaders {
			get {
				return _tokenizationHeaders;
			}
			set {
				_tokenizationHeaders = value;
			}
		}

		/// <summary>
		/// An array of tokenisation query strings key/value pairs.
		/// </summary>
		/// <value>The license authentication query string key/value pairs, as set during object construction. Can be empty, but cannot be null.</value>
		[Obsolete("[ClearVR] This field will be removed after 31-01-2023. Please use tokenizationQueryStrings instead.")]
		public KeyValuePair<String, String>[] tokenisationQueryStrings {
			get {
				return tokenizationQueryStrings;
			}
			set {
				tokenizationQueryStrings = value;
			}
		}

		/// <summary>
		/// An array of tokenization query strings key/value pairs.
		/// </summary>
		/// <value>The license authentication query string key/value pairs, as set during object construction. Can be empty, but cannot be null.</value>
		public KeyValuePair<String, String>[] tokenizationQueryStrings {
			get {
				return _tokenizationQueryStrings;
			}
			set {
				_tokenizationQueryStrings = value;
			}
		}
		/// <summary>
		/// Override the decryption key, in BASE64 format.
		/// > [!WARNING]
		/// > This is an advanced field which should not be used in your application unless you know exactly what you're doing.
		/// </summary>
		// Intentionally not exposed in the constructor
		public String keyOverrideBase64 {
			get {
				return _keyOverrideBase64;
			}
			set {
				_keyOverrideBase64 = value;
			}
		}

		/// <summary>
		/// Override the decryption IVR, in BASE64 format.
		/// > [!WARNING]
		/// > This is an advanced field which should not be used in your application unless you know exactly what you're doing.
		/// </summary>
		// Intentionally not exposed in the constructor
		public String ivOverrideBase64 {
			get {
				return _ivOverrideBase64;
			}
			set {
				ivOverrideBase64 = value;
			}
		}

		/// <summary>
		/// Default constructor. Specify fields like url, token and/or certificate data manually.
		/// </summary>
		/// <param name="argClearVRDRMLicenseServerType"></param>
		public DRMInfo(ClearVRDRMLicenseServerTypes argClearVRDRMLicenseServerType) : this(argClearVRDRMLicenseServerType, "", "", null, null, null, "") {}

		/// <summary>
		/// Construct a DRMInfo object for content that is protected by a certificate/key and/or a token/password.
		/// This is a convenience constructor
		/// </summary>
		/// <param name="argClearVRDRMLicenseServerType">The DRMLicenseServerType. Cannot be null.</param>
		/// <param name="argUrl">The DRM license server URL. Can be null.</param>
		/// <param name="argToken">The token to use during validation. Can be null.</param>
		/// <param name="argCertificate">The certificate to use. Can be null.</param>
		/// <param name="argKey">The key to use. Can be null.</param>
		/// <param name="argCAChain">The CA Chain certificate to use. Can be null.</param>
		/// <param name="argPassword">The password required to decrypt the certificate(s). Can be null.</param>
		public DRMInfo(ClearVRDRMLicenseServerTypes argClearVRDRMLicenseServerType,
				String argUrl,
				String argToken,
				byte[] argCertificate,
				byte[] argKey,
				byte[] argCAChain,
				String argPassword) : this(argClearVRDRMLicenseServerType, argUrl, argToken, argCertificate, argKey, argCAChain, argPassword, null, null, null, null) {
		}
		/// <summary>
		/// Construct a DRMInfo object for content that is protected with a license- and tokenization key/value pair.
     	/// This constructor can be used for content that is not protected by a certificate/key and/or a token/password.
     	/// This is a convenience constructor.
		/// </summary>
		/// <param name="argClearVRDRMLicenseServerType">The DRMLicenseServerType. Cannot be null. WHen using this constructor, this value is typically set to Unspecified.</param>
		/// <param name="argLicenseAuthenticationHeaders">The license authentication headers. The argument can be null, but keys and values can not be null.</param>
		/// <param name="argLicenseAuthenticationQueryStrings">The license authentication query strings value. The argument can be null, but keys and values can not be null.</param>
		/// <param name="argTokenisationHeaders">The tokenisation headers. The argument can be null, but keys and values can not be null.</param>
		/// <param name="argTokenisationQueryStrings">The tokenisation query strings. The argument can be null, but keys and values can not be null.</param>
		public DRMInfo(ClearVRDRMLicenseServerTypes argClearVRDRMLicenseServerType,
				KeyValuePair<String, String>[] argLicenseAuthenticationHeaders,
				KeyValuePair<String, String>[] argLicenseAuthenticationQueryStrings,
				KeyValuePair<String, String>[] argTokenisationHeaders,
				KeyValuePair<String, String>[] argTokenisationQueryStrings) : this(argClearVRDRMLicenseServerType, null, null, null, null, null, null, argLicenseAuthenticationHeaders, argLicenseAuthenticationQueryStrings, argTokenisationHeaders, argTokenisationQueryStrings) {
		}

		/// <summary>
		/// Construct a DRMInfo object by setting all its fields explicitly. Note that there are convenience constructors that might suit your need even better.
		/// </summary>
		/// <param name="argClearVRDRMLicenseServerType">The DRMLicenseServerType. Cannot be null.</param>
		/// <param name="argUrl">The DRM license server URL. Can be null.</param>
		/// <param name="argToken">The token to use during validation. Can be null.</param>
		/// <param name="argCertificate">The certificate to use. Can be null.</param>
		/// <param name="argKey">The key to use. Can be null.</param>
		/// <param name="argCAChain">The CA Chain certificate to use. Can be null.</param>
		/// <param name="argPassword">The password required to decrypt the certificate(s). The argument can be null, but keys and values can not be null.</param>
		/// <param name="argLicenseAuthenticationHeaders">The license authentication headers. The argument can be null, but keys and values can not be null.</param>
		/// <param name="argLicenseAuthenticationQueryStrings">The license authentication query strings value. The argument can be null, but keys and values can not be null.</param>
		/// <param name="argTokenisationHeaders">The tokenisation headers. The argument can be null, but keys and values can not be null.</param>
		/// <param name="argTokenisationQueryStrings">The tokenisation query strings. The argument can be null, but keys and values can not be null.</param>
		public DRMInfo(ClearVRDRMLicenseServerTypes argClearVRDRMLicenseServerType,
				String argUrl,
				String argToken,
				byte[] argCertificate,
				byte[] argKey,
				byte[] argCAChain,
				String argPassword,
				KeyValuePair<String, String>[] argLicenseAuthenticationHeaders,
				KeyValuePair<String, String>[] argLicenseAuthenticationQueryStrings,
				KeyValuePair<String, String>[] argTokenisationHeaders,
				KeyValuePair<String, String>[] argTokenisationQueryStrings) {
			this._clearVRDRMLicenseServerType = argClearVRDRMLicenseServerType;
			this._url = argUrl;
			this._token = argToken;
			this._certificate = argCertificate;
			this._key = argKey;
			this._caChain = argCAChain;
			this._password = argPassword;
			this._licenseAuthenticationHeaders = argLicenseAuthenticationHeaders;
			this._licenseAuthenticationQueryStrings = argLicenseAuthenticationQueryStrings;
			this._tokenizationHeaders = argTokenisationHeaders;
			this._tokenizationQueryStrings = argTokenisationQueryStrings;
		}

		// Internal constuctor, used for cloning.
		internal DRMInfo(cvri.DRM argCoreDRM) {
			this._clearVRDRMLicenseServerType = ClearVRDRMLicenseServerTypesMethods.GetFromStringValue(argCoreDRM.LicenseServerType);
			this._url = argCoreDRM.LicenseServerURL;
			this._token = argCoreDRM.Token;
			this._certificate = Convert.FromBase64String(argCoreDRM.CertificatePEMAsBase64);
			this._key = Convert.FromBase64String(argCoreDRM.KeyPEMAsBase64);
			this._caChain = Convert.FromBase64String(argCoreDRM.CAChainPEMAsBase64);
			this._password = argCoreDRM.PEMPassword;
			this._keyOverrideBase64 = argCoreDRM.KeyOverrideBase64;
			this._ivOverrideBase64 = argCoreDRM.IVOverrideBase64;
			
			// LicenseAuthenticationHeaders
			String[] keys = argCoreDRM.LicenseAuthHeaderKey.ToArray();
			String[] values = argCoreDRM.LicenseAuthHeaderValue.ToArray();
			if (!(keys == null || values == null)) {
				int count = keys.Length;
				if (count > 0) {
					_licenseAuthenticationHeaders = new KeyValuePair<string, string>[count];
					for (int i = 0; i < count; i++) {
						String key = keys[i] != null ? keys[i] : "";
						String value = values[i] != null ? values[i] : "";
						_licenseAuthenticationHeaders[i] = new KeyValuePair<String, String>(key, value);
					}
				}
			}
			
			// LicenseAuthenticationQueryStrings
			keys = argCoreDRM.LicenseAuthQueryStringKey.ToArray();
			values = argCoreDRM.LicenseAuthQueryStringValue.ToArray();
			if (!(keys == null || values == null)) {
				int count = keys.Length;
				if (count > 0) {
					_licenseAuthenticationQueryStrings = new KeyValuePair<string, string>[count];
					for (int i = 0; i < count; i++) {
						String key = keys[i] != null ? keys[i] : "";
						String value = values[i] != null ? values[i] : "";
						_licenseAuthenticationQueryStrings[i] = new KeyValuePair<String, String>(key, value);
					}
				}
			}

			// TokenizationHeaders
			keys = argCoreDRM.TokenizationHeaderKey.ToArray();
			values = argCoreDRM.TokenizationHeaderValue.ToArray();
			if (!(keys == null || values == null)) {
				int count = keys.Length;
				if (count > 0) {
					_tokenizationHeaders = new KeyValuePair<string, string>[count];
					for (int i = 0; i < count; i++) {
						String key = keys[i] != null ? keys[i] : "";
						String value = values[i] != null ? values[i] : "";
						_tokenizationHeaders[i] = new KeyValuePair<String, String>(key, value);
					}
				}
			}

			// TokenizationQueryStrings
			keys = argCoreDRM.TokenizationQueryStringKey.ToArray();
			values = argCoreDRM.TokenizationQueryStringValue.ToArray();
			if (!(keys == null || values == null)) {
				int count = keys.Length;
				if (count > 0) {
					_tokenizationQueryStrings = new KeyValuePair<string, string>[count];
					for (int i = 0; i < count; i++) {
						String key = keys[i] != null ? keys[i] : "";
						String value = values[i] != null ? values[i] : "";
						_tokenizationQueryStrings[i] = new KeyValuePair<String, String>(key, value);
					}
				}
			}
		}

		/// <summary>
		/// Construct a deep copy of the object.
		/// </summary>
		/// <returns>A deep copy of the current object.</returns>
		public DRMInfo Copy() {
			DRMInfo copy = new DRMInfo(
				clearVRDRMLicenseServerType, 
				url, 
				token, 
				certificate, 
				key, 
				caChain, 
				password, 
				licenseAuthenticationHeaders, 
				licenseAuthenticationQueryStrings, 
				tokenizationHeaders, 
				tokenizationQueryStrings
			);
			copy._ivOverrideBase64 = this._ivOverrideBase64;
			copy._keyOverrideBase64 = this._keyOverrideBase64;
			return copy;
		}

		private String GetLicenseAuthenticationHeadersAsPrettyString() {
			if (licenseAuthenticationHeaders == null) {
				return "";
			}
			int count = licenseAuthenticationHeaders.Count();
			if (count == 0) {
				return "";
			}
			StringBuilder builder = new StringBuilder();
			for(int i = 0; i < count; i++) {
				builder.Append(System.String.Format("K: {0}, V: {1}", Utils.MaskString(licenseAuthenticationHeaders[i].Key), Utils.MaskString(licenseAuthenticationHeaders[i].Value)));
				if(i != count -1) {
					builder.Append(", ");
				}
			}
			return builder.ToString();
		}

		private String GetLicenseAuthenticationQueryStringsAsPrettyString() {
			if (licenseAuthenticationQueryStrings == null) {
				return "";
			}
			int count = licenseAuthenticationQueryStrings.Count();
			if (count == 0) {
				return "";
			}
			StringBuilder builder = new StringBuilder();
			for(int i = 0; i < count; i++) {
				builder.Append(System.String.Format("K: {0}, V: {1}", Utils.MaskString(licenseAuthenticationQueryStrings[i].Key), Utils.MaskString(licenseAuthenticationQueryStrings[i].Value)));
				if(i != count -1) {
					builder.Append(", ");
				}
			}
			return builder.ToString();
		}

		private String GetTokenisationHeadersAsPrettyString() {
			if (tokenizationHeaders == null) {
				return "";
			}
			int count = tokenizationHeaders.Count();
			if (count == 0) {
				return "";
			}
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < count; i++) {
				builder.Append(System.String.Format("K: {0}, V: {1}", Utils.MaskString(tokenizationHeaders[i].Key), Utils.MaskString(tokenizationHeaders[i].Value)));
				if(i != count -1) {
					builder.Append(", ");
				}
			}
			return builder.ToString();
		}

		private String GetTokenisationQueryStringsAsPrettyString() {
			if (tokenizationQueryStrings == null) {
				return "";
			}
			int count = tokenizationQueryStrings.Count();
			if (count == 0) {
				return "";
			}
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < count; i++) {
				builder.Append(System.String.Format("K: {0}, V: {1}", Utils.MaskString(tokenizationQueryStrings[i].Key), Utils.MaskString(tokenizationQueryStrings[i].Value)));
				if(i != count -1) {
					builder.Append(", ");
				}
			}
			return builder.ToString();
		}

		public override String ToString() {
            return String.Format("DRM Server type: {0}, url: {1}, certificate file size: {2}, key file size: {3}, ca chain file size: {4}, token: {5}, password: {6}, license authentication headers: {7}, license authentication query strings: {8}, tokenization headers: {9}, tokenisation query strings: {10}', key override (BASE64): {11}, IV override (BASE64): {12}", clearVRDRMLicenseServerType, this.url, Utils.GetLengthSafely(certificate), Utils.GetLengthSafely(key), Utils.GetLengthSafely(caChain), token, Utils.MaskString(password), GetLicenseAuthenticationHeadersAsPrettyString(), GetLicenseAuthenticationQueryStringsAsPrettyString(), GetTokenisationHeadersAsPrettyString(), GetTokenisationQueryStringsAsPrettyString(), Utils.MaskString(keyOverrideBase64), Utils.MaskString(ivOverrideBase64));
		}

		internal cvri.DRM ToCoreProtobuf() {
			cvrinterface.DRM coreDRM = new cvri.DRM();
			coreDRM = new cvri.DRM();
			if (clearVRDRMLicenseServerType != ClearVRDRMLicenseServerTypes.Unspecified) {
				coreDRM.LicenseServerType = clearVRDRMLicenseServerType.GetStringValue(); // Core proto does not specify an enum, but rather a string. 
			}
			if (url != null) {
				coreDRM.LicenseServerURL = url;
			}
			if (password != null) {
				coreDRM.PEMPassword = password;
			}
			if (token != null) {
				coreDRM.Token = token;
			}
			if (caChain != null) {
				coreDRM.CAChainPEMAsBase64 = System.Convert.ToBase64String(caChain);
			}
			if (certificate != null) {
				coreDRM.CertificatePEMAsBase64 = System.Convert.ToBase64String(certificate);
			}
			if (key != null) {
				coreDRM.KeyPEMAsBase64 = System.Convert.ToBase64String(key);
			}
			if (!String.IsNullOrEmpty(keyOverrideBase64)) {
				coreDRM.KeyOverrideBase64 = keyOverrideBase64;
			}
			if (!String.IsNullOrEmpty(ivOverrideBase64)) {
				coreDRM.IVOverrideBase64 = ivOverrideBase64;
			}

			// We have to guarantee to the core that all key/values have equal length.
			if (licenseAuthenticationHeaders != null) {
				foreach(KeyValuePair<String, String> keyValuePair in licenseAuthenticationHeaders) {
					String key = keyValuePair.Key;
					String value = keyValuePair.Value;
					coreDRM.LicenseAuthHeaderKey.Add(key != null ? key : "");
					coreDRM.LicenseAuthHeaderValue.Add(value != null ? value : "");
				}
			}
			if (licenseAuthenticationQueryStrings != null) {
				foreach(KeyValuePair<String, String> keyValuePair in licenseAuthenticationQueryStrings) {
					String key = keyValuePair.Key;
					String value = keyValuePair.Value;
					coreDRM.LicenseAuthQueryStringKey.Add(key != null ? key : "");
					coreDRM.LicenseAuthQueryStringValue.Add(value != null ? value : "");
				}
			}
			if (tokenizationHeaders != null) {
				foreach(KeyValuePair<String, String> keyValuePair in tokenizationHeaders) {
					String key = keyValuePair.Key;
					String value = keyValuePair.Value;
					coreDRM.TokenizationHeaderKey.Add(key != null ? key : "");
					coreDRM.TokenizationHeaderValue.Add(value != null ? value : "");
				}
			}
			if (tokenizationQueryStrings != null) {
				foreach(KeyValuePair<String, String> keyValuePair in tokenizationQueryStrings) {
					String key = keyValuePair.Key;
					String value = keyValuePair.Value;
					coreDRM.TokenizationQueryStringKey.Add(key != null ? key : "");
					coreDRM.TokenizationQueryStringValue.Add(value != null ? value : "");
				}
			}
			return coreDRM;
		}
	}


	/// <summary>
	/// Legacy enum that was used for presets for known camera types.
	/// > [!WARNING]
	/// > The FishEyeCameraAndLensTypes enum is deprecated. One must use the [FisheyePresets](xref:com.tiledmedia.clearvr.FisheyePresets) enum instead.
	/// </summary>
	[Obsolete("This enum has been deprecated and will be removed after 2023/01/31. It has been replaced by the FisheyePresets enum.", true)]
	public enum FishEyeCameraAndLensTypes {
		/// <summary>
		/// No preset used for the fish eye camera. Can be used if a custom camera and lens type combination is used not covered by any of the provided presets.
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.Custom instead.", false)]
		CustomCameraAndLens = 0, // com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.CustomFisheyeCameraAndLens
		/// <summary>
		/// use the preset for the blackmagic URSA Mini Canon 815
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.BlackmagicUrsaMiniCanon8_15_8mm instead.", false)]
		BlackmagicURSAMiniCanon815 = 1, // com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.BlackmagicUrsaminiCanon815
		/// <summary>
		/// Use the preset for the Z cam K1 pro with Pro Izugar Mkx 22mft sensor
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.Zcamk1proIzugarMkx22mft_3dot25mm instead.", false)]
		ZCamK1ProIzugarMkx22mft = 2, // com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.ZcamK1ProIzugarmkx22Mft
		/// <summary>
		/// Use the preset for the Z cam K2 pro with Pro Izugar Mkx 200 sensor
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.Zcamk2proIzugarMkx200_3dot8mm instead.", false)]
		ZCamK2ProIzugarMkx200 = 3, // com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.ZcamK2ProIzugarmkx200
		/// <summary>
		/// Use the preset for the Red Komodo 6K Camera with Canon 815 lens
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.RedKomodo6kcanon8_12_8mm instead.", false)]
		RedKomodo6KCanon812 = 4, // com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.RedKomodo6KCanon812
		/// <summary>
		/// Use the preset for the Blackmagic Ursa 12K Camera with Canon 815 lens at 8 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.BlackmagicUrsa12kcanon8_15_8mm_8k_16_9 instead.", false)]
		BlackmagicUrsa12KCanon8158Mmf48K169 = 5, /* com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.BlackmagicUrsa12KCanon8158Mmf48K169 */
		/// <summary>
		/// Use the preset for the Red V-Raptor 8K Camera with Canon 815 lens at 8 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.RedVraptor8kcanon8_15_8mm instead.", false)]
		RedVRaptor8KCanon8158 = 6, /* com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.RedVRaptor8KCanon8158 */
		/// <summary>
		/// Use the preset for the Red V-Raptor 8K Camera with Canon 815 lens at 10 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.RedVraptor8kcanon8_15_10mm instead.", false)]
		RedVRaptor8KCanon81510 = 7, /* com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.RedVRaptor8KCanon81510 */
		/// <summary>
		/// Use the preset for the Red V-Raptor 8K Camera with Canon 815 lens at 13 mm, focal length 4 8K sensor in 16:9.
		/// </summary>
		[Obsolete("This key is obsolete, see FisheyePresets.RedVraptor8kcanon8_15_13mm instead.", false)]
		RedVRaptor8KCanon81513 = 8 /* com.tiledmedia.clearvr.cvrinterface.FishEyeCameraAndLens.RedVRaptor8KCanon81513 */
	}

	/// <summary>
	/// Lens types for fish eye cameras
	/// </summary>
	public enum FishEyeLensTypes {
		/// <summary>
		/// Default lens for the fish eye camera
		/// </summary>
		DefaultFisheyeLens = 0 /* com.tiledmedia.clearvr.cvrinterface.FishEyeLensType.DefaultFisheyeLensType */,
		/// <summary>
		/// Equisolid lens for the fish eye camera
		/// </summary>
		Equisolid = 1 /* com.tiledmedia.clearvr.cvrinterface.FishEyeLensType.Equisolid */,
		/// <summary>
		/// Equidistant lens for the fish eye camera
		/// </summary>
		Equidistant = 2, /* com.tiledmedia.clearvr.cvrinterface.FishEyeLensType.Equidistant */
		/// <summary>
		/// Polynomial lens model for the fish eye camera
		/// > [!WARNING]
		/// > This value is not yet supported. Setting this value will result in undefined behaviour.
		/// > Support will be added in a future update.
		/// </summary>
		Polynomial = 3 /* com.tiledmedia.clearvr.cvrinterface.FishEyeLensType.Polynomial */
	}

	/// <summary>
	/// Parameters to specify seek operation. Note that frame accurate seek is not guaranteed.
	/// </summary>
	public class SeekParameters {
		internal TimingParameters timingParameters;
		internal TransitionTypes transitionType;
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="argNewPositionInMilliseconds">The position in milliseconds to seek to. It will depend on the type of content how accurate the seek will be.</param>
		/// <param name="argFlags">Binary OR-ed set of flags, or 0 (SeekFlags.None) in casee of no flags.</param>
		[Obsolete("This constructor is obsolete. Please use SeekParameters(TimingParameters) instead.", true)]
		public SeekParameters(long argNewPositionInMilliseconds, long argFlags) {
			// This constructor is intentionally left empty.
		}
		/// <summary>
		/// Simplified constructor, defaulting to no SeekFlags.
		/// </summary>
		/// <param name="argNewPositionInMilliseconds">The position in milliseconds to seek to. It will depend on the type of content how accurate the seek will be.</param>
		[Obsolete("This constructor is obsolete. Please use SeekParameters(TimingParameters) instead.", true)]
		public SeekParameters(long argNewPositionInMilliseconds) {
			// This constructor is intentionally left empty.
		}

		/// <summary>
		/// Default constructor, allowing you to specify the new seek position, and how to interpret this new seek position.
		/// </summary>
		/// <param name="argTimingParameters">Describes the new position (in milliseconds) and how this new position should be interpreted. If set to null, VOD clips will seet to position 0, LIVE clips will seek to the live edge.</param>
		/// <param name="argTransitionType">Defines how the seek should be handled: Default value: Fast. Currently, no other value is supported and you will always default to this value when doing a seek.</param>
		public SeekParameters(TimingParameters argTimingParameters = null, TransitionTypes argTransitionType = TransitionTypes.Fast) {
			timingParameters = argTimingParameters;
			transitionType = argTransitionType;
		}

		/// <summary>Return a protobuf representation of the seek parameters. As long as there is no SeekParams protobuf message, this can return null.</summary>
		internal cvri.TimingParams ToCoreProtobuf() {
			// TODO: TransitionType is not serialized here. 
			return timingParameters != null ? timingParameters.ToCoreProtobuf() : null;
		}
	}

	/// <summary>
	/// Specifies what content to pre-load.
	/// This class is deprecated and can no longer be used. Cache prewarming has been removed in v8 of the SDK
	/// </summary>
	public class PrewarmCacheParameters {
		[Obsolete("Cache prewarming has been removed from the ClearVR SDK. Please remove any reference to it from your code.",true)]
		public PrewarmCacheParameters(ContentItem argContentItem, long argFlags) {
			// Intentionally left empty.
		}

		[Obsolete("Cache prewarming has been removed from the ClearVR SDK. Please remove any reference to it from your code.",true)]
		public PrewarmCacheParameters(ContentItem argContentItem) {
			// Intentionally left empty.
		}
	}

	/// <summary>
	/// Parameters required for switching to a different ContentItem.
	/// </summary>
	public class SwitchContentParameters : PlaybackParameters {

		/// <summary>
		/// The transition type. See [TransitionTypes](xref:com.tiledmedia.clearvr.TransitionTypes) for more information.
		/// </summary>
		public TransitionTypes transitionType {
			get {
				return _transitionType;
			}
			set {
				_transitionType = value;
			}
		}
		private TransitionTypes _transitionType = TransitionTypes.Fast;

		/// <summary>
		/// Customize switch content behaviour, currently unused.
		/// </summary>
		[Obsolete("This field has been deprecated in v9.0 and will be removed after 2023-03-31. Please remove any references to it from your code. There is no equivalent property available anymore.", true)]
		public long flags {
			get { return 0; }
			private set { /* NOOP, deprecated */ }
		}

		/// <summary>
		/// Default Constructor. Under typical conditions one should only set argContentItem and argTimingParameters and keep ther other fields at their default values.
		/// </summary>
		/// <param name="argContentItem">The content item to switch to. Cannot be null.</param>
		/// <param name="argTimingParameters">The timing parameters, defining playback start position and how it should be interpreted with respect to switch content. If set to tits default value null, playback will start at the beginning of the clip (VOD) or at the live edge (LIVE).</param>
		/// <param name="argTransitionType">Determines how the transition from the current content item to the specified content item should be performed. Default value: TransitionTypes.Fast.</param>
		/// <param name="argAudioTrackAndPlaybackParameters">The audio decoder and playback engine to use after switching content. Keep at its default value: null</param>
		/// <param name="argSyncSettings">Enable sync with supplied settings. Default value: null (sync = disabled). Note that specifying SyncSettings is not yet supported on iOS!</param>
		/// <param name="argFlags">Customize switch content API behaviour. Currently unused. Leave at default value: 0</param>
		/// <param name="argApproximateDistanceFromLiveEdgeInMilliseconds">Specifies the approximate offset that should be kept from the live edge. Default value: 0 (msec). Note that a non-0 minimum value might be enforced. Changing the default value is strongly discouraged.</param>
		/// <param name="argPreferredAudioTrackLanguage">Override the preferred audio track language by setting this to the preferred ISO-639 language code. Only works if language codes are embedded in the source stream. Default value: "", which is interpreted as "automatically pick an audio track".</param>
		[Obsolete("This constructor is obsolete and can no longer be used. Use the (ContentItem, TimingParameters, LayoutParameters) constructor instead and set the other, optional, fields manually.", true)]
		public SwitchContentParameters(ContentItem argContentItem, TimingParameters argTimingParameters = null, TransitionTypes argTransitionType = TransitionTypes.Fast, AudioTrackAndPlaybackParameters argAudioTrackAndPlaybackParameters = null, SyncSettings argSyncSettings = null, long argFlags = 0, long argApproximateDistanceFromLiveEdgeInMilliseconds = 0, String argPreferredAudioTrackLanguage = "") : base(argContentItem, argTimingParameters, null) {
			// Intentionally left empty.
		}

		/// <summary>
		/// The default constructor taking the minimum subset of arguments required.
		/// > [!WARNING]
		/// > Since v9.x
		/// > Although accepted, passing `null` as `argLayoutParameters` is NOT recommened. Instead, use the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) to configure your audio/video feed layout. Legacy Display Object positioning will be performed hen set to null, but this behaviour will be removed after 2023-03-31.
		/// > When playing Mosaic content, this parameter *MUST* be specified and cannot be null.
		/// </summary>
		/// <param name="argContentItem">The content item to play</param>
		/// <param name="argTimingParameters">The timing parameters, specifying how to start playback. Setting this to `null` will trigger default behaviour and is a valid value, see [TimingParameters](xref:com.tiledmedia.clearvr.TimingParameters) for details.</param>
		/// <param name="argLayoutParameters">The LayoutParameters specifying which DisplayObject will render what Feed. For backwards compatibility with SDKs prior to v9.0, you can set this argument to `null`. This will trigger default display object placement behaviour, but this mode will be removed after 2023-03-31.</param>
		public SwitchContentParameters(ContentItem argContentItem, TimingParameters argTimingParameters, LayoutParameters argLayoutParameters) : base(argContentItem, argTimingParameters, argLayoutParameters) { }

		private SwitchContentParameters(PlaybackParameters argPlaybackParameters) : base(argPlaybackParameters) { }

		internal SwitchContentParameters Copy() {
			SwitchContentParameters copy = new SwitchContentParameters((PlaybackParameters) this);
			copy._transitionType = this._transitionType;
			return copy;
		}

		/// <summary>Return a protobuf representation of the switch content parameters</summary>
		internal cvri.SwitchContentParamsMediaFlow ToCoreProtobuf() {
			return new cvri.SwitchContentParamsMediaFlow() {
				SwitchContentParams = new cvri.SwitchContentParams() {
					ContentItem = contentItem.ToCoreProtobuf(),
					StartPositionParams = timingParameters != null ? timingParameters.ToCoreProtobuf() : null,
					ViewportAndDisplayObjectPose = clearVRViewportAndObjectPose != null ? clearVRViewportAndObjectPose.ToCoreProtobuf() : new _ClearVRViewportAndObjectPose().ToCoreProtobuf(),
					Transition = _transitionType.ToCoreProtobuf(),
					SyncEnabled = syncSettings != null,
					SyncSettings = syncSettings != null ? syncSettings.ToCoreProtobuf() : null,
					ApproxDistanceFromLiveEdge = approximateDistanceFromLiveEdgeInMilliseconds,
					FeedConfiguration = layoutParameters != null ? layoutParameters.ToCoreProtobuf() : null
				},
				AudioTrackAndPlaybackParametersMediaFlow = audioTrackAndPlaybackParameters != null ? audioTrackAndPlaybackParameters.ToCoreProtobuf() : null
			};
		}
		
        public override String ToString() {
            return String.Format("{0}, TransitionType: {1}", base.ToString(), transitionType);
        }		
	}

	public class PlaybackParameters {
		/// <summary>
		/// The ContentItem to load, cannot be null.
		/// </summary>
		public ContentItem contentItem {
			get { return _contentItem; } 
			set { _contentItem = value; }
		}
		
		/// <summary>
		/// The TimingParameters, e.g. the initial playout start position in milliseconds and how to interpet this position. If set to null, default values will be used.
		/// </summary>
		public TimingParameters timingParameters {
			get { return _timingParameters; } 
			set { _timingParameters = value; }
		}

		/// <summary>
		/// The AudioTrackAndPlaybackParameters. Will be set to the platform specific default values if none were provided during construction.
		/// </summary>
		// Note: this is internal as we do not expose dynamically configuring the AudioTrackAndPlaybackParameters yet. There is also partial overlap witht he LayoutParameter's audioTrackIndex.
		internal AudioTrackAndPlaybackParameters audioTrackAndPlaybackParameters {
			get {
				return _audioTrackAndPlaybackParameters;
			}
			set {
				MediaPlayerBase.VerifyAudioTrackAndPlaybackParameters(ref value);
				if(value != null) {
					_audioTrackAndPlaybackParameters = value;
				}
			}
		}

		/// <summary>
		/// Settings for sync.
		/// </summary>
		public SyncSettings syncSettings {
			get { return _syncSettings; }
			set { _syncSettings = value; } 
		}

        /// <summary>
        /// Approximate offset from live edge (in milliseconds). Ignored for non-live (VOD) content. The default value is 0 (see note below).
        /// The actual value depends on whether a minimum offset is enforced and on the encoding parameters of the live feed (e.g. segment duration).
        /// Since: v8.0
        /// 
        /// > [!NOTE]
        /// > A minimum offset might be enforced.
		/// 
        /// > [!WARNING]
        /// > Changing this value directly impacts the camera-lens-to-client-device latency.
        /// > This is an advanced parameter, changing it is strongly discouraged. Please leave at its default value 0 unless you know exactly what you are doing.
        /// </summary>
        /// <value>The approximate distance from the live edge, in milliseconds</value>
        public long approximateDistanceFromLiveEdgeInMilliseconds {
            get { return _approximateDistanceFromLiveEdgeInMilliseconds; }
			set { _approximateDistanceFromLiveEdgeInMilliseconds = value; }
        }

		/// <summary>
		/// If set to an ISO-639 language code, an audio track matching this value will be selected by default.
		/// </summary>
		/// <value>The ISO-639 language code as a string.</value>
		[Obsolete("This API has been deprecated and will be removed after 2023-01-31. Query the preferredAudioLanguage field instead.", false)]
		public String preferredAudioTrackLanguage {
			get {
				return preferredAudioLanguage;
			}
		}

		/// <summary>
		/// If set to an ISO-639 language code, an audio track matching this value will be selected by default.
		/// </summary>
		/// <value>The ISO-639 language code as a string.</value>
		public String preferredAudioLanguage {
			get {
				return _layoutParameters.preferredAudioLanguage;
			}
		}
		/// <summary>
		/// If set to an ISO-639 language code, a subtitles track matching this value will be selected by default.
		/// </summary>
		/// <value>The ISO-639 language code as a string.</value>
		public String preferredSubtitlesLanguage {
			get {
				return _layoutParameters.preferredSubtitlesLanguage;
			}
		}
		/// <summary>
		/// Describes the FeedConfiguration, where the video(s) will be positioned.
		/// > [!WARNING]
		/// > Although accepted, passing null is NOT recommened. Instead, use the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) to configure your audio/video layout. Legacy mesh positioning will be performed when set to null, but this behaviour will be removed after 2023-03-30.
		/// </summary>
		/// <value>The feed configuration</value>
		public LayoutParameters layoutParameters {
			get {
				return _layoutParameters;
			}
			set {
				_layoutParameters = value; // Allowed to be null.
			}
		}

		// This field is used internally only. Can be removed once we have fully migrated to the LayoutManager and dropped support for its Legacy mode.
		internal _ClearVRViewportAndObjectPose clearVRViewportAndObjectPose {
			get {
				return _clearVRViewportAndObjectPose;
			}
			set {
				_clearVRViewportAndObjectPose = value;
			}
		}
		private _ClearVRViewportAndObjectPose _clearVRViewportAndObjectPose;

		private ContentItem _contentItem;
		internal TimingParameters _timingParameters; // Internal, as we need to modify it ourselves if null is provided
		internal AudioTrackAndPlaybackParameters _audioTrackAndPlaybackParameters; // Internal, as we need to modify it ourselves if null is provided
		private SyncSettings _syncSettings;
        private long _approximateDistanceFromLiveEdgeInMilliseconds;
		private LayoutParameters _layoutParameters;

		/// <summary>
		/// The default constructor taking the minimum subset of arguments required.
		/// > [!WARNING]
		/// > Since v9.x
		/// > Although accepted, passing `null` as `argLayoutParameters` is NOT recommened. Instead, use the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) to configure your audio/video feed layout. Legacy Display Object positioning will be performed hen set to null, but this behaviour will be removed after 2023-03-31.
		/// > When playing Mosaic content, this parameter *MUST* be specified and cannot be null.
		/// </summary>
		/// <param name="argContentItem">The content item to play</param>
		/// <param name="argTimingParameters">The timing parameters, specifying how to start playback. Setting this to `null` will trigger default behaviour and is a valid value, see [TimingParameters](xref:com.tiledmedia.clearvr.TimingParameters) for details.</param>
		/// <param name="argLayoutParameters">The LayoutParameters specifying which DisplayObject will render what Feed. For backwards compatibility with SDKs prior to v9.0, you can set this argument to `null`. This will trigger default display object placement behaviour, but this mode will be removed after 2023-03-31.</param>
		internal PlaybackParameters(ContentItem argContentItem, TimingParameters argTimingParameters, LayoutParameters argLayoutParameters) { 
			this.contentItem = argContentItem;
			this.timingParameters = argTimingParameters;
			this.layoutParameters = argLayoutParameters;
			this.audioTrackAndPlaybackParameters = null;
			this.syncSettings = null;
			this.approximateDistanceFromLiveEdgeInMilliseconds = 0;
		}
		
		// protected copy constructor
		protected PlaybackParameters(PlaybackParameters argOtherPlaybackParameters) {
			this.contentItem = argOtherPlaybackParameters._contentItem;
			this.timingParameters = argOtherPlaybackParameters._timingParameters;
			this.layoutParameters = argOtherPlaybackParameters._layoutParameters;
			this.syncSettings = argOtherPlaybackParameters.syncSettings;
			this.approximateDistanceFromLiveEdgeInMilliseconds = argOtherPlaybackParameters.approximateDistanceFromLiveEdgeInMilliseconds;
			this.audioTrackAndPlaybackParameters = argOtherPlaybackParameters._audioTrackAndPlaybackParameters;
			this.clearVRViewportAndObjectPose = argOtherPlaybackParameters._clearVRViewportAndObjectPose;
		}
		
		public override String ToString() {
			return String.Format("ContentItem: {0}, TimingParameters: {1}. LayoutParameters: '{2}', AudioTrackAndPlaybackParameters: {3}, SyncSettings: {4}", Utils.GetAsStringEvenIfNull(contentItem, "null"), Utils.GetAsStringEvenIfNull(timingParameters, "null"), Utils.GetAsStringEvenIfNull(layoutParameters, "null"), Utils.GetAsStringEvenIfNull(audioTrackAndPlaybackParameters, "null"), Utils.GetAsStringEvenIfNull(syncSettings, "null"));
		}
	}

	/// <summary>
	/// Parameters used to prepare a ClearVRPlayer for content playout.
	/// </summary>
	public class PrepareContentParameters : PlaybackParameters {
		/// <summary>
		/// Flags can be used to tune content loading behaviour. Currently, no flags are supported. This will allways be set to the default value: 0.
		/// </summary>
        // Note that the flags field is not yet serialized in the protobuf equivalent because this field does not yet exist on the protobuf message.
		[Obsolete("This field has been deprecated in v9.0 and will be removed after 2023-01-31. Please remove any references to it from your code. There is no equivalent property available anymore.", true)]
		public long flags {
			get { return 0; }
			set { /* NOOP, deprecated */ }
		}

		/// <summary>
		/// The time it takes before loading the specified clip is considered to have timed out. Any negative value or 0 will be considered as "use the default value", which is 30000 msec.
		/// </summary>
		public int timeoutInMilliseconds {
			get { return _timeoutInMilliseconds; }
			set { _timeoutInMilliseconds = value; }
		}

		private int _timeoutInMilliseconds;
		/// <summary>
		/// Specifies the parameters used for loading the ContentItem during ClearVRPlayer initialization.
		/// </summary>
		/// <param name="argContentItem">The content item to load. Can not be null.</param>
		/// <param name="argTimingParameters">Configures when playback should start on the provided ContentItem. If set to the default value null, VOD clips will start playback at position 0 while LIVE clips will start at the live edge.</param>
		/// <param name="argSyncSettings">Enable sync with supplied settings. Default value: null (e.g. sync is disabled)</param>
		/// <param name="argFlags">The initialization flags for preparing the content item. Default value: 0. Keep at 0 for now."</param>
		/// <param name="argTimeoutInMilliseconds">Any value &lt;= 0 will result in the default timeout to be used. This default value is 30000 milliseconds. This timeout will only be triggered if loading a specific content item took longer than the specified amount of time. In this case, one will receive a ClearVRCoreWrapperContentLoadingTimeout message.</param>
        /// <param name="argApproximateDistanceFromLiveEdgeInMilliseconds">Specifies the approximate offset that should be kept from the live edge. Default value: 0 (msec). Note that a non-0 minimum value might be enforced. Changing the default value is strongly discouraged, see also [approximateDistanceFromLiveEdgeInMilliseconds](xref:com.tiledmedia.clearvr.PlaybackParameters.approximateDistanceFromLiveEdgeInMilliseconds) for details. </param>
		/// <param name="argPreferredAudioTrackLanguage">Override the preferred audio track language by setting this to the preferred ISO-639 language code. Only works if language codes are embedded in the source stream. Default value: "", which is interpreted as "automatically pick an audio track".</param>
		[Obsolete("This constructor is deprecated and can no longer be used. Use the (ContentItem, TimingParameters, SyncSettings, long, int, long, LayoutParameters) constructor instead.", true)]
		public PrepareContentParameters(ContentItem argContentItem, TimingParameters argTimingParameters, SyncSettings argSyncSettings, long argFlags, int argTimeoutInMilliseconds, long argApproximateDistanceFromLiveEdgeInMilliseconds, String argPreferredAudioTrackLanguage) : base(argContentItem, argTimingParameters, null) { 
			/* Deprecated, intewntionally left empty*/
		}
		
		/// <summary>
		/// Specifies the parameters used for loading the ContentItem during ClearVRPlayer initialization.
		/// </summary>
		/// <param name="argContentItem">The content item to load. Can not be null.</param>
		/// <param name="argTimingParameters">Configures when playback should start on the provided ContentItem. If set to the default value null, VOD clips will start playback at position 0 while LIVE clips will start at the live edge.</param>
		/// <param name="argSyncSettings">Enable sync with supplied settings. Default value: null (e.g. sync is disabled)</param>
		/// <param name="argFlags">The initialization flags for preparing the content item. Default value: 0. Keep at 0 for now."</param>
		/// <param name="argTimeoutInMilliseconds">Any value &lt;= 0 will result in the default timeout to be used. This default value is 30000 milliseconds. This timeout will only be triggered if loading a specific content item took longer than the specified amount of time. In this case, one will receive a ClearVRCoreWrapperContentLoadingTimeout message.</param>
        /// <param name="argApproximateDistanceFromLiveEdgeInMilliseconds">Specifies the approximate offset that should be kept from the live edge. Default value: 0 (msec). Note that a non-0 minimum value might be enforced. Changing the default value is strongly discouraged, see also [approximateDistanceFromLiveEdgeInMilliseconds](xref:com.tiledmedia.clearvr.PlaybackParameters.approximateDistanceFromLiveEdgeInMilliseconds) for details. </param>
		/// <param name="argLayoutParameters">Configure the layout of the stream, refer to the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager] for details.</param>
		/// <returns></returns>
		[Obsolete("This constructor has been deprecated and can no longer be used. Instead, use the (ContentItem, TimingParameters, LayoutParameters) constuctor and optionally set other properties depending on your needs. ", true)]
		public PrepareContentParameters(ContentItem argContentItem, TimingParameters argTimingParameters = null, SyncSettings argSyncSettings = null, long argFlags = 0, int argTimeoutInMilliseconds = 0, long argApproximateDistanceFromLiveEdgeInMilliseconds = 0, LayoutParameters argLayoutParameters = null) : base(argContentItem, argTimingParameters, argLayoutParameters) { }

		/// <summary>
		/// The default constructor taking the minimum subset of arguments required.
		/// > [!WARNING]
		/// > Since v9.x
		/// > Although accepted, passing `null` as `argLayoutParameters` is NOT recommened. Instead, use the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) to configure your audio/video feed layout. Legacy Display Object positioning will be performed hen set to null, but this behaviour will be removed after 2023-03-31.
		/// > When playing Mosaic content, this parameter *MUST* be specified and cannot be null.
		/// </summary>
		/// <param name="argContentItem">The content item to play</param>
		/// <param name="argTimingParameters">The timing parameters, specifying how to start playback. Setting this to `null` will trigger default behaviour and is a valid value, see [TimingParameters](xref:com.tiledmedia.clearvr.TimingParameters) for details.</param>
		/// <param name="argLayoutParameters">The LayoutParameters specifying which DisplayObject will render what Feed. For backwards compatibility with SDKs prior to v9.0, you can set this argument to `null`. This will trigger default display object placement behaviour, but this mode will be removed after 2023-03-31.</param>
		public PrepareContentParameters(ContentItem argContentItem, TimingParameters argTimingParameters, LayoutParameters argLayoutParameters) : base(argContentItem, argTimingParameters, argLayoutParameters) {
			this._timeoutInMilliseconds = 0;
		} 

		private PrepareContentParameters(PlaybackParameters argPlaybackParameters) : base(argPlaybackParameters) { }

		internal PrepareContentParameters Copy() {
			PrepareContentParameters copy = new PrepareContentParameters((PlaybackParameters) this);
			copy.timeoutInMilliseconds = this.timeoutInMilliseconds;
			return copy;
		}

		/// <summary>Return a protobuf representation of the prepare content parameters</summary>
		// Note that the flags field is not yet serialized in the protobuf equivalent because this field does not yet exist on the protobuf message.
        internal cvri.PrepareContentParametersMediaflow ToCoreProtobuf(PlatformOptionsBase argPlatformOptions) {
			// Note that the _flags field is utterly unterminated here. We should remove it some day as it has been replaced by PlatformOptions.initializeFlags.
			var proto = new cvri.PrepareContentParametersMediaflow() {
				InitializeParams = new cvri.InitializeParams() {
					InitializeParamsHeader = new cvri.InitializeParamsHeader() {
						License = com.tiledmedia.clearvr.protobuf.ByteString.CopyFrom(argPlatformOptions.licenseFileBytes),
						DisableCachePrewarming = ((argPlatformOptions.initializeFlags & (long)InitializeFlags.NoCachePrewarming) != 0),
						DeviceAppID = "" // The deviceAppID is always an empty String.
					},
					ContentItem = contentItem.ToCoreProtobuf(),
					StartPositionParams = timingParameters != null ? timingParameters.ToCoreProtobuf() : null,
					ViewportAndDisplayObjectPose = clearVRViewportAndObjectPose != null ? clearVRViewportAndObjectPose.ToCoreProtobuf() : new _ClearVRViewportAndObjectPose().ToCoreProtobuf(),
					ApproxDistanceFromLiveEdge = approximateDistanceFromLiveEdgeInMilliseconds,
					FeedConfiguration = layoutParameters != null ? layoutParameters.ToCoreProtobuf() : null
				},
				AudioTrackAndPlaybackParametersMediaFlow = audioTrackAndPlaybackParameters.ToCoreProtobuf(),
				StartClearVRCoreTimeoutInMilliseconds = timeoutInMilliseconds,

			};
			proto.InitializeParams.SyncEnabled = (syncSettings != null);
			if (syncSettings != null) {
                proto.InitializeParams.SyncSettings = syncSettings.ToCoreProtobuf();
			}
			return proto;
		}

		internal static PrepareContentParameters FromCoreProtobuf(cvrinterface.PrepareContentParametersMediaflow corePrepareContentParams, ClearVRLayoutManager argClearVRLayoutManager) {
			SyncSettings syncSettings = null;
			if (corePrepareContentParams.InitializeParams.SyncEnabled) 
				syncSettings = SyncSettings.FromCoreProtobuf(corePrepareContentParams.InitializeParams.SyncSettings);
			
			AudioTrackAndPlaybackParameters audioTrackAndPlaybackParameters = null;

			if (corePrepareContentParams.AudioTrackAndPlaybackParametersMediaFlow != null)
				audioTrackAndPlaybackParameters = AudioTrackAndPlaybackParameters.FromCoreProtobuf(corePrepareContentParams.AudioTrackAndPlaybackParametersMediaFlow);


			PrepareContentParameters pcp = new PrepareContentParameters(
				ContentItem.FromCoreProtobuf(corePrepareContentParams.InitializeParams.ContentItem),
				TimingParameters.FromCoreProtobuf(corePrepareContentParams.InitializeParams.StartPositionParams),
				LayoutParameters.FromCoreProtobuf(corePrepareContentParams.InitializeParams.FeedConfiguration, argClearVRLayoutManager));
			pcp.audioTrackAndPlaybackParameters = audioTrackAndPlaybackParameters;
			pcp.syncSettings = syncSettings;
			pcp.timeoutInMilliseconds = corePrepareContentParams.StartClearVRCoreTimeoutInMilliseconds;
			pcp.approximateDistanceFromLiveEdgeInMilliseconds = corePrepareContentParams.InitializeParams.ApproxDistanceFromLiveEdge;
			return pcp;
		}

        public override String ToString() {
            return String.Format("{0}, TimeoutInMilliseconds: {1}", base.ToString(), timeoutInMilliseconds);
		}
	}

	/// <summary>
	/// Describes the exact fish eye lens parameters. A number of pre-defined lenses are available, see [FisheyePresets](xref:com.tiledmedia.clearvr.FisheyePresets).
	/// </summary>
	public class FishEyeSettings {
        /// <summary>
        /// The ClearVR SDK supports various well-known camera and lens-type combinations out of the box, saving you from the trouble to specify all parameters manually.
		/// If you set this value to anything other than `Custom`, all other fields will be ignored. 
		/// > [!NOTE]
		/// > You are strongly encouraged to use one of the available presets. If a preset for your Camera and Lens Type combination is missing, please contact Tiledmedia to have it added for you.
		/// > Specifying a custom Camera and Lens Type is considered an expert feature that requires in-depth knowledge of the matter at hand.
        /// </summary>
        internal FisheyePresets fisheyePreset { get; set; }
        /// <summary>
        /// Preset lens types for a number of well-known fish eye cameras.
        /// </summary>
        internal FishEyeLensTypes fisheyeLensType { get; set; }
        /// <summary>
        /// The 35mm equivalent focal length of the lense-sensor of the camera in mm
        /// </summary>
        internal float focalLength { get; set; }
        /// <summary>
        /// the invert of the size of a pixel in mm^-1
        /// </summary>
        internal float sensorPixelDensity { get; set; }
        /// <summary>
        /// The width in pixels used to compute the sensor pixel density
        /// </summary>
        internal int referenceWidth { get; set; }
        /// <summary>
        /// The height in pixels used to compute the sensor pixel density
        /// </summary>
        internal int referenceHeight { get; set; }

		/// <summary>
        /// The ClearVR SDK supports various well-known camera and lens-type combinations out of the box, saving you from the trouble to specify all parameters manually.
		/// If you set this value to anything other than `Custom`, all other fields will be ignored. 
		/// TYpically, it suffices to only specify the first argument (argFishEyeCameraAndLensType). You can leave all other arguments at their default value.
		/// > [!WARNING]
		/// > Specifying a custom camera and lens type is considered an expert feature of this SDK. If you run into problems, please contact Tiledmedia.
		/// > [!WARNING]
		/// > The FishEyeCameraAndLensTypes enum is deprecated. One must use the [FisheyePresets](xref:com.tiledmedia.clearvr.FisheyePresets) enum instead.
		/// </summary>
		/// <param name="argFishEyeCameraAndLensType">Select a particular camera and lens combination for default settings, or specify everything with CUSTOM</param>
		/// <param name="argFishEyeLensType">The shape of the fisheye lens"</param>
		/// <param name="argFocalLength">The effective focal length for the lens in millimeters</param>
		/// <param name="argSensorPixelDensity">The pixel density for the sensor per square millimeter</param>
		/// <param name="argReferenceWidth">The Reference width for which density parameter is correct. Defaults to the available pixels, but might be unequal if a zoom is applied</param>
		/// <param name="argReferenceHeight">The Reference height for which density parameter is correct. Defaults to the available pixels, but might be unequal if a zoom is applied</param>
		[Obsolete("This constructor has been deprecated and will be removed after 2023/01/31. It has been replaced by the constructor using the FisheyePresets enum.", true)]
        public FishEyeSettings(FishEyeCameraAndLensTypes argFishEyeCameraAndLensType,
                                FishEyeLensTypes argFishEyeLensType = FishEyeLensTypes.DefaultFisheyeLens,
                                float argFocalLength = 0,
                                float argSensorPixelDensity = 0,
                                int argReferenceWidth = 0,
                                int argReferenceHeight = 0) {
            fisheyePreset = FisheyePresets.Custom;
            fisheyeLensType = argFishEyeLensType;
            focalLength = argFocalLength;
            sensorPixelDensity = argSensorPixelDensity;
            referenceWidth = argReferenceWidth;
            referenceHeight = argReferenceHeight;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fisheyePreset"></param>
		/// <param name="fisheyeLensType"></param>
		/// <param name="focalLength"></param>
		/// <param name="sensorPixelDensity"></param>
		/// <param name="referenceWidth"></param>
		/// <param name="referenceHeight"></param>
		public FishEyeSettings(
			FisheyePresets fisheyePreset, 
			FishEyeLensTypes fisheyeLensType = FishEyeLensTypes.DefaultFisheyeLens, 
			float focalLength = 0, 
			float sensorPixelDensity = 0, 
			int referenceWidth = 0, 
			int referenceHeight = 0
		) {
			this.fisheyePreset = fisheyePreset;
			this.fisheyeLensType = fisheyeLensType;
			this.focalLength = focalLength;
			this.sensorPixelDensity = sensorPixelDensity;
			this.referenceWidth = referenceWidth;
			this.referenceHeight = referenceHeight;
		}

        public FishEyeSettings Copy() {
            return new FishEyeSettings(this.fisheyePreset, this.fisheyeLensType, this.focalLength, this.sensorPixelDensity, this.referenceWidth, this.referenceHeight);
        }

        public override String ToString() {
            return String.Format("camera and lens: {0}, lens type: {1}, focal length: {2}, sensor pixel density: {3}, reference width: {4}, reference height: {5}", fisheyePreset, fisheyeLensType, focalLength, sensorPixelDensity, referenceWidth, referenceHeight);
        }

		/// <summary>Return a protobuf representation of the FishEyeSettings</summary>
		internal cvri.FishEyeSettings ToCoreProtobuf() {
			if (fisheyeLensType == FishEyeLensTypes.Polynomial) {
				throw new Exception("[ClearVR] Overiding Polynomial fish-eye lens is not yet supported. Use a predefined camera_lens or contact tiledmedia");
			}
			return new cvri.FishEyeSettings() {
				FisheyePreset = FisheyePresetsMethods.GetStringValue(this.fisheyePreset),
				LensType = (cvri.FishEyeLensType) fisheyeLensType,
				IdealLensModelSettings = new cvri.IdealLensModelSettings() {
					FocalLength = focalLength,
					SensorPixelDensity = sensorPixelDensity,
					ReferenceWidth = referenceWidth,
					ReferenceHeight = referenceHeight,
				}
			};
		}

		/// <summary>
		///  Convert core protobuf fish eye settings to FishEyeSettings
		/// </summary>
		/// <param name="coreFishEyeSettings"></param>
		/// <returns>SDK representation of protobuf FishEyeSettings</returns>
		internal static FishEyeSettings FromCoreProtobuf(cvri.FishEyeSettings coreFishEyeSettings) {
			return new FishEyeSettings(
				FisheyePresetsMethods.FromStringValue(coreFishEyeSettings.FisheyePreset), 
				(FishEyeLensTypes) coreFishEyeSettings.LensType, 
				coreFishEyeSettings.IdealLensModelSettings != null ? coreFishEyeSettings.IdealLensModelSettings.FocalLength : 0, 
				coreFishEyeSettings.IdealLensModelSettings != null ? coreFishEyeSettings.IdealLensModelSettings.SensorPixelDensity : 0, 
				coreFishEyeSettings.IdealLensModelSettings != null ? coreFishEyeSettings.IdealLensModelSettings.ReferenceWidth : 0, 
				coreFishEyeSettings.IdealLensModelSettings != null ? coreFishEyeSettings.IdealLensModelSettings.ReferenceHeight : 0);
		}
	}

	/// <summary>
	/// Parameters when testing whether a (or multiple) clip can be played back or not.
	/// </summary>
	public class ContentSupportedTesterParameters {
		internal ContentItem[] contentItemList;
		/// <summary>
		/// Aggregates screen width, screen height and device type. 
		/// Since v8.0, this replaces the screenWidth, screenHeight and deviceType fields.
		/// </summary>
		internal DeviceParameters deviceParameters = null;
		/// <summary>
		/// Since v5.1
		/// Specify the HTTP proxy settings. Note that the lower-level SDK will attempt to detect proxy host and port automatically if host and port are at their default values ("&lt;auto&gt;" and -1 respectively).
		/// Due to platform security constraints, we cannot detect username and password automatically. We must rely on the application to provide those. See [SetProxyParameters](xref:com.tiledmedia.clearvr.ProxyParameters.SetProxyParameters(String,System.Int32,String,String)) for details.
		/// </summary>
		public readonly ProxyParameters httpProxyParameters = new ProxyParameters(ProxyTypes.Http);
		/// <summary>
		/// Since v5.1
		/// Specify the HTTPS proxy settings. Note that the lower-level SDK will attempt to detect proxy host and port automatically if host and port are at their default values ("&lt;auto&gt;" and -1 respectively).
		/// Due to platform security constraints, we cannot detect username and password automatically. We must rely on the application to provide those. See [SetProxyParameters](xref:com.tiledmedia.clearvr.ProxyParameters.SetProxyParameters(String,System.Int32,String,String)) for details.
		/// </summary>
		public readonly ProxyParameters httpsProxyParameters = new ProxyParameters(ProxyTypes.Https);
		/// <summary>
		/// This field allows you to override the user agent field in each video-streaming related HTTP request.
		/// As viewport-adaptive streaming results in many HTTP requests, one is STRONGLY discouraged to set this field as it will increase network overhead.. 
		/// If needed, be sure to keep this String as short as possible.
		/// Default value: "" (an empty string)
		/// </summary>
		public String overrideUserAgent = "";
		/// <summary>
		/// In some cases, a stereoscopic clip cannot be rendered stereoscopic due to insufficient decoder capabilities. For example, playing a 12K stereoscopic clip requires a hardware decoder that can decode at least 6K. If you would've tried to play this clip on a 4K decoder, playback would simply fail.
		/// If you enable this option (e.g. set it to true), the player will attempt to render the clip in monoscopic in case insufficient decoder capacity is detected for stereoscopic playback, permitting that the decoder has sufficient capacity to do so.
		/// Default value: false
		/// > [!WARNING]
		/// > Note that this can only be configured once, either when calling the ClearVRPlayer.TestIsContentSupported() API or clearVRPlayer.Initialize(), whichever comes first. From that point onward, it cannot be changed until you completely stop the ClearVRPlayer.
		/// </summary>
		public bool allowDecoderContraintsInducedStereoToMono = false;

		/// <summary>
		/// Configure telemetry services reporting. See [TelemetryConfiguration](xref:com.tiledmedia.clearvr.TelemetryConfiguration) for details.
		/// Default value: null.
		/// </summary>
		public TelemetryConfiguration telemetryConfiguration = null;

		/// <summary>
		/// Default constructor.
		/// If one wants to specify HTTP/HTTPS proxy parameters, one can use the [SetProxyParameters](xref:com.tiledmedia.clearvr.ProxyParameters.SetProxyParameters(String,System.Int32,String,String)) as you cannot directly assign to the [](xref:com.tiledmedia.clearvr.ContentSupportedTesterParameters.httpProxyParameters) and [](xref:com.tiledmedia.clearvr.ContentSupportedTesterParameters.httpsProxyParameters) fields.
		/// </summary>
		/// <param name="argContentItemList">A list of ContentItems that need to be checked. Cannot be null, nor empty. Passed by `ref`ernce, as their contentSupportedStatus fields will be overwritten.</param>
		/// <param name="argDeviceParameters">The device parameters. Keep at the default value null for auto-detection.</param>
		/// <param name="argOverrideUserAgent">OVerride the user agent, send with every HTTP request. One is strongly advised to always leave this at its default value, an empty string.</param>
		/// <param name="argAllowDecoderContraintsInducedStereoToMono">If enabled, a stereoscopic clip will be played as monoscopic if the available video decoder has insufficient decoding capacity to decode it stereoscopically. Note that this can only be configured once, either when calling the TestIsContentSupported() API or Initializing a ClearVRPlayer, whichever comes first. Default value: false</param>		
		/// <param name="argTelemetryConfiguration">Configure telemetry services reporting. See [TelemetryConfiguration](xref:com.tiledmedia.clearvr.TelemetryConfiguration) for details. Default value: null</param>
		public ContentSupportedTesterParameters(ref ContentItem[] argContentItemList, DeviceParameters argDeviceParameters = null, String argOverrideUserAgent = "", Boolean argAllowDecoderContraintsInducedStereoToMono = false, TelemetryConfiguration argTelemetryConfiguration = null) {
			contentItemList = argContentItemList;
			overrideUserAgent = argOverrideUserAgent;
			if(argDeviceParameters == null) {
				argDeviceParameters = new DeviceParameters(); // Construct new DeviceParameters based on default values.
			}
			deviceParameters = argDeviceParameters;
			deviceParameters.Verify();
			allowDecoderContraintsInducedStereoToMono = argAllowDecoderContraintsInducedStereoToMono;
		}

		/// <summary>
		/// Default constructor.
		/// If one wants to specify HTTP/HTTPS proxy parameters, one can use the [SetProxyParameters](xref:com.tiledmedia.clearvr.ProxyParameters.SetProxyParameters(String,System.Int32,String,String)) as you cannot directly assign to the [](xref:com.tiledmedia.clearvr.ContentSupportedTesterParameters.httpProxyParameters) and [](xref:com.tiledmedia.clearvr.ContentSupportedTesterParameters.httpsProxyParameters) fields.
		/// </summary>
		/// <param name="argContentItemList">A list of ContentItems that need to be checked. Cannot be null, nor empty. Passed by `ref`ernce, as their contentSupportedStatus fields will be overwritten.</param>
		/// <param name="argDeviceType">The device type. Keep at the default value (DeviceTypes.Unknown) for auto-detection.</param>
		/// <param name="argScreenWidth">The width of the screen in pixels. Keep at the default value 0 for auto-detection.</param>
		/// <param name="argScreenHeight">The height of the screen in pixels. Keep at the default value 0 for auto-detection.</param>
		/// <param name="argOverrideUserAgent">OVerride the user agent, send with every HTTP request. One is strongly advised to always leave this at its default value, an empty string.</param>
		[Obsolete("This constructor is obslete and will be removed after 2020/12/31. Please use ContentSupportedTesterParameters(ref ContentItemList, DeviceParameters, String) instead.", true)]
		public ContentSupportedTesterParameters(ref ContentItem[] argContentItemList, DeviceTypes argDeviceType = DeviceTypes.Unknown, short argScreenWidth = 0, short argScreenHeight = 0, String argOverrideUserAgent = "") : 
		 	this(ref argContentItemList, new DeviceParameters(argScreenWidth, argScreenHeight, argDeviceType), argOverrideUserAgent) {
		}

		internal bool Verify() {
			if (contentItemList == null || contentItemList.Length == 0) {
				return false;
			}
			if(!deviceParameters.Verify()) {
				return false;
			}
			return true;
		}

		/// <summary>Return a protobuf representation of the ContentSupportedTesterParameters</summary>
		internal cvri.ContentSupportedTesterParametersMediaFlow ToCoreProtobuf() {
			cvri.ContentSupportedTesterParametersMediaFlow contentSupportedParams = new cvri.ContentSupportedTesterParametersMediaFlow();
			contentSupportedParams.CheckIsSupportedParams = new cvri.CheckIsSupportedParams();
			foreach (var contentItem in contentItemList) {
				contentSupportedParams.CheckIsSupportedParams.ContentItems.Add(contentItem.ToCoreProtobuf());
            }
			contentSupportedParams.CreateContextParams = new cvri.CreateContextParams() {
				PersistenceFolderPath = "",
				DeviceParams = deviceParameters.ToCoreProtobuf(),
				SDKType = ClearVRPlayer.SDK_TYPE.ToCoreProtobuf()
			};
			contentSupportedParams.HttpProxyParamsMediaFlow = httpProxyParameters.ToCoreProtobuf();
			contentSupportedParams.HttpsProxyParamsMediaFlow = httpsProxyParameters.ToCoreProtobuf();
			contentSupportedParams.OverrideUserAgent = overrideUserAgent;
			contentSupportedParams.AllowDecoderContraintsInducedStereoToMono = allowDecoderContraintsInducedStereoToMono;
			if(telemetryConfiguration != null) {
				contentSupportedParams.CreateContextParams.TelemetryConfig = telemetryConfiguration.ToCoreProtobuf();
			}			
			return contentSupportedParams;
		}
	}
	/// <summary>
	/// The [TestIsContentSupported](xref:com.tiledmedia.clearvr.ClearVRPlayer.TestIsContentSupported(com.tiledmedia.clearvr.ContentSupportedTesterParameters,Action{com.tiledmedia.clearvr.ContentSupportedTesterReport,System.Object[]},Action{com.tiledmedia.clearvr.ClearVRMessage,System.Object[]},System.Object[])) API returns a ContentSupportedTesterReport object upon completion.
	/// When parsing, one should first verify that the test was a success by calling [GetIsSuccess()](xref:com.tiledmedia.clearvr.ClearVRMessage.GetIsSuccess) on the `argClearVRMessage` argument in the callback.
	/// <list type="bullet">
	/// <item>
	/// <term>Success == true</term>
	/// <description> the contentItemList will hold an array of the same [ContentItem](xref:com.tiledmedia.clearvr.ContentItem) as you specified in the [ContentSupportedTesterParameters](xref:com.tiledmedia.clearvr.ContentSupportedTesterParameters) argument of the original test call, as you passed those along by reference. Note that, even in case of a successful test, a `ContentItem` can still have a `ContentSupportedStatus` 'Unknown' because not all content item types are checken. Currently, only ClearVR clips (`manifest.json`) are tested.</description>
	/// </item>
	/// <item>
	/// <term>Success == false</term>
	/// <description> one can check the `argClearVRMessage` argument for details on why it has failed.</description>
	/// </item>
	/// </list>
	/// </summary>
	public class ContentSupportedTesterReport {
		private ContentItem[] _contentItemList;
		private System.Object[] _optionalArguments;
		/// <summary>
		/// Holds the optional arguments initially passed along in the call to [TestIsContentSupported](xref:com.tiledmedia.clearvr.ClearVRPlayer.TestIsContentSupported(com.tiledmedia.clearvr.ContentSupportedTesterParameters,Action{com.tiledmedia.clearvr.ContentSupportedTesterReport,System.Object[]},Action{com.tiledmedia.clearvr.ClearVRMessage,System.Object[]},System.Object[]))
		/// </summary>
		/// <value>The objects (nothing, one or multiple).</value>
		public object[] optionalArguments {
			get {
				return _optionalArguments;
			}
		}

		/// <summary>
		/// The list of ContentItems. These have all been checked and are a reference of the original once passed along.
		/// </summary>
		/// <value></value>
		public ContentItem[] contentItemList {
			get {
				return _contentItemList;
			}
		}

		internal ContentSupportedTesterReport(ref ContentItem[] argContentItemList, System.Object[] argOptionalArguments) {
			_contentItemList = argContentItemList;
			_optionalArguments = argOptionalArguments;
		}

		/// <summary>
		/// Returns the contents of this report as a string.
		/// </summary>
		/// <returns></returns>
		public override String ToString() {
			return String.Format("Content item list: {0}", String.Join(",", _contentItemList.Select(x => x.ToString()).ToArray()));
		}
	}

	/// <summary>
	/// Enum describing whether a ContentItem can be played back or not.
	/// Note that this only holds for device configuration as specified when testing using the [TestIsContentSupported](xref:com.tiledmedia.clearvr.ClearVRPlayer.TestIsContentSupported(com.tiledmedia.clearvr.ContentSupportedTesterParameters,Action{com.tiledmedia.clearvr.ContentSupportedTesterReport,System.Object[]},Action{com.tiledmedia.clearvr.ClearVRMessage,System.Object[]},System.Object[])) API
	/// </summary>
	// This enum must be aligned with the equivalent enum in MF-iOS, MF-PC and MF-Android
	public enum ContentSupportedStatus {
		/// <summary>By default, it is unknown whether a ContentItem is supported or not. Note that even after a succesful test, the status might still be Unknown as only ClearVR clips can be tested.</summary>
		Unknown = 0,
		/// <summary>Indicates that the ContentItem can be played on the DeviceType and screen dimensions used while testing.</summary>
		Supported = 1,
		/// <summary>
		/// Indicates that the ContentItem can be played on the DeviceType and screen dimensions used while testing BUT as monoscopic only.
		/// > [!NOTE]
		/// > This value will only be returned if allowDecoderContraintsInducedStereoToMono is set to true on PlatformOptions or ContentSupportedTesterParameters, whichever comes first.
		/// </summary>
		SupportedAsMonoscopicOnly = 2,
		/// <summary>Indicates that the ContentItem can NOT be played on the DeviceType and screen dimensions used while testing.</summary>
		NotSupported = 3
	}

	internal class ContentSupportedStatusMethods {
		internal static ContentSupportedStatus FromCoreStatus(cvri.IsSupportedResult argStatus) {
            switch (argStatus) {
				case cvri.IsSupportedResult.Supported:
					return ContentSupportedStatus.Supported;
				case cvri.IsSupportedResult.SupportedAsMonoscopicOnly:
					return ContentSupportedStatus.SupportedAsMonoscopicOnly;
				case cvri.IsSupportedResult.Unsupported:
					return ContentSupportedStatus.NotSupported;
                default:
                    return ContentSupportedStatus.Unknown;
            }
        }
    }

	/// <summary>
	/// The InternalReport is converted into a public report. Serialization of the report happens in the MediaFlow, deserialization happens here.
	/// </summary>
	internal class ContentSupportedTesterInternalReport {
		internal ContentSupportedStatus[] contentSupportedStatuses;
		internal ContentSupportedTesterInternalReport(ContentSupportedStatus[] argContentSupportedStatuses) {
			contentSupportedStatuses = argContentSupportedStatuses;
		}

		/// <summary>
		/// Deserializes a serialized ContentSUpportedTesterInternalReport ClearVRMessage.
		/// In contrast to other deserializers, this method does not return a bool success.
		/// </summary>
		/// <param name="argSerializedString">The serialized message to deserialize.</param>
		/// <param name="argContentSupportedTesterInternalReport">The target object to write the deserialized report into.</param>
		internal static void Deserialize(String argSerializedString, out ContentSupportedTesterInternalReport argContentSupportedTesterInternalReport) {
			argContentSupportedTesterInternalReport = null;
			String[] splittedString = argSerializedString.Split(';');
			int count = argSerializedString.Contains(';') ? splittedString.Length : 0;
			ContentSupportedStatus[] contentSupportedStatuses = new ContentSupportedStatus[count];
			for(int i = 0; i < count; i++) {
				try {
					contentSupportedStatuses[i] = (ContentSupportedStatus) Int32.Parse(splittedString[i]);
				} catch { // ignore any exception
					contentSupportedStatuses[i] = ContentSupportedStatus.Unknown;
				}
			}
			argContentSupportedTesterInternalReport = new ContentSupportedTesterInternalReport(contentSupportedStatuses);
		}
	}	

	/// <summary>
	/// This object holds detailed information about the position in the currently playing clip, as well as the lower and upper seek bounds.
	/// This object is especially useful when interested in the distance to the live edge in case of LIVE clip.
	/// Interpretation of the various timing fields should be based on the flag field. 
	/// > [!IMPORTANT]
	/// > The timing information on this object is only useful if the [](xref:com.tiledmedia.clearvr.TimingReport.GetIsSuccess) API returns true. In case of false, the timing fields should be ignored as their values and meaning are undefined.
	/// </summary>
	public class TimingReport {
		private ClearVRMessage _clearVRMessage;
		private TimingTypes _timingType;
		private long _upperSeekBoundInMilliseconds;
		private long _lowerSeekBoundInMilliseconds;
		private long _currentPositionInMilliseconds;
		private long _contentDurationInMilliseconds;
		private EventTypes _eventType;

		internal TimingReport(ClearVRMessage argClearVRMessage,
								TimingTypes argTimingType,
								long argCurrentPosition,
								long argLowerSeekBound,
								long argUpperSeekBound,
								long argContentDuration,
								EventTypes argEventType) {
			_clearVRMessage = argClearVRMessage;
			_timingType = argTimingType;
			_currentPositionInMilliseconds = argCurrentPosition;
			_lowerSeekBoundInMilliseconds = argLowerSeekBound;
			_upperSeekBoundInMilliseconds = argUpperSeekBound;
			_contentDurationInMilliseconds = argContentDuration;
			_eventType = argEventType;
		}

		/// <summary>
        /// initialize TimingReport through protobuf message.
        /// </summary>
        /// <param name="argTimingReportMessage">the protobuf message containing the timing report</param>
		internal TimingReport(cvri.TimingReport argTimingReportMessage) {
			_clearVRMessage = ClearVRMessage.GetGenericOKMessage();
			if (argTimingReportMessage.ErrorCode != 0) {
				_clearVRMessage = new ClearVRMessage((int) ClearVRMessageTypes.Warning, argTimingReportMessage.ErrorCode, argTimingReportMessage.ErrorMessage, ClearVRResult.Failure);
            }
			_timingType = TimingTypesMethods.GetAsTimingType((int)argTimingReportMessage.TimingType);
			_upperSeekBoundInMilliseconds = argTimingReportMessage.SeekUpperBound;
			_lowerSeekBoundInMilliseconds = argTimingReportMessage.SeekLowerBound;
			_currentPositionInMilliseconds = argTimingReportMessage.CurrentPosition;
			_contentDurationInMilliseconds = argTimingReportMessage.ContentDuration;
			_eventType = EventTypesMethods.FromCoreProtobuf(argTimingReportMessage.EventType);
		}

		/// <summary>
		/// The ClearVRMessage will be a GenericOK message in case of a valid report. It's [](xref:com.tiledmedia.clearvr.ClearVRMessage.GetIsSuccess) method will return false if an error was reported.
		/// You can check the fields of the ClearVRMessage for details if that would be the case. 
		/// </summary>
		/// <value>The ClearVRMessage.</value>
		public ClearVRMessage clearVRMessage {
			get {
				return _clearVRMessage;
			}
		}

		/// <summary>
		/// The SeekFlag indicates how the timing parameters on this object should be interpreted (e.g. as absolute content time or as wallclock synchronized time since epoch.)
		/// The value is undefined if GetIsSuccess() returns false.
		/// </summary>
		/// <value>The appropriate SeekFlag</value>
		[Obsolete("The flag field is obsolete and has been replaced by the TimingTypes timingType field.", true)]
		public System.Enum flag {
			get {
				return null;
			}
		}

		/// <summary>
		/// Determines how to interpret the reported values like position and upper/lower bound. See [TimingTypes](xref:com.tiledmedia.clearvr.TimingTypes) for details.
		/// </summary>
		public TimingTypes timingType {
			get {
				return _timingType;
			}
		}

		/// <summary>
		/// The current position with respect to the TimingType.
		/// The value is undefined if GetIsSuccess() returns false.
		/// </summary>
		/// <value>A positive integer denoting the current position.</value>
		public long currentPositionInMilliseconds {
			get {
				return _currentPositionInMilliseconds;
			}
		}
		/// <summary>
		/// The lower bound for seeking with respect to the TimingType.
		/// For live content, this is also known as the max timeshift.
		/// The value is undefined if GetIsSuccess() returns false.
		/// </summary>
		/// <value>A positive integer denoting the earliest seekable position.</value>
		public long lowerSeekBoundInMilliseconds {
			get {
				return _lowerSeekBoundInMilliseconds;
			}
		}

		/// <summary>
		/// The upper bound for seeking with respect to the TimingType.
		/// For live content, this is also known as the timeshift.
		/// The value is undefined if GetIsSuccess() returns false.
		/// </summary>
		/// <value>A positive integer denoting the latest seekable position.</value>
		public long upperSeekBoundInMilliseconds {
			get {
				return _upperSeekBoundInMilliseconds;
			}
		}
		
		/// <summary>
		/// The content duration.
		/// The value is undefined if GetIsSuccess() returns false.
		/// </summary>
		/// <value>A positive integer denoting the current duration.</value>
		public long contentDurationInMilliseconds {
			get {
				return _contentDurationInMilliseconds;
			}
		}

		/// <summary>
		/// Query whether this a Live, FinishedLive or VOD event. See [EventTypes](xref:com.tiledmedia.clearvr.EventTypes) for details.
		/// </summary>
		public EventTypes eventType {
			get {
				return _eventType;
			}
		}

		/// <summary>
		/// The value is undefined if GetIsSuccess() returns false.
		/// </summary>
		/// <returns></returns>
		public long GetDistanceFromUpperBoundInMilliseconds() {
        	return upperSeekBoundInMilliseconds - currentPositionInMilliseconds;
    	}

		/// <summary>
		/// Whether the query for the TimingReport was successfully handlded or not.
		/// </summary>
		/// <returns>true if the query was a success, false otherwise.</returns>
		public bool GetIsSuccess() {
			return _clearVRMessage.GetIsSuccess();
		}

		/// <summary>
		/// Parses a TimingReport proto message as byte[] and converts it to a TimingReport
		/// </summary>
		/// <param name="argRawProtoTimingReport">the TimingReport as byte[] that needs to be converted</param>
		/// <param name="argTimingReport">the variable in which the result will be stored</param>
		/// <returns>bool depending if the operation failed or succeeded.</returns>
		internal static bool FromCoreProtobuf(byte[] argRawProtoTimingReport, out TimingReport argTimingReport) {
			try {
				argTimingReport = new TimingReport(cvri.TimingReport.Parser.ParseFrom(argRawProtoTimingReport));
				return true;
			} catch (Exception e) {
				UnityEngine.Debug.LogError(String.Format("[ClearVR] Unable to parse timing report from the proto message. error: {0}", e));
				argTimingReport = null;
				return false;
			}
		}

		public override String ToString() {
            return String.Format("TimingType: {0}, position: {1}, bounds: [{2}, {3}], duration: {4}, event type: {5}, message: {6}", timingType.ToString2(), currentPositionInMilliseconds, lowerSeekBoundInMilliseconds, upperSeekBoundInMilliseconds, contentDurationInMilliseconds, eventType, clearVRMessage);
    	}
	}


	/// <summary>
	/// Helpers class holdig device-specific parameters.
	/// </summary>
	public class DeviceParameters {
		private short _screenWidth;
		private short _screenHeight;
		private DeviceTypes _deviceType;
		/// <summary>
		/// Returns the configured screen width.
		/// </summary>
		/// <value>Screen width in pixels.</value>
		public short screenWidth {
			get {
				return _screenWidth;
			}
		}
		/// <summary>
		/// Returns the configured screen height.
		/// </summary>
		/// <value>Screen height in pixels.</value>
		public short screenHeight {
			get {
				return _screenHeight;
			}
		}
		/// <summary>
		/// Returns the configured device type.
		/// </summary>
		/// <value>DeviceType</value>
		public DeviceTypes deviceType {
			get {
				return _deviceType;
			}
		}
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="argScreenWidth">The screen width in pixels. The default value 0 = auto-detect. For headsets, this is the full dual-eye equivalent (so if one eye has a resolution of 2160 pixels, this value should be 4320.</param>
		/// <param name="argScreenHeight">The screen height in pixels. The default value 0 = auto-detect.</param>
		/// <param name="argDeviceType">The Device Type. The default value DeviceTypes.Unknown will trigger auto-detection.</param>
		public DeviceParameters(short argScreenWidth = 0, short argScreenHeight = 0, DeviceTypes argDeviceType = DeviceTypes.Unknown) {
			_screenWidth = argScreenWidth;
			_screenHeight = argScreenHeight;
			_deviceType = argDeviceType;
			if(argScreenWidth == 0) {
				_screenWidth = (short) Screen.width;
			}
			if(argScreenHeight == 0) {
				_screenHeight = (short) Screen.height;
			}
			if(deviceType == DeviceTypes.Unknown) {
				// At this point, there are two options:
				// 1. The DeviceType is utterly unknown.
				// 2. The DeviceTYpe has been manually overridden by calling Utils.RedetectDeviceType(DeviceTypes.XYZ) from the app code.
				// In case of 1., Utils.GetDeviceType() will automatically call Utils.RedetectDeviceType()
				// In case of 2., Utils.GetDeviceType() will return the custom value set by the app.
				_deviceType = Utils.GetDeviceType();
			} else {
				Utils.RedetectDeviceType(deviceType);
			}
		}
		
		internal bool Verify() {
			if(screenWidth == 0 || screenHeight == 0) {
				UnityEngine.Debug.LogError("[ClearVR] Unable to detect screen dimensions or no screen dimensions set.");
				return false;
			}
			if(deviceType == DeviceTypes.Unknown) {
				UnityEngine.Debug.LogError("[ClearVR] Unable to detect DeviceType.");
				return false;
			}
			// As part of #3489, we always signal the largest of the two display resolution components as "width" to the core, and the other as "height".
			// If we change anything here, we should also take care of fixing MediaPlayerBase._initialScreenOrientation = ScreenOrientation.LandscapeLeft;
			if(screenWidth < screenHeight) {
				short temp = screenHeight;
				_screenHeight = screenWidth;
				_screenWidth = temp;
			}
			return true;
		}

		/// <summary>Return a protobuf representation of the device parameters</summary>
		internal cvri.DeviceParams ToCoreProtobuf() {
			return new cvri.DeviceParams{ 
				DeviceType = this._deviceType.ToCoreProtobuf(),
				ScreenWidth = (uint) this._screenWidth,
				ScreenHeight = (uint) this._screenHeight
				// Chipset, DeviceName and OSVersion protobuf fields are ALWAYS set by the MediaFlows, not by the SDKs.
			};
		}

		public override string ToString() {
			return String.Format("DeviceType: {0}, screen dimensions: {1}x{2}", deviceType, screenWidth, screenHeight);
		}
	}

	/// <summary>
	/// The PopulateMediaInfoParameters class is used in conjunction with the PopulateMediaInfo() API.
	/// </summary>
	public class PopulateMediaInfoParameters {
		private ContentItem _contentItem;
		/// <summary>
		/// This field is currently unterminated and not send upstream to any MediaFlow as the core does not care about it yet.
		/// Can be null
		/// </summary>
		private TimingParameters _timingParameters; 
		private DeviceParameters _deviceParameters;
		
		/// <summary>
		/// The Content item to populate its media info.
		/// </summary>
		/// <value>The content item</value>
		public ContentItem contentItem {
			get {
				return _contentItem;
			}
		}

		internal TimingParameters timingParameters {
			get {
				return _timingParameters;
			}
		}
		/// <summary>
		/// The device parameters that must be taken into consideration when populating the media info.
		/// </summary>
		/// <value>The device parameters.</value>
		public DeviceParameters deviceParameters {
			get {
				return _deviceParameters;
			}
		}

		/// <summary>
		/// Populate media info retrieves the provided content item and parses its media info (available audo tracks, DRM properties, etc.)
		/// 
		/// > [!IMPORTANT]
		/// > This is considered an advanced API, and one should typically refrain from using it.
		/// </summary>
		/// <param name="argContentItem">The ContentItem to parse.</param>
		/// <param name="argDeviceParameters">The DeviceParameters that should be taken into account when parsing the content item.</param>
		public PopulateMediaInfoParameters(ContentItem argContentItem, DeviceParameters argDeviceParameters = null) : this(argContentItem, null, argDeviceParameters) {
		}

		/// <summary>
		/// Populate media info retrieves the provided content item and parses its media info (available audo tracks, DRM properties, etc.)
		/// 
		/// > [!IMPORTANT]
		/// > This is considered an advanced API, and one should typically refrain from using it.
		/// </summary>
		/// <param name="argContentItem">The ContentItem to parse.</param>
		/// <param name="argTimingParameters">The parameters specifying the preferred start position in the ContentItem. Keep at its default value null.</param>
		/// <param name="argDeviceParameters">The DeviceParameters that should be taken into account when parsing the content item.</param>
		internal PopulateMediaInfoParameters(ContentItem argContentItem, TimingParameters argTimingParameters = null, DeviceParameters argDeviceParameters = null) {
			_contentItem = argContentItem;
			_deviceParameters = argDeviceParameters;
			_timingParameters = argTimingParameters;
			if(_deviceParameters == null) {
				_deviceParameters = new DeviceParameters();
			}
			_deviceParameters.Verify();
		}

		/// <summary>Return a protobuf representation of the populate media info parameters</summary>
		internal cvri.PopulateMediaInfoParams ToCoreProtobuf() {
			cvri.PopulateMediaInfoParams mediaInfoParams = new cvri.PopulateMediaInfoParams();
			mediaInfoParams.ContentItem = contentItem.ToCoreProtobuf();
			return mediaInfoParams;
		}
	}

	public enum ABRStartModes : Int32 {
		[StringValue("default")]
		Default,
		[StringValue("lowest")]
		Lowest,
		[StringValue("middle")]
		Middle,
		[StringValue("highest")]
		Highest
	}

	internal static class ABRStartModesMethods {
		/// <summary>
		/// Will get the string value for a given enums value, this will
		/// only work if you assign the StringValue attribute to
		/// the items in your enum.
		/// </summary>
		/// <param name="argValue"></param>
		/// <returns></returns>
		public static String GetStringValue(this ABRStartModes argValue) {
			// Get the type
			Type type = argValue.GetType();

			// Get fieldinfo for this type
			FieldInfo fieldInfo = type.GetField(argValue.ToString());

			// Get the stringvalue attributes
			StringValueAttribute[] attribs = fieldInfo.GetCustomAttributes(
				typeof(StringValueAttribute), false) as StringValueAttribute[];

			// Return the first if there was a match.
			return attribs.Length > 0 ? attribs[0].StringValue : null;
		}

		public static ABRStartModes GetFromStringValue(String argValue) {
			foreach(ABRStartModes value in Enum.GetValues(typeof(ABRStartModes))) {
				if(value.GetStringValue() == argValue) {
					return value;
				}
			}
			return ABRStartModes.Default;
		}
	}

	/// <summary>
    /// This object is used for advanced configuration of the livestream sync feature.
    /// </summary>
	public class SyncSettings {
		internal cvri.SyncSettings syncSettings;
		/// <summary>
		/// SyncSettings constructor
		/// 
		/// Synchronisation is enabled by passing a SyncSettings object != null into the PrepareContentParameters and SwitchContentParameters constructor. By passing null, synchronisation is disabled.
		/// LIVE ContentItems might have a _sync live edge_ which is not equal to the regular _live edge_ to accomodate for propagation latencies across CDNs.
		/// </summary>
		/// <param name="argClientLatency"> (ms) Latency the client will stay behind the content item specific live sync target. Value should be between 0 and 300000.</param>
		/// <param name="argMaxTargetLag"> (ms) Maximum difference between the client sync target and the current content time. A seek will be triggered if outside this range. If set the value should be at least 5000. If unset (0) this feature is disabled.</param>
		/// <param name="argMaxPlaybackRate"> Maximum value for the playback rate the sync algorithm selects. Value should be between 1 and 1.5. If unset (0) defaults to 1.2.</param>
		/// <param name="argMinPlaybackRate"> Minimum value for the playback rate the sync algorithm selects. Value should be between 0.5 and 1. If unset (0) defaults to 0.8.</param>
		/// <param name="argMaxPlaybackRateChange"> Maximum change for the sync algorithm playback rate per second. Value should be positive. If unset (0) defaults to 0.1.</param>
		/// <param name="argDisableInitialSeek"> Disable the initial seek that might be triggered upon enabling sync. If unset defaults to false.</param>
		/// <param name="syncMode"> Specifies the way sync is achieved. Supported modes: Seek and PlaybackRate. Default value: PlaybackRate.</param> 
		public SyncSettings(int argClientLatency = 0, int argMaxTargetLag = 0, float argMaxPlaybackRate = 1.2F, float argMinPlaybackRate = 0.8F, float argMaxPlaybackRateChange = 0.1F, bool argDisableInitialSeek = false, SyncModes syncMode = SyncModes.PlaybackRate) {
			syncSettings = new cvri.SyncSettings{
				ClientLatency = argClientLatency,
				MaxTargetLag = argMaxTargetLag,
				MaxPlaybackRate = argMaxPlaybackRate,
				MinPlaybackRate = argMinPlaybackRate,
				MaxPlaybackRateChange = argMaxPlaybackRateChange,
				SyncMode = SyncModeMethods.ToCoreProtobuf(syncMode),
				DisableInitialSeek = argDisableInitialSeek,
			};
		}

		private SyncSettings(cvri.SyncSettings coreSyncSettings) {
			syncSettings = coreSyncSettings;
        }

		internal static SyncSettings FromCoreProtobuf(cvri.SyncSettings coreSyncSettings) {
			return new SyncSettings(coreSyncSettings);
		}

		internal int clientLatency {
			get {
				return syncSettings.ClientLatency;
			}
		}
		public int maxTargetLag {
			get {
				return syncSettings.MaxTargetLag;
			}
		}
		public float maxPlaybackRate {
			get {
				return syncSettings.MaxPlaybackRate;
			}
		}
		public float minPlaybackRate {
			get {
				return syncSettings.MinPlaybackRate;
			}
		}
		public float maxPlaybackRateChange {
			get {
				return syncSettings.MaxPlaybackRateChange;
			}
		}
		public SyncModes syncMode {
			get {
				return SyncModeMethods.FromCoreProtobuf(syncSettings.SyncMode);
			}
		}
		public bool disableInitialSeek {
			get {
				return syncSettings.DisableInitialSeek;
			}
		}

		/// <summary>Return a protobuf representation of the sync settings</summary>
		internal cvri.SyncSettings ToCoreProtobuf() {
			return syncSettings;
		}

		public override String ToString() {
			return String.Format("client latency: {0}, max target lag: {1}, playback rate range: [{2}, {3}], max playback rate change: {4}, sync mode {5}, disable initial seek {6}", syncSettings.ClientLatency, syncSettings.MaxTargetLag, syncSettings.MinPlaybackRate, syncSettings.MaxPlaybackRate, syncSettings.MaxPlaybackRateChange, syncSettings.SyncMode, syncSettings.DisableInitialSeek);
		}
	}

	/// <summary>
	/// The mode of the synchronization algorithm
	/// </summary>
	public enum SyncModes : Int32 {
		///<summary>The sync algorithm wil try to reach the sync target by changing the playback rate.</summary>
		PlaybackRate,
		///<summary>The sync algorithm wil try to reach the sync target by seeking inside the buffer.</summary>
		Seek,
	}

	static class SyncModeMethods {
		internal static SyncModes FromCoreProtobuf(this cvri::SyncMode argProtoSyncMode) {
			switch(argProtoSyncMode) {
				case cvri::SyncMode.Seek:
					return SyncModes.Seek;
				default:
					return SyncModes.PlaybackRate;
			}
		}
		internal static cvri::SyncMode ToCoreProtobuf(this SyncModes argSyncMode) {
			switch(argSyncMode) {
				case SyncModes.Seek:
					return cvri::SyncMode.Seek;
				default:
					return cvri::SyncMode.PlaybackRate;
			}
		}
	}

	/// <summary>
	/// The state of the synchronization algorithm
	/// </summary>
	public enum SyncState : Int32 {
		///<summary>The sync algorithm is disabled, no synchronization actions will be taken until it is explicitely enabled.</summary>
		Disabled,
		///<summary>The sync algorithm is enabled and currently taking steps to get the user in sync.</summary>
		Syncing,
		///<summary>The sync algorithm is enabled and the user is in sync.</summary>
		InSync
	}

	static class SyncStateMethods {
		internal static SyncState FromCoreProtobuf(this cvri::SyncState argProtoSyncState) {
			switch(argProtoSyncState) {
			case cvri::SyncState.Syncing:
				return SyncState.Syncing;
			case cvri::SyncState.InSync:
				return SyncState.InSync;
			default:
				return SyncState.Disabled;
			}
		}
	}

	public class SyncStateChanged {
		private cvri.SyncStatusMessage syncStatusMessage;

		internal SyncStateChanged(cvri.SyncStatusMessage argSyncStatusMessage) {
			syncStatusMessage = argSyncStatusMessage;
		}

		internal static bool ParseSyncStatusMessageToSyncStateChanged(String argProtoSyncStatusMessageBase64, out SyncStateChanged argSyncStateChanged) {
			try {
				var raw = System.Convert.FromBase64String(argProtoSyncStatusMessageBase64);
				argSyncStateChanged = new SyncStateChanged(cvri.SyncStatusMessage.Parser.ParseFrom(raw));
				return true;
			} catch {
				argSyncStateChanged = null;
				return false;
			}
		}

		/// <summary>
		/// The new sync state.
		/// </summary>
		public SyncState syncState {
			get {
				return SyncStateMethods.FromCoreProtobuf(syncStatusMessage.SyncState);
			}
		}
	}

	public class CallCoreResponse {
		private cvri.CallCoreResponse callCoreResponse;

		internal CallCoreResponse(cvri.CallCoreResponse argCallCoreResponse) {
			callCoreResponse = argCallCoreResponse;
		}

		internal static bool ParseCallCoreResponse(String argProtoCallCoreResponseBase64, out CallCoreResponse argCallCoreResponse) {
			try {
				var raw = System.Convert.FromBase64String(argProtoCallCoreResponseBase64);
				argCallCoreResponse = new CallCoreResponse(cvri.CallCoreResponse.Parser.ParseFrom(raw));
				return true;
			} catch {
				argCallCoreResponse = null;
				return false;
			}
		}

		public ClearVRMessageCodes errorCode { get { return (ClearVRMessageCodes) callCoreResponse.ErrorCode; }}
		public string errorMessage { get { return callCoreResponse.ErrorMessage; }}

		/// <summary>
		/// The optional arguments can be defined when calling the asynchronous PollSyncStatus API.
		/// </summary>
		public object[] optionalArguments {
			/// <summary>
			/// Gets the optional arguments that were passed in the clearVRPlayer.sync.PollSyncStatus() API.
			/// </summary>
			/// <value>The optional arguments, if at all specified.</value>
			get {
				return _optionalArguments;
			}
			/// <summary>
			/// This setter is internal, the getter is public.
			/// </summary>
			/// <value></value>
			internal set {
				_optionalArguments = value;
			}
		}

		private object[] _optionalArguments;

	    public override String ToString() {
    	    return String.Format(
				"ErrorCode: {1}; ErrorMessage: {2}",
				errorCode, errorMessage);
    	}
	}

	public class SyncStatus {
		private cvri.SyncStatusMessage syncStatusMessage;

		internal SyncStatus(cvri.SyncStatusMessage argSyncStatusMessage) {
			syncStatusMessage = argSyncStatusMessage;
		}

		internal static bool ParseCallCoreResponseToSyncStatus(String argProtoCallCoreResponseBase64, out SyncStatus argSyncStatus) {
			try {
				var raw = System.Convert.FromBase64String(argProtoCallCoreResponseBase64);
				var callCoreResponse = cvri.CallCoreResponse.Parser.ParseFrom(raw);
				argSyncStatus = new SyncStatus(callCoreResponse.SyncStatusMessage);
				return true;
			} catch {
				argSyncStatus = null;
				return false;
			}
		}

		/// <summary>
		/// (ms) Latency the client will stay behind the content item specific sync target.
		/// </summary>
		public long clientLatency {
			get {
				return syncStatusMessage.ClientLatency;
			}
		}
		/// <summary>
		/// (ms) Maximum difference between the client sync target and the current content time. A seek will be triggered if outside this range.
		/// </summary>
		public long maxTargetLag {
			get {
				return syncStatusMessage.MaxTargetLag;
			}
		}
		/// <summary>
		/// Maximum value for the playback rate the sync algorithm selects. 
		/// </summary>
		public float maxPlaybackRate {
			get {
				return syncStatusMessage.MaxPlaybackRate;
			}
		}
		/// <summary>
		/// Minimum value for the playback rate the sync algorithm selects. 
		/// </summary>
		public float minPlaybackRate {
			get {
				return syncStatusMessage.MinPlaybackRate;
			}
		}
		/// <summary>
		/// Maximum change for the sync algorithm playback rate per second.
		/// </summary>
		public float maxPlaybackRateChange {
			get {
				return syncStatusMessage.MaxPlaybackRateChange;
			}
		}

		/// <summary>
		/// Indicator of the current state of sync.
		/// </summary>
		public SyncState syncState {
			get {
				return SyncStateMethods.FromCoreProtobuf(syncStatusMessage.SyncState);
			}
		}
		/// <summary>
		/// Current content playback rate.
		/// </summary>
		public double playbackRate {
			get {
				return syncStatusMessage.PlaybackRate;
			}
		}
		/// <summary>
		/// (epoch ms) Current network time protocol time.
		/// </summary>
		public long ntpTime {
			get {
				return syncStatusMessage.NTPTime;
			}
		}
		/// <summary>
		/// (epoch ms) Current sync target for live content.
		/// </summary>
		public long targetTime {
			get {
				return syncStatusMessage.TargetTime;
			}
		}
		/// <summary>
		/// (epoch ms) Current time of the content in terms of the target time.
		/// </summary>
		public long actualTime {
			get {
				return syncStatusMessage.ActualTime;
			}
		}
		/// <summary>
		/// (ms) Lag of the content behind the sync target, difference between target and actual.
		/// </summary>
		public long targetLag {
			get {
				return syncStatusMessage.TargetLag;
			}
		}

		/// <summary>
		/// (ms) Current sync edge latency. Configured on the livestream. Defines the sync edge as ntp minus sync edge latency.
		/// </summary>
		public long syncEdgeLatency {
			get {
				return syncStatusMessage.SyncEdgeLatency;
			}
		}

		/// <summary>
		/// The optional arguments can be defined when calling the asynchronous PollSyncStatus API.
		/// </summary>
		public object[] optionalArguments {
			/// <summary>
			/// Gets the optional arguments that were passed in the clearVRPlayer.sync.PollSyncStatus() API.
			/// </summary>
			/// <value>The optional arguments, if at all specified.</value>
			get {
				return _optionalArguments;
			}
			/// <summary>
			/// This setter is internal, the getter is public.
			/// </summary>
			/// <value></value>
			internal set {
				_optionalArguments = value;
			}
		}

		private object[] _optionalArguments;

	    public override String ToString() {
    	    return String.Format(
				"\nConfiguration: clientLatency={0}, maxTargetLag={1}, maxPlaybackRate={2}, minPlaybackRate={3}, maxPlaybackRateChange={4}\n" +
				"Status: syncState={5}, playbackRate={6}, targetLag={7}, ntpTime={8}, targetTime={9}, actualTime={10}, syncEdgeLatency={11}",
				clientLatency, maxTargetLag, maxPlaybackRate, minPlaybackRate, maxPlaybackRateChange,
				syncStatusMessage.SyncState, playbackRate, targetLag, ntpTime, targetTime, actualTime, syncEdgeLatency);
    	}
	}
	/// <summary>
	/// The TimingParameters object is used when loading content, seeking and switching content. 
	/// It defines the (new) start position (in milliseconds) and how this should be interpreted.
	/// </summary>
	public class TimingParameters {
		internal long positionInMilliseconds;
		internal TimingTypes timingType;

		/// <summary>
		/// Default constructor.
		/// For VOD content items, you can use startPosition = 0 and timingType = TimingTypes.ContentTime
		/// For LIVE content items, you can use startPosition = 0 and timingType = TimingTypes.LiveEdge to play from the live edge.
		/// 
		/// This object can also be used to control more complex content loading operations, like synchronized seamless camera switching when using the [SwitchContent](xref:com.tiledmedia.clearvr.MediaControllerInterface.SwitchContent(com.tiledmedia.clearvr.SwitchContentParameters,Action{com.tiledmedia.clearvr.ClearVREvent,com.tiledmedia.clearvr.ClearVRPlayer},Action{com.tiledmedia.clearvr.ClearVREvent,com.tiledmedia.clearvr.ClearVRPlayer},System.Object[])) API.
		/// 
		/// > [!WARNING]
		/// > When playing a live stream, the following has changed:
		/// > <list type="number">
		/// > <item>
		/// > <term></term>
		/// > <description>In pre-v8.0 versions of the ClearVR SDK you would seek to the Live Edge in a live stream regardless the `SeekFlags` you might have specified when calling `Seek(0)`.</description>
		/// > </item>
		/// > <item>
		/// > <term></term>
		/// > <description>Since v8.0, in order to seek to the Live Edge, you MUST set `TimingTypes.LiveEdge`, e.g.: `Seek(new TimingParameters(0, TimingTypes.LiveEdge))`.</description>
		/// > </item>
		/// > <item>
		/// > <term></term>
		/// > <description>`Seek(new TimingParameters(0, TimingTypes.ContentTime))` will seek to the earliest available content (which might not start at position 0 depending on the cache window on the Content Delivery Network).</description>
		/// > </item>
		/// > </list>
		/// </summary>
		/// <param name="argPositionInMilliseconds">The (new) position, in milliseconds.</param>
		/// <param name="argTimingType">How to interpret the (new) position. Default value: TimingTypes.ContentTime.</param>
		public TimingParameters(long argPositionInMilliseconds, TimingTypes argTimingType = TimingTypes.ContentTime) {
			positionInMilliseconds = argPositionInMilliseconds;
			timingType = argTimingType;
		}

		/// <summary>Return a protobuf representation of the timing parameters</summary>
		internal cvri.TimingParams ToCoreProtobuf() {
			cvri.TimingParams timingParams = new cvri.TimingParams();
			timingParams.Target = positionInMilliseconds;
			timingParams.TimingType = timingType.ToCoreProtobuf(); 
			return timingParams;
		}

		internal static TimingParameters FromCoreProtobuf(cvri.TimingParams coreTimingParameters) {
			if(coreTimingParameters == null) {
				return null;
			}
			return new TimingParameters(coreTimingParameters.Target, TimingTypesMethods.FromCoreProtobuf(coreTimingParameters.TimingType));
		}

		public override string ToString() {
			return String.Format("TimingParameters - position: {0}. timingType: {1}", positionInMilliseconds, timingType.ToString2());
		}
	}

	/// <summary>
	/// ////Internal class used to track player state when application lost focus.
	/// </summary>
	internal class SuspendResumeState {
		internal AudioTrackAndPlaybackParameters audioTrackAndPlaybackParameters;
		internal float muteState;
		internal ClearVRAsyncRequest clearVRAsyncRequest;

		internal SuspendResumeState(AudioTrackAndPlaybackParameters audioTrackAndPlaybackParameters, float muteState, ClearVRAsyncRequest clearVRAsyncRequest) {
			this.audioTrackAndPlaybackParameters = audioTrackAndPlaybackParameters;
			this.muteState = muteState;
			this.clearVRAsyncRequest = clearVRAsyncRequest;
		}
	}

	/// <summary>
	/// The ABRLevel object contains information about the currently active ABRLevel.
	/// </summary>
	[Obsolete("Usage of ABRLevel has been replaced by ActiveTracks, please listen to the ActiveTracksChanged event.", true)]
	public class ABRLevel {
		/// <summary>
		/// The currently active URL.
		/// </summary>
		public readonly String url;

		internal ABRLevel(String argURL) {
			url = argURL;
		}
		/// <summary>
		/// Deserializes the payload of the ClearVRCoreWrapperABRLevelActivated ClearVRMessage.
		/// </summary>
		/// <param name="argMessage">The message to deserialize.</param>
		/// <returns>True if message could be parsed, false otherwise. If false is returned, the value of the out parameter is undefined. If true is returned, the out parameter will be not null.</returns>
		internal static bool Deserialize(String argMessage, out ABRLevel argABRLevel) {
			if(String.IsNullOrEmpty(argMessage)) {
				argABRLevel= null;
				return false;
			}
			argABRLevel = new ABRLevel(argMessage);
			return true;
		}

		public override String ToString() {
			return String.Format("URL: {0}", this.url);
		}
	}

	/// <summary>
	/// TrackID is an object containing the feed index and the track index of the track (video, audio or subtitle)
	/// The feed index tells you to which feed this track belongs (which can be found with clearVRPlayer.mediaInfo.getContentInfo())
	/// The track index tells you which track this is in the list of tracks (useful when there are multiple video / audio / subtitle tracks) within the feed.
	/// 
	/// Note that this object in immutable after construction.
	/// </summary>
	//Immutable on purpose, refer to LayoutParameters for details.
	public class TrackID {
		private cvri.TrackID coreTrackID;
		public Int32 feedIndex { 
			get {
				return coreTrackID.FeedIdx;
			} 
		}
		public Int32 trackIndex { 
			get {
				return coreTrackID.TrackIdx;
			} 
		}
		/// <summary>
		/// The default constructor. Used primarily when constructing your [LayoutParameters](xref:com.tiledmedia.clearvr.LayoutParameters) and configuring what audio and/or subtitle feed and track should be selected.
		/// </summary>
		/// <param name="argFeedIndex">The feed index, -2 means: automatically pick the first available feed.</param>
		/// <param name="argTrackIndex">The track index inside the specified feed, -2 means: automatically pick the first available track.</param>
		public TrackID(Int32 argFeedIndex, Int32 argTrackIndex) {
			coreTrackID = new cvri.TrackID();
			coreTrackID.FeedIdx = argFeedIndex;
			coreTrackID.TrackIdx = argTrackIndex;
		}

		public override String ToString() {
			return String.Format("FeedIndex: {0}, trackIndex: {1}", feedIndex, trackIndex);
		}

		public override bool Equals(object obj) {
			var other = obj as TrackID;
			if (other == null) {
				return false;
			}

			return this.feedIndex == other.feedIndex && this.trackIndex == other.trackIndex;
		}

		public override int GetHashCode() {
			int hash = 3;
			hash = (hash * 1) + feedIndex.GetHashCode();
			hash = (hash * 10) + trackIndex.GetHashCode();
			return hash;
		}
	}
	
	/// <summary>
	/// Helper class that holds the info of one content item.
	/// For detailed information about the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo), [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo), [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo), [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo), [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) objects and their relationship, refer to the [ContentItem](~/readme/contentitem.md) documentation.
	/// </summary>
	public class ContentInfo {
		/// <summary>
		/// Array containing all feeds that this content item contains.
		/// </summary>
		public FeedInfo[] feeds;
		internal ContentInfo(cvri.ContentInfoMessage argCoreContentInfo) {
			if(argCoreContentInfo == null) {
				UnityEngine.Debug.LogWarning("[ClearVR] ContentInfo was successfully deserialized but did not contain any data. Please report this issue to Tiledmedia.");
				feeds = new FeedInfo[0];
				return;
			}
			feeds = new FeedInfo[argCoreContentInfo.Feeds.Count];
			for (var i = 0; i < argCoreContentInfo.Feeds.Count; i++) {
				feeds[i] = new FeedInfo(argCoreContentInfo, i);
			}
			_eventType = EventTypesMethods.FromCoreProtobuf(argCoreContentInfo.EventType);
		}

		// Added for API consistency with other protobuf-bridge classes.
		internal static ContentInfo FromCoreProtobuf(cvri.ContentInfoMessage argCoreContentInfo) {
			if(argCoreContentInfo == null) {
				return null;
			}
			return new ContentInfo(argCoreContentInfo);
		}


		internal ContentInfo(String argBase64Message) : this(ContentInfo.GetCoreContentInfoFromBase64Message(argBase64Message)) { }
		
		private static cvri.ContentInfoMessage GetCoreContentInfoFromBase64Message(String argBase64Message) {
			if(String.IsNullOrEmpty(argBase64Message)) {
				return null;
			}
			byte[] raw = System.Convert.FromBase64String(argBase64Message);
			return cvri.ContentInfoMessage.Parser.ParseFrom(raw);
		}
		public EventTypes eventType {
			get {
				return _eventType;
			}
		}

		private EventTypes _eventType;

		/// <summary>
		/// This convenience API returns a list of all [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) that have at least one active [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo).
		/// Note that the active [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo) can be located on a [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) that is different than any of the active [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo).
		/// </summary>
		/// <returns>The array can have size 0 (in theory) but can never be null.</returns>
		public FeedInfo[] GetFeedsWithActiveVideoTrack() {
			List<FeedInfo> feedsWithActiveVideoTrack = new List<FeedInfo>();
			foreach(FeedInfo feedInfo in feeds) {
				if(feedInfo.GetActiveVideoTrack() != null) {
					feedsWithActiveVideoTrack.Add(feedInfo);
				}
			}
			return feedsWithActiveVideoTrack.ToArray();
		}

		/// <summary>
		/// This convenience API returns the [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) of the Feed that has the active audio track.
		/// Notes:
		/// <list type="bullet">
		/// <item>
		/// <term></term>
		/// <description>There can be 0 or at most 1 active audio track at any point in time.</description>
		/// </item>
		/// <item>
		/// <term></term>
		/// <description>The Feed with the active audio track can be different to the Feed that has the active video track(s).</description>
		/// </item>
		/// </list>
		/// </summary>
		/// <returns>The [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo) of the currently active audio track, or null if no audio track is active.</returns>
		public FeedInfo GetFeedWithActiveAudioTrack() {
			foreach(FeedInfo feedInfo in feeds) {
				if(feedInfo.GetActiveAudioTrack() != null) {
					return feedInfo;
				}
			}
			return null;
		}

		/// <summary>
		/// Get the [TrackID](xref:com.tiledmedia.clearvr.TrackID) that identifies the currently active audio track and the [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) it can be found on.
		/// This API can used in conjunction with the SetLayout API,
		/// > [!NOTE]
		/// > Also refer to the [GetActiveAudioTrack()](xref:com.tiledmedia.clearvr.FeedInfo.GetActiveAudioTrack) API.
		/// </summary>
		/// <returns>The TrackID that uniquely identifies the active audio track, or null if no audio track is active.</returns>
		public TrackID GetActiveAudioTrackID() {
			FeedInfo activeAudioTrackFeed = GetFeedWithActiveAudioTrack();
			if(activeAudioTrackFeed != null) {
				AudioTrackInfo activeAudioTrackInfo = activeAudioTrackFeed.GetActiveAudioTrack();
				if(activeAudioTrackInfo != null) {
					return activeAudioTrackInfo.trackID;
				}
			}
			return null;
		}

		/// <summary>
		/// Get the [TrackID](xref:com.tiledmedia.clearvr.TrackID) that identifies the currently active subtitle track and the [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) it can be found on.
		/// This API can used in conjunction with the SetLayout API,
		/// > [!NOTE]
		/// > Also refer to the [GetActiveSubtitleTrack()](xref:com.tiledmedia.clearvr.FeedInfo.GetActiveSubtitleTrack) API.
		/// </summary>
		/// <returns>The TrackID that uniquely identifies the active subtitle track, or null if no subtitle track is active.</returns>
		public TrackID GetActiveSubtitleTrackID() {
			FeedInfo activeSubtitleTrackFeed = GetFeedWithActiveSubtitleTrack();
			if(activeSubtitleTrackFeed != null) {
				SubtitleTrackInfo activeSubtitleTrackInfo = activeSubtitleTrackFeed.GetActiveSubtitleTrack();
				if(activeSubtitleTrackInfo != null) {
					return activeSubtitleTrackInfo.trackID;
				}
			}
			return null;
		}

		/// <summary>
		/// Get the [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) of the currently active subtitle track..
		/// </summary>
		/// <returns>The SubtitleTrackInfo of the active subtitle track, or null if no subtitle track is active.</returns>
		public SubtitleTrackInfo GetActiveSubtitleTrack() {
			FeedInfo activeSubtitleTrackFeed = GetFeedWithActiveSubtitleTrack();
			if(activeSubtitleTrackFeed != null) {
				return activeSubtitleTrackFeed.GetActiveSubtitleTrack();
			}
			return null;
		}

		/// <summary>
		/// This convenience API returns the [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) of the Feed that has the active subtitle track.
		/// Notes:
		/// <list type="bullet">
		/// <item>
		/// <term></term>
		/// <description>There can be 0 or at most 1 active subtitle track at any point in time.</description>
		/// </item>
		/// <item>
		/// <term></term>
		/// <description>The Feed with the active subtitle track can be different to the Feed that has the active video and/or audio track(s).</description>
		/// </item>
		/// </list>
		/// </summary>
		/// <returns>The [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo) of the currently active subtitle track, or null if no subtitle track is active.</returns>
		public FeedInfo GetFeedWithActiveSubtitleTrack() {
			foreach(FeedInfo feedInfo in feeds) {
				if(feedInfo.GetActiveSubtitleTrack() != null) {
					return feedInfo;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns a list of [TrackIDs](xref:com.tiledmedia.clearvr.TrackID) of which at least one audio track could be selected in the current state.
		/// Please refer to [GetSelectableAudioTrackIDs](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableAudioTrackIDs) for the definition of a "selectable" audio track.
		/// 
		/// > [!NOTE]
		/// > In most cases the [GetSelectableAudioTracks()](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableAudioTracks) API is more convenient.
		///
		/// /// </summary>
		/// <returns>A list with a length of zero, one or more items. This can never be null.</returns>
		public TrackID[] GetSelectableAudioTrackIDs() {
			List<TrackID> selectableAudioTrackIDs = new List<TrackID>();
			foreach(AudioTrackInfo audioTrackInfo in GetSelectableAudioTracks()){ 
					selectableAudioTrackIDs.Add(audioTrackInfo.trackID);

			}
			return selectableAudioTrackIDs.ToArray();
		}

		/// <summary>
		/// Returns a list of [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo) of which at least one audio track can be selected in the current state in any of the Feeds.
		/// Please refer to [GetSelectableAudioTracks](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableAudioTracks) for the definition of a "selectable" audio track.
		/// </summary>
		/// <returns>A list with a length of zero, one or more items. This can never be null.</returns>
		public AudioTrackInfo[] GetSelectableAudioTracks() {
			List<AudioTrackInfo> selectableAudioTrackInfos = new List<AudioTrackInfo>();
			foreach(FeedInfo feed in feeds) {
				AudioTrackInfo[] selectableAudioTrackInfosInFeed = feed.GetSelectableAudioTracks();
				if(selectableAudioTrackInfosInFeed.Count() > 0) {
					selectableAudioTrackInfos.AddRange(selectableAudioTrackInfosInFeed);
				}
			}
			return selectableAudioTrackInfos.ToArray();
		}		

		/// <summary>
		/// Returns the number of selectable audio tracks in the current player state.
		/// See [GetSelectableAudioTrackIDs](xref:com.tiledmedia.clearvr.ContentInfo.GetSelectableAudioTrackIDs) for details.
		/// </summary>
		/// <returns>Returns an integer value. 0 means: no audio tracks can be selected.</returns>
		public int GetNumberOfSelectableAudioTracks() {
			return GetSelectableAudioTrackIDs().Count();
		}

		/// <summary>
		/// Returns a list of [TrackIDs](xref:com.tiledmedia.clearvr.TrackID) of which at least one subtitles track could be selected in the current state.
		/// Please refer to [GetSelectableSubtitlesTrackIDs](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableSubtitlesTrackIDs) for details.
		/// </summary>
		/// <returns>A list with a length of zero, one or more items. This can never be null.</returns>
		public TrackID[] GetSelectableSubtitlesTrackIDs() {
			List<TrackID> selectableSubtitlesTrackIDs = new List<TrackID>();
			foreach(SubtitleTrackInfo subtitleTrackInfo in GetSelectableSubtitlesTracks()) {
				selectableSubtitlesTrackIDs.Add(subtitleTrackInfo.trackID);
			}
			return selectableSubtitlesTrackIDs.ToArray();
		}

		/// <summary>
		/// Returns a list of [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) of which at least one subtitles track could be selected in the current state.
		/// Please refer to [GetSelectableSubtitlesTracks](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableSubtitlesTracks) for details.
		/// </summary>
		/// <returns>A list with a length of zero, one or more items. This can never be null</returns>
		public SubtitleTrackInfo[] GetSelectableSubtitlesTracks() {
			List<SubtitleTrackInfo> selectableSubtitlesTracksOut = new List<SubtitleTrackInfo>();
			foreach(FeedInfo feed in feeds) {
				SubtitleTrackInfo[] selectableSubtitlesTracks = feed.GetSelectableSubtitlesTracks();
				if(selectableSubtitlesTracks.Count() > 0) {
					selectableSubtitlesTracksOut.AddRange(selectableSubtitlesTracks);
				}
			}
			return selectableSubtitlesTracksOut.ToArray();
		}

		/// <summary>
		/// Returns the number of selectable subtitles tracks in the current player state.
		/// See [GetSelectableSubtitlesTrackIDs](xref:com.tiledmedia.clearvr.ContentInfo.GetSelectableSubtitlesTrackIDs) for details.
		/// </summary>
		/// <returns>Returns an integer value. 0 means: no Subtitles tracks can be selected.</returns>
		public int GetNumberOfSelectableSubtitlesTracks() {
			return GetSelectableSubtitlesTrackIDs().Count();
		}

		/// <summary>
		/// Each ClearVR DisplayObject shows one Feed. Information about this Feed is stored in the FeedInfo object.
		/// This API returns the FeedInfo that matches the feed index currently associated to the provided [ClearVRDisplayObjectControllerBase](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectControllerBase).
		/// </summary>
		/// <param name="argClearVRDisplayObjectController">The DisplayObjectController of interest. Should not be null.</param>
		/// <returns>The FeedInfo, or null if not available or when the argClearVRDisplayObjectController is null. .</returns>
		public FeedInfo GetFeedInfoByDisplayObjectController(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
			if(argClearVRDisplayObjectController == null) {
				return null;
			}
			// The FeedInfo list in ContentInfo is in order, meaning that the feed index is equal to the index in the FeedInfo list.
			int feedIndex = argClearVRDisplayObjectController.activeFeedIndex;
			if(feedIndex >= 0 && feedIndex < feeds.Count()) {
				return feeds[feedIndex];
			}
			return null;
		}

		public override String ToString() {
			StringBuilder builder = new StringBuilder(String.Format(" \nEvent type: {0}. Feeds ({1}) \n", eventType, feeds.Length));
			int i = 0;
			foreach(FeedInfo feed in feeds) {
				builder.Append(String.Format("|-- Index {0}\n", i));
				builder.Append(String.Format("{0}\n", feed));
				if(i != feeds.Length - 1) {
					builder.Remove(builder.Length -1, 1); // Trim last newline character
				}
				i++;
			}
			return builder.ToString();
		}
	}

	/// <summary>
	/// Helper class that holds the info of one subtitle.
	/// </summary>
	public class ClearVRSubtitle {

		private cvri.SubtitleInfo coreSubtitleInfo;

		internal ClearVRSubtitle(cvri.SubtitleInfo argCoreSubtitleInfo) {
			coreSubtitleInfo = argCoreSubtitleInfo;
		}
		internal ClearVRSubtitle(String argBase64Message) : this(ClearVRSubtitle.GetCoreSubtitleInfoFromBase64Message(argBase64Message)) { }
		private static cvri.SubtitleInfo GetCoreSubtitleInfoFromBase64Message(String argBase64Message) {
			return cvri.SubtitleInfo.Parser.ParseFrom(System.Convert.FromBase64String(argBase64Message));
		}

		public String GetText() {return coreSubtitleInfo.Text;}
		public Int32 GetFeedIndex() {return coreSubtitleInfo.FeedIndex;}
	}

	/// <summary>
	/// Helper class that holds the info of one feed.
	/// For detailed information about the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo), [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo), [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo), [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo), [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) objects and their relationship, refer to the [ContentItem](~/readme/contentitem.md) documentation.
	/// </summary>
	public class FeedInfo {
		/// <summary>
		/// array that contains all the video tracks that this feed contains.
		/// </summary>
		public VideoTrackInfo[] videoTracks {
			get;
			private set;
		}
		/// <summary>
		/// array that contains all the audio tracks that this feed contains.
		/// </summary>
		public AudioTrackInfo[] audioTracks {
			get;
			private set;
		}
		/// <summary>
		/// array that contains all the subtitle tracks that this feed contains.
		/// </summary>
		public SubtitleTrackInfo[] subtitleTracks {
			get;
			private set;
		}
		/// <summary>
		/// Get the URL
		/// </summary>
		public String url { 
			get;
			private set;
		}
		/// <summary>
		/// The index of the feed in the ContentInfo's list of feeds.
		/// </summary>
		public int feedIndex {
			get;
			private set;
		}
		
		internal FeedInfo(cvri.ContentInfoMessage argCoreContentInfoMessage, int argFeedIndex) {
			feedIndex = argFeedIndex;
			// Video
			cvri.FeedInfo feed = argCoreContentInfoMessage.Feeds[argFeedIndex];
			var coreVideoTrackList = feed.VideoTracks;
			videoTracks = new VideoTrackInfo[coreVideoTrackList.Count];
			var coreActiveVideoTracks = argCoreContentInfoMessage.ActiveVideoTracks;
			for (int i = 0; i < coreVideoTrackList.Count; i++) {
				cvri.VideoTrackInfo coreVideoTrack = coreVideoTrackList[i];
				bool isActive = false;
				for(int j = 0, size = coreActiveVideoTracks.Count; j < size; j++) {
					cvri.TrackID activeVideoTrackID = coreActiveVideoTracks[j];
					if(activeVideoTrackID.FeedIdx == argFeedIndex) {
						if(activeVideoTrackID.TrackIdx == coreVideoTrack.VideoTrackIdx) {
							isActive = true;
							break;
						}
					}
				}
				videoTracks[i] = new VideoTrackInfo(coreVideoTrackList[i], isActive, argFeedIndex);
			}

			// Audio
			var coreAudioTrackList = feed.AudioTracks;
			audioTracks = new AudioTrackInfo[coreAudioTrackList.Count];
			cvri.TrackID coreActiveAudioTrack = argCoreContentInfoMessage.ActiveAudioTrack != null ? argCoreContentInfoMessage.ActiveAudioTrack : null;

			for (int i = 0, size = coreAudioTrackList.Count; i < size; i++) {
				cvri.AudioTrackInfo coreAudioTrack = coreAudioTrackList[i];
				bool isActive = false;
				if(coreActiveAudioTrack != null) {
					if (coreActiveAudioTrack.FeedIdx == argFeedIndex) {
						if (coreActiveAudioTrack.TrackIdx == coreAudioTrack.AudioTrackIdx) {
							isActive = true;
						}
					}
				}
				audioTracks[i] = new AudioTrackInfo(coreAudioTrack, isActive, argFeedIndex);
			}

			// Subtitle
			var coreSubtitleTrackList = feed.SubtitleTracks;
			subtitleTracks = new SubtitleTrackInfo[coreSubtitleTrackList.Count];
			cvri.TrackID coreActiveSubtitleTrack = argCoreContentInfoMessage.ActiveSubtitleTrack != null ? argCoreContentInfoMessage.ActiveSubtitleTrack : null;
			for (int i = 0; i < coreSubtitleTrackList.Count; i++) {
				cvri.SubtitleTrackInfo coreSubtitleTrack = coreSubtitleTrackList[i];
				bool isActive = false;
				if(coreActiveSubtitleTrack != null) {
					if (coreActiveSubtitleTrack.FeedIdx == argFeedIndex) {
						if (coreActiveSubtitleTrack.TrackIdx == coreSubtitleTrack.SubtitleTrackIdx) {
							isActive = true;
						}
					}
				}
				subtitleTracks[i] = new SubtitleTrackInfo(coreSubtitleTrack, isActive, argFeedIndex);
			}
			url = feed.URL;
		}

		/// <summary>
		/// Returns the currently active video track(s).
		/// </summary>
		/// <returns>An array of all active video tracks as VideoTrackInfo objects. The size can be 0, but this API will never return null.</returns>
		[Obsolete("This API has been renamed to GetActiveVideoTrack(), as there can at most be only one active VideoTrack per feed. This API will be removed after 2023-12-31, please update your code.", false)] // Deprecated on 2023-01-12
		public VideoTrackInfo[] GetActiveVideoTracks() {
			List<VideoTrackInfo> activeVideoTracks = new List<VideoTrackInfo>();
			foreach(VideoTrackInfo videoTrackInfo in videoTracks) {
				if(videoTrackInfo.isActive) {
					activeVideoTracks.Add(videoTrackInfo);
				}
			}
			return activeVideoTracks.ToArray();
		}

		/// <summary>
		/// Returns the currently active video track in this Feed, or null if no Video track is active.
		/// </summary>
		/// <returns>The currently active VideoTrack, or null if none are active.</returns>
		public VideoTrackInfo GetActiveVideoTrack() {
			foreach(VideoTrackInfo videoTrackInfo in videoTracks) {
				if(videoTrackInfo.isActive) {
					return videoTrackInfo;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Returns the number of VideoTracks in this Feed.
		/// This returns the same value as `videoTracks.Length`.
		/// </summary>
		/// <returns>The number of video tracks.</returns>
		public int GetNumberOfVideoTracks() {
			return videoTracks.Length;
		}

		/// <summary>
		/// Returns the number of AudioTracks in this Feed.
		/// This returns the same value as `audioTracks.Length`.
		/// </summary>
		/// <returns>The number of audio tracks.</returns>
		public int GetNumberOfAudioTracks() {
			return audioTracks.Length;
		}

		/// <summary>
		/// Returns the number of SubtitleTracks in this Feed.
		/// This returns the same value as `subtitle.Length`.
		/// </summary>
		/// <returns>The number of subtitle tracks.</returns>
		public int GetNumberOfSubtitleTracks() {
			return subtitleTracks.Length;
		}

		/// <summary>
		/// Returns the currently active audio track.
		/// </summary>
		/// <returns>An AudioTrackInfo object which represents the active audio track, or null if no audio track is active.</returns>
		public AudioTrackInfo GetActiveAudioTrack() {
			foreach(AudioTrackInfo audioTrackInfo in audioTracks) {
				if(audioTrackInfo.isActive) {
					return audioTrackInfo;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the currently active subtitle track.
		/// </summary>
		/// <returns>A SubtitleTrack object which represents the active subtitle track, or null if no subtitle track is active.</returns>
		public SubtitleTrackInfo GetActiveSubtitleTrack() {
			foreach(SubtitleTrackInfo subtitleTrackInfo in subtitleTracks) {
				if(subtitleTrackInfo.isActive) {
					return subtitleTrackInfo;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns a list of selectable audio tracks. Selectable audio tracks are defined as:
		/// > <list type="number">
		/// > <item>
		/// > <term></term>
		/// > <description>the currently active audio track</description>
		/// > </item>
		/// > <item>
		/// > <term></term>
		/// > <description>the audiotracks that are _not_ bound to any video track (e.g. they are not multiplexed together with another (A/V) track).</description>
		/// > </item>
		/// > <item>
		/// > <term></term>
		/// > <description>the audiotracks that are bound to a video track that is currently active.</description>
		/// > </item>
		/// > </list>
		/// This list will never contain duplicates.
		/// 
		/// > [!NOTE] 
		/// > If you are looking for the subtitle track metadata, see the [GetSelectableAudioTracks()](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableAudioTracks) API.
		/// 
		/// </summary>
		/// <returns>The output list will contains zero, one or more [TrackIDs](xref:com.tiledmedia.clearvr.TrackID) objects which uniquely identify selectable audio tracks. This API will never return null.</returns>
		public TrackID[] GetSelectableAudioTrackIDs() {
			List<TrackID> selectableAudioTrackIDs = new List<TrackID>();
			foreach(AudioTrackInfo audioTrack in GetSelectableAudioTracks()) {
				selectableAudioTrackIDs.Add(audioTrack.trackID);
			}
			return selectableAudioTrackIDs.ToArray();
		}

		/// <summary>
		/// Returns a list of AudioTrackInfos of selectable audio tracks within this Feed. Selectable audio tracks are defined as:
		/// > <list type="number">
		/// > <item>
		/// > <term></term>
		/// > <description>the currently active audio track</description>
		/// > </item>
		/// > <item>
		/// > <term></term>
		/// > <description>the audiotracks that are _not_ bound to any video track (e.g. they are not multiplexed together with another (A/V) track).</description>
		/// > </item>
		/// > <item>
		/// > <term></term>
		/// > <description>the audiotracks that are bound to a video track that is currently active.</description>
		/// > </item>
		/// > </list>
		/// This list will never contain duplicates.
		/// </summary>
		/// <returns>The output list will contains zero, one or more [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo) objects. Each AudioTrackInfo is unique identified by its audioTrackID field. This API will never return null.</returns>
		public AudioTrackInfo[] GetSelectableAudioTracks() {
			VideoTrackInfo activeVideoTrack = GetActiveVideoTrack(); // Can be null
			List<AudioTrackInfo> selectableAudioTrackInfos = new List<AudioTrackInfo>();
			foreach(AudioTrackInfo audioTrackInfo in audioTracks) {
				if(audioTrackInfo.isActive ||
					audioTrackInfo.boundToVideoTrackIndex == -1 || 
					(activeVideoTrack != null && activeVideoTrack.trackIndex == audioTrackInfo.boundToVideoTrackIndex)) {
						selectableAudioTrackInfos.Add(audioTrackInfo);
					
				}
			}
			return selectableAudioTrackInfos.ToArray();
		}

		/// <summary>
		/// Returns the number of selectable audio tracks on this Feed.
		/// See [GetSelectableAudioTrackIDs](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableAudioTrackIDs) for important details on the definition of a "selectable" audio track.
		/// </summary>
		/// <returns>Returns the number of selectable audio tracks as a non-negative integer value. 0 means that there are no selectable audio tracks.</returns>
		public int GetNumberOfSelectableAudioTracks() {
			return GetSelectableAudioTrackIDs().Count();
		}

		/// <summary>
		/// Returns a list of selectable subtitle tracks.
		/// This list will never contain duplicates.
		/// > [!NOTE] 
		/// > If you are looking for the subtitle track metadata, see the [GetSelectableSubtitlesTracks()](xref:com.tiledmedia.clearvr.FeedInfo.GetSelectableSubtitlesTracks) API.
		/// </summary>
		/// <returns>The output list will contains zero, one or more [TrackIDs](xref:com.tiledmedia.clearvr.TrackID) objects which will uniquely identify selectable subtitle tracks. This API will never return null.</returns>
		public TrackID[] GetSelectableSubtitlesTrackIDs() {
			List<TrackID> selectableSubtitlesTrackIDs = new List<TrackID>();
			foreach(SubtitleTrackInfo subtitleTrack in GetSelectableSubtitlesTracks()) {
				selectableSubtitlesTrackIDs.Add(subtitleTrack.trackID);
			}
			return selectableSubtitlesTrackIDs.ToArray();
		}

		/// <summary>
		/// Returns a list of selectable subtitle tracks.
		/// This list will never contain duplicates.
		/// </summary>
		/// <returns>The output list will contains zero, one or more [TrackIDs](xref:com.tiledmedia.clearvr.TrackID) objects which will uniquely identify selectable subtitle tracks. This API will never return null.</returns>
		public SubtitleTrackInfo[] GetSelectableSubtitlesTracks() {
			VideoTrackInfo activeVideoTrack = GetActiveVideoTrack();
			List<SubtitleTrackInfo> selectableSubtitlesTrackIDs = new List<SubtitleTrackInfo>();
			foreach(SubtitleTrackInfo subtitleTrackInfo in subtitleTracks) {
				if(subtitleTrackInfo.isActive ||
					subtitleTrackInfo.boundToVideoTrackIndex == -1 || 
					(activeVideoTrack.trackIndex == subtitleTrackInfo.boundToVideoTrackIndex)) {
						selectableSubtitlesTrackIDs.Add(subtitleTrackInfo);
				}
			}
			return selectableSubtitlesTrackIDs.ToArray();
		}

		public override String ToString() {
			StringBuilder builder = new StringBuilder(String.Format("|  |--Video tracks ({0})\n", videoTracks.Length));
			int i = 0;
			foreach(VideoTrackInfo videoTrackInfo in videoTracks) {
				builder.Append(String.Format("|  |  |--{0}\n", videoTrackInfo));
				i++;
			}
			builder.Append(String.Format("|  |--Audio tracks ({0})\n", audioTracks.Length));
			i = 0;
			foreach(AudioTrackInfo audioTrackInfo in audioTracks) {
				builder.Append(String.Format("|  |  |--{0}\n", audioTrackInfo));
				i++;
			}
			builder.Append(String.Format("|  |--Subtitle tracks ({0})\n", subtitleTracks.Length));
			i = 0;
			foreach(SubtitleTrackInfo subtitleTrackInfo in subtitleTracks) {
				builder.Append(String.Format("|  |  |--{0}\n", subtitleTrackInfo));
				i++;
			}
			return builder.ToString();
		}
	}

	/// <summary>
	/// Helper class that holds information of one video track, like codec, dimensions and framerate.
	/// For detailed information about the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo), [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo), [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo), [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo), [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) objects and their relationship, refer to the [ContentItem](~/readme/contentitem.md) documentation.
	/// </summary>
	public class VideoTrackInfo {
		private cvri.VideoTrackInfo coreVideoTrackInfo;
		/// <summary>
		/// Whether this video track is active or not.
		/// </summary>
		public bool isActive { get; private set; }

		/// <summary>
		/// The Feed index associated with this VideoTrack.
		/// </summary>
		public int feedIndex { get; private set; }

		/// <summary>
		/// The index of the video track as int.
		/// </summary>
		public int trackIndex {
			get { return coreVideoTrackInfo.VideoTrackIdx; }
		}
		/// <summary>
		/// The width of the video as int.
		/// </summary>
		public int width {
			get { return coreVideoTrackInfo.Width; }
		}
		/// <summary>
		/// The height of the video as int.
		/// </summary>
		public int height {
			get { return coreVideoTrackInfo.Height; }
		}

		/// <summary>
		/// Whether the VideoTrack is supported by the device or not. This can be Unknown. Refer to [ContentSupportedStatus](xref:com.tiledmedia.clearvr.ContentSupportedStatus) for more more information.
		/// </summary>
		public ContentSupportedStatus supportedStatus {
			get { return ContentSupportedStatusMethods.FromCoreStatus(coreVideoTrackInfo.Supported); }
		}

		/// <summary>
		/// The numerator of the framerate integer fraction.
		/// The framerate is an integer fraction: fps = num / denom
		/// </summary>
		public int framerateNum {
			get { return coreVideoTrackInfo.FramerateNum; }
		}

		/// <summary>
		/// The denominator of the framerate integer fraction.
		///  framerate is a fraction: fps = num / denom
		/// </summary>
		public int framerateDenom {
			get { return coreVideoTrackInfo.FramerateDenom; }
		}

		/// <summary>
		/// Get the framerate as a fraction. This is the same as calling `framerateNum / framerateDenom` yourself
		/// </summary>
		public float framerate {
			get { return (float) framerateNum / (float) framerateDenom; }
		}

		/// <summary>
		/// The content format of the video as ContentFormat.
		/// </summary>
		public ContentFormat contentFormat {
			get { return ContentFormatMethods.FromStringValue(coreVideoTrackInfo.ProjectionType); }
		}

		/// <summary>
		/// The type of DRM used in this video track as DRMTypes.
		/// </summary>
		public DRMTypes drmType {
			get { return DRMTypesMethods.FromCoreProtobuf(coreVideoTrackInfo.DRM); }
        }

		/// <summary>
		/// Get the video codec type.
		/// </summary>
		public VideoCodecTypes videoCodecType { 
			get { return VideoCodecTypesMethods.FromCoreProtobuf(coreVideoTrackInfo.Codec); } 
		}

		/// <summary>
		/// Get the bitrate of this track in kilobits per second (kbps), _as advertised in the manifest_. 
		/// > [!NOTE]
		/// > This will return 0 if no bitrate is advertised in the manifest file.
		/// > [!WARNING]
		/// > For ClearVR content, this will return the advertised bitrate for the entire (spatially segmented) video. The value should _not_ be used as an indication of the required internet connectivity as only a small part of the entire video is ever fetched over the network.
		/// </summary>
		public Int32 bitrateInKbps { 
			get { return coreVideoTrackInfo.BitrateInKbps; } 
		}

		/// <summary>
		/// Get the URL
		/// </summary>
		public String url { 
			get { return coreVideoTrackInfo.URL; } 
		}

		/// <summary>
		/// Returns the [TrackID](xref:com.tiledmedia.clearvr.TrackID), a unique identifier for this track and the [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) it belongs to. It can be used when constructing your FeedConfiguration. 
		/// </summary>
		/// <value>The TracKID, it cannot be null.</value>
		public TrackID trackID { 
			get { return new TrackID(feedIndex, trackIndex); }
		}

		/// <summary>
		/// Since v9.1.2
		/// The aspect ratio is defined as width / height. If height = 0, this returns 0.
		/// </summary>
		public float aspectRatio {
			get {
				if(height != 0) {
					return (float) width / (float) height;
				} else {
					return 0;
				}
			}
		}

		internal VideoTrackInfo(cvri.VideoTrackInfo argCoreVideoTrackInfo, bool argIsActive, int argFeedIndex) {
			this.coreVideoTrackInfo = argCoreVideoTrackInfo;
			this.isActive = argIsActive;
			this.feedIndex = argFeedIndex;
        }

		/// <summary>
		/// Returns a pretty formatted quality descriptor string, in the format of `[width]x[height]p[framerate]`. The framerate is truncated to three decimals.
		/// </summary>
		/// <returns>The pretty-printed quality descriptor.</returns>
		public String GetQualityDescriptor() {
			return String.Format("{0}x{1}p{2:0.000}", width, height, framerate);
		}

		public override string ToString() {
			return String.Format("Index: {0}, active: {1}, supported: {2}, codec: {3}, dimensions: {4}x{5}, framerate: {6:0.000} ({7}/{8}), bitrate: {9} kbps, content format: {10}, DRM type: {11}. URL: {12}",
                trackIndex, isActive, supportedStatus, videoCodecType, width, height, framerate, framerateNum, framerateDenom, bitrateInKbps, contentFormat, drmType, url);
		}
	}

	/// <summary>
	/// Helper class that holds the info of one audio track like codec, sample rate and number of channels.
	/// For detailed information about the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo), [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo), [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo), [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo), [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) objects and their relationship, refer to the [ContentItem](~/readme/contentitem.md) documentation.
	/// </summary>
	public class AudioTrackInfo {
		private cvri.AudioTrackInfo coreAudioTrackInfo;

		/// <summary>
		/// Whether this audio track is active or not.
		/// </summary>
		public bool isActive { get; private set; }
		
		/// <summary>
		/// The Feed index associated with this audio track.
		/// </summary>
		public int feedIndex { get; private set; }

		/// <summary>
		/// The index of the audio track as int.
		/// </summary>
		public int trackIndex {
			get { return coreAudioTrackInfo.AudioTrackIdx; }
		}
		/// <summary>
		/// Some audio tracks cannot be individually accessed and played as they are bound to a specific video track. This is for example the case when the audio track and video track are multiplexed together into the same file.
		/// If this is the case, this API will return the index of that video track. If the audio track can be individually accessed, this API will return -1.
		/// This returns the index of the video track this audio track is bound to, or -1 if this audio track is not bound to any specific video track and can be individually accessed.
		/// </summary>
		public int boundToVideoTrackIndex {
			get { return coreAudioTrackInfo.BoundToVideoTrackIdx; }
		}
		/// <summary>
		/// The audio codec type of this audio track as AudioCodecTypes
		/// </summary>
		public AudioCodecTypes audioCodecType {
			get { return AudioCodecTypesMethods.FromCoreProtobuf(coreAudioTrackInfo.Codec); }
		}
		/// <summary>
		/// The sample rate of this audio track as int.
		/// </summary>
		public int sampleRate {
			get { return coreAudioTrackInfo.SampleRate; }
		}
		/// <summary>
		/// The number of channels of this audio track as int.
		/// </summary>
		public int numberOfChannels {
			get { return coreAudioTrackInfo.NumberOfChannels; }
		}

		/// <summary>
		/// The name of the audio track as String.
		/// </summary>
		public String name {
			get { return coreAudioTrackInfo.Name; }
		}

		/// <summary>
		/// The language of the audio track as String.
		/// </summary>
		public String language {
			get { return coreAudioTrackInfo.Language; }
		}

		/// <summary>
		/// The type of DRM used in this audio track as DRMTypes.
		/// </summary>
		/// <value></value>
		public DRMTypes drmType {
			get { return DRMTypesMethods.FromCoreProtobuf(coreAudioTrackInfo.DRM); }
		}

		/// <summary>
		/// Get the URL
		/// </summary>
		public String url { 
			get { return coreAudioTrackInfo.URL; } 
		}

		/// <summary>
		/// Get the bitrate of this track in kilobits per second (kbps), _as advertised in the manifest_. 
		/// > [!NOTE]
		/// > This will return 0 if no bitrate is advertised in the manifest file.
		/// </summary>
		public Int32 bitrateInKbps { 
			get { return coreAudioTrackInfo.BitrateInKbps; } 
		}

		/// <summary>
		/// Returns the [TrackID](xref:com.tiledmedia.clearvr.TrackID), a unique identifier for this track and the [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) it belongs to. It can be used when constructing your FeedConfiguration. 
		/// </summary>
		/// <value>The TrackID, it cannot be null.</value>
		public TrackID trackID { 
			get { return new TrackID(feedIndex, trackIndex); }
		}

		internal AudioTrackInfo(cvri.AudioTrackInfo argCoreAudioTrackInfo, bool argIsActive, int argFeedIndex) {
			this.coreAudioTrackInfo = argCoreAudioTrackInfo;
			this.isActive = argIsActive;
			this.feedIndex = argFeedIndex;
		}

		public override string ToString() {
			return String.Format("Index: {0}, active: {1}, bound to video track index: {2}, audio codec type: {3}, sample rate: {4}, number of channels: {5}, bitrate: {6} kbps, name: '{7}', language: '{8}', DRM type: {9}. URL: {10}",
				trackIndex, isActive, boundToVideoTrackIndex, audioCodecType, sampleRate, numberOfChannels, bitrateInKbps, name, language, drmType, url);
		}
    }

	/// <summary>
	/// Helper class that holds information of one subtitle track.
	/// For detailed information about the [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo), [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo), [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo), [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo), [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) objects and their relationship, refer to the [ContentItem](~/readme/contentitem.md) documentation.
	/// </summary>
	public class SubtitleTrackInfo {
		private cvri.SubtitleTrackInfo coreSubtitleTrackInfo;

		/// <summary>
		/// Whether this subtitle track is active or not.
		/// </summary>
		public bool isActive { get; private set; }
		
		/// <summary>
		/// The Feed index associated with this Subtitle Track.
		/// </summary>
		public int feedIndex { get; private set; }

		/// <summary>
		/// The index of the subtitle track as int.
		/// </summary>
		public int trackIndex {
			get { return coreSubtitleTrackInfo.SubtitleTrackIdx; }
		}
		/// <summary>
		/// Some subtitle tracks cannot be individually accessed and played as they are bound to a specific video track. This is for example the case when the subtitle track and video track are multiplexed together into the same file.
		/// If this is the case, this API will return the index of that video track. If the subtitle track can be individually accessed, this API will return -1.
		/// This returns the index of the video track this subtitle track is bound to, or -1 if this subtitle track is not bound to any specific video track and can be individually accessed.
		/// </summary>
		public int boundToVideoTrackIndex {
			get { return coreSubtitleTrackInfo.BoundToVideoTrackIdx; }
		}

		/// <summary>
		/// Get the URL
		/// </summary>
		public String url { 
			get { return coreSubtitleTrackInfo.URL; } 
		}

		/// <summary>
		/// Get the Language
		/// </summary>
		public String language { 
			get { return coreSubtitleTrackInfo.Language; } 
		}

		/// <summary>
		/// Returns the [TrackID](xref:com.tiledmedia.clearvr.TrackID), a unique identifier for this track and the [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) it belongs to. It can be used when constructing your FeedConfiguration. 
		/// </summary>
		/// <value>The TracKID, it cannot be null.</value>
		public TrackID trackID { 
			get { return new TrackID(feedIndex, trackIndex); }
		}

		/// <summary>
		/// The name of the subtitle track, can be an empty string if not specified.
		/// </summary>
		/// <value>For example "EN hearing impaired"</value>
		public String name {
			get { return coreSubtitleTrackInfo.Name; }
		}

		internal SubtitleTrackInfo(cvri.SubtitleTrackInfo coreSubtitleTrackInfo, bool argIsActive, int argFeedIndex) {
			this.coreSubtitleTrackInfo = coreSubtitleTrackInfo;
			this.isActive = argIsActive;
			this.feedIndex = argFeedIndex;
		}

		public override string ToString() {
			return String.Format("Index: {0}, active: {1}, bound to video track index: {2}. URL: {3}. Language: {4}. Name: {5}", trackIndex, isActive, boundToVideoTrackIndex, url, language, name);
		}
	}

	/// <summary>
	/// This enum is used to distinguish between the various Render Pipelines Unity supports.
	/// </summary>
	internal enum RenderPipelineTypes {
		/// <summary>
		/// The Render Pipeline is unknown.
		/// </summary>
		Unknown,
		/// <summary>
		/// The default, classic, Unity Render Pipeline.
		/// </summary>
		BuiltInPipeline,
		/// <summary>
		/// The Universal Render Pipeline (aka URP). This also includes the formally known Light Weight Render Pipeline (LWRP)
		/// </summary>
		UniversalPipeline,
		/// <summary>
		/// The High Definition Render Pipeline (aka HDRP)
		/// </summary>
		HighDefinitionPipeline
	}

	/// <summary>
	/// Parameters to specify the mapping between a DisplayObject and a feed
	/// </summary>
    [Serializable]
	public class DisplayObjectMapping {
		[field:SerializeField] private DisplayObjectClassTypes _displayObjectClassType;
		/// <summary>
		/// The class of the Display Object, refer to [DisplayObjectClassTypes](xref:com.tiledmedia.clearvr.DisplayObjectClassTypes) for details.
		/// </summary>
		public DisplayObjectClassTypes displayObjectClassType {
			get {
				return _displayObjectClassType;
			}
			set {
				_displayObjectClassType = value;
			}
		}

		[field:SerializeField] private int _displayObjectID = -1; // A negative value means: not set
		/// <summary>
		/// The ID of the Display Object. This ID is NOT constant between runs
		/// </summary>
		// This is made internal because of #6561
		internal int displayObjectID {
			get { return _displayObjectID; }
			// Setter is only available internally
			// Note that we DO NOT and CANNOT set the displayObjectID on the ClearVRDisplayObjectController
			set {  _displayObjectID = value;} 
		}

		[field:SerializeField] private int _feedIndex;
		/// <summary>
		/// The feed index of the feed to which this Display Object is mapped.
		/// </summary>
		public int feedIndex {
			get { return _feedIndex; }
			set {  _feedIndex = value;}
		}

		[field:SerializeField] private ClearVRDisplayObjectControllerBase _clearVRDisplayObjectController;
		/// <summary>
		/// The ClearVRDisplayObjectController which this Display Object is mapped.
		/// </summary>
		public ClearVRDisplayObjectControllerBase clearVRDisplayObjectController {
			get {
				return _clearVRDisplayObjectController;
			}
			internal set { // Since we set it in the LayoutManager
				_clearVRDisplayObjectController = value;
			}
		}

        [field:SerializeField] private ContentFormat _contentFormat;
		/// <summary>
		/// The ContentFormat of the Display Object.
		/// </summary>
		public ContentFormat contentFormat {
			get {
				return _contentFormat;
			}
			set {
				_contentFormat = value;
			}
		}

		/// <summary>
		/// Constructor that fully set all the value of the display object to feed mapping
		/// </summary>
		/// <param name="argClearVRDisplayObjectController">The ClearVRDisplayObjectController..</param>
		/// <param name="argFeedIndex"> The ID of the feed, in the selected ContentItem, we want to map on the display object</param>
		/// <param name="argDisplayObjectClass"> The DisplayObject class of the selected display object.</param>
		/// <param name="argContentFormat">The content format of this display object.</param>
		public DisplayObjectMapping(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController, int argFeedIndex, DisplayObjectClassTypes argDisplayObjectClass, ContentFormat argContentFormat) {
			clearVRDisplayObjectController = argClearVRDisplayObjectController;
			displayObjectClassType = argDisplayObjectClass;
			feedIndex = argFeedIndex;
			contentFormat = argContentFormat;
		}

		internal static DisplayObjectMapping FromCoreProtobuf(cvri.DisplayObjectFeedTriple argDisplayObjectMappingCore, ClearVRLayoutManager argClearVRLayoutManager) {
			if(argDisplayObjectMappingCore == null || argClearVRLayoutManager == null) {
				return null;
			}
			ClearVRDisplayObjectControllerBase doc = argClearVRLayoutManager.GetClearVRDisplayObjectControllerByDisplayObjectID(argDisplayObjectMappingCore.DisplayObjectId);
			if(doc == null) {
				UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Unable to find the ClearVRDisplayObjectController with ID {0} in the ClearVRLayoutManager. Behaviour is undefined, videos might be disappearing.", argDisplayObjectMappingCore.DisplayObjectId));
			}

			// When serializing To and From CoreProtobuf we loose the DisplayObjectController mapping.
			DisplayObjectMapping dom = new DisplayObjectMapping(doc,
				argDisplayObjectMappingCore.FeedId,
				DisplayObjectClassTypesMethods.FromCoreProtobuf(argDisplayObjectMappingCore.DisplayObjectClass),
				ContentFormat.Unknown
			);
			// Explicitly set the Display Object ID
			dom.displayObjectID = argDisplayObjectMappingCore.DisplayObjectId;
			return dom;
		}

		// Clones the object.
		internal DisplayObjectMapping Clone() {
			var dom = new DisplayObjectMapping(this.clearVRDisplayObjectController, this.feedIndex, this.displayObjectClassType, this.contentFormat);
			dom.displayObjectID = this.displayObjectID;
			return dom;
		}

		/// <summary>Return a protobuf representation of the DisplayObjectFeedTriple.</summary>
		internal cvri.DisplayObjectFeedTriple ToCoreProtobuf() {
			return new cvri.DisplayObjectFeedTriple() {
				DisplayObjectClass = displayObjectClassType.ToCoreProtobuf(),
				DisplayObjectId = _displayObjectID,
				FeedId = _feedIndex
			};
		}

		public override String ToString() {
			return String.Format("DOID: {0}, FeedIndex: {1}, class; {2}, ContentFormat: {3}. DOC: {4}", displayObjectID, feedIndex, displayObjectClassType, contentFormat, _clearVRDisplayObjectController);
		}

	}

	/// <summary>
	/// Parameters to specify the feed to display object mapping for the SetFeedLayout API
	/// This object has a mutable builder pattern.
	/// </summary>
    [Serializable]
	public class LayoutParameters {
		// We cannot serialize the protobuf message unfortunately (as this struct is used directly in the Unity Editor), so we resort to having manual fields instead.
		[field:SerializeField] private String _name = "New Layout";
        public string name {
			get {
				return _name;
			}
			set {
				_name = value;
			}
		}

        [field:SerializeField] private List<DisplayObjectMapping> _displayObjectMappings = new List<DisplayObjectMapping>();
        public List<DisplayObjectMapping> displayObjectMappings {
			get {
				return _displayObjectMappings;
			}
			set {
				_displayObjectMappings = value;
			}
		}
        
		[field:SerializeField] private Int32 _audioFeedIndex = -2;
		[field:SerializeField] private Int32 _audioTrackIndex = -2;

		/// <summary>
		/// Specifies the audio feed index and track index.
		/// </summary>
		// Note that the returned TrackID object is immutable.
		public TrackID audioTrackID {
			get {
				return new TrackID(_audioFeedIndex, _audioTrackIndex);
			}
			set {
				_audioFeedIndex = value != null ? value.feedIndex : -2;
				_audioTrackIndex = value != null ? value.trackIndex : -2;
			}
		}

		[field:SerializeField] private Int32 _subtitleFeedIndex = -2;
		[field:SerializeField] private Int32 _subtitleTrackIndex = -2;
		// Note that the returned TrackID object is immutable.
		/// <summary>
		/// Specifies the subtitle feed index and track index.
		/// </summary>
		public TrackID subtitleTrackID {
			get {
				return new TrackID(_subtitleFeedIndex, _subtitleTrackIndex);
			}
			set {
				_subtitleFeedIndex = value != null ? value.feedIndex : -2;
				_subtitleTrackIndex = value != null ? value.trackIndex: -2;
			}			
		}

		[field:SerializeField] private String _preferredAudioLanguage = "";
		/// <summary>
		/// The preferred audio language as an ISO-639 language code.
		/// </summary>
		public String preferredAudioLanguage {
			get {
				return _preferredAudioLanguage;
			}
			set {
				_preferredAudioLanguage = value;
			}
		}
		[field:SerializeField] private String _preferredSubtitlesLanguage = "";
		public String preferredSubtitlesLanguage {
			get {
				return _preferredSubtitlesLanguage;
			}
			set {
				_preferredSubtitlesLanguage = value;
			}
		}
		[field:SerializeField] private bool _disableFallbackSwitch = false;
		/// <summary>
		/// This parameter applies to mosaic playback.
		/// Imagine you have:
		/// 1. one fullscreen display object 0 showing feed 0 in highest quality
		/// 2. one thumbnail display object 1 showing feed 1 in thumbnail quality
		/// 
		/// Now you switch feed 0 and 1, showing feed 1 on display object 0 and feed 0 on display object 1.
		/// 
		/// Switching behaviour depends on the value of this argument:
		/// 
		/// If `disableFallbackSwitch == false` (default value): the switch is immediate but you will briefly see feed 1 in thumbnail quality on the fullscreen display object 0.
		/// If `disableFallbackSwitch == true`: the switch will complete once feed 1 is available in highest quality. The responsiveness depends on the network conditions, but the user will always see the highest quality.
		/// 
		/// > [!WARNING]
		/// > This is an advanced parameter, you are strongly recommened to NOT change its default value
		/// </summary>
		/// <value></value>
		public bool disableFallbackSwitch {
			get {
				return _disableFallbackSwitch;
			}
			set {
				_disableFallbackSwitch = value;
			}
		}

		/// <summary>
		/// Default constructor. Initialize the configuration to an empty mapping with default audio and subtitle selection
		/// </summary>
		/// <param name="argAudioTrackID">The audio [TrackID](xref:com.tiledmedia.clearvr.TrackID). Default value: null (meaning: auto-select the first available audio track.</param>
		/// <param name="argSubtitleTrackID">The subtitle [TrackID](xref:com.tiledmedia.clearvr.TrackID). Default value: null (meaning: auto-select the first available subtitle track.</param>
		/// <param name="argDisableFallbackSwitch">Whether to disable fallback-based fast switching. Default value: false. You are strongly recommended to not change this value</param>		
		public LayoutParameters(TrackID argAudioTrackID = null, TrackID argSubtitleTrackID = null, bool argDisableFallbackSwitch = false) {
			this.audioTrackID = argAudioTrackID;
			this.subtitleTrackID = argSubtitleTrackID;
			this.disableFallbackSwitch = argDisableFallbackSwitch;
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		public LayoutParameters(LayoutParameters argOtherLayoutParameters) {
			this.name = argOtherLayoutParameters.name;
			this.audioTrackID = argOtherLayoutParameters.audioTrackID;
			this.subtitleTrackID = argOtherLayoutParameters.subtitleTrackID;
			this.preferredAudioLanguage = argOtherLayoutParameters.preferredAudioLanguage;
			this.preferredSubtitlesLanguage = argOtherLayoutParameters.preferredSubtitlesLanguage;
			this.disableFallbackSwitch = argOtherLayoutParameters.disableFallbackSwitch;
			if(argOtherLayoutParameters.displayObjectMappings != null) {
				foreach(DisplayObjectMapping dom in argOtherLayoutParameters.displayObjectMappings) {
					this.displayObjectMappings.Add(dom.Clone());
				}
			}
		}

		/// <summary>
		/// Returns a the DisplayObjectMapping object that has displayObjectClassType set to FullScreen.
		/// Remember that there can be only 0 or 1 DisplayObjectMappings with this DisplayObjectClassType.
		/// </summary>
		/// <returns>Returns null if not present, the appropriate DisplayObjectMapping otherwise.</returns>
		public DisplayObjectMapping GetFullScreenDisplayObjectMapping() {
			foreach(DisplayObjectMapping dom in displayObjectMappings) {
				if(dom.displayObjectClassType == DisplayObjectClassTypes.FullScreen) {
					return dom;
				}
			}
			return null;
		}

		/// <summary>
		/// Internal convenience API that checks whether any of the DOMs contain a DOCSprite.
		/// </summary>
		/// <returns>True if such DOCSprite is found, false otherwise.</returns>
		internal bool GetIsAnyDisplayObjectControllerSprite() {
			foreach(DisplayObjectMapping dom in displayObjectMappings) {
				if(dom.clearVRDisplayObjectController != null) {
					if(dom.clearVRDisplayObjectController.meshTextureMode == MeshTextureModes.Sprite) {
						return true;
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Creates and returns a deep-copy of the object.
		/// </summary>
		public LayoutParameters Clone() {
			return new LayoutParameters(this);
		}

		internal static LayoutParameters FromCoreProtobuf(cvri.SetFeedLayoutParams argLayoutParamsCore, ClearVRLayoutManager argClearVRLayoutManager) {
			if(argLayoutParamsCore == null) {
				return null;
			}
			LayoutParameters layoutParameters = new LayoutParameters(new TrackID(argLayoutParamsCore.AudioFeedId, argLayoutParamsCore.AudioTrackIdx), new TrackID(0 /* TODO */, argLayoutParamsCore.SubtitleTrackIdx));
			layoutParameters.name = argLayoutParamsCore.Name;
			layoutParameters.preferredAudioLanguage = argLayoutParamsCore.PreferredAudioLanguage;
			layoutParameters.preferredSubtitlesLanguage = argLayoutParamsCore.PreferredSubtitlesLanguage;
			layoutParameters.disableFallbackSwitch = argLayoutParamsCore.DisableFallbackSwitch;
			foreach(cvri.DisplayObjectFeedTriple domCore in argLayoutParamsCore.DisplayObjectMapping) {
				layoutParameters.displayObjectMappings.Add(DisplayObjectMapping.FromCoreProtobuf(domCore, argClearVRLayoutManager));
			}
			return layoutParameters;
		}
		
		internal cvri.SetFeedLayoutParams ToCoreProtobuf() {
			cvri.SetFeedLayoutParams sflp = new cvri.SetFeedLayoutParams() {
				Name = name,
				AudioFeedId = audioTrackID.feedIndex,
				AudioTrackIdx = audioTrackID.trackIndex,
				SubtitleFeedId = subtitleTrackID.feedIndex,
				SubtitleTrackIdx = subtitleTrackID.trackIndex,
				PreferredAudioLanguage = preferredAudioLanguage,
				PreferredSubtitlesLanguage = preferredSubtitlesLanguage,
				DisableFallbackSwitch = disableFallbackSwitch
			};
			foreach(DisplayObjectMapping dom in displayObjectMappings) {
				if(dom.clearVRDisplayObjectController != null && dom.clearVRDisplayObjectController.isActiveAndEnabled) {
					sflp.DisplayObjectMapping.Add(dom.ToCoreProtobuf());
				}
			}
			return sflp;
		}

		/// <summary>Return a protobuf representation of the DisplayObjectFeedTriple wrapped into a CallCore message.</summary>
		internal cvri.CallCoreRequest ToCallCoreCoreProtobuf() {
			cvri.CallCoreRequest tmpCallCoreRequest = new cvri.CallCoreRequest();
			tmpCallCoreRequest.CallCoreRequestType = cvri.CallCoreRequestType.SetFeedLayout;
			tmpCallCoreRequest.SetFeedLayoutRequest = this.ToCoreProtobuf();
			return tmpCallCoreRequest;
		}

		public override String ToString() {
			return String.Format("Name: {0}\nVideo - displayObjectMappings: {1}\nDisable Fallback Switching: {2}\nAudio: {3}, preferredAudioLanguage: '{4}'\nSubtitle: {5}, {6}", name, displayObjectMappings.Count > 0 ? string.Join("\n", displayObjectMappings) : "None", disableFallbackSwitch, audioTrackID, preferredAudioLanguage, subtitleTrackID, preferredSubtitlesLanguage);
		}
	}

	internal enum NRPActionTypes {
		Wait,
		MarkNRPAsInitialized,
		RegisterClearVRDisplayObjectController,
		UnregisterClearVRDisplayObjectController
	}

	internal class NRPAction {
		internal NRPActionTypes asyncNRPActionType;
		internal System.Object payload;

		internal NRPAction(NRPActionTypes argAsyncNRPActionType, System.Object argPayload = null) {
			asyncNRPActionType = argAsyncNRPActionType;
			payload = argPayload;
		}

		public override String ToString() {
			return String.Format("Type: {0}, Payload: '{1}' (might be null)", asyncNRPActionType, payload);
		}
	}
	internal enum AsyncLayoutManagerActionTypes {
		PreloadLegacyDisplayObject
	}

	internal class AsyncLayoutManagerAction {
		internal AsyncLayoutManagerActionTypes asyncLayoutManagerActionType;
		internal System.Object payload;

		internal AsyncLayoutManagerAction(AsyncLayoutManagerActionTypes argAsyncLayoutManagerActionType, System.Object argPayload = null) {
			asyncLayoutManagerActionType = argAsyncLayoutManagerActionType;
			payload = argPayload;
		}

		public override String ToString() {
			return String.Format("Type: {0}, Payload: '{1}' (might be null)", asyncLayoutManagerActionType, payload);
		}
	}
	/// <summary>
	/// This class has been removed in v9.0 and can no longer be used.
	/// </summary>
	[Obsolete("This class has been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.", true)]
	// Will be removed after 2023-01-31
	public class ClearVRViewportAndObjectPose {
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public ClearVRPose viewportPose = new ClearVRPose();
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public ClearVRDisplayObject displayObject = new ClearVRDisplayObject();
	}

	/// <summary>
	/// This class has been removed in v9.0 and can no longer be used.
	/// </summary>
	[Obsolete("This class has been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.", true)]
	// Will be removed after 2023-01-31
	public class ClearVRDisplayObject {
		/// <summary>
		/// The Pose
		/// </summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public ClearVRPose pose;
		/// <summary>
		/// The scale
		/// </summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public ClearVRScale scale;
	}	

	/// <summary>
	/// This class has been removed in v9.0 and can no longer be used.
	/// </summary>
	// Will be removed after 2023-01-31
	[Obsolete("This class has been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.", true)]
	public class ClearVRPose {
		/// <summary>Position X coordinate.</summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public double posX;
		/// <summary>Position Y coordinate.</summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public double posY;
		/// <summary>Position Z coordinate.</summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public double posZ;
		/// <summary>Orientation w quaternion component.</summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public double w;
		/// <summary>Orientation x quaternion component.</summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public double x;
		/// <summary>Orientation y quaternion component.</summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public double y;
		/// <summary>Orientation z quaternion component.</summary>
		[Obsolete("This field and class have been deprecated in v9.0 and can no longer be used. Use the LayoutManager instead to position your mesh.")]
		public double z;
	}

	/// <summary>
	/// This class is used to configure the log level and logging output of each component of Tiledmedia SDK.
	/// Do not forget to call [ClearVRPlayer.EnableLogging(loggingConfiguration)](xref:com.tiledmedia.clearvr.ClearVRPlayer.EnableLogging(com.tiledmedia.clearvr.LoggingConfiguration)) to apply the configuration.
	/// > [!WARNING]
	/// > Take special care to keep only the default constructed instance of this class in the ClearVRPlayer to avoid performance impacts
	/// </summary>
	public class LoggingConfiguration {

		/// <summary>
		/// Static getter for a Logging configuration configured with default values:
		/// - Debug log level
		/// - Dump logs into file clearvr.tmlog
		/// - Dump interaction recording into recorder.tmerp (recorder.tmerj is used for debug)
		/// > [!NOTE]
		/// > Can not be called statically or from a constructor.
		/// </summary>
		/// <returns>A LoggingConfiguration instance with default values</returns>
		public static LoggingConfiguration GetDefaultLoggingConfiguration() {
			LoggingConfiguration loggingConfiguration = new LoggingConfiguration();
			loggingConfiguration.globalLogLevel = LogLevels.Debug;
			loggingConfiguration.globalLogFileName = ClearVRConstants.CLEARVR_LOG_GLOBAL_FILE_NAME;
			loggingConfiguration.globalLogFolder = GetDefaultLoggingFolder();
			loggingConfiguration.interactionEventRecorderFileName = ClearVRConstants.CLEARVR_EVENT_RECORDER_PROTO_FILE_NAME;
			loggingConfiguration.globalLogToMemory = false;
			loggingConfiguration.globalLogToStderr = false;
			loggingConfiguration.logNetwork = false;
			return loggingConfiguration;
		}

		public static String GetDefaultLoggingFolder() {
			try {
				return Application.persistentDataPath + System.IO.Path.DirectorySeparatorChar;
			} catch {
				throw new Exception("[ClearVR] Tried set default logging folder statically of from a constructer. Use Awake or OnEnable instead.");
			}
		}

		/// <summary>
		/// globalLogLevel specify the default log verbosity for all Tiledmedia SDK components. Component log level can be overriden by setting explicitly the log level of that component
		/// In production this value should always be  LogLevels.Warn
		/// </summary>
		public LogLevels globalLogLevel {
			get {
				return _globalLogLevel;
			}
			set {
				_globalLogLevel = value;
			}
		}
		private LogLevels _globalLogLevel = LogLevels.Warn;

		/// <summary>
		/// Specify the output folder of the logs of all components.
		/// </summary>
		public string globalLogFolder {
			get {
				return _globalLogFolder;
			}
			set {
				_globalLogFolder = value;
			}
		}
		private string _globalLogFolder = "";

		/// <summary>
		/// Specify the output file name of the interaction recorder. Empty string means interaction recorder is diabled. The output folder is the same as for the global log files.
		/// </summary>
		public string interactionEventRecorderFileName {
			get {
				return _interactionEventRecorderFileName;
			}
			set {
				_interactionEventRecorderFileName = value;
			}
		}
		private string _interactionEventRecorderFileName = "";

		/// <summary>
		/// Specify the output file name of the logs. Empty string means stdout and is the default value. A non-empty string means logging to a file located at the path specified in globalLogPath. By default all components log to the same output if not overriden
		/// </summary>
		public string globalLogFileName {
			get {
				return _globalLogFileName;
			}
			set {
				_globalLogFileName = value;
			}
		}
		private string _globalLogFileName = "";
		/// <summary>
		/// Specify whether all components should log to `stderr` or not. Default value: false (meaning that they will log to file).
		/// </summary>
		public bool globalLogToStderr {
			get {
				return _globalLogToStderr;
			}
			set {
				_globalLogToStderr = value;
			}
		}
		private bool _globalLogToStderr = false;

		/// <summary>
		/// Specify whether all components should perform in-memory logging or not. Default value: false.
		/// </summary>
		public bool globalLogToMemory {
			get {
				return _globalLogToMemory;
			}
			set {
				_globalLogToMemory = value;
			}
		}
		private bool _globalLogToMemory = false;

		/// <summary>
		/// Enable network logging. Default value: false.
		/// </summary>
		public bool logNetwork {
			get {
				return _logNetwork;
			}
			set {
				_logNetwork = value;
			}
		}
		private bool _logNetwork;

		/// <summary>
		/// To override the core log level
		/// </summary>
		public LogLevels coreLogLevel {
			get {
				if (_coreLogLevelIsOverridden) {
					return _coreLogLevel;
				} else {
					return globalLogLevel;
				}
			}
			set {
				_coreLogLevelIsOverridden = true;
				_coreLogLevel = value;
			}
		}
		private LogLevels _coreLogLevel = LogLevels.Warn;
		private bool _coreLogLevelIsOverridden = false;

		/// <summary>
		/// To override the core log file name
		/// </summary>
		public string coreLogFileName {
			get {
				if (_coreLogFileNameIsOverridden) {
					return _coreLogFileName;
				} else {
					return globalLogFileName;
				}
			}
			set {
				_coreLogFileNameIsOverridden = true;
				_coreLogFileName = value;
			}
		}
		private string _coreLogFileName = "";
		private bool _coreLogFileNameIsOverridden = false;

		/// <summary>
		/// To override whether the core component logs to stderr or not. Default value: false.
		/// </summary>
		public bool coreLogToStderr {
			get {
				if (_coreLogToStderrIsOverridden) {
					return _coreLogToStderr;
				} else {
					return globalLogToStderr;
				}
			}
			set {
				_coreLogToStderrIsOverridden = true;
				_coreLogToStderr = value;
			}
		}
		private bool _coreLogToStderr = false;
		private bool _coreLogToStderrIsOverridden = false;

		/// <summary>
		/// To override whether the core component performs in-memory logging or not. Default value: false.
		/// </summary>
		public bool coreLogToMemory {
			get {
				if (_coreLogToMemoryIsOverridden) {
					return _coreLogToMemory;
				} else {
					return globalLogToMemory;
				}
			}
			set {
				_coreLogToMemoryIsOverridden = true;
				_coreLogToMemory = value;
			}
		}
		private bool _coreLogToMemory = false;
		private bool _coreLogToMemoryIsOverridden = false;

		/// <summary>
		/// To override the NRP log level
		/// </summary>
		public LogLevels nrpLogLevel {
			get {
				if (_nrpLogLevelIsOverridden) {
					return _nrpLogLevel;
				} else {
					return globalLogLevel;
				}
			}
			set {
				_nrpLogLevelIsOverridden = true;
				_nrpLogLevel = value;
			}
		}
		private LogLevels _nrpLogLevel = LogLevels.Warn;
		private bool _nrpLogLevelIsOverridden = false;

		/// <summary>
		/// To override the NRP log file name
		/// </summary>
		public string nrpLogFileName {
			get {
				if (_nrpLogFileNameIsOverridden) {
					return _nrpLogFileName;
				} else {
					return globalLogFileName;
				}
			}
			set {
				_nrpLogFileNameIsOverridden = true;
				String realValue = value;
				_nrpLogFileName = value;
			}
		}
		private string _nrpLogFileName = "";
		private bool _nrpLogFileNameIsOverridden = false;

		/// <summary>
		/// To override whether the NRP component logs to stderr or not. Default value: false.
		/// </summary>
		public bool nrpLogToStderr {
			get {
				if (_nrpLogToStderrIsOverridden) {
					return _nrpLogToStderr;
				} else {
					return globalLogToStderr;
				}
			}
			set {
				_nrpLogToStderrIsOverridden = true;
				_nrpLogToStderr = value;
			}
		}
		private bool _nrpLogToStderr = false;
		private bool _nrpLogToStderrIsOverridden = false;

		/// <summary>
		/// To override whether the NRP component performs in-memory logging or not. Default value: false.
		/// </summary>
		public bool nrpLogToMemory {
			get {
				if (_nrpLogToMemoryIsOverridden) {
					return _nrpLogToMemory;
				} else {
					return globalLogToMemory;
				}
			}
			set {
				_nrpLogToMemoryIsOverridden = true;
				_nrpLogToMemory = value;
			}
		}
		private bool _nrpLogToMemory = false;
		private bool _nrpLogToMemoryIsOverridden = false;

		/// <summary>
		/// To override the MediaFlow log level
		/// </summary>
		public LogLevels mfLogLevel {
			get {
				if (_mfLogLevelIsOverridden) {
					return _mfLogLevel;
				} else {
					return globalLogLevel;
				}
			}
			set {
				_mfLogLevelIsOverridden = true;
				_mfLogLevel = value;
			}
		}
		private LogLevels _mfLogLevel = LogLevels.Warn;
		private bool _mfLogLevelIsOverridden = false;

		/// <summary>
		/// To override the MediaFlow log file name
		/// </summary>
		public string mfLogFileName {
			get {
				if (_mfLogFileNameIsOverridden) {
					return _mfLogFileName;
				} else {
					return globalLogFileName;
				}
			}
			set {
				_mfLogFileNameIsOverridden = true;
				_mfLogFileName = value;
			}
		}
		private string _mfLogFileName = "";
		private bool _mfLogFileNameIsOverridden = false;

		/// <summary>
		/// To override whether the MediaFlow component logs to stderr or not. Default value: false.
		/// </summary>
		public bool mfLogToStderr {
			get {
				if (_mfLogToStderrIsOverridden) {
					return _mfLogToStderr;
				} else {
					return globalLogToStderr;
				}
			}
			set {
				_mfLogToStderrIsOverridden = true;
				_mfLogToStderr = value;
			}
		}
		private bool _mfLogToStderr = false;
		private bool _mfLogToStderrIsOverridden = false;

		/// <summary>
		/// To override whether the MediaFlow component performs in-memory logging or not. Default value: false.
		/// </summary>
		public bool mfLogToMemory {
			get {
				if (_mfLogToMemoryIsOverridden) {
					return _mfLogToMemory;
				} else {
					return globalLogToMemory;
				}
			}
			set {
				_mfLogToMemoryIsOverridden = true;
				_mfLogToMemory = value;
			}
		}
		private bool _mfLogToMemory = false;
		private bool _mfLogToMemoryIsOverridden = false;

		/// <summary>
		/// To override the SDK log level
		/// </summary>
		public LogLevels sdkLogLevel {
			get {
				if (_sdkLogLevelIsOverridden) {
					return _sdkLogLevel;
				} else {
					return globalLogLevel;
				}
			}
			set {
				_sdkLogLevelIsOverridden = true;
				_sdkLogLevel = value;
			}
		}
		private LogLevels _sdkLogLevel = LogLevels.Warn;
		private bool _sdkLogLevelIsOverridden = false;

		/// <summary>
		/// To override the SDK log file name
		/// </summary>
		public string sdkLogFileName {
			get {
				if (_sdkLogFileNameIsOverridden) {
					return _sdkLogFileName;
				} else {
					return globalLogFileName;
				}
			}
			set {
				_sdkLogFileNameIsOverridden = true;
				_sdkLogFileName = value;
			}
		}
		private string _sdkLogFileName = "";
		private bool _sdkLogFileNameIsOverridden = false;

		/// <summary>
		/// To override whether the SDK component logs to stderr or not. Default value: false.
		/// </summary>
		public bool sdkLogToStderr {
			get {
				if (_sdkLogToStderrIsOverridden) {
					return _sdkLogToStderr;
				} else {
					return globalLogToStderr;
				}
			}
			set {
				_sdkLogToStderrIsOverridden = true;
				_sdkLogToStderr = value;
			}
		}
		private bool _sdkLogToStderr = false;
		private bool _sdkLogToStderrIsOverridden = false;

		/// <summary>
		/// To override whether the SDK component performs in-memory logging or not. Default value: false.
		/// </summary>
		public bool sdkLogToMemory {
			get {
				if (_sdkLogToMemoryIsOverridden) {
					return _sdkLogToMemory;
				} else {
					return globalLogToMemory;
				}
			}
			set {
				_sdkLogToMemoryIsOverridden = true;
				_sdkLogToMemory = value;
			}
		}
		private bool _sdkLogToMemory = false;
		private bool _sdkLogToMemoryIsOverridden = false;

		/// <summary>
		/// To override SigmaAudio log level
		/// </summary>
		public LogLevels sigmaAudioLogLevel {
			get {
				if (_sigmaAudioLogLevelIsOverridden) {
					return _sigmaAudioLogLevel;
				} else {
					return globalLogLevel;
				}
			}
			set {
				_sigmaAudioLogLevelIsOverridden = true;
				_sigmaAudioLogLevel = value;
			}
		}
		private LogLevels _sigmaAudioLogLevel = LogLevels.Warn;
		private bool _sigmaAudioLogLevelIsOverridden = false;
		/// <summary>
		/// To override SigmaAudio log file name
		/// </summary>
		public string sigmaAudioLogFileName {
			get {
				if (_sigmaAudioLogFileNameIsOverridden) {
					return _sigmaAudioLogFileName;
				} else {
					return globalLogFileName;
				}
			}
			set {
				_sigmaAudioLogFileNameIsOverridden = true;
				String realValue = value;
				_sigmaAudioLogFileName = value;
			}
		}
		private string _sigmaAudioLogFileName = "";
		private bool _sigmaAudioLogFileNameIsOverridden = false;

		/// <summary>
		/// To override whether the SigmaAudio component logs to stderr or not. Default value: false.
		/// </summary>
		public bool sigmaAudioLogToStderr {
			get {
				if (_sigmaAudioLogToStderrIsOverridden) {
					return _sigmaAudioLogToStderr;
				} else {
					return globalLogToStderr;
				}
			}
			set {
				_sigmaAudioLogToStderrIsOverridden = true;
				_sigmaAudioLogToStderr = value;
			}
		}
		private bool _sigmaAudioLogToStderr = false;
		private bool _sigmaAudioLogToStderrIsOverridden = false;

		/// <summary>
		/// To override whether the SigmaAudio component performs in-memory logging or not. Default value: false.
		/// </summary>
		public bool sigmaAudioLogToMemory {
			get {
				if (_sigmaAudioLogToMemoryIsOverridden) {
					return _sigmaAudioLogToMemory;
				} else {
					return globalLogToMemory;
				}
			}
			set {
				_sigmaAudioLogToMemoryIsOverridden = true;
				_sigmaAudioLogToMemory = value;
			}
		}
		private bool _sigmaAudioLogToMemory = false;
		private bool _sigmaAudioLogToMemoryIsOverridden = false;

		internal cvrinterface.InitializeLoggingRequest ToCoreProtobuf() {

			cvri.InitializeLoggingRequest loggingConfig = new cvri.InitializeLoggingRequest() {
				GlobalLogVerbosity = globalLogLevel.ToCoreProtobuf(),
				GlobalLogFolder = globalLogFolder,
				GlobalLogFileName = globalLogFileName,
				GlobalLogToMemory = globalLogToMemory,
				GlobalLogToStderr = globalLogToStderr,
				LogNetwork = logNetwork,
				InteractionRecorderFileName = interactionEventRecorderFileName
			};
			//core overrides
			if (_coreLogLevelIsOverridden) {
				cvri.ComponentSpecificLoggingLevel csll = new cvri.ComponentSpecificLoggingLevel() {
					Component = cvri.LogComponent.TmCore,
					LogVerbosity = _coreLogLevel.ToCoreProtobuf()
				};
				loggingConfig.OverrideLogLevelConfiguration.Add(csll);
			}
			if (_coreLogFileNameIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.TmCore,
					LogFileName = _coreLogFileName
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_coreLogToStderrIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.TmCore,
					LogToStderr = _coreLogToStderr
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_coreLogToMemoryIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.TmCore,
					LogToMemory = _coreLogToMemory
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}

			//NRP overrides
			if (_nrpLogLevelIsOverridden) {
				cvri.ComponentSpecificLoggingLevel csll = new cvri.ComponentSpecificLoggingLevel() {
					Component = cvri.LogComponent.Nrp,
					LogVerbosity = _nrpLogLevel.ToCoreProtobuf()
				};
				loggingConfig.OverrideLogLevelConfiguration.Add(csll);
			}
			if (_nrpLogFileNameIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.Nrp,
					LogFileName = _nrpLogFileName
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_nrpLogToStderrIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.Nrp,
					LogToStderr = _nrpLogToStderr
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_nrpLogToMemoryIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.Nrp,
					LogToMemory = _nrpLogToMemory
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}

			//MF overrides
			if (_mfLogLevelIsOverridden) {
				cvri.ComponentSpecificLoggingLevel csll = new cvri.ComponentSpecificLoggingLevel() {
					Component = cvri.LogComponent.MediaFlow,
					LogVerbosity = _mfLogLevel.ToCoreProtobuf()
				};
				loggingConfig.OverrideLogLevelConfiguration.Add(csll);
			}
			if (_mfLogFileNameIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.MediaFlow,
					LogFileName = _mfLogFileName
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_mfLogToStderrIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.MediaFlow,
					LogToStderr = _mfLogToStderr
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_mfLogToMemoryIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.MediaFlow,
					LogToMemory = _mfLogToMemory
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			
			//SDK overrides
			if (_sdkLogLevelIsOverridden) {
				cvri.ComponentSpecificLoggingLevel csll = new cvri.ComponentSpecificLoggingLevel() {
					Component = cvri.LogComponent.Sdk,
					LogVerbosity = _sdkLogLevel.ToCoreProtobuf()
				};
				loggingConfig.OverrideLogLevelConfiguration.Add(csll);
			}
			if (_sdkLogFileNameIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.Sdk,
					LogFileName = _sdkLogFileName
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_sdkLogToStderrIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.Sdk,
					LogToStderr = _sdkLogToStderr
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_sdkLogToMemoryIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.Sdk,
					LogToMemory = _sdkLogToMemory
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}

			//SigmaAudio overrides
			if (_sigmaAudioLogLevelIsOverridden) {
				cvri.ComponentSpecificLoggingLevel csll = new cvri.ComponentSpecificLoggingLevel() {
					Component = cvri.LogComponent.SigmaAudio,
					LogVerbosity = _sigmaAudioLogLevel.ToCoreProtobuf()
				};
				loggingConfig.OverrideLogLevelConfiguration.Add(csll);
			}
			if (_sigmaAudioLogFileNameIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.SigmaAudio,
					LogFileName = _sigmaAudioLogFileName
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_sigmaAudioLogToStderrIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.SigmaAudio,
					LogToStderr = _sigmaAudioLogToStderr
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}
			if (_sigmaAudioLogToMemoryIsOverridden) {
				cvri.ComponentSpecificLoggingOutput cslo = new cvri.ComponentSpecificLoggingOutput() {
					Component = cvri.LogComponent.SigmaAudio,
					LogToMemory = _sigmaAudioLogToMemory
				};
				loggingConfig.OverrideLoggingOutputConfiguration.Add(cslo);
			}

			return loggingConfig;
		}

		public override String ToString() {
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(String.Format("GlobalLogLevel: {0}, GlobalLogFolder: {1}, GlobalFileName: {2}, GlobalLogToStdErr: {3}, GlobalLogToMemory: {4}, LogNetwork: {5}, InteractionEventRecorderFileName: {6}\n", globalLogLevel.ToString(), globalLogFolder, globalLogFileName, globalLogToStderr, globalLogToMemory, logNetwork, interactionEventRecorderFileName));
			//Core overrides
			if (_coreLogLevelIsOverridden) {
				stringBuilder.Append(String.Format("    coreLogLevel: {0}\n", coreLogLevel));
			}
			if (_coreLogFileNameIsOverridden) {
				stringBuilder.Append(String.Format("    coreLogFileName: {0}\n", coreLogFileName));
			}
			if (_coreLogToStderrIsOverridden) {
				stringBuilder.Append(String.Format("    coreLogToStderr: {0}\n", coreLogToStderr));
			}
			if (_coreLogToMemoryIsOverridden) {
				stringBuilder.Append(String.Format("    coreLogToMemory: {0}\n", coreLogToMemory));
			}
			//NRP overrides
			if (_nrpLogLevelIsOverridden) {
				stringBuilder.Append(String.Format("    nrpLogLevel: {0}\n", nrpLogLevel));
			}
			if (_nrpLogFileNameIsOverridden) {
				stringBuilder.Append(String.Format("    nrpLogFileName: {0}\n", nrpLogFileName));
			}
			if (_nrpLogToStderrIsOverridden) {
				stringBuilder.Append(String.Format("    nrpLogToStderr: {0}\n", nrpLogToStderr));
			}
			if (_nrpLogToMemoryIsOverridden) {
				stringBuilder.Append(String.Format("    nrpLogToMemory: {0}\n", nrpLogToMemory));
			}
			//MF overrides
			if (_mfLogLevelIsOverridden) {
				stringBuilder.Append(String.Format("    mfLogLevel: {0}\n", mfLogLevel));
			}
			if (_mfLogFileNameIsOverridden) {
				stringBuilder.Append(String.Format("    mfLogFileName: {0}\n", mfLogFileName));
			}
			if (_mfLogToStderrIsOverridden) {
				stringBuilder.Append(String.Format("    mfLogToStderr: {0}\n", mfLogToStderr));
			}
			if (_mfLogToMemoryIsOverridden) {
				stringBuilder.Append(String.Format("    mfLogToMemory: {0}\n", mfLogToMemory));
			}
			//SDK overrides
			if (_sdkLogLevelIsOverridden) {
				stringBuilder.Append(String.Format("    sdkLogLevel: {0}\n", sdkLogLevel));
			}
			if (_sdkLogFileNameIsOverridden) {
				stringBuilder.Append(String.Format("    sdkLogFileName: {0}\n", sdkLogFileName));
			}
			if (_sdkLogToStderrIsOverridden) {
				stringBuilder.Append(String.Format("    sdkLogToStderr: {0}\n", sdkLogToStderr));
			}
			if (_sdkLogToMemoryIsOverridden) {
				stringBuilder.Append(String.Format("    sdkLogToMemory: {0}\n", sdkLogToMemory));
			}
			//SigmaAudio overrides
			if (_sigmaAudioLogLevelIsOverridden) {
				stringBuilder.Append(String.Format("    sigmaAudioLogLevel: {0}\n", sigmaAudioLogLevel));
			}
			if (_sigmaAudioLogFileNameIsOverridden) {
				stringBuilder.Append(String.Format("    sigmaAudioLogFileName: {0}\n", sigmaAudioLogFileName));
			}
			if (_sigmaAudioLogToStderrIsOverridden) {
				stringBuilder.Append(String.Format("    sigmaAudioLogToStderr: {0}\n", sigmaAudioLogToStderr));
			}
			if (_sigmaAudioLogToMemoryIsOverridden) {
				stringBuilder.Append(String.Format("    sigmaAudioLogToMemory: {0}\n", sigmaAudioLogToMemory));
			}
			return stringBuilder.ToString();
		}
	}

	/// <summary>
	/// This object contains the configuration of one or more Telemetry targets. See [TelemetryTarget](xref:com.tiledmedia.clearvr.TelemetryTarget) for details.
	/// You would set this on `platformOptions.telemetryConfiguration`
	/// 
	/// 
	/// Example:
	/// <code language="cs"><![CDATA[
	/// // Define telemetry target configuration (in this example for a NewRelic end-point)
	/// TelemetryTargetConfigNewRelic telemetryTargetConfig = new TelemetryTargetConfigNewRelic("YOUR_ACCOUNT_ID", "YOUR_LICENSE", "YOUR_END_POINT");
	/// // Configure your telemetry target (you can have multiple)
	/// TelemetryTarget telemetryTarget = new TelemetryTarget(TelemetryIPSignallingTypes.TelemetryIpSignallingMasked, new List<TelemetryTargetConfigBase>() { telemetryTargetConfig });
	/// // Set the telemetry configuration.
	/// platformOptions.telemetryConfiguration = new TelemetryConfiguration(new List<TelemetryTarget>() { telemetryTarget });
	/// ]]></code>
	/// </summary>

	public class TelemetryConfiguration {
		/// <summary>
		/// Set to true if you want to disable sending telemetry data to the Tiledmedia backend.
		/// Default value: false
		/// </summary>
		public bool disableTiledmediaTelemetry{ get; set; } = false;
		/// <summary>
		/// Set to true if you want to disable sending telemetry data to the Tiledmedia New Relic backend.
		/// Default value: false
		/// </summary>
		// Rarely used, so only exposed via a setter
		public bool disableTiledmediaTelemetryToNewRelic{ get; set; } = false;
		/// <summary>
		/// Configure your Telemetry target(s). Can be null.
		/// </summary>
    	public List<TelemetryTarget> telemetryTargets { get; set; }
		
		/// <summary>
		/// Configure your telemetry targets.
		/// </summary>
		/// <param name="telemetryTargets">Your telemetry target(s). Can be null.</param>
		/// <param name="disableTiledmediaTelemetry">Set to true if you want to disable sending telemetry data to the Tiledmedia backend. Default value: false </param>
		public TelemetryConfiguration(List<TelemetryTarget> telemetryTargets, bool disableTiledmediaTelemetry = false) {
			this.disableTiledmediaTelemetry = disableTiledmediaTelemetry;
			this.telemetryTargets = telemetryTargets;
		}

		internal cvri.TelemetryConfiguration ToCoreProtobuf() {
			cvri.TelemetryConfiguration coreProtobuf = new cvri.TelemetryConfiguration() {
				DisableTiledmediaTelemetry = disableTiledmediaTelemetry,
				DisableTiledmediaTelemetryToNewRelic = disableTiledmediaTelemetryToNewRelic
			};
			if(telemetryTargets != null) {
				foreach(TelemetryTarget target in telemetryTargets) {
					coreProtobuf.TelemetryTargets.Add(target.ToCoreProtobuf());
				}
			}
			return coreProtobuf;
		}

		public override String ToString() {
			return String.Format("Disable Tiledmedia telemetry: {0}, disable Tiledmedia telemetry to New Relic: {1}, Telemetry targets: {2}", disableTiledmediaTelemetry, disableTiledmediaTelemetryToNewRelic, String.Join(", ", telemetryTargets));
		}
	}

	/// <summary>
	/// Configure your Telemetry Target(s) of a specific [type](xref:com.tiledmedia.clearvr.TelemetryTargetTypes).
	/// </summary>
	public class TelemetryTarget {
		public TelemetryIPSignallingTypes telemetryIPSignallingType { get; set; } = TelemetryIPSignallingTypes.TelemetryIpSignallingMasked;
		public List<TelemetryTargetConfigBase> telemetryTargetConfigurations { get; set; }
		/// <summary>
		/// Configure your Telemetry Target. You can have multiple Telemtry Targets Configurations, but each configuration must be of the same time (e.g. you can specify multiple New Relic targets).
		/// </summary>
		/// <param name="telemetryIPSignallingType">Whether IP adresses should signalled unmasked, masked, or not at all.</param>
		/// <param name="telemetryTargetConfigBase">A list of configurations of the target. Note that all configurations must be of the same type. If you want to use different targets at the same time, you need to set multiple Targets in the [TelemetryConfiguration](xref:com.tiledmedia.clearvr.TelemetryConfiguration).</param>
		// Interal note: the TargetTypoe will be inferred from the TargetConfiguration.
		public TelemetryTarget(TelemetryIPSignallingTypes telemetryIPSignallingType, List<TelemetryTargetConfigBase> telemetryTargetConfigBase) {
			this.telemetryIPSignallingType = telemetryIPSignallingType;
			this.telemetryTargetConfigurations = telemetryTargetConfigBase;
		}

		internal cvri.TelemetryTarget ToCoreProtobuf() {
			cvri.TelemetryTarget coreProtobuf = new cvri.TelemetryTarget() {
				IPSignallingType = telemetryIPSignallingType.ToCoreProtobuf()
			};
			if(telemetryTargetConfigurations != null) {
				Type lastTargetConfigBaseType = null;
				foreach(TelemetryTargetConfigBase configBase in telemetryTargetConfigurations) {
					// This needs to be 
					if(lastTargetConfigBaseType != null && lastTargetConfigBaseType != configBase.GetType()) {
						UnityEngine.Debug.LogWarning(String.Format("[ClearVR] Not all Telemetry Targets are of identical type in. Got type '{0}', expected type '{1}'. Skipping: {2}", lastTargetConfigBaseType, configBase.GetType(), configBase.ToString()));
						continue;
					}
					lastTargetConfigBaseType = configBase.GetType();
					coreProtobuf.Type = configBase.GetTelemetryTargetType().ToCoreProtobuf();
					if(configBase is TelemetryTargetConfigNewRelic) {
						coreProtobuf.NewRelicConfig = (cvri.TelemetryTargetConfigNewRelic)configBase.ToCoreProtobuf();
					}
				}
			}
			return coreProtobuf;
		}

		public override String ToString() {
			return String.Format("Telemetry IP signalling: {0}, target config: {1}", telemetryIPSignallingType, String.Join(", ", telemetryTargetConfigurations));
		}
	}

	/// <summary>
	/// Abstract base class for TelemetryTargetConfigurations. 
	/// All Telemtry Targets shoudl extend this base class.
	/// </summary>
	public abstract class TelemetryTargetConfigBase {
		internal abstract TelemetryTargetTypes GetTelemetryTargetType();
		internal abstract System.Object ToCoreProtobuf();

		public override String ToString() {
			return String.Format("Telemetry target config type: {0}", GetTelemetryTargetType());
		}
	}

	/// <summary>
	/// The Telemetry Target configuration for New Relic metrics aggregation. See [NewRelic.com](http://newrelic.com/) for details.
	/// </summary>
	public class TelemetryTargetConfigNewRelic : TelemetryTargetConfigBase {
		/// <summary>
		/// Example: "3804000"
		/// </summary>
		public string accountID { get; set; }
		/// <summary>
		/// Example: "eu01XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
		/// </summary>
		public string license { get; set; }
		/// <summary>
		/// <code language="cs"><![CDATA[
		/// Example: https://insights-collector.eu01.nr-data.net/v1/accounts/<ACCOUNT_ID>/events
		/// ]]></code>
		/// </summary>
		public string url { get; set; }
		
		/// <summary>
		/// Configure the NewRelic telemetry target parameters
		/// </summary>
		/// <param name="accountID">Your NewRelic account id.</param>
		/// <param name="license">Your NewRelic license.</param>
		/// <param name="url">Your NewRelic url end-point.</param>
		public TelemetryTargetConfigNewRelic(String accountID, String license, String url) {
			this.accountID = accountID;
			this.license = license;
			this.url = url;
		}

		internal override TelemetryTargetTypes GetTelemetryTargetType() {
			return TelemetryTargetTypes.TelemetryTargetNewRelic;
		}

		internal override System.Object ToCoreProtobuf() {
			cvri.TelemetryTargetConfigNewRelic coreProtobuf = new cvri.TelemetryTargetConfigNewRelic() {
				AccountID = accountID,
				License = license,
				URL = url
			};
			return coreProtobuf;
		}

		public override String ToString() {
			return String.Format("{0}, accountID: {1}, license: {2}, url: {3}", base.ToString(), com.tiledmedia.clearvr.Utils.MaskString(accountID), com.tiledmedia.clearvr.Utils.MaskString(license), url);
		}
	}

	/// <summary>
	/// Used to send custom key/value pair data to the configured Telemetry Targets. Refer to [TelemetryUpdateTargetCustomData](xref:com.tiledmedia.clearvr.TelemetryUpdateTargetCustomData) for details.
	/// </summary>
	public class TelemetryUpdateCustomData {
		/// <summary>
		/// The custom data that one wants to send.
		/// </summary>
		public List<TelemetryUpdateTargetCustomData> telemetryUpdateTargetCustomData { get; set; }
		/// <summary>
		/// Default constructor. Specify the custom data one wants to send to the telemetry target(s) of choice.
		/// </summary>
		/// <param name="targetSpecificCustomMetadata"></param>
		public TelemetryUpdateCustomData(List<TelemetryUpdateTargetCustomData> targetSpecificCustomMetadata) {
			this.telemetryUpdateTargetCustomData = targetSpecificCustomMetadata;
		}

		internal cvri.TelemetryUpdateCustomData ToCoreProtobuf() {
			cvri.TelemetryUpdateCustomData coreProtobuf = new cvri.TelemetryUpdateCustomData() { };
			if(telemetryUpdateTargetCustomData != null) {
				foreach(TelemetryUpdateTargetCustomData targetCustomMetadata in telemetryUpdateTargetCustomData) {
					if(targetCustomMetadata != null) {
						coreProtobuf.TargetSpecificCustomMetadata.Add(targetCustomMetadata.ToCoreProtobuf());
					}
				}
			}
			return coreProtobuf;
		}
		
		public override String ToString() {
			return String.Format("Telemetry update target custom data: {0}", String.Join(", ", telemetryUpdateTargetCustomData));
		}
	}

	/// <summary>
	/// Data fields to set send custom data to the telemtry target of choice.
	/// </summary>
	public class TelemetryUpdateTargetCustomData {
		/// <summary>
		/// The index of the telemetry target. The index matches the order in which you defined the TelemetryTargets in [TelemetryConfiguration.telemetryTargets](xref:com.tiledmedia.clearvr.TelemetryConfiguration).
		/// </summary>
		/// <value></value>
		public Int32 telemetryTargetIndex { get; set; }
		/// <summary>
		/// A set of Key/Value pairs. Key and Value are not allowed to be null. If either is set to null, the pair will be skipped.
		/// </summary>
		public List<KeyValuePair<String,String>> customDatas { get; set; }
		/// <summary>
		/// Construct the TelemetryUpdateTargetCustomData object.
		/// </summary>
		/// <param name="telemetryTargetIndex">The index of the telemetry target. The index matches the order in which you defined the TelemetryTargets in [TelemetryConfiguration.telemetryTargets](xref:com.tiledmedia.clearvr.TelemetryConfiguration).</param>
		/// <param name="customDatas">This list of Key/value pairs can not contain nulls.</param>
		public TelemetryUpdateTargetCustomData(Int32 telemetryTargetIndex, List<KeyValuePair<String,String>> customDatas) {
			this.telemetryTargetIndex = telemetryTargetIndex;
			this.customDatas = customDatas;
		}

		internal cvri.TelemetryUpdateTargetCustomData ToCoreProtobuf() {
			cvri.TelemetryUpdateTargetCustomData coreProtobuf = new cvri.TelemetryUpdateTargetCustomData() {
				TelemetryTargetIdx = telemetryTargetIndex
			};
			if(customDatas != null ) {
				foreach(KeyValuePair<String, String> customData in customDatas) {
					if(customData.Key == null) {
						UnityEngine.Debug.LogWarning("[ClearVR] One of your TelemetryUpdateTargetCustomData key/value pairs has a 'null' key, This key/value pair will be skipped.");
						continue;
					}
					coreProtobuf.CustomDataKey.Add(customData.Key);
					if(customData.Value == null) {
						UnityEngine.Debug.LogWarning("[ClearVR] One of your TelemetryUpdateTargetCustomData key/value pairs has a 'null' value, This key/value pair will be skipped.");
						continue;
					}
					coreProtobuf.CustomDataKey.Add(customData.Value);
				}
			}
			return coreProtobuf;
		}
		
		public override String ToString() {
			return String.Format("Telemetry target index: {0}, custom datas: {1}", telemetryTargetIndex, String.Join(";", customDatas));
		}
	}	
}
