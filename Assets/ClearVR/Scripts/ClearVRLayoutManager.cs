// Uncomment this line to enable the vsync counter
//#define ENABLE_VSYNC_DEBUG 

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

namespace com.tiledmedia.clearvr {
    /// <summary>
    /// For details on the ClearVRLayoutManager and how to use it, please refer to the documentation [here](~/readme/layoutmanager.md).
    /// On runtime, you never access the ClearVRLayoutManager directly, you always interface with the ClearVRPlayer instead.
    /// </summary>
    public class ClearVRLayoutManager : MonoBehaviour {
        public static readonly string LEGACY_LAYOUT_NAME = "Legacy layout"; // Public, for customers trying to customize Legacy Layout behaviour
        internal static readonly string LEGACY_DISPLAY_OBJECT_NAME = "[ClearVR] Legacy Display Object";
        /// <summary>
        /// This is a list of active ClearVRDisplayObjectControllers. `Active` DOS being defined as a DOC that did call RegisterClearVRDisplayObjectControllerAsync() but not call UnregisterClearVRDisplayObjectControllerAsync().
        /// </summary>
        private List<ClearVRDisplayObjectControllerBase> _activeDisplayObjectControllers = new List<ClearVRDisplayObjectControllerBase>();
        /// <summary>
        /// This is a list of PENDING DOCs, Pending in the sense that they are either about to be added or removed from out list of _activeDisplayObjectControllers during the next tick.
        /// </summary>
        private List<Tuple<bool, ClearVRDisplayObjectControllerBase>> _pendingDisplayObjectControllers = new List<Tuple<bool, ClearVRDisplayObjectControllerBase>>();
        /// <summary>
        /// A list of LayoutParameters, as configured through the LayoutManager's user interface in the Unity Editor.
        /// > [!WARNING]
        /// > Never access or modify this list programmatically!
        /// </summary>
        [SerializeField] 
        internal List<LayoutParameters> layoutParametersList;
        private Dictionary<ClearVRDisplayObjectControllerBase, int> displayObjectIDsMapping = new Dictionary<ClearVRDisplayObjectControllerBase, int>();
		private const Int32 NRP_ID_NOT_SET = -2;
		private Int32 nrpID = NRP_ID_NOT_SET; // -1 is reserved in the NRP, during normal operation the NRP will set this to Int32 >= 0
        private LinkedList<NRPAction> asyncNRPActionsList = new LinkedList<NRPAction>(); // We need random access
        private Queue<AsyncLayoutManagerAction> asyncLayoutManagerActionsQueue = new Queue<AsyncLayoutManagerAction>(); // This is fifo
        private PlatformOptionsBase _platformOptionsBase = null;
        internal ClearVRDisplayObjectEventsInternal clearVRDisplayObjectEvents = new ClearVRDisplayObjectEventsInternal();
		 // We need the ClearVREvents proxy class for 2018 compatibility
        internal class ClearVRDisplayObjectEventsInternal : UnityEngine.Events.UnityEvent<ClearVRDisplayObjectControllerBase, ClearVRDisplayObjectEvent> {}
        // A list of DOCs that could not be registered to the NRP (yet) as they do/did not have a DisplayObjectMapping in any of the LayoutParameters at the moment they announced themselves.
        private List<ClearVRDisplayObjectControllerBase> _notRegisteredDisplayObjectControllers = new List<ClearVRDisplayObjectControllerBase>();
        private bool _isNRPInitializeCalled = false;
        /// <summary>
        /// The main DOC is defined as the DOC that renders the ClearVR mesh. Note that there can be only be 1 ClearVR mesh active at any point in time.
        /// </summary>
        // TODO: This should never be public and should be removed once legacy layout support is removed.
        internal ClearVRDisplayObjectControllerBase mainDisplayObjectController = null;
        /// <summary>
        /// This GameObject is merely for backwards compatibility with apps build for v8.x or older.
        /// </summary>
        private GameObject _clearVRLegacyDisplayObject = null;
        bool mediaPlayerCanAcceptNRPVsyncCalls = false;

        private int _vsyncCounter = 0;

        void Awake() {
            // This happens if we instantiate a LayoutManager as part of our legacy support.
            if(layoutParametersList == null) {
                layoutParametersList = new List<LayoutParameters>();
            }
            VerifyLayoutParameters();
            NativeRendererPluginBase.Setup(); // Binds IssuePluginEvent method
        }

#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
        private void OnValidate() {
            if(layoutParametersList != null) {
                AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
                if (layoutParametersList.Count == 0) {
                    displayObjectIDsMapping.Clear();
                }
                VerifyLayoutParameters();
            }
        }
#endif
        private void OnAfterAssemblyReload() {
            VerifyLayoutParameters();
        }

        internal bool VerifyLayoutParameters() {
            // This is a fix for #5777.
            // We must make sure that we do not hand out a DOID that is still in use by an active DOC that is no longer in any DOM.
            var list = _FindAllActiveClearVRDisplayObjectControllers();
            // This list will overlap with DOIDs as found via the DOMs in GetLowestAvailableDisplayObjectID() down below, but that is not a problem
            List<Int32> doidsInUse = new List<Int32>();
            foreach(var entry in list) {
                if(entry.displayObjectID > 0) {
                    doidsInUse.Add(entry.displayObjectID);
                }
            }
            Dictionary<ClearVRDisplayObjectControllerBase, int> newDisplayObjectIDsMapping = new Dictionary<ClearVRDisplayObjectControllerBase, int>();
            foreach(LayoutParameters layoutParameters in layoutParametersList) {
                if(layoutParameters == null) {
                    continue;
                }
                foreach (DisplayObjectMapping displayObjectMapping in layoutParameters.displayObjectMappings) {
                    if (displayObjectMapping.clearVRDisplayObjectController != null) {
                        try {
                            int cvrDisplayObjectID = -1;
                            if (displayObjectIDsMapping.TryGetValue(displayObjectMapping.clearVRDisplayObjectController, out cvrDisplayObjectID) && (cvrDisplayObjectID > 0 || layoutParameters.name == LEGACY_LAYOUT_NAME /* DOID 0 is expected on Legacy Layout */)) {
                                // The display object has already been used in an other template, hence we can fetch its ID
                                displayObjectMapping.displayObjectID = cvrDisplayObjectID;
                            } else {
                                if(layoutParameters.name == LEGACY_LAYOUT_NAME) {
                                    displayObjectMapping.displayObjectID = 0;
                                } else {
                                    bool isDOCAlreadyActive = false;
                                    foreach(ClearVRDisplayObjectControllerBase doc in _activeDisplayObjectControllers) {
                                        if(displayObjectMapping.clearVRDisplayObjectController == doc) {
                                            if(doc.displayObjectID >= 0) {
                                                // This DOC is already active and registered at the NRP. We should not give it a new DOID.
                                                displayObjectMapping.displayObjectID = doc.displayObjectID;
                                                isDOCAlreadyActive = true;
                                                break;
                                            }
                                        }
                                    }
                                    foreach(Tuple<bool, ClearVRDisplayObjectControllerBase> tuple in _pendingDisplayObjectControllers) {
                                        if(displayObjectMapping.clearVRDisplayObjectController == tuple.Item2) {
                                            if(tuple.Item2.displayObjectID >= 0) {
                                                // This DOC is already active and registered at the NRP. We should not give it a new DOID.
                                                displayObjectMapping.displayObjectID = tuple.Item2.displayObjectID;
                                                isDOCAlreadyActive = true;
                                                break;
                                            }
                                        }
                                    }
                                    if(!isDOCAlreadyActive) {
                                        displayObjectMapping.displayObjectID = GetLowestAvailableDisplayObjectID(doidsInUse);
                                    }
                                }
                            }
                            if(displayObjectMapping.clearVRDisplayObjectController != null) {
                                if (!newDisplayObjectIDsMapping.ContainsKey(displayObjectMapping.clearVRDisplayObjectController)){
                                    newDisplayObjectIDsMapping.Add(displayObjectMapping.clearVRDisplayObjectController, displayObjectMapping.displayObjectID);
                                }
                            }
                        } catch (System.Exception ex) {
                            Debug.LogError(string.Format("[ClearVR] Something went wrong while assigning a display object ID. Error: {0}", ex.Message));
                            return false;
                        }
                    }
                }
            }
            displayObjectIDsMapping.Clear();

            foreach (KeyValuePair<ClearVRDisplayObjectControllerBase, int> entry in newDisplayObjectIDsMapping) {
                // do something with entry.Value or entry.Key
                displayObjectIDsMapping.Add(entry.Key, entry.Value);
            }
            // We need to copy the new mapping to the old one, as apparently we can't modify the old one while iterating over it.
            foreach (LayoutParameters layoutParameters in layoutParametersList) {
                foreach (DisplayObjectMapping displayObjectMapping in layoutParameters.displayObjectMappings) {
                    if(displayObjectMapping.clearVRDisplayObjectController != null) {
                        if (displayObjectIDsMapping.ContainsKey(displayObjectMapping.clearVRDisplayObjectController)) {
                            displayObjectMapping.displayObjectID = displayObjectIDsMapping[displayObjectMapping.clearVRDisplayObjectController];
                        }
                    }
                }
            }
            if(Application.isPlaying) {
                // We retry to register any non-registered ClearVRDisplayObjectControllers
                // Here, we clone the original list as it gets modified in RegisterClearVRDisplayObjectControllerAsync()
                List<ClearVRDisplayObjectControllerBase> clone = new List<ClearVRDisplayObjectControllerBase>(_notRegisteredDisplayObjectControllers);
                foreach(ClearVRDisplayObjectControllerBase doc in clone) {
                    RegisterClearVRDisplayObjectControllerSyncMaybe(doc);
                }
            }
#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
#endif
            return true;
        }

        // Never returns null, can return an empty list. Includes all DisplayObjectIDs > 0.
        private List<Int32> GetListOfAssignedDisplayObjectIDs() {
            List<Int32> doids = new List<Int32>();
            foreach(LayoutParameters layoutParameters in layoutParametersList) {
                if(layoutParameters == null) {
                    continue;
                }
                foreach (DisplayObjectMapping displayObjectMapping in layoutParameters.displayObjectMappings) {
                    if (displayObjectMapping.clearVRDisplayObjectController != null) {
                        if(displayObjectMapping.displayObjectID > 0) {
                            if(!doids.Contains(displayObjectMapping.displayObjectID)) {
                                doids.Add(displayObjectMapping.displayObjectID);
                            }
                        }
                    }
                }
            }
            return doids;
        }

        private Int32 GetLowestAvailableDisplayObjectID(List<Int32> argReservedDOIDs) {
            Int32 rangeStart = 1; // We start at 1, 0 is reserved for backwards compatibility (Legacy support) and negative values are not allowed.
            List<Int32> doids = GetListOfAssignedDisplayObjectIDs();
            if(argReservedDOIDs != null) { // Should always be the case
                doids.AddRange(argReservedDOIDs); // We do not care if the same number is found multiple times.
            }
            while(doids.Contains(rangeStart)) {
                rangeStart += 1 ;
            }
            return rangeStart;
        } 

        void Update() {
            while(asyncLayoutManagerActionsQueue.Count > 0) {
                AsyncLayoutManagerAction asyncLayoutManagerAction = asyncLayoutManagerActionsQueue.Dequeue();
                switch(asyncLayoutManagerAction.asyncLayoutManagerActionType) {
                    case AsyncLayoutManagerActionTypes.PreloadLegacyDisplayObject: {
                        _PreloadLegacyDisplayObject();
                        break;
                    }
                    default: {
                        throw new Exception(String.Format("[ClearVR] Got unexpected asynchronous LayoutManager action {0} on asyncLayoutManagerAction. This is a bug and should be fixed.", asyncLayoutManagerAction));
                    }
                }
            }
            // Register (and Unregister) can happen before the NRP is Loaded (or after it is Unloaded). Therefor we push these requests on a queue that are the handled on the main Update() cycle, provided the NRP is Loaded.
            while(asyncNRPActionsList.Count > 0) {
                NRPAction asyncNRPAction = asyncNRPActionsList.First.Value;
                if(_HandleNRPAction(asyncNRPAction)) {
                    asyncNRPActionsList.RemoveFirst();
                    if(asyncNRPAction.asyncNRPActionType == NRPActionTypes.Wait) {
                        break; // We wait for the next VSync before triggering the next action
                    }
                } else {
                    break; // Unable to handle the NRPAction. We wait for the next VSync to try again.
                }
            }

            _UpdatePendingDisplayObjectControllers();
            if(GetIsNRPLoaded() && mediaPlayerCanAcceptNRPVsyncCalls) {
                // Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, String.Format("{0} - doCOunt: {1}", this.name, doCount));
                bool anyDisplayObjectDescriptionWasLock = false;
                for(int i = 0, size = _activeDisplayObjectControllers.Count; i< size && !anyDisplayObjectDescriptionWasLock; i++) {
                    anyDisplayObjectDescriptionWasLock =_activeDisplayObjectControllers[i].UpdateMDS(_vsyncCounter);
                }
                if (!anyDisplayObjectDescriptionWasLock) {
                    NativeRendererPluginBase.VSyncAsync(_vsyncCounter);
                } else {
#if ENABLE_VSYNC_DEBUG
                    ClearVRLogger.LOGW("Skip scheduling the NRP VSync because at least one DisplayObjectDescription was still locked during VSync nb {0}",_vsyncCounter);
#else 
                    ClearVRLogger.LOGW("Skip scheduling the NRP VSync because at least one DisplayObjectDescription was still locked");
#endif
                }
            }
#if ENABLE_VSYNC_DEBUG
            _vsyncCounter++;
#endif
        }

        private void _UpdatePendingDisplayObjectControllers(){
            while(_pendingDisplayObjectControllers.Count > 0) {
                Tuple<bool, ClearVRDisplayObjectControllerBase> tuple = _pendingDisplayObjectControllers[0];
                if(tuple.Item1) { // true == add, false = remove
                    _activeDisplayObjectControllers.Add(tuple.Item2);
                } else {
                    _activeDisplayObjectControllers.Remove(tuple.Item2);
                }
                _pendingDisplayObjectControllers.RemoveAt(0);
            }
        }

        /// <summary>
        /// Handles the specified NRPAction, if the NRP is in a state that it can handle it.
        /// It is up to the callee to pop the NRPAction in case it was executed.
        /// </summary>
        /// <param name="argNRPAction">The NRPAction to perform</param>
        /// <returns>true if the NRP action was executed, false otherwise.</returns>
        private bool _HandleNRPAction(NRPAction argNRPAction) {
            if(!GetIsNRPLoaded()) {
                return false;
            }
            switch(argNRPAction.asyncNRPActionType) {
                case NRPActionTypes.Wait:
                    break;
                case NRPActionTypes.MarkNRPAsInitialized:
                    _isNRPInitializeCalled = true;
                    break;
                case NRPActionTypes.RegisterClearVRDisplayObjectController:
                    if(_isNRPInitializeCalled) {
                        _RegisterClearVRDisplayObjectController((ClearVRDisplayObjectControllerBase) argNRPAction.payload);
                    } else {
                        return false;
                    }
                    break;
                case NRPActionTypes.UnregisterClearVRDisplayObjectController: 
                    if(_isNRPInitializeCalled) {
                        _UnregisterClearVRDisplayObjectController((ClearVRDisplayObjectControllerBase) argNRPAction.payload);
                    } else {
                        return false;
                    }
                    break;
                default:
                    throw new Exception(String.Format("[ClearVR] Got unexpected asynchronous nrp action {0} on asyncNRPActionsQueue. This is a bug and should be fixed.", argNRPAction));
            }
            return true;
        }

        internal void LoadSync(PlatformOptionsBase argPlatformOptionsBase) {
            _platformOptionsBase = argPlatformOptionsBase;
            CVRNRPLoadParametersStruct cvrNRPLoadParametersStruct = new CVRNRPLoadParametersStruct(NRPBridgeTypesMethods.GetNRPBridgeType(), IntPtr.Zero, _platformOptionsBase);
            IntPtr pCVRNRPLoadParametersStruct = Marshal.AllocHGlobal(Marshal.SizeOf(cvrNRPLoadParametersStruct));
            Marshal.StructureToPtr(cvrNRPLoadParametersStruct, pCVRNRPLoadParametersStruct, false);
            // Load and initialize the native renderer plugin
            nrpID = NativeRendererPluginBase.CVR_NRP_Load(pCVRNRPLoadParametersStruct);
            Marshal.FreeHGlobal(pCVRNRPLoadParametersStruct);
            pCVRNRPLoadParametersStruct = IntPtr.Zero;
            asyncLayoutManagerActionsQueue.Enqueue(new AsyncLayoutManagerAction(AsyncLayoutManagerActionTypes.PreloadLegacyDisplayObject, null));
        }

        // For performance reasons we do not push the Vsync action through the LayoutManager
        internal void InitializeAsync() {
            if(GetIsNRPLoaded()) {
                // Note that initialization is happening on the render thread at "a moment in time"
                NativeRendererPluginBase.InitializeAsync();
                // We wait for three vsyncs before we can Register our mesh. We needs this because the NRP's Initialize is executed async and we need it to complete before we can do this.
                asyncNRPActionsList.AddFirst(new NRPAction(NRPActionTypes.MarkNRPAsInitialized));
                asyncNRPActionsList.AddFirst(new NRPAction(NRPActionTypes.Wait));
                asyncNRPActionsList.AddFirst(new NRPAction(NRPActionTypes.Wait));
                asyncNRPActionsList.AddFirst(new NRPAction(NRPActionTypes.Wait));
                // here we ask all DOC to reregister themselves.
                // DOCs that have already scheduled a Register request will not do that again, this is protected in MaybeRegisterAtLayoutManager()
                var list = _FindAllActiveClearVRDisplayObjectControllers();
                foreach(ClearVRDisplayObjectControllerBase doc in list) {
                    doc.MaybeRegisterAtLayoutManager(); // It is safe to call this multiple times, and we can even call Stop() if Initialize() was never called.
                }
            }
        }

        internal void UnloadSync() {
            _StopAllClearVRDisplayObjectControllers(); // We stop all of them just in case DestroyAsync() was not called.
            if(asyncNRPActionsList.Count > 0) {
                ClearVRLogger.LOGW("Flushing {0} asynchronous NRP actions on asyncNRPActionsList.", asyncNRPActionsList.Count);
            }
            asyncNRPActionsList.Clear(); // We flush any pending requests.
            if(asyncLayoutManagerActionsQueue.Count > 0) {
                ClearVRLogger.LOGW("Flushing {0} asynchronous LayoutManager actions on asyncLayoutManagerActionsQueue.", asyncLayoutManagerActionsQueue.Count);
            }
            asyncLayoutManagerActionsQueue.Clear();
            if(GetIsNRPLoaded()) {
                NativeRendererPluginBase.CVR_NRP_Unload(nrpID);
                nrpID = NRP_ID_NOT_SET;
            }
            _isNRPInitializeCalled = false; // Reset to false.
        }

        internal void DestroyAsync() {
             _StopAllClearVRDisplayObjectControllers();
            if(GetIsNRPLoaded()) {
                NativeRendererPluginBase.DestroyAsync();
            }
        }

        private void _StopAllClearVRDisplayObjectControllers() {
            var list = _FindAllActiveClearVRDisplayObjectControllers();
            foreach(ClearVRDisplayObjectControllerBase doc in list) {
                doc.Stop(); // It is safe to call this multiple times, and we can even call Stop() if Initialize() was never called.
            }
        }

        private ClearVRDisplayObjectControllerBase[] _FindAllActiveClearVRDisplayObjectControllers() {
            return FindObjectsOfType<ClearVRDisplayObjectControllerBase>();
        }

        internal void RegisterCallbackToTheCoreAsync() {
            // DirectRF has been disabled while #5339 is pending a fix (aka "span" crash).
            // if(GetIsNRPLoaded()) {
            //     NativeRendererPluginBase.CVR_NRP_RegisterCallbackToTheCore();
            // }
        }

        internal void RegisterClearVRDisplayObjectControllerSyncMaybe(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
            _HandleNRPActionSyncMaybe(new NRPAction(NRPActionTypes.RegisterClearVRDisplayObjectController, argClearVRDisplayObjectController));
        }
        
        // This should only ever be called if the DOC was registered.
        internal void UnregisterClearVRDisplayObjectControllerSyncMaybe(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
            _HandleNRPActionSyncMaybe(new NRPAction(NRPActionTypes.UnregisterClearVRDisplayObjectController, argClearVRDisplayObjectController));
        }

        private void _HandleNRPActionSyncMaybe(NRPAction argNRPAction) {
            if(!_HandleNRPAction(argNRPAction)) {
                asyncNRPActionsList.AddLast(argNRPAction);
            }
        }
        
        internal Int32 GetDisplayObjectID(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
            if(argClearVRDisplayObjectController == null) {
                return -1;
            }
            int displayObjectID = argClearVRDisplayObjectController.displayObjectID;
            if (displayObjectID != -1) {
                return displayObjectID;
            }
            // Remember that a DisplayObjectMapping (DOM) is NOT uniquely defined by its DisplayObjectController (DOC).
            // One DOC can be linked to multiple DOMs.
            // Here, we are interested in the DOID of a DOC, which is identical across DOMs, hence we are allowed to iterate across all LayoutParameters
            DisplayObjectMapping dom = null;
            foreach(var layoutParameters in layoutParametersList) {
                foreach(var _dom in layoutParameters.displayObjectMappings) {
                    if(_dom.clearVRDisplayObjectController == argClearVRDisplayObjectController) {
                        dom = _dom;
                        break;
                    }
                }
            }
            if(dom != null) {
                return dom.displayObjectID;
            }
           return -1;
        }

        internal ClearVRDisplayObjectControllerBase GetClearVRDisplayObjectControllerByDisplayObjectID(Int32 argDisplayObjectID) {
            // Remember that a DisplayObjectMapping (DOM) is NOT uniquely defined by its DisplayObjectController (DOC).
            // One DOC can be linked to multiple DOMs.
            // Here, we are interested in the DOC of DOID, which is identical across DOMs, hence we are allowed to iterate across all LayoutParameters
            foreach(var layoutParameters in layoutParametersList) {
                foreach(var _dom in layoutParameters.displayObjectMappings) {
                    if(_dom.displayObjectID == argDisplayObjectID) {
                        return _dom.clearVRDisplayObjectController;
                    }
                }
            }
            // We reach this point in case a DisplayObjectController was not (yet) registered to the LayoutManager. 
            // This can happen, for example, when the DOC's gameObject is not active (either intentionally or as a result of the order in which related actions took place).
            // In that case, we iterate our dictionary that keeps track of the DOC <-> DOID mappings. This one is always supposed to be "up-to-date" with the latest known state.
            foreach(var pair in displayObjectIDsMapping) {   
                if(pair.Value == argDisplayObjectID) {
                    return pair.Key;
                }
            }
            return null;
        }

        internal List<ClearVRDisplayObjectControllerBase> GetClearVRDisplayObjectControllersByActiveFeedIndex(Int32 argActiveFeedIndex) {
            List<ClearVRDisplayObjectControllerBase> docs = new List<ClearVRDisplayObjectControllerBase>();
            foreach(var layoutParameters in layoutParametersList) {
                foreach(var _dom in layoutParameters.displayObjectMappings) {
                    if(_dom.clearVRDisplayObjectController.activeFeedIndex == argActiveFeedIndex) {
                        docs.Add(_dom.clearVRDisplayObjectController);
                    }
                }
            }
            return docs;
        }

        // Internally, we pass the raw object around (NOT A COPY).
        // Publicly, one can only work with a COPY of the object.
        internal LayoutParameters GetLayoutParametersByNameNoCopy(String argName) {
            return _GetLayoutParametersByNameNoCopy(argName, false);
        }

        private LayoutParameters _GetLayoutParametersByNameNoCopy(String argName, bool argForceNoLegacySetupCall) {
            if(argName == LEGACY_LAYOUT_NAME && !argForceNoLegacySetupCall) {
                _SetupLegacyLayout(); // Can be safely called multiple times.
            }
            foreach(LayoutParameters lp in layoutParametersList) {
                if(lp.name == argName) {
                    return lp;
                }
            }
            return null;
        }

		/// <summary>
        /// Register that mesh into ClearVRNativeRendererPlugin (as a new object)
        /// This mesh will hold the video texture and its UVs will be updated for every frame.
        /// Note that the actual mesh is calculated in the ClearVRNativeRendererPlugin, the Mesh is mainly
        /// used as a simple interface to use this procedurally generated mesh.
        /// </summary>
        private void _RegisterClearVRDisplayObjectController(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
            if(argClearVRDisplayObjectController == null) {
                ClearVRLogger.LOGW("Trying to register a ClearVRDisplayObjectControllerBase that is null.");
                return;
            }
            if(argClearVRDisplayObjectController.isRegisteredAndInitialized) {
                // Already registered, no need to register again.
                return;
            }
            int displayObjectID = GetDisplayObjectID(argClearVRDisplayObjectController);
            if(displayObjectID < 0) {
                // Hitting this point is perfectly fine. It can happen for example when a DOC is Instantiated but not mapped in any DOM (known by the LayoutManager)
                if(!_notRegisteredDisplayObjectControllers.Contains(argClearVRDisplayObjectController)) {
                    _notRegisteredDisplayObjectControllers.Add(argClearVRDisplayObjectController);
                }
                return;
            } else {
                if(_notRegisteredDisplayObjectControllers.Contains(argClearVRDisplayObjectController)) {
                    _notRegisteredDisplayObjectControllers.Remove(argClearVRDisplayObjectController);
                }
            }
            // First we need to register this new mesh into the NRP and have the NRP allocate a meshDescriptionStruct for us.
            SharedPointersWithSDK argSharedPointersWithSDK = NativeRendererPluginBase.CVR_NRP_RegisterDisplayObject_Wrapped(displayObjectID, argClearVRDisplayObjectController.meshTextureMode, argClearVRDisplayObjectController.gameObject.name);
            if(argSharedPointersWithSDK.displayObjectDescriptorHeaderPtr == IntPtr.Zero) {
                // Unable to register, so we add it back to the list. This is a valid condition when we're shutting down.
                _notRegisteredDisplayObjectControllers.Add(argClearVRDisplayObjectController);
                return;
            }
            if(!_activeDisplayObjectControllers.Contains(argClearVRDisplayObjectController)) {
                _pendingDisplayObjectControllers.Add(new Tuple<bool, ClearVRDisplayObjectControllerBase>(true, argClearVRDisplayObjectController));
            }
            argClearVRDisplayObjectController.Initialize(_platformOptionsBase, argSharedPointersWithSDK, null);
            argClearVRDisplayObjectController.clearVRDisplayObjectEvents.AddListener(CbClearVRDisplayObjectEvent);
        }

		/// <summary>
		/// Unregister a mesh from ClearVRNativeRendererPlugin.
		/// Once unregistered, the mesh will no longer be updated. This function will hide the parent gameobject.
		/// The mesh can be registered again by calling RegisterClearVRDisplayObjectController() or the game object can be destroyed by the app without any risk.
		/// </summary>
		private void _UnregisterClearVRDisplayObjectController(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController) {
			//Ask the NRP to unregister this mesh. It will stop updating the mesh and free the meshDescriptionStruct. The mesh can now be destroyed without any risk.
            if(argClearVRDisplayObjectController == null) {
                ClearVRLogger.LOGW("Trying to unregister a ClearVRDisplayObjectController that is null.");
                return;
            }
            int displayObjectID = GetDisplayObjectID(argClearVRDisplayObjectController);
            if(displayObjectID < 0) {
                // If a DOC has made itself known at the LayoutManager, but if that DOC is not registered in any DOM, it will be added to the _notRegisteredDisplayObjectControllers list.
                // If at some point the DOC is mapped in a DOM, it will be removed from the _notRegisteredDisplayObjectControllers list and registered at the NRP.
                // If this never happened, we can safely pop this DOC, that requested to be unregistered, from that list as well as it has never been registered at the NRP.
                // The latter means that it is also not added to the _activeDisplayObjectControllers list.
                if(_notRegisteredDisplayObjectControllers.Contains(argClearVRDisplayObjectController)) {
                    _notRegisteredDisplayObjectControllers.Remove(argClearVRDisplayObjectController);
                } else {
                    ClearVRLogger.LOGW(String.Format("[ClearVR] Cannot UNREGISTER DisplayObjectID {0}. Not registered?", displayObjectID));
                }
                return;
            }
            if(_notRegisteredDisplayObjectControllers.Contains(argClearVRDisplayObjectController)) {
                _notRegisteredDisplayObjectControllers.Remove(argClearVRDisplayObjectController);
            }
            if(_activeDisplayObjectControllers.Contains(argClearVRDisplayObjectController)) {
                _pendingDisplayObjectControllers.Add(new Tuple<bool, ClearVRDisplayObjectControllerBase>(false, argClearVRDisplayObjectController));
            }
            NativeRendererPluginBase.CVR_NRP_UnregisterDisplayObject(displayObjectID);
            argClearVRDisplayObjectController.clearVRDisplayObjectEvents.RemoveListener(CbClearVRDisplayObjectEvent);
		}

        // argLayoutParameters: remember that this is a COPY of the original.
        // For return value, refer to ClearVRPlayer-equivalent API documentation.
        internal bool AddOrUpdateAndVerifyLayoutParameters(LayoutParameters argLayoutParameters) {
            if(argLayoutParameters == null) {
                return true;
            }
			LayoutParameters currentLayoutInLayoutManager = _GetLayoutParametersByNameNoCopy(argLayoutParameters.name, false);
			if(currentLayoutInLayoutManager != null) {
				// Replace existing item.
				int index = layoutParametersList.IndexOf(currentLayoutInLayoutManager);
				layoutParametersList.Remove(currentLayoutInLayoutManager);
				layoutParametersList.Insert(index, argLayoutParameters);
			} else {
				// Add as a new layout
				layoutParametersList.Add(argLayoutParameters);
			}
			// Verify new layout.
			return VerifyLayoutParameters();
        }

        // argLayoutParameters: remember that this is a COPY of the original.
        // For return value, refer to ClearVRPlayer-equivalent API documentation.
        internal bool RemoveLayoutParameters(LayoutParameters argLayoutParameters) {
            if(argLayoutParameters == null) {
                return true;
            }
			LayoutParameters currentLayoutInLayoutManager = _GetLayoutParametersByNameNoCopy(argLayoutParameters.name, true);
			if(currentLayoutInLayoutManager != null) {
				// Replace existing item.
				layoutParametersList.Remove(currentLayoutInLayoutManager);
			} else {
				return false; // Could not be found.
			}
			// Verify new layout.
			return VerifyLayoutParameters();
        }

        private void CbClearVRDisplayObjectEvent(ClearVRDisplayObjectControllerBase argClearVRDisplayObjectController, ClearVRDisplayObjectEvent argClearVRDisplayObjectEvent) {
            clearVRDisplayObjectEvents.Invoke(argClearVRDisplayObjectController, argClearVRDisplayObjectEvent);
        }

        internal void SetRenderModeOnAllDisplayObjects(RenderModes argNewRenderMode) {
            for(int i = 0, size = _activeDisplayObjectControllers.Count; i < size; i++) {
                _activeDisplayObjectControllers[i].SetRenderMode(argNewRenderMode);
            }
            for(int i = 0, size = _pendingDisplayObjectControllers.Count; i < size; i++) {
                _pendingDisplayObjectControllers[i].Item2.SetRenderMode(argNewRenderMode);
            }
        }

        internal void SetRenderModeOnAllDisplayObjectsConditionally(RenderModes argNewRenderMode, RenderModes argRequiredCurrentRenderMode) {
            for(int i = 0, size = _activeDisplayObjectControllers.Count; i < size; i++) {
                if(_activeDisplayObjectControllers[i].renderMode == argRequiredCurrentRenderMode) {
                    _activeDisplayObjectControllers[i].SetRenderMode(argNewRenderMode);
                }
            }
            for(int i = 0, size = _pendingDisplayObjectControllers.Count; i < size; i++) {
                if(_pendingDisplayObjectControllers[i].Item2.renderMode == argRequiredCurrentRenderMode) {
                    _pendingDisplayObjectControllers[i].Item2.SetRenderMode(argNewRenderMode);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argMediaPlayerCanAcceptNRPVsyncCalls">True if CorePrepared state was reached, false as soon as Stopping is reached</param>
        internal void SetMediaPlayerCanAcceptNRPVSyncs(bool argMediaPlayerCanAcceptNRPVsyncCalls) {
            mediaPlayerCanAcceptNRPVsyncCalls = argMediaPlayerCanAcceptNRPVsyncCalls;
        }

        private void _PreloadLegacyDisplayObject() {
            if(_clearVRLegacyDisplayObject != null) {
                // Nothing to be done, already set-up
                return;
            }
            // Check if the Legacy mesh was already instantiated.
            _clearVRLegacyDisplayObject = GameObject.Find(LEGACY_DISPLAY_OBJECT_NAME);
            if(_clearVRLegacyDisplayObject != null) {
                // Nothing to be done, already set-up
                return;
            }
            // Manually construct the Legacy Display Object.
            _clearVRLegacyDisplayObject = new GameObject(LEGACY_DISPLAY_OBJECT_NAME);
            _clearVRLegacyDisplayObject.SetActive(false);
            // When one tries to override the legacy behaviour during player initialization, we might run into the condition where the LegacyDisplayObject is created before platformOptions are known.
            // In that case, we assume non-OVROverlay playback
            // Note that this usage is not officially supported 
            if(_platformOptionsBase != null && _platformOptionsBase.textureBlitMode.GetIsOVROverlayMode()) {
                _clearVRLegacyDisplayObject.AddComponent<ClearVRDisplayObjectControllerOVROverlay>();
            } else {
                _clearVRLegacyDisplayObject.AddComponent<ClearVRDisplayObjectControllerMesh>();
            }
            _clearVRLegacyDisplayObject.AddComponent<ClearVRLegacyDisplayObjectSupport>();
            _clearVRLegacyDisplayObject.transform.localScale = new Vector3(15, 15, 15); // The default mesh scale has always been 15.
        }

        private void _SetupLegacyLayout() {
            // Do not call GetLayoutParametersByNameNoCopy() as that would cause a circular call
            foreach(LayoutParameters lp in layoutParametersList) {
                if(lp.name == LEGACY_LAYOUT_NAME) {
                    // Already setup
                    return;
                }
            }
            if(_clearVRLegacyDisplayObject == null) {
                // When one tries to override the legacy behaviour during player initialization, we might run into the condition where the LegacyDisplayObject is created before platformOptions are known.
                // It might be that _PreloadLegacyDisplayObject() was not yet called (as it is scheduled on the Update() loop here, see above).
                // Notes:
                // 1. We make the assumption that the callee was calling from the Main Unity thread.
                // 2. This usage is not officially supported
                _PreloadLegacyDisplayObject();
            }
            // Create a single LayoutParameters object.
            LayoutParameters layoutParameters = new LayoutParameters();
            layoutParameters.name = LEGACY_LAYOUT_NAME;
            // Configure a single DOM with backwards-compatible properties.
            DisplayObjectMapping dom = new DisplayObjectMapping(_clearVRLegacyDisplayObject.GetComponent<ClearVRDisplayObjectControllerBase>() as ClearVRDisplayObjectControllerBase, 0, DisplayObjectClassTypes.FullScreen, ContentFormat.Unknown);
            dom.displayObjectID = 0; // 0 is a reserved DOID for backwards compatibility
            // Add the DOM to the LayoutParameters.
            layoutParameters.displayObjectMappings.Add(dom);
            // We CANNOT call AddOrUpdateAndVerifyLayoutParameters(layoutParameters); here are it would cause an infinite loop, ref #5524
            layoutParametersList.Add(layoutParameters);
            VerifyLayoutParameters();
            // Ref. #5445. The legacy DisplayObject must be activated synchronously.
            // This means that this method can only be called from the main unity thread.
            _clearVRLegacyDisplayObject.SetActive(true);
        }

        internal bool GetIsLegacyDisplayObjectActive() {
            return _clearVRLegacyDisplayObject != null ? _clearVRLegacyDisplayObject.activeInHierarchy : false;
        }

#if NET45_OR_GREATER
        // Request  inlining on .NET 4.5+
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private bool GetIsNRPLoaded() {
            return nrpID >= 0;
        }

        void OnDestroy() {
            if(_clearVRLegacyDisplayObject != null) {
                UnityEngine.Object.Destroy(_clearVRLegacyDisplayObject);
                _clearVRLegacyDisplayObject = null;
            }
        }
    }
}