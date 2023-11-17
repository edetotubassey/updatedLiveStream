using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace com.tiledmedia.clearvr {
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    [Serializable]
    /// <summary>
    /// The ClearVRDisplayObjectControllerMesh is the default mesh for rendering a video.
    /// It renders a dynamic mesh which can change its shape every frame.
    /// </summary>
    public class ClearVRDisplayObjectControllerMesh : ClearVRDisplayObjectControllerBase {
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        internal Mesh mesh = null;
        private Mesh _originalMesh = null;
        private const MeshTextureModes _meshTextureMode = MeshTextureModes.UVShuffling;

        // This field is used in ClearVRLayoutParametersPropertyDrawer. Do not remove.
#pragma warning disable 0414
        [HideInInspector] 
        [SerializeField] 
        private int _editorGUI_ID_00 = (int) _meshTextureMode;
#pragma warning restore 0414

        public override MeshTextureModes meshTextureMode { 
            get {
                return _meshTextureMode;
            } 
            set { }
        }
        void Awake() {
            // These components are guarnteed to always be there.
            _meshFilter = this.GetComponent<MeshFilter>();
            _meshRenderer = _meshFilter.GetComponent<MeshRenderer>();
            ControllerAwake();
        }

        override protected void InitializeRendererAndMaterial() {
            _originalMesh = _meshFilter.mesh;
            mesh = new Mesh();
            mesh.name = "Procedural mesh";
            // Initialize with empty vertices/normals/triangles and UVs to allocate the appropriate amount of memory that we will access from the native renderer plugin
            mesh.vertices = new Vector3[_displayObjectDescriptorWrapper.vertexCount];
            mesh.normals = new Vector3[_displayObjectDescriptorWrapper.vertexCount];
            mesh.uv = new Vector2[_displayObjectDescriptorWrapper.vertexCount];
            mesh.uv2 = new Vector2[_displayObjectDescriptorWrapper.vertexCount];
            mesh.triangles = new int[_displayObjectDescriptorWrapper.indexCount];
            // Set proper bounds on this mesh, otherwise it would be culled by the camera (default values would be 0 for all bounds fields as all vertices are 0 at this point)
            mesh.MarkDynamic();
            _meshFilter.mesh = mesh;
            _originalMaterial = _meshRenderer.material;
            if(_originalMaterial != null) {
                _originalShader = _originalMaterial.shader;
            }
            _material = _meshRenderer.material;

            CVRNRPMeshDataPointersStruct cvrNRPMeshDataPointersStruct = new CVRNRPMeshDataPointersStruct(mesh, displayObjectID); 
            IntPtr pCVRNRPMeshDataPointersStruct = Marshal.AllocHGlobal(Marshal.SizeOf(cvrNRPMeshDataPointersStruct));
            Marshal.StructureToPtr(cvrNRPMeshDataPointersStruct, pCVRNRPMeshDataPointersStruct, false);
            // The NRP will clone the pointer synchronously, so we are free to dealloc it after this method returns.
            NativeRendererPluginBase.CVR_NRP_SetMeshDataPointers(pCVRNRPMeshDataPointersStruct);
            Marshal.FreeHGlobal(pCVRNRPMeshDataPointersStruct);
            pCVRNRPMeshDataPointersStruct = IntPtr.Zero;
        }

        internal override void UpdateBounds() {
            base.UpdateBounds();
            Bounds bounds = new Bounds();
            bounds.center = new Vector3(0, 0, 0);
            Vector3 newBounds = new Vector3(_displayObjectDescriptorWrapper.boundsX * transform.localScale.x, _displayObjectDescriptorWrapper.boundsY * transform.localScale.y, _displayObjectDescriptorWrapper.boundsZ * transform.localScale.z);
            if (newBounds.x == 0) {
                newBounds.x = 1;
            }
            if (newBounds.y == 0) {
                newBounds.y = 1;
            }
            if (newBounds.z == 0) {
                newBounds.z = 1;
            }
            bounds.extents = newBounds;
            if (bounds.extents.x == 0 || bounds.extents.y == 0 || (bounds.extents.z == 0 && clearVRMeshType.GetIsOmnidirectional())) {
                ClearVRLogger.LOGW("ClearVRDisplayObjectController bounds are likely invalid. Got: {0}, {1}, {2} for mesh type: {3}", bounds.extents.x, bounds.extents.y, bounds.extents.z, clearVRMeshType);
            }
            if (mesh != null) {
                mesh.bounds = bounds;
            }
        }

        // Enable or disable whether we actually see the mesh that rendering video
        public override void EnableOrDisableMeshRenderer(bool argIsEnabled) {
            _meshRenderer.enabled = argIsEnabled && !forceDisableMeshRendererAtAllTimes;
        }

        public override bool IsMeshRendererEnabled() {
            if (_meshRenderer != null) {
                return _meshRenderer.enabled;
            } 
            return this.GetComponent<MeshFilter>().GetComponent<MeshRenderer>().enabled;
        }

        internal override void Stop() {
            base.Stop();
            MeshFilter _meshFilter = this.GetComponent<MeshFilter>();
            if (mesh != null) {
                UnityEngine.Object.Destroy(mesh);
            }
            mesh = null;
            if(_meshFilter != null) {
                _meshFilter.mesh = _originalMesh;
            }
            if(_meshRenderer != null) {
                _meshRenderer.material = _originalMaterial;
                if(_meshRenderer.material != null) {
                    _meshRenderer.material.shader = _originalShader;
                }
            }
        }
    };
}
