#region Using Statements

using UnityEngine;
using NaughtyAttributes;
using System.Runtime.InteropServices;

#endregion

namespace EarthFlow.Grass
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GrassInstance
    {
        public Vector3 position; // 12 bytes
        public uint packedData; // 4 bytes (packed rotation and scale)
    }

    public class TerrainGrassTile : BaseFeatureTile<TerrainGrass>
    {
        #region Private Fields

        private GraphicsBuffer[] _argsBuffers = null;
        private GraphicsBuffer _visibleIndicesBuffer = null;
        private GraphicsBuffer _instanceBuffer = null;

        #endregion

        #region Public Properties
        #endregion

        #region Unity Methods

        protected override void Update()
        {
            if (!isActiveAndEnabled) return;
            if (!TerrainGeneralTile.IsDisplayed) return;

            base.Update();
            
            if (TerrainGeneral.Cam == null) return;
            if (_visibleIndicesBuffer == null) return;
            if (_instanceBuffer == null) return;

            int lod = BaseFeature.GrassPrefab.GetCurrentLOD(
                TerrainGeneral.Cam,
                TerrainGeneralTile.Bounds
            );

            if (lod < 0) return;

            UpdateCullingBuffers();
            GraphicsBuffer.CopyCount(_visibleIndicesBuffer, _argsBuffers[lod], sizeof(uint));

            Graphics.DrawMeshInstancedIndirect(
                BaseFeature.GrassPrefab.LodsMeshes[lod],
                0,
                BaseFeature.GrassPrefab.LodsMaterials[lod],
                TerrainGeneralTile.Bounds,
                _argsBuffers[lod],
                0,
                TerrainGeneralTile.MPB
            );
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            TerrainGeneralTile.MPB.SetVector(BaseFeature.ScaleRangeID, BaseFeature.RandomScaleRange);

            SetupLODArgumentBuffers();
            SetupTRSBuffers();
            SetupCullingBuffers();
            SetupDefaultOtherFeatures();
        }

        public override void CleanUp()
        {
            base.CleanUp();

            if (_argsBuffers != null)
                foreach (var b in _argsBuffers)
                    b?.Release();

            _visibleIndicesBuffer?.Release();
            _instanceBuffer?.Release();
        }

        #endregion

        #region Private Methods

        private void SetupDefaultOtherFeatures()
        {
            TerrainGeneralTile.MPB.SetFloat(BaseFeature.UseWindID, 0f);
            TerrainGeneralTile.MPB.SetFloat(BaseFeature.UseInteractionID, 0f);

            GraphicsBuffer interactionPointsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(float) * 4);
            interactionPointsBuffer.SetData(new Vector4[] { Vector4.zero });
            TerrainGeneralTile.MPB.SetBuffer(BaseFeature.InteractionPointsID, interactionPointsBuffer);

            interactionPointsBuffer.Release();
        }

        private void SetupLODArgumentBuffers()
        {
            int lodCount = BaseFeature.GrassPrefab.LodsMeshes.Length;

            _argsBuffers = new GraphicsBuffer[lodCount];

            for (int lod = 0; lod < lodCount; lod++)
            {
                Mesh mesh = BaseFeature.GrassPrefab.LodsMeshes[lod];

                uint[] args = new uint[5]
                {
                    mesh.GetIndexCount(0),
                    (uint)BaseFeature.InstanceCount, // can change per LOD later
                    mesh.GetIndexStart(0),
                    mesh.GetBaseVertex(0),
                    0
                };

                _argsBuffers[lod] = new GraphicsBuffer(
                    GraphicsBuffer.Target.IndirectArguments,
                    1,
                    args.Length * sizeof(uint)
                );

                _argsBuffers[lod].SetData(args);
            }
        }

        private void SetupTRSBuffers()
        {
            _instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, BaseFeature.InstanceCount, Marshal.SizeOf(typeof(GrassInstance)));

            TerrainGeneralTile.MPB.SetBuffer(BaseFeature.InstanceBufferID, _instanceBuffer);

            BaseFeature.GrassDataComputeShader.SetBuffer(BaseFeature.GrassDataKernel, BaseFeature.InstanceBufferID, _instanceBuffer);
            BaseFeature.GrassDataComputeShader.SetVector(BaseFeature.BoundsCenterID, TerrainGeneralTile.Bounds.center);
            BaseFeature.GrassDataComputeShader.SetVector(BaseFeature.BoundsExtentsID, TerrainGeneralTile.Bounds.extents);
            BaseFeature.GrassDataComputeShader.SetVector(BaseFeature.ScaleRangeID, BaseFeature.RandomScaleRange);
            BaseFeature.GrassDataComputeShader.SetInt(BaseFeature.RandomSeedID, TerrainGeneral.RandomSeed);
            BaseFeature.GrassDataComputeShader.SetInt(BaseFeature.InstanceCountID, BaseFeature.InstanceCount);

            int threadGroups = Mathf.CeilToInt(BaseFeature.InstanceCount / (float)TerrainGrass.THREAD_GROUP_SIZE);
            BaseFeature.GrassDataComputeShader.Dispatch(BaseFeature.GrassDataKernel, threadGroups, 1, 1);
        }

        private void SetupCullingBuffers()
        {
            _visibleIndicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, BaseFeature.InstanceCount, sizeof(uint));

            BaseFeature.GrassCullingComputeShader.SetInt(BaseFeature.CullDistanceSqID, BaseFeature.CullDistance * BaseFeature.CullDistance);
            BaseFeature.GrassCullingComputeShader.SetInt(BaseFeature.InstanceCountID, BaseFeature.InstanceCount);

            TerrainGeneralTile.MPB.SetBuffer(BaseFeature.VisibleIndicesBufferID, _visibleIndicesBuffer);
        }

        private void UpdateCullingBuffers()
        {
            _visibleIndicesBuffer.SetCounterValue(0);

            BaseFeature.GrassCullingComputeShader.SetBuffer(BaseFeature.GrassCullingKernel, BaseFeature.InstanceBufferID, _instanceBuffer);
            BaseFeature.GrassCullingComputeShader.SetBuffer(BaseFeature.GrassCullingKernel, BaseFeature.VisibleIndicesBufferID, _visibleIndicesBuffer);

            BaseFeature.GrassCullingComputeShader.SetVector(BaseFeature.CameraPositionID, TerrainGeneral.Cam.transform.position);
            BaseFeature.GrassCullingComputeShader.SetVectorArray(BaseFeature.FrustumPlanesID, TerrainGeneral.FrustumPlaneVectors);

            int groups = Mathf.CeilToInt(BaseFeature.InstanceCount / (float)TerrainGrass.THREAD_GROUP_SIZE);
            BaseFeature.GrassCullingComputeShader.Dispatch(BaseFeature.GrassCullingKernel, groups, 1, 1);
        }

        #endregion
    }
}
