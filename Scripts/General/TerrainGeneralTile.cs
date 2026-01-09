#region Using Statements

using UnityEngine;
using NaughtyAttributes;

#endregion

namespace EarthFlow.General
{
    public class TerrainGeneralTile : BaseFeatureTile<TerrainGeneral>
    {
        #region Serialize Fields
        #endregion

        #region Private Fields

        private Vector3 _previousPosition = Vector3.zero;
        private Quaternion _previousRotation = Quaternion.identity;
        private Vector3 _previousScale = Vector3.one;

        private bool _isDisplayed = true;

        private Bounds _bounds = new Bounds();
        private MaterialPropertyBlock _mpb = null;

        #endregion

        #region Public Properties

        public bool IsDisplayed => _isDisplayed;
        public Bounds Bounds => _bounds;
        public MaterialPropertyBlock MPB
        {
            get
            {
                _mpb ??= new MaterialPropertyBlock();
                return _mpb;
            }
        }

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            if (!isActiveAndEnabled) return;
            
            base.Awake();

            _mpb = new MaterialPropertyBlock();
        }

        protected override void Update()
        {
            if (!isActiveAndEnabled) return;

            base.LateUpdate();

            if (TerrainGeneral.Cam == null) return;
            _isDisplayed = GeometryUtility.TestPlanesAABB(TerrainGeneral.FrustumPlanes, _bounds);
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            CancelInvoke(nameof(Tick));
            InvokeRepeating(nameof(Tick), 0f, BaseFeature.TickInterval);

            _bounds.center = transform.position;
            _bounds.size = TerrainGeneral.TileSize;

            _mpb.SetVector(BaseFeature.BoundsExtentsID, new Vector3(_bounds.extents.x, 0, _bounds.extents.z));
            _mpb.SetVector(BaseFeature.BoundsCenterID, _bounds.center);
        }

        public override void CleanUp()
        {
            base.CleanUp();

            _mpb?.Clear();

            CancelInvoke(nameof(Tick));
        }

        public override void Tick()
        {
            if (_previousPosition != transform.position)
            {
                _previousPosition = transform.position;
                OnValidate();
            }

            if (_previousRotation != transform.rotation)
            {
                _previousRotation = transform.rotation;
                OnValidate();
            }

            if (_previousScale != transform.localScale)
            {
                _previousScale = transform.localScale;
                OnValidate();
            }
        }

        #endregion

        #region Private Methods
        #endregion

        #region Editor Methods
#if UNITY_EDITOR

        void OnDrawGizmos()
        {
            if (_isDisplayed) Gizmos.color = Color.green;
            else Gizmos.color = Color.blue;

            Gizmos.DrawWireCube(transform.position, BaseFeature.TileSize);
        }

#endif
        #endregion
    }
}
