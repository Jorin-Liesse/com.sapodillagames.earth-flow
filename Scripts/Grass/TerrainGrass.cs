#region Using Statements

using UnityEngine;
using NaughtyAttributes;

#endregion

namespace EarthFlow.Grass
{
    public class TerrainGrass : BaseFeature<TerrainGrass>
    {
        #region Const Fields

        public const int THREAD_GROUP_SIZE = 256;

        #endregion

        #region Serialize Fields

        [SerializeField, BoxGroup("Grass Settings")] private int _instanceCount = 1000;
        [SerializeField, BoxGroup("Grass Settings"), MinMaxSlider(0.0f, 10.0f)] private Vector2 _randomScaleRange = new Vector2(0.5f, 1.5f);
        [SerializeField, BoxGroup("Grass Settings")] private int _cullDistance = 50;
        [SerializeField, BoxGroup("Grass Settings")] private ComputeShader _grassDataComputeShader = null;
        [SerializeField, BoxGroup("Grass Settings")] private ComputeShader _grassCullingComputeShader = null;
        [SerializeField, BoxGroup("Grass Settings")] private GrassPrefab _grassPrefab = null;

        #endregion

        #region Private Fields

        private string _boundsExtentsIDString = "_BoundsExtents";
        private string _boundsCenterIDString = "_BoundsCenter";

        private string _instanceBufferIDString = "_InstanceBuffer";
        private string _visibleIndicesBufferIDString = "_VisibleIndicesBuffer";
        private string _visibleCountIDString = "_VisibleCount";
        private string _cullDistanceSqIDString = "_CullDistanceSq";
        private string _cameraPositionIDString = "_CameraPosition";
        private string _frustumPlanesIDString = "_FrustumPlanes";

        private string _scaleRangeIDString = "_ScaleRange";
        private string _randomSeedIDString = "_RandomSeed";
        private string _instanceCountIDString = "_InstanceCount";

        private string _useInteractionIDString = "_UseInteraction";
        private string _useWindIDString = "_UseWind";
        private string _interactionPointsIDString = "_InteractionPoints";

        private string _grassDataKernelString = "CSGrassData";
        private string _grassCullingKernelString = "CSGrassCulling";

        private int _boundsExtentsID = -1;
        private int _boundsCenterID = -1;

        private int _instanceBufferID = -1;
        private int _visibleIndicesBufferID = -1;
        private int _visibleCountID = -1;
        private int _cullDistanceSqID = -1;
        private int _cameraPositionID = -1;
        private int _frustumPlanesID = -1;

        private int _scaleRangeID = -1;
        private int _randomSeedID = -1;
        private int _instanceCountID = -1;

        private int _useInteractionID = -1;
        private int _useWindID = -1;
        private int _interactionPointsID = -1;

        private int _grassDataKernel = -1;
        private int _grassCullingKernel = -1;

        #endregion

        #region Public Properties

        public int InstanceCount => _instanceCount;
        public Vector2 RandomScaleRange => _randomScaleRange;
        public int CullDistance => _cullDistance;
        public ComputeShader GrassDataComputeShader => _grassDataComputeShader;
        public ComputeShader GrassCullingComputeShader => _grassCullingComputeShader;
        public GrassPrefab GrassPrefab => _grassPrefab;

        public int BoundsExtentsID => _boundsExtentsID;
        public int BoundsCenterID => _boundsCenterID;

        public int InstanceBufferID => _instanceBufferID;
        public int VisibleIndicesBufferID => _visibleIndicesBufferID;
        public int VisibleCountID => _visibleCountID;
        public int CullDistanceSqID => _cullDistanceSqID;
        public int CameraPositionID => _cameraPositionID;
        public int FrustumPlanesID => _frustumPlanesID;

        public int ScaleRangeID => _scaleRangeID;
        public int RandomSeedID => _randomSeedID;
        public int InstanceCountID => _instanceCountID;

        public int UseInteractionID => _useInteractionID;
        public int UseWindID => _useWindID;
        public int InteractionPointsID => _interactionPointsID;

        public int GrassDataKernel => _grassDataKernel;
        public int GrassCullingKernel => _grassCullingKernel;

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            if (!isActiveAndEnabled) return;
            
            base.Awake();

            _boundsExtentsID = Shader.PropertyToID(_boundsExtentsIDString);
            _boundsCenterID = Shader.PropertyToID(_boundsCenterIDString);

            _instanceBufferID = Shader.PropertyToID(_instanceBufferIDString);
            _visibleIndicesBufferID = Shader.PropertyToID(_visibleIndicesBufferIDString);
            _visibleCountID = Shader.PropertyToID(_visibleCountIDString);
            _cullDistanceSqID = Shader.PropertyToID(_cullDistanceSqIDString);
            _cameraPositionID = Shader.PropertyToID(_cameraPositionIDString);
            _frustumPlanesID = Shader.PropertyToID(_frustumPlanesIDString);
            
            _scaleRangeID = Shader.PropertyToID(_scaleRangeIDString);
            _randomSeedID = Shader.PropertyToID(_randomSeedIDString);
            _instanceCountID = Shader.PropertyToID(_instanceCountIDString);
            
            _useInteractionID = Shader.PropertyToID(_useInteractionIDString);
            _useWindID = Shader.PropertyToID(_useWindIDString);
            _interactionPointsID = Shader.PropertyToID(_interactionPointsIDString);

            _grassDataKernel = _grassDataComputeShader.FindKernel(_grassDataKernelString);
            _grassCullingKernel = _grassCullingComputeShader.FindKernel(_grassCullingKernelString);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}
