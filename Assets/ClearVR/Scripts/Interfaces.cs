using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.tiledmedia.clearvr {
    /// <summary>
    /// This internal interface is not exposed as a public API, but is rather used to enforce a common API across platform specific implementation of the MediaPlayer class. 
    /// Do not use this interface.
    /// </summary>
    internal interface InternalInterface {
        /// <summary>
        /// Prepare core libraries.
        /// </summary>
        void PrepareCore();
        /// <summary>
        /// Called every Update() cycle
        /// </summary>
		void Update();
        /// <summary>
        /// Called every LateUpdate() cycle
        /// </summary>
        void LateUpdate();
        /// <summary>
        /// Called upon destruction of the MediaPlayer object.
        /// </summary>
        void Destroy();
        /// <summary>
        /// Called after state stopped is reached.
        /// </summary>
        void CleanUpAfterStopped();
        /* Mandatory callbacks */
		void CbClearVRCoreWrapperMessage(ClearVRMessage argClearVRMessage);
		void CbClearVRCoreWrapperRequestCompleted(ClearVRAsyncRequestResponse argClearVRAsyncRequestResponse, ClearVRMessage argClearVRMessage);
        void SendSensorInfo();
        /// <summary>
        /// Helper method that is used by ClearVRPlayer to determine whether we should call Pause() or not. Fix for #2225
        /// </summary>
        /// <returns>True if calling Pause is allowed, false otherwise.</returns>
        bool GetIsPauseAllowed();

        List<ClearVRAsyncRequest> clearVRAsyncRequests {get;}
        void ScheduleClearVRAsyncRequest(ClearVRAsyncRequest clearVRAsyncRequest, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        ClearVRAsyncRequest _StartPlayout();
        ClearVRAsyncRequest _PrepareCore();
        ClearVRAsyncRequest _Pause();
        ClearVRAsyncRequest _Unpause(TimingParameters argTimingParameters);
        ClearVRAsyncRequest _CallCore(byte[] argMessage);
        ClearVRAsyncRequest _PopulateMediaInfo(PopulateMediaInfoParameters argPopulateMediaInfoParameters); // Also used for the legacy ParseMediaInfo API
        ClearVRAsyncRequest _Seek(SeekParameters argSeekParameters);
        ClearVRAsyncRequest _SetStereoMode(bool argStereo);
        ClearVRAsyncRequest _SwitchContent(SwitchContentParameters argSwitchContentParameters);
        ClearVRAsyncRequest _PrepareContentForPlayout(PrepareContentParameters argPrepareContentParameters);
        ClearVRAsyncRequest _Stop(bool argForceLastMinuteCleanUpAfterPanic);
        TimingReport _GetTimingReport(TimingTypes argTimingType);
        String _CallCoreSync(String argMessage);
        void StopInternal(); // This is a short-hand API to quickly stop the stack. For internal use only
        bool _InitializePlatformBindings();
        bool _GetIsPlayerShuttingDown();
        /// <summary>
        /// Get Content Parameter API.
        /// 
        /// Since v7.4.2
        /// 
        /// </summary>
        /// <param name="argContentID">The contentID of .</param>
        /// <param name="argKey">The key to query.</param>
        /// <returns>The value of the queried key, or an empty string if it could not be queried.</returns>
        String GetClearVRCoreContentParameter(int argContentID, String argKey);
        /// <summary>
        /// Some parameters can be queried per contentID. This is an advanced API that should not be used.
        /// 
        /// Since v7.4.2
        /// 
        /// </summary>
        /// <param name="argContentID">The contentID telling .</param>
        /// <param name="argKey">The key to query.</param>
        /// <param name="argIndex">The index in the array to query.</param>
        /// <returns>The value of the queried key, or an empty string if it could not be queried.</returns>
        String GetClearVRCoreContentArrayParameter(int argContentID, String argKey, int argIndex);
        /// <summary>
        /// Get the current AudioTrackAndPLaybackParameters
        /// </summary>
        AudioTrackAndPlaybackParameters GetAudioTrackAndPlaybackParameters();
        /// <summary>
        /// Retrieve current mute state of the underlying (MediaFlow) library.
        /// The mute state is defined as follows:
        /// A value between [1, 2]:
        ///    - the gain is NOT muted.
        ///    - The gainPriorToMuting is value - 1
        /// A value between [-2. -1]
        ///    - the gain is muted
        ///    - the gainPriorToMuting is value + 2
        /// 0 should be interpreted as "use the platform-specific defaults"/ "the platform-specific defaults are set".
        /// </summary>
        /// <returns>The mute state as a float.</returns>
        float GetMuteState();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        void UpdateScreenOrientation(ScreenOrientation argScreenOrientation);
#endif
        bool GetDidReachPreparingContentForPlayoutStateButNotInStoppingOrStoppedState();
    }
    /// <summary>
    /// The MediaPlayer interface specifies interactions with the MediaPlayer object.
    /// </summary>
	public interface MediaPlayerInterface {
        /// <summary>
        /// The ClearVRCore exposes a large number of parameters which can be queried when the player is in a certain state. Please refer to the ClearVRCore documentation for an exhaustive list of all parameters.
        /// Notes.
        /// 1. Some common day-to-day-use parameters are exposed through convenience methods.
        /// 2. Note that many parameters can only be queried at a specific moment in time.
        /// 3. Some parameters can only be queried with an array index. See GetClearVRCoreArrayParameter() and the ClearVRCore documentation for details.
        /// </summary>
        /// <param name="argKey">The key to query.</param>
        /// <returns>The value of the queried key, or an empty string if it could not be queried.</returns>
        String GetClearVRCoreParameter(String argKey);
        /// <summary>
        /// Since v4.3
        /// 
        /// The ClearVRCore exposes a large number of parameters which can be queried when the player is in a certain state. Please refer to the ClearVRCore documentation for an exhaustive list of all parameters.
        /// This method can be used to query parameters that take a mandatory array index.
        /// 
        /// Notes.
        /// 1. Some common day-to-day-use parameters are exposed through convenience methods.
        /// 2. Note that many parameters can only be queried at a specific moment in time.
        /// 3. Most parameters do not take an array index. See GetClearVRCoreParameter() and the ClearVRCore documentation for details.
        /// 4. One needs to make sure that your query is properly bounded.
        /// </summary>
        /// <param name="argKey">The key to query.</param>
        /// <param name="argIndex">The index in the array to query.</param>
        /// <returns>The value of the queried key, or an empty string if it could not be queried.</returns>
        String GetClearVRCoreArrayParameter(String argKey, int argIndex);
        /// <summary>
        /// Set a specific ClearVRCore parameter. See also GetClearVRCoreParameter() for more details.
        /// </summary>
        /// <param name="argKey">The key to set.</param>
        /// <param name="argValue">The value to set the key to.</param>
        /// <returns></returns>
        bool SetClearVRCoreParameter(String argKey, String argValue);
        /// <summary>
        /// Populate media info on a specific ContentItem. Note that populating media info does not load the content item and does not prepare it for immediate playout.
        /// For that, one needs to Initialize() or SwitchContent() instead.
        /// This is an advanced API and should typically not be used.
        /// </summary>
        /// <param name="argPopulateMediaInfoParameters">The parameters required for this API</param>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">The callback to trigger upon completion.</param>
        /// <param name="argOptionalArguments">Any optional argument to pass along in the provided callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use PopulateMediaInfo(PopulateMediaInfoParameters, Action, Action, Params) instead.", false)]
        void PopulateMediaInfo(PopulateMediaInfoParameters argPopulateMediaInfoParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Populate media info on a specific ContentItem. Note that populating media info does not load the content item and does not prepare it for immediate playout.
        /// For that, one needs to Initialize() or SwitchContent() instead.
        /// This is an advanced API and should typically not be used.
        /// </summary>
        /// <param name="populateMediaInfoParameters">The parameters required for this API</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument to pass along in the provided callback.</param>
        void PopulateMediaInfo(PopulateMediaInfoParameters populateMediaInfoParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// Prepares a ContentItem for playout. This is an asynchronous call and one must wait for the StateChangedContentPreparedForPlayout callback to check whether it was successful or not.
        /// Callback event: StateChangedContentPreparedForPlayout
        /// </summary>
        /// <param name="argPrepareContentParameters">The parameters used when preparing the content for playout.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31 Please use PrepareContentForPlayout(PrepareContentParameters, Action, Action, Params) instead.", false)]
        void PrepareContentForPlayout(PrepareContentParameters argPrepareContentParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Prepares a ContentItem for playout. This is an asynchronous call and one must wait for the StateChangedContentPreparedForPlayout callback to check whether it was successful or not.
        /// Callback event: StateChangedContentPreparedForPlayout
        /// </summary>
        /// <param name="prepareContentParameters">The parameters used when preparing the content for playout.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument to pass along in the provided callback.</param>
        void PrepareContentForPlayout(PrepareContentParameters prepareContentParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// This API is used to switch between monoscopic and stereoscopic rendering (if the content and player allows for it). Note that this only changes how the content is *rendered*, NOT how it is retrieved from the network. If you want to switch between monoscopic or stereoscopic content retrieval, you should use the SetStereoMode() API instead.
        /// It is especially suited for temporarily disabling stereoscopic rendering when you show an in-player menu (to prevent depth-fighting from happening) as this API is instantaneous while the SetStereoMode() API will take a bit of time to respond, reducing user experience.
        /// Callback event: RenderModeChanged
        /// </summary>
        /// <param name="argNewRenderMode">The new render mode to switch to.</param>
        [Obsolete("This API has been deprecated in v9.0 and can no longer be used. Refer to ClearVRDisplayObjectControllerBase.renderMode instead to set the RenderMode per DisplayObject.", true)]
        void SetRenderMode(RenderModes argNewRenderMode);
        /// <summary>
        /// Gets the currently active RenderMode.
        /// </summary>
        /// <returns>The active RenderMode.</returns>
        [Obsolete("This API has been deprecated in v9.0 and can no longer be used. Refer to ClearVRDisplayObjectControllerBase.renderMode instead to get the RenderMode per DisplayObject.", true)]
        RenderModes GetRenderMode();
        /// <summary>
        /// Returns the currently active platform options. Note that you can *only* set these prior to initialization. Changing any of its parameters after initialization results in undefined baheviour and must be avoided.
        /// </summary>
        /// <returns>The currently active PlatformOptions. Cast to the platform-specific PlatformOptions to access any platform-specific fields.</returns>
        PlatformOptionsBase GetPlatformOptions();
        /// <summary>
        /// The SetStereoMode() API can be used to switch between monoscopic and stereoscopic playback if the content supports it. Note that this actually toggles the retrieval of the right-eye tiles, 
        /// so enabling it might take a bit of time and might result in buffering depending on network conditions. If one would only temporarily switch between monoscopic and stereoscopic rendering (e.g. because an in-player menu pops-up and you want to make sure there is no depth-fighting between UI and video), one is advised to use the SetRenderMode() API instead as that only changes how the video rendered, not how it is retrieved from the network.
        /// Callback event: StereoModeChanged. Optionally, one will receive the RenderModeChanged event if a switch in rendering was required (e.g. when you went from stereoscopic to monoscopic).        
        /// </summary>
        /// <param name="argStereo">true if you want to switch to stereoscopic rendering, false if monoscopic rendering is requested.</param>
        [Obsolete("This API has been deprecated and has been removed. Please use SetStereoMode(bool, Action, Action, Params) instead.", true)]
        ClearVRAsyncRequest SetStereoMode(bool argStereo);
        /// <summary>
        /// The SetStereoMode() API can be used to switch between monoscopic and stereoscopic playback if the content supports it. Note that this actually toggles the retrieval of the right-eye tiles, 
        /// so enabling it might take a bit of time and might result in buffering depending on network conditions. If one would only temporarily switch between monoscopic and stereoscopic rendering (e.g. because an in-player menu pops-up and you want to make sure there is no depth-fighting between UI and video), one is advised to use the SetRenderMode() API instead as that only changes how the video rendered, not how it is retrieved from the network.
        /// Callback event: StereoModeChanged. Optionally, one will receive the RenderModeChanged event if a switch in rendering was required (e.g. when you went from stereoscopic to monoscopic).        
        /// </summary>
        /// <param name="argStereo">true if you want to switch to stereoscopic rendering, false if monoscopic rendering is requested.</param>
        
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use SetStereoMode(bool, Action, Action, Params) instead.", false)]
        void SetStereoMode(bool argStereo, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// The SetStereoMode() API can be used to switch between monoscopic and stereoscopic playback if the content supports it. Note that this actually toggles the retrieval of the right-eye tiles, 
        /// so enabling it might take a bit of time and might result in buffering depending on network conditions. If one would only temporarily switch between monoscopic and stereoscopic rendering (e.g. because an in-player menu pops-up and you want to make sure there is no depth-fighting between UI and video), one is advised to use the SetRenderMode() API instead as that only changes how the video rendered, not how it is retrieved from the network.
        /// Callback event: StereoModeChanged. Optionally, one will receive the RenderModeChanged event if a switch in rendering was required (e.g. when you went from stereoscopic to monoscopic).        
        /// > [!NOTE]
        /// > This is considered an advanced API and should be used with care. Also, it only applies to ClearVR content playback.
        /// </summary>
        /// <param name="stereo">true if you want to switch to stereoscopic rendering, false if monoscopic rendering is requested.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument to pass along in the provided callback.</param>
        void SetStereoMode(bool stereo, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// The PrewarmCache API allows one to preload initialization data for a specific clip prior to actually using it.
        /// Note that the cache is linked to the ClearVRPlayer instance. If you destroy the ClearVRPlayer, the cache will be flushed as well.
        /// This is typically used together with the SwitchContent() API.
        /// 
        /// As each cache warmup will require up to 1 or 2 megabyte of data (varying per content item), please be considerate when to actually call this API as
        /// downloading extra data might interfere with regular playback on limited internet connections. For example, paused state might be a good
        /// moment to prewarm the cache on an item or two.
        /// 
        /// Warning: please do NOT hammer this API (e.g. by looping over your entire content list as fast as possible).
        /// 
        /// Callback event: PrewarmCacheCompleted
        /// </summary>
        /// <param name="argPrewarmCacheParameter">The parameters that should be passed along to prewarm the cache on a specific ContentItem.</param>
        [Obsolete("Cache prewarming has been removed from the ClearVR SDK. Please remove any reference to it from your code.", true)] // Marked as no longer usable on 2020-12-01
        void PrewarmCache(PrewarmCacheParameters argPrewarmCacheParameter, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /* Determine current player state */
        /// <summary>
        /// Advanced API to determine whether media info was parsed or not. 
        /// </summary>
        /// <returns>True if media info was parsed, false otherwise.</returns>
        [Obsolete("This API has been removed from the ClearVR SDK. Please remove any reference to it from your code. mediaInfo.GetCanPerformanceMetricesBeQueried() *might* be a suitable replacement.", true)]
        bool GetIsMediaInfoParsed();
        /// <summary>
        /// Whether the media player was initialized or not. Note that "initialized" does not neccesarily mean that it is already playing content.
        /// </summary>
        /// <returns>True if the underlying media player is at least initialized, false otherwise.</returns>
        bool GetIsInitialized();
        /// <summary>
        /// Whether the player is busy playing a content item.
        /// </summary>
        /// <returns>True if busy, false otherwise.</returns>
        bool GetIsPlayerBusy();
        /// <summary>
        /// Convenience method to check whether the player is in playing state.
        /// </summary>
        /// <returns>True if in playing state, false if in any other state.</returns>
        bool GetIsInPlayingState();
        /// <summary>
        /// Convenience method to check whether the player is in pausing/paused state.
        /// </summary>
        /// <returns>True if in pausing or paused state, false otherwise.</returns>
        bool GetIsInPausingOrPausedState();

        /// <summary>
        /// Convenience method to check whether the player is in paused state.
        /// </summary>
        /// <returns>True if in paused state, false otherwise.</returns>
        bool GetIsInPausedState();
        /// <summary>
        /// Since v4.1.2
        /// Enable or disable viewport tracking. This is an advanced API that allows you to disable viewport tracking, effectively fixating the high quality viewport regardless of camera orientation.
        /// By default, viewport tracking is enabled.
        /// 
        /// Notes.
        /// 1. This is an advanced API, typicaly only used in exceptional cases.
        /// </summary>
        /// <param name="argIsEnabledOrDisabled">True to enable tracking, false otherwise.</param>
        void EnableOrDisableViewportTracking(bool argIsEnabledOrDisabled);
        /// <summary>
        /// Convenience method to check whether the player is in Finished state. The player will only reach Finished state in case it reached the end of the clip AND content looping is disabled.
        /// Notes.
        /// 1. When the player reaches this state, the viewport can no longer be updated and you cannot switch to Pause state. You can call Seek() though and playback will resume immediately if you would do so.
        ///
        /// Since: v4.1.2
        /// </summary>
        /// <returns>True if in Finished state, false otherwise.</returns>
        bool GetIsInFinishedState();
        /// <summary>
        /// Convenience method to check whether the player is in Stopped state. When in Stopped state, the player can no longer be used and should be destroyed.
        /// Notes.
        /// 1. Before the Stopped state, the mediaPlayer will first transition through the Stopping state. This is a transient state and one MUST wait for it to complete before destroying the mediaPlayer. Failing to do so will result in unexpected behaviour and resources might leak.
        ///
        /// Since: v4.1.2
        /// </summary>
        /// <returns>True if in Stopped state, false otherwise.</returns>
        bool GetIsInStoppedState();
        /// <summary>
        /// Returns the unique device app id as a string. The device app is defined as a MD5-hashed combination of a unique user id and your application's id.
        /// It will return an empty string if queried in an invalid state, or if the device app id could not be generated.
        /// Notes.
        /// 1. One is recommended to query the device app id when one receives the ClearVREventTypes.StateChangedCorePrepared event. 
        /// </summary>
        /// <returns>The unique id as a string or an empty string if unavailable.</returns>
        String GetDeviceAppId();

        /// <summary>
        /// GetDefaultViewportPose return the position and the orientation that the default camera should have. For omnidirectional content this pose is at the origin with no rotation. For planar content this pose should be at the totally unzoomed position with no rotation.
        /// </summary>
		Pose GetDefaultViewportPose();

        /// <summary>
        /// GetRecommendedZoomRange return the recommanded zoom range. min correspond to the maximal zoom, closest to the display object, and max correspond to the minimal zoom.
        /// > [!NOTE]
        /// > This API has been deprecated in v9.x and can no longer be used. Refer to the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) instead.
        /// </summary>
        void GetRecommendedZoomRange(out float min, out float max);

        /// <summary>
        /// ResetViewportAndDisplayObjectToDefaultPoses move the display object and the camera to it default pose based on the current content type.
        /// > [!NOTE]
        /// > This API has been deprecated in v9.x and can no longer be used. Refer to the [LayoutManager](xref:com.tiledmedia.clearvr.ClearVRLayoutManager) instead.
        /// </summary>
        [Obsolete("This API has been deprecated and can no longer be used. Please upgrade to using the LayoutManager instead.", true)]
        void ResetViewportAndDisplayObjectToDefaultPoses();
        /// <summary>
        /// Convenience method which helps check whether one can query performance metrics or not.
        /// </summary>
        /// <returns>True if metrics can be queried, false otherwise.</returns>
        bool GetCanPerformanceMetricesBeQueried();
        /// <summary>
        /// This API allows you to update the user agent included in every HTTP request. One is strongly discouraged to setting a user agent in the first place because of network overhead.
        /// The user agent is a global setting, impacting all HTTP requests from that point onward. For example, if one would configure a user agent "ABC" on the `ContentSupportedTesterParameters`, 
        /// but would call this API to change it, than the user agent might change while the content supported tester is running.
        /// To set a user agent for the TestIsContentSupported API, refer to [ContentSupportedTesterParameters](xref:com.tiledmedia.clearvr.ContentSupportedTesterParameters).
        /// To set a user agent for regular playback, refer to [PlatformOptionsbase.overrideUserAgent](xref:com.tiledmedia.clearvr.PlatformOptionsBase.overrideUserAgent).
        /// Since v7.3.2
        /// </summary>
        /// <param name="argNewUserAgent">The new user agent to include in each HTTP request.</param>
        void UpdateOverrideUserAgent(String argNewUserAgent);

        /// <summary>
        /// Since v9.x
        /// This API is used to change the feed to display object mapping within a given content.
        /// The provided LayoutParameters will overwrite the existing LayoutParameters on the ClearVRLayoutManager, or will be added in case they are not yet present.
        /// </summary>
        /// <param name="argLayoutParameters">Specifies required parameters to change the feeds layout.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void SetLayout(LayoutParameters argLayoutParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] argOptionalArguments);
        /// <summary>
        /// Since v9.x
        /// Convenience API that allows one to set the [RenderMode](xref:com.tiledmedia.clearvr.RenderModes) on all *active* DisplayObjects at once.
        /// </summary>
        /// <param name="argNewRenderMode">The RenderMode to set.</param>
        void SetRenderModeOnAllDisplayObjects(RenderModes argNewRenderMode);
        /// <summary>
        /// Since v9.x
        /// Convenience API that allows one to set the [RenderMode](xref:com.tiledmedia.clearvr.RenderModes) on all *active* DisplayObjects at once, under the condition that they are in the RenderMode as specified by argRequiredCurrentRenderMode.
        /// </summary>
        /// <param name="argNewRenderMode">The RenderMode to set.</param>
        /// <param name="argRequiredCurrentRenderMode">The RenderMode will only be changed on DisplayObjects that are in this RenderMode.</param>
        void SetRenderModeOnAllDisplayObjectsConditionally(RenderModes argNewRenderMode, RenderModes argRequiredCurrentRenderMode);
        /// <summary>
        /// Since v9.2
        /// Allows you send custom key/value pairs to the Telemetry target(s) of choice. Note that the Telemetry target(s) must have been configured a-priori on [platformOptions.telemetryConfiguration](xref:com.tiledmedia.clearvr.PlatformOptionsBase.telemetryConfiguration) or on the (rarely used and advanced API) [ContentSupportedTesterParameters](com.tiledmedia.clearvr.ContentSupportedTesterParameters).
        /// Example:
        /// <code language="cs"><![CDATA[
        /// clearVRPlayer.mediaPlayer.TelemetryUpdateCustomData(new TelemetryUpdateCustomData(new List<TelemetryUpdateTargetCustomData>() {
        ///     new TelemetryUpdateTargetCustomData(0, new List<KeyValuePair<string, string>>() {
        ///         new KeyValuePair<string, string>("Hello", "This is a test")
        ///     })}),
        ///         onSuccess: (clearVREvent, clearVRPlayer) => UnityEngine.Debug.Log("Telemetry custom data update: success"),
        ///         onFailure: (clearVREvent, clearVRPlayer) => UnityEngine.Debug.Log(String.Format("Telemetry custom data update: failed. Details: {0}", clearVREvent))
        ///     );
        /// ]]></code>
        /// </summary>
        /// <param name="TelemetryUpdateCustomData">The updated key/value pairs to send.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void TelemetryUpdateCustomData(TelemetryUpdateCustomData telemetryUpdateCustomData, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
	}

    /// <summary>
    /// The controller interface is used to control media playback. APIs like Pause(), Unpause() and Seek() are all part of this interface.
    /// </summary>
    public interface MediaControllerInterface {
        /// <summary>
        /// After initialization and content preparation, use this method to actually commence playback. Typically, playback should start more or less immediately after calling this method as video is supposed to be cached by then.
        /// > [!WARNING]
        /// > During the lifecycle of a `ClearVRPlayer` object one can only call this method once. To unpause playback, use Unpause() instead.
        /// Callback event: `StateChangedPlaying`
        /// </summary>
        void StartPlayout(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        //TODO: deprecation date 2022-12-31
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use StartPlayout(Action, Action, Params) instead.", false)]
        void StartPlayout(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Pause playback.
        /// > [!NOTE]
        /// > Due to the nature of spatio-temporal video streaming, pausing is only possible on a GOP boundary and might thus take a short while.
        /// Callback event: StateChangedPausing --> StateChangedPaused
        /// </summary>
        void Pause(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use Pause(Action, Action, Params) instead.", false)]
        void Pause(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Since v9.1
        /// Unpause/resume playback. This API allows you to specify where to resume playback by setting the argTimingParameters argument. 
        /// </summary>
        /// <param name="argTimingParameters">Set to null for default behaviour: VOD = resume from last played position, LIVE = resume from last played position unless that fell out of the live window. In the latter case, playback will resume from the live edge.</param>
        /// <param name="onSuccess">Callback triggered when the request was successfully serviced.</param>
        /// <param name="onFailure">Callback triggered when the request failed to complete.</param>
        /// <param name="optionalArguments">Any optional argument you would like to receive in the onSuccess and onFailure callback.</param>
        void Unpause(TimingParameters argTimingParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// Unpause/resume playback.
        /// Callback event: StateChangedPlaying
        /// </summary>
        void Unpause(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use Unpause(Action, Action, Params) instead.", false)]
        void Unpause(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Convenience method to easily toggle between pause and unpause. See Pause() and Unpause() for details.
        /// </summary>
        void TogglePause(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Stop the player. One MUST wait for the StateChangedStopped and MUST clean-up the ClearVRPlayer object once this callback is received OR (preferable) wait for the StateChangedStopped event.
        /// Please note that the player can stop itself for whatever reason at whatever moment in time. In this case, one will have to perform final clean-up in StateChangedStopped anyway.
        /// Callback event: StateChangedStopped
        /// </summary>
        void Stop(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use Stop(Action, Action, Params) instead.", false)]
        void Stop(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        [Obsolete("This Stop() has been deprecated and can no longer be used. Call Stop(Action, Action, Params instead) instead.", true)]
        ClearVRAsyncRequest Stop();
        /// <summary>
        /// Enable or disable content looping. Note that you can only call this API after the player has reached StateChangedPlaying. If you would like to configure content looping before loading the first ContentItem, please refer to platformOptions.loopContent.
        /// </summary>
        /// <param name="argIsContentLoopEnabled"></param>
        /// <returns></returns>
        bool SetLoopContent(bool argIsContentLoopEnabled);
        /// <summary>
        /// Seek to a new position in the content. The new position should be provided in milliseconds and will be bound between 0 and the content duration.
        /// Notes:
        /// 1. Due to the nature of spatio-temporal streaming, seek will jump to the closest I-frame.
        /// 2. This method can only be called in Running/Pausing/Paused/Buffering state.
        /// </summary>
        /// <param name="argSeekParameters">This object specifies the arguments that should be passed to the seek request.</param>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">An optional callback that should be triggered after the request completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use void Seek(SeekParameters, Action, Action, Params) instead.", false)]
        void Seek(SeekParameters argSeekParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Seek to a new position in the content. The new position should be provided in milliseconds and will be bound between 0 and the content duration.
        /// Notes:
        /// 1. Due to the nature of spatio-temporal streaming, seek will jump to the closest I-frame.
        /// 2. This method can only be called in Running/Pausing/Paused/Buffering state.
        /// </summary>
        /// <param name="seekParameters">This object specifies the arguments that should be passed to the seek request.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void Seek(SeekParameters seekParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);

        /// <summary>
        /// Get the current content position in milliseconds.
        /// </summary>
        /// <returns>The current position in the content, in milliseconds.</returns>
        [Obsolete("This API has been replaced by the controller.GetTimingReport(TimingTypes.ContentTime) API.", true)]
        long GetCurrentContentTimeInMilliseconds();
        /// <summary>
        /// Get the current wallclock content position in milliseconds.
        /// This API is especially useful when playing back live, synchronized, content.
        /// </summary>
        /// <returns>The current wallclock content position in the content, in milliseconds.</returns>
        [Obsolete("This API has been replaced by the controller.GetTimingReport(TimingTypes.WallclockTime) API.", true)]
        long GetCurrentWallclockContentTimeInMilliseconds();
        /// <summary>
        /// This API is used to switch to a new ContentItem.
        /// </summary>
        /// <param name="argSwitchContentParameters">Specifies required parameters to switch content.</param>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">An optional callback that should be triggered after the request completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Use SwitchContent(SwitchContentParameters, Action, Action, Params) instead.", false)]
        void SwitchContent(SwitchContentParameters argSwitchContentParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// This API is used to switch to a new ContentItem.
        /// </summary>
        /// <param name="switchContentParameters">Specifies required parameters to switch content.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void SwitchContent(SwitchContentParameters switchContentParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /* Audio control */
        /// <summary>
        /// Mute or unmute audio playback. Check the specification of the return type in the notes below!
        /// 
        /// Notes.
        /// 1. this method returns false if it successfully UNMUTED the audio OR failed to execute the mute/unmute request.
        /// </summary>
        /// <param name="argIsMuted">True if audio should be muted, false is audio should be unmuted.</param>
        /// <returns>True if audio is muted, false if audio is unmuted OR if the call failed.</returns>
        bool SetMuteAudio(bool argIsMuted);
        /// <summary>
        /// Change audio gain. Expected scale: [0, 1]
        /// </summary>
        /// <param name="argGain">The new gain to set.</param>
		void SetAudioGain(float argGain);
        /// <summary>
        /// Retrieve current audio gain.
        /// Notes:
        /// Returns 0 if audio gain could not be queried.
        /// </summary>
        /// <returns>The current audio gain.</returns>
		float GetAudioGain();
        /// <summary>
        /// Check whether audio is muted or not.
        /// </summary>
        /// <returns>True is audio is muted, false otherwise.</returns>
		bool GetIsAudioMuted();
        /// <summary>
        /// Sets the active audio track index. Use -1 to select no audio track (effectively disabling audio playback). 
        /// Note that this call is asynchronous and one must wait for the AudioTrackSwitched event before the switch is done.
        /// Callback event: AudioTrackSwitched
        /// </summary>
        /// <param name="argAudioTrackAndPlaybackParameters">What audio track to select, and what audio decoder/playback engine to use. When unsure, just use an object from the default constructor new AudioTrackAndPlaybackParameters().</param>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">Optional callback that should be triggered upon completion. Can be null</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass along.</param>
        [Obsolete("This API has been deprecated. Use void SetFeedLayout(LayoutParameters, Action, Action, params) instead.", true)] // Marked as no longer usable on 2022-04-01
        void SetAudioTrack(AudioTrackAndPlaybackParameters argAudioTrackAndPlaybackParameters, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Sets the active audio track index. Use -1 to select no audio track (effectively disabling audio playback). 
        /// Note that this call is asynchronous and one must wait for the AudioTrackSwitched event before the switch is done.
        /// Callback event: AudioTrackSwitched
        /// </summary>
        /// <param name="audioTrackAndPlaybackParameters">What audio track to select, and what audio decoder/playback engine to use. When unsure, just use an object from the default constructor new AudioTrackAndPlaybackParameters().</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass along.</param>
        [Obsolete("This API has been deprecated. Use void SetFeedLayout(LayoutParameters, Action, Action, params) instead.", true)] // Marked as no longer usable on 2022-04-01
        void SetAudioTrack(AudioTrackAndPlaybackParameters audioTrackAndPlaybackParameters, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// Get the currently selected audio track index.
        /// </summary>
        /// <returns>&gt;=0 if an audio track is selected, -1 if audio is explicitely disabled, -2 is no audio track is (yet) selected, -3 if it was impossible to retrieve the selected audio track (should be treated as an error). </returns>
        [Obsolete("This API has been deprecated, use the AudioTracksChanged event and the ContentInfo it embeds to determine the currently active audio track.", true)] // Marked as no longer usable on 2022-05-01")]
        int GetAudioTrack();
        /// <summary>
        /// Gets a comprehensive overview of the current content position and the seekable range.
        /// 
        /// This API is particularly convenient when playing a live event and one wants to determine the seekable range of that live event and compare it to the current position.
        /// One can use one of the following {@link SeekFlags} to determine how to interpret the returned values, but one can NEVER use a combination of {@link SeekFlags}.
        /// SeekFlags.ContentTime: query timing properties in reference to the ContentTime. Applicable to VOD and LIVE.
        /// SeekFlags.WallclockTime: query timing properties in reference to the ContentTime. Applicable to LIVE content only.
        /// SeekFlags.RelativeTime: not applicable, do not use.
        /// SeekFlags.LiveEdge: not applicable, do not use.
        /// SeekFlags.Seamless: not applicable, do not use.
        /// 
        /// The [TimingReport](xref:com.tiledmedia.clearvr.TimingReport)'s getIsSucces() method can be used to determine if the query was successfully handled or not. In case of no success, the clearVRMessage field can be used to query details on the failure.
        /// 
        /// </summary>
        /// <param name="argSeekFlag">A SINGLE SeekFlag as defined above.</param>
        /// <returns>a [TimingReport](xref:com.tiledmedia.clearvr.TimingReport) in case of success AND failure. This method never returns null.</returns>
        [Obsolete("This API has been replaced by the GetTimingReport(TimingTypes) API.", true)] // Marked as no longer usable on 2020-12-01
        TimingReport GetTimingReport(System.Object argSeekFlag);
        /// <summary>
        /// Gets a comprehensive overview of the current content position and the seekable range.
        /// 
        /// This API is particularly convenient when playing a live event and one wants to determine the seekable range of that live event and compare it to the current position.
        /// One can use one of the following [TimingTypes](xref:com.tiledmedia.clearvr.TimingTypes) to determine how to interpret the returned values.
        /// <list type="bullet">
        /// <item>
        /// <term>[ContentTime](xref:com.tiledmedia.clearvr.TimingTypes.ContentTime)</term>
        /// <description> query timing properties in reference to the ContentTime. Applicable to VOD and LIVE.</description>
        /// </item>
        /// <item>
        /// <term>[WallclockTime](xref:com.tiledmedia.clearvr.TimingTypes.WallclockTime)</term>
        /// <description> query timing properties in reference to the ContentTime. Applicable to LIVE content only.</description>
        /// </item>
        /// <item>
        /// <term>[RelativeTime](xref:com.tiledmedia.clearvr.TimingTypes.RelativeTime)</term>
        /// <description> not applicable, do not use.</description>
        /// </item>
        /// <item>
        /// <term>[LiveEdge](xref:com.tiledmedia.clearvr.TimingTypes.LiveEdge)</term>
        /// <description> not applicable, do not use.</description>
        /// </item>
        /// <item>
        /// <term>[Seamless](xref:com.tiledmedia.clearvr.TimingTypes.Seamless)</term>
        /// <description> not applicable, do not use.</description>
        /// </item>
        /// </list>
        /// 
        /// The [TimingReport](xref:com.tiledmedia.clearvr.TimingReport)'s getIsSucces() method must be used to determine if the query was successfully handled or not. In case of no success, the clearVRMessage field can be used to query details on the failure.
        /// 
        /// </summary>
        /// <param name="argTimingType">The TimingType, as defined above.</param>
        /// <returns>a [TimingReport](xref:com.tiledmedia.clearvr.TimingReport) in case of success AND failure. This method never returns null, so in case of failure the TimingReport will contain undefined values..</returns>
        TimingReport GetTimingReport(TimingTypes argTimingType);
        /// <summary>
        /// Set the rate at which the media is being played back.
        /// 
        /// Notes:
        /// Only values between 0.5 and 2.0 are valid.
        /// 
        /// </summary>
        /// <param name="value">the new playback rate</param>
        void SetPlaybackRate(float value);
        /// <summary>
        /// The rate at which the media is being played back.
        /// </summary>
        /// <returns>the playback rate as Float value. Returns 0.0f if playback rate can't be queried.</returns>
        float GetPlaybackRate();
    }
    
    /// <summary>
    /// Various ClearVRCoreWrapper performance statistics are exposed via the ClearVRCoreWrapperStatistics object and its subclasses ClearVRCoreWrapperVideoStatisticsInterface and ClearVRCoreWrapperAudioStatisticsInterface
    /// Notes:
    /// 1. Android specific: each call to any of its members involves several JNI calls, which are considered to be "expensive". It is therefor advised to use any of these fields with moderation and to not query them every Update() cycle.
    /// </summary>
    public interface ClearVRCoreWrapperStatisticsInterface {
        /// <summary>
        /// the ClearVRCoreWrapperStatistics *must* be properly destroyed to release any claimed resources.
        /// </summary>
        void Destroy();
    }

    /// <summary>
    /// This class is used to expose various video-performance related metrics.
    /// </summary>
    public interface ClearVRCoreWrapperVideoStatisticsInterface {
        /* fields */

        /// <summary>
        /// The average decoder inter-frame latency is defined as the time between two consecutively decoded frames.
        /// Notes.
        /// 1. when in Paused state, this value is expected to increase.
        /// 2. if you are interested in the inter-video-frame render latency (e.g. the average "render framerate"), please check interFrameRenderLatencyMean.
        /// </summary>
        /// <value>The mean inter-frame decoder latency in milliseconds or 0 if unknown.</value>
        float interFrameDecoderLatencyMean { get; }
        /// <summary>
        /// The average decoder inter-frame latency standard deviation is defined as the time between two consecutively decoded frames.
        /// Notes.
        /// 1. when in Paused state, this value is expected to increase.
        /// </summary>
        /// <value>The average inter-frame decoder latency standard deviation in milliseconds, or 0 if unknown.</value>
        float interFrameDecoderLatencyStandardDeviation { get; }
        /// <summary>
        /// The average render inter-frame latency is defined as the time between two consecutively rendered frames.
        /// Notes.
        /// 1. when in Paused state, this value is expected to increase.
        /// </summary>
        /// <value>The mean inter-frame render latency in milliseconds or 0 if unknown.</value>
        float interFrameRenderLatencyMean { get; }
        /// <summary>
        /// The average render inter-frame latency standard deviation is defined as the time between two consecutively rendered frames.
        /// Notes.
        /// 1. when in Paused state, this value is expected to increase.
        /// </summary>
        /// <value>The average inter-frame render latency standard deviation in milliseconds, or 0 if unknown.</value>
        float interFrameRenderLatencyStandardDeviation { get; }
        /// <summary>
        /// Number of frames rendered since creation of the ClearVRPlayer object. 
        /// A rendered frame is defined as a frame that was actually shown to the user.
        /// </summary>
        /// <value>The number of rendered frames.</value>
        long framesRendered { get; }
        /// <summary>
        /// Number of frames dropped since creation of the ClearVRPlayer object. In well behaving environment, this value should be close to zero (0).
        /// A dropped frame is defined as a frame that could not be shown to the user, e.g. because the application framerate was too low, or because there was a hick-up in the decoder pipeline.
        /// </summary>
        /// <value>The number of dropped frames.</value>
        long framesDropped { get; }
        /// <summary>
        /// An advanced metric giving an indication of the quality of the vsync rhythm. In ideal situations, this value should be (very close to) 100%.
        /// This value is averaged over approximately 6 seconds.
        /// </summary>
        /// <value>The vsync quality as a percentage.</value>
        float vsyncQuality { get; }
        /// <summary>
        /// An advanced metric giving an indication of the quality of the frame release rhythm. In ideal situations, this value should be (very close to) 100%. The lower this vallue, the more likely the end-user will notice stuttering/jittering.
        /// This value is averaged over approximately 6 seconds.
        /// </summary>
        /// <value>The frame release quality as a percentage.</value>
        float frameReleaseQuality { get; }
        /// <summary>
        /// An advanced metric measuring the average decoder input queue size. This value should be less than 0.3 under normal conditions. A value larger than 1 indicates that the device might not be able to keep up with the video, resulting in very poor user experience.
        /// This value is averaged over approximately 6 seconds.
        /// </summary>
        /// <value>The average decodere input queue size.</value>
        float averageDecoderInputQueueSize { get; }
        /// <summary>
        /// An advanced metric measuring the average decoder output queue size (e.g. how many decoded frames are queued up and ready for release). This value should be less than 3 under normal conditions. A value larger than 5 indicates that the device might not be able to keep up with the video, resulting in very poor user experience.
        /// This value is averaged over approximately 6 seconds.
        /// </summary>
        /// <value>The average decodere output queue size.</value>
        float averageDecoderOutputQueueSize { get; }
        /// <summary>
        /// The average end-to-end inter-frame latency is defined as the time between the moment an encoded (HEVC) frame was created and it actually being displayed to the end-user.
        /// This metric should be less than 90 msec and can be as low as 30 msec.
        /// Notes.
        /// 1. when in Paused state, this value is expected to increase.
        /// 2. this metric has a bit of overshoot. Actual end-to-end latency is slightly lower than reported.
        /// </summary>
        /// <value>The mean end-to-end inter-frame latency in milliseconds or 0 if unknown.</value>
        float endToEndFrameLatencyMean { get; }
        /// <summary>
        /// The average end-to-end inter-frame latency is defined as the time between the moment an encoded (HEVC) frame was created and it actually being displayed to the end-user.
        /// This metric should be less than 90 msec and can be as low as 30 msec.
        /// Notes.
        /// 1. when in Paused state, this value is expected to increase.
        /// 2. this metric has a bit of overshoot. Actual end-to-end latency is slightly lower than reported.
        /// </summary>
        /// <value>The average end-to-end inter-frame latency standard deviation in milliseconds, or 0 if unknown.</value>
        float endToEndFrameLatencyStandardDeviation { get; }
        /// <summary>
        /// The average application inter-frame latency is defined as the time between two consequtive application frame updates. 
        /// In Unity, this equals to the time between two Update() cycles.
        /// This metric should be (very) close to the vsync.
        /// </summary>
        /// <value>The mean application inter-frame latency in milliseconds or 0 if unknown.</value>
        float interFrameApplicationLatencyMean { get; }
        /// <summary>
        /// The average application inter-frame latency is defined as the time between two consequtive application frame updates. 
        /// In Unity, this equals to the time between two Update() cycles.
        /// This metric should be (very) close to the vsync.
        /// In well behaving application, standard deviation should be very low (e.g. less than 3 msec). Higher values might indicate a performance bottleneck.
        /// </summary>
        /// <value>The average application inter-frame latency standard deviation in milliseconds, or 0 if unknown.</value>
        float interFrameApplicationLatencyStandardDeviation { get; }

        /* methods */

        /// <summary>
        /// Returns the inter-frame render latency as a formatted string. Example: mean +/- stddev. Both values have two decimal values to it.
        /// </summary>
        /// <returns>A formatted string.</returns>
        String GetInterFrameRenderLatencyAsPrettyString();
        /// <summary>
        /// Returns the inter-frame decoder latency as a formatted string. Example: mean +/- stddev. Both values have two decimal values to it.
        /// </summary>
        /// <returns>A formatted string.</returns>
        String GetInterFrameDecoderLatencyAsPrettyString();
        /// <summary>
        /// Returns the inter-frame end-to-end latency as a formatted string. Example: mean +/- stddev. Both values have two decimal values to it.
        /// Note that this metric has a slight overshoot, and in reality is lower than reported.
        /// </summary>
        /// <returns>A formatted string.</returns>
        String GetEndToEndFrameLatencyAsPrettyString();
        /// <summary>
        /// Returns the inter-frame application latency as a formatted string. Example: mean +/- stddev. Both values have two decimal values to it.
        /// </summary>
        /// <returns>A formatted string.</returns>
        String GetInterFrameApplicationLatencyAsPrettyString();

        /// <summary>
        /// Returns the inter frame render latency as a two-decimal pretty print framerate string. Example: 59.94.
        /// </summary>
        /// <returns>The pretty-printed framerate.</returns>
        float GetInterFrameRenderRateInFramesPerSecond();
        /// <summary>
        /// Returns the inter frame decoder latency as a two-decimal pretty print framerate string. Example: 59.94.
        /// </summary>
        /// <returns>The pretty-printed framerate.</returns>
        float GetInterFrameDecoderRateInFramesPerSecond();
        /// <summary>
        /// Returns the inter frame application latency as a two-decimal pretty print framerate string. Example: 59.94.
        /// </summary>
        /// <returns>The pretty-printed framerate.</returns>
        float GetInterFrameApplicationRateInFramesPerSecond();
        /// <summary>
        /// Destroys the object, freeing any resources it might have claimed.
        /// </summary>
        void Destroy();
    }

    /// <summary>
    /// This class is used to expose various audio-performance related metrics.
    /// </summary>
    public interface ClearVRCoreWrapperAudioStatisticsInterface {
        /* fields */

        /// <summary>
        /// Number of audio frames rendered since creation of the ClearVRPlayer object. An audio frame is defined as a set of PCM samples of a specific length. For AAC-encoded audio, this audio frame is approximately 21 msec.
        /// A rendered frame is defined as a frame that was actually played to the user.
        /// </summary>
        /// <value>The number of rendered frames.</value>
        long framesRendered { get; }
        /// <summary>
        /// Number of frames dropped since creation of the ClearVRPlayer object. In well behaving environment, this value should be close to zero (0).
        /// A dropped audio frame is defined as an audio frame that could not be played to the user, e.g. because there was a hick-up in the pipeline or performance was not sufficient.
        /// </summary>
        /// <value>The number of dropped frames.</value>
        long framesDropped { get; }
        /// <summary>
        /// The number of buffer underruns during playback. A buffer underrun is defined as a moment in time where no audio data was available to fill the playback buffer. A buffer underrun can result in an audible glitch. 
        /// </summary>
        /// <value>The number of underruns.</value>
        int playbackUnderrunCount { get; }
        
        /* methods */

        /// <summary>
        /// Destroys the object, freeing any resources it might have claimed.
        /// </summary>
        void Destroy();
    }

    /// <summary>
    /// This interface specifies various convenience methods that allws quick access to the most important media stream properties. 
    /// </summary>
    public interface MediaInfoInterface {
        /// <summary>
        /// Query the content duration. 
        /// Notes:
        /// 1. for VOD content, this method returns the duration of the clip
        /// 2. for live content, this method returns 0
        /// 3. if called at an illegal moment (e.g. when no content has yet been loaded), this method returns 0
        /// 4. this is considered an "expensive" method, one is encouraged to call this method too often, but rather cache its value.
        /// </summary>
        /// <returns>The content duration in milliseconds, or 0 otherwise (see notes)</returns>
        [Obsolete("This API has been replaced by the controller.GetTimingReport(TimingTypes.ContentTime) (and controller.GetTimingReport(TimingTypes.WallclockTime)) API.", true)]
        long GetContentDurationInMilliseconds();
        /// <summary>
        /// Returns the highest available resolution and framerate as a formatted string: [width]x[height]p[framerate] or 0x0p0 if unknown.
        /// Helpfull if one wants to determine the highest available representation
        /// > [!WARNING]
        /// > This API has been deprecated and removed from the SDK. Monitor the [ActiveTracksChanged](xref:com.tiledmedia.clearvr.ClearVREventTypes.ActiveTracksChanged) event instead. The [video tracks](xref:com.tiledmedia.clearvr.VideoTrackInfo) per [feed](xref:com.tiledmedia.clearvr.FeedInfo) in the [content info](xref:com.tiledmedia.clearvr.ContentInfo) are ordered in descending order, highest quality first.
        /// </summary>
        /// <returns>The highest available quality as a formatted string.</returns>
        [Obsolete("This API has been deprecated and removed from the SDK. Monitor the ActiveTracksChanged event instead. The video tracks per feed in the content info are ordered in descending order, highest quality first.", true)]
        String GetHighestQualityResolutionAndFramerate();
        /// <summary>
        /// Returns the resolution and framerate of the currently active represenation as a formatted string: [width]x[height]p[framerate] or 0x0p0 if unknown.
        /// </summary>
        /// <returns>The current quality as a formatted string.</returns>
        [Obsolete("This API has been deprecated and removed from the SDK. Monitor the ActiveTracksChanged event instead. You can query the video track info of the currently active Feed(s) to determine the resolution and framerate of the currently active rendition.", true)]
        String GetCurrentResolutionAndFramerate();
        /// <summary>
        /// Returns the number of audio tracks
        /// Notes:
        /// 1. Returns 0 even if the number of audio tracks could not be queried.
        /// > [!NOTE]
        /// > This API has been removed in v9.x. Instead, refer to the [ActiveTracksChanged event](xref:com.tiledmedia.clearvr.ClearVREventTypes.ActiveTracksChanged) and its [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo) instead.
        /// </summary>
        /// <returns>The number of available audio tracks.</returns>
        [Obsolete("This API has been deprecated in v9.x and can no longer be used. Refer to ActiveTracksChanged and its ContentInfo.GetNumberOfSelectableAudioTracks() API instead.", true)]
        int GetNumberOfAudioTracks();
        /// <summary>
        /// Returns the content format. Can be used to figure out whether the content is 360 or 180 degrees and if it is monoscopic or stereoscopic.
        /// Notes.
        /// 1. if, for whatever reason, the content format changes, the callback event ContentFormatChanged will be invoked.
        /// </summary>
        /// <returns>The current content format.</returns>
        [Obsolete("This API has been deprecated in v9.x and can no longer be used. Use the ClearVRDisplayObjectController.GetContentFormat() API instead.", true)]
        ContentFormat GetContentFormat();
        /// <summary>
        /// Convenience method that returns whether the current content is stereoscopic or not.
        /// </summary>
        /// <returns>True if the content is stereoscopic, false if monoscopic.</returns>
        [Obsolete("This convenience API has been deprecated in v9.x and can no longer be used. Use the ClearVRDisplayObjectController.GetContentFormat().GetIsStereoscopic() API instead.", true)]
        bool GetIsContentFormatStereoscopic();
        /// <summary>
        /// Query the event type of the currently active content item. 
        /// Note that this might change after every SwitchContent(), so one is encouraged to requery this parameter in the ClearVREventTypes.ContentSwitched event.
        /// </summary>
        /// <returns>Returns the appropriate EventTypes enum.</returns>
        [Obsolete("This API is still functional but has been deprecated in v9.1 and will be removed after 2023-06-30. You are encouraged to use the GetContentInfo() API instead to retrieve the EventType (and much more info) on the currently playing ContentItem.", false)]
        EventTypes GetEventType();
        /// <summary>
        /// Get information (e.g. the resolution of available ABR representations, number of audio tracks, etc.) about the specified contentItem and all the feeds it contains.
        /// This information can only be fetched after the ContentPreparedForPlayout state with a ContentItem that is known (is prepared/playing right now, or has been prepared/played by using the SwitchContent API)
        /// </summary>
        /// <param name="contentItem">The content item you would like to retrieve this information from, can be `null`. If set to `null`, the currently active content item will be queried.</param>
        /// <returns>a [ContentInfo](xref:com.tiledmedia.clearvr.ContentInfo) object, which contains one or multiple [FeedInfo](xref:com.tiledmedia.clearvr.FeedInfo) which in turn contains [VideoTrackInfo](xref:com.tiledmedia.clearvr.VideoTrackInfo), [AudioTrackInfo](xref:com.tiledmedia.clearvr.AudioTrackInfo) and [SubtitleTrackInfo](xref:com.tiledmedia.clearvr.SubtitleTrackInfo) (if available). This API returns null if the call fails or a ContentInfo object with 0 feeds if the queried content item is not loaded/active.</returns>
        ContentInfo GetContentInfo(ContentItem contentItem = null);
    }

    /// <summary>
    /// The performance interface exposes various methods to query the performance of the ClearVRCoreWrapper and the underlying ClearVRCore. Note that the ClearVRCore exposes many more metrics, refer to the ClearVRCore documentation and the "perf.*" parameter keys for details.
    /// </summary>
    public interface PerformanceInterface {
        /// <summary>
        /// Convience method exposing the ClearVRCore "perf.network.current_avg_kbps" metric.
        /// Notes.
        /// 1. This is considered an "expensive" operation and one is encouraged *not* to call this method every frame.
        /// </summary>
        /// <returns>The current network throughput in megabit per second.</returns>
        float GetAverageBitrateInMbps();
        /// <summary>
        /// The ClearVRCoreWrapper class exposes various runtime performance metrics. Use this object to access them. Note that this *must* be destroyed in order to not leak any memory. Typically, this is being taken care of by the underlying layers.
        /// You typically get a reference to this object once for every ClearVRPlayer object you instantiate. The object stays valid thourhgout the entire lifetime of said object, but will be destroyed if the underlying ClearVRPlayer object is destroyed.
        /// </summary>
        /// <value>A ClearVRCoreWrapperStatistics object that can be queried for metrics.</value>
        StatisticsBase clearVRCoreWrapperStatistics { get; }
    }

    /// <summary>
    /// The sync interface can be used to control and view synchronization for live events. 
    /// </summary>
    public interface SyncInterface {
        /// <summary>
        /// Enable the sync algorithm. This is only supported for live content.
        /// </summary>
        /// <param name="argSyncSettings">Configure advanced sync options.</param>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">The response event handler for this call</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use the new EnableSync(SyncSettings, Action, Action, Params) API instead.", false)]
        void EnableSync(SyncSettings argSyncSettings, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Enable the sync algorithm. This is only supported for live content.
        /// </summary>
        /// <param name="syncSettings">Configure advanced sync options.</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void EnableSync(SyncSettings syncSettings, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// Disable the sync algorithm.
        /// </summary>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">The response event handler for this call</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use the new DisableSync(Action, Action, Params) API instead.", false)]
        void DisableSync(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Disable the sync algorithm.
        /// </summary>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void DisableSync(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// Poll the current sync status. This can be called independently of sync being currently enabled or disabled.
        /// </summary>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">The response event handler for this call</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use the new DisableSync(Action, Action, Params) API instead.", false)]
        void PollSyncStatus(Action<SyncStatus, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Poll the current sync status. This can be called independently of sync being currently enabled or disabled.
        /// </summary>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void PollSyncStatus(Action<SyncStatus, ClearVRPlayer, object[]> onSuccess, Action<ClearVRMessage, ClearVRPlayer, object[]> onFailure, params object[] optionalArguments);
    }

    /// <summary>
    /// Please do *not* use these APIs in your own project. They are for debugging purposes only and will change without notice
    /// </summary>
    public interface DebugInterface {
        /// <summary>
        /// Send a message with instructions in the form of a protobuf message to the core.
        /// </summary>
        /// <param name="rawMessage">The raw message.</param>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">The response event handler for this call</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void CallCore(byte[] argRawMessage, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        /// <summary>
        /// Send a message to the core.
        /// Generally the message should be serialized protobuf defined in Core.proto.
        /// </summary>
        /// <param name="base64Message">The raw message as a string.</param>
        string CallCoreSync(string base64Message);
        /// <summary>
        /// Send a message to the core to switch the ABR level.
        /// </summary>
        /// <param name="shouldIncrease">Should the ABR level go up or down? true if up, false if down..</param>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void SwitchABRLevel(bool shouldIncrease, Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// Send a message to the core to switch the ABR level.
        /// </summary>
        /// <param name="argUp">Should the ABR level go up or down? true if up, false if down..</param>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">An optional callback that should be triggered after the request was completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use the new SwitchABR(bool, Action, Action, Params) API instead.", false)]
        void SwitchABRLevel(bool argUp, Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
        
        /// <summary>
        /// Send a message to the core to make it forcefully crash.
        /// </summary>
        /// <param name="onSuccess">An optional callback that should be triggered after the request was succesfully completed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="onFailure">An optional callback that should be triggered in case the request has failed. You are highly encouraged to implement the callback, but it can be null.</param>
        /// <param name="optionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        void ForceClearVRCoreCrash(Action<ClearVREvent, ClearVRPlayer> onSuccess, Action<ClearVREvent, ClearVRPlayer> onFailure, params object[] optionalArguments);
        /// <summary>
        /// Send a message to the core to make it forcefully crash.
        /// </summary>
        /// <param name="argCbClearVRAsyncRequestResponseReceived">The response event handler for this call</param>
        /// <param name="argOptionalArguments">Any optional argument that you would like to pass inside the callback.</param>
        [Obsolete("This API has been deprecated and will be removed 2023/01/31. Please use the new ForceClearVRCoreCrash(Action, Action, Params) API instead.", false)]
        void ForceClearVRCoreCrash(Action<ClearVREvent, ClearVRPlayer> argCbClearVRAsyncRequestResponseReceived, params object[] argOptionalArguments);
    }

    /// <summary>
    /// This is an internal interface and should not be used by the end-user.
    /// </summary>
    public interface PlatformOptionsInterface {
        /// <summary>
        /// Verifies whether the PlatformOptions are correct. 
        /// </summary>
        /// <returns>True if verification succeeded, false otherwise.</returns>
        bool Verify(ClearVRLayoutManager argClearVRLayoutManager);
    }

    
    internal interface ClearVRDisplayObjectControllerInterfaceInternal {
        void Initialize(PlatformOptionsBase argPlatformOptions, SharedPointersWithSDK sharedPointersWithSDK, System.Object argReserved);
        void BindNativeTextureToShader(Texture argNativeTexture);
        void EnableOrDisableMeshRenderer(bool argIsEnabled);
        void RecreateNativeTextures();
        void UpdateNativeTextures(IntPtr argTextureId0, IntPtr argTextureId1, IntPtr argTextureId2);
        void DestroyNativeTextures();
        void UpdateApplicationMeshState();
        void UpdateShader();
    }

    public interface ClearVRDisplayObjectControllerInterface {
        /// <summary>
        /// Change the main color of the video texture. This can also be used to make the sphere transparent by setting the alpha component accordingly.
        /// For common performance reasons not specifically related to ClearVR streaming, you should carefully assess the potential negative performance impact of using transparency on mobile devices.
        /// </summary>
        /// <param name="argNewColor">The new color.</param>
        /// <returns>True if successful, false otherwise.</returns>
        bool SetMainColor(Color argNewColor);
        /// <summary>
        /// Since v4.1.2
        /// Returns the layout of the fallback tiles.
        /// 
        /// Notes.
        /// 1. You should only call this method after you have received the FirstFrameRendered event.
        /// 2. Remember that the layout of the fallback can change when switching between clips. If it changed, you will receive a new FirstFrameRendered event and you should requery the FallbackLayout.
        /// 3. Currently, only the layout of the fallback tiles of the *left* eye are signalled.
        /// 4. The layout of the left eye's fallback tiles is fixed, it will not change during runtime
        /// </summary>
        /// <returns>The layout of the fallback tiles.</returns>
        [Obsolete("This API has been deprecated in v9.0 and can no longer be used. There is no equivalent new API.", true)]
        System.Object GetFallbackLayout();
         /// <summary>
        /// Since v4.1.2
        /// Returns the Texture2D object of the currently active texture. Note that the tiles in this texture are shuffled.
        /// 
        /// Notes.
        /// 1. You should only call this method after you have received the FirstFrameRendered event.
        /// 2. This texture might change as a result of switchContent() and/or ABR events.
        /// 3. This texture will NOT change when switching from monoscopic to stereoscopic rendering or vice versa.
        /// </summary>
        /// <returns>The currently active video texture as a properly bounded Texture2D.</returns>
        Texture GetTexture();
        /// <summary>
        /// Since v9.0
        /// Returns the content format. Can be used to figure out whether the content is 360 or 180 degrees and if it is monoscopic or stereoscopic.
        /// Notes.
        /// 1. if, for whatever reason, the content format changes, the callback event ContentFormatChanged will be invoked on the [clearVRPlayer.clearVRDisplayObjectEvents](xref:com.tiledmedia.clearvr.ClearVRPlayer.clearVRDisplayObjectEvents) event queue.
        /// </summary>
        /// <returns>The current content format.</returns>        ContentFormat GetContentFormat();
        void EnableOrDisableStereoscopicRendering(bool argValue, bool argForceUpdate = false);
        /// <summary>
        /// This API is used to switch between monoscopic and stereoscopic rendering (if the content and player allows for it). Note that, in case of ClearVR contrent, this only changes how the content is *rendered*, NOT how it is retrieved from the network. If you want to switch between monoscopic or stereoscopic content **retrieval**, you should use the [SetStereoMode](xref:com.tiledmedia.clearvr.MediaPlayerInterface.SetStereoMode(System.Boolean,Action{com.tiledmedia.clearvr.ClearVREvent,com.tiledmedia.clearvr.ClearVRPlayer},Action{com.tiledmedia.clearvr.ClearVREvent,com.tiledmedia.clearvr.ClearVRPlayer},System.Object[])) API instead. Note that the SetStereoMode API is considered an advanced API and should not be used under typical conditions.
        /// The SetRenderMode API is especially suited for temporarily disabling stereoscopic rendering when you show an in-player menu (to prevent depth-fighting from happening) as this API is instantaneous while the SetStereoMode() API will take a bit of time to respond, reducing user experience.
        /// 
        /// You will receive a ClearVREventTypes.RenderModeChanged event on the [clearVRPlayer.clearVRDisplayObjectEvents](xref:com.tiledmedia.clearvr.ClearVRPlayer.clearVRDisplayObjectEvents) event queue when the render mode has changed. If an incorrect request is made (e.g. when you try to enable stereoscopic content playback on a monoscopic video), the request will be bounced and an appropriate error message can be found in the event's [ClearVRMessage](xref:com.tiledmedia.clearvr.ClearVRMessage).
        /// </summary>
        /// <param name="argNewRenderMode">The new render mode.</param>
        void SetRenderMode(RenderModes argNewRenderMode);
        /// <summary>
        /// Returns the content format. Can be used to figure out whether the content is 360 or 180 degrees and if it is monoscopic or stereoscopic.
        /// Notes.
        /// 1. if, for whatever reason, the content format changes, the callback event ContentFormatChanged will be invoked per DisplayObject.
        /// </summary>
        /// <returns>The current content format.</returns>
        ContentFormat GetContentFormat();
        /// <summary>
        /// Since v9.0
        /// Query whether the DisplayObject is active or not. 
        /// > [!NOTE]
        /// > Note that `active` is NOT the same as `gameobject.activeSelf`. You can use this API in conjunction with the [ClearVRDisplayObjectEventTypes.ActiveStateChanged](xref:com.tiledmedia.clearvr.ClearVRDisplayObjectEventTypes.ActiveStateChanged) as it gets triggered once the state has changed.  
        ///  </summary>
        /// <returns></returns>
        bool GetIsActive();
    }
}