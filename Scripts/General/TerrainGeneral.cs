#region Using Statements

using UnityEngine;
using NaughtyAttributes;

using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

#endregion

namespace EarthFlow.General
{
    public class TerrainGeneral : BaseFeature<TerrainGeneral>
    {
        #region Const Fields

        public const float FRUSTUM_PADDING = 1.2f;

        #endregion

        #region Serialize Fields

        [SerializeField, BoxGroup("Tick Settings")] private bool _live = false;
        [SerializeField, BoxGroup("Tick Settings"), EnableIf("_live")] private bool _instantlyUpdate = false;
        [SerializeField, BoxGroup("Tick Settings"), EnableIf("_live")] private float _tickInterval = 0.1f;

        [SerializeField, BoxGroup("Tile Settings")] private Vector2Int _gridSize = new Vector2Int(10, 10);
        [SerializeField, BoxGroup("Tile Settings")] private Vector3 _tileSize = new Vector3(10, 1, 10);
        [SerializeField, BoxGroup("Tile Settings")] private TerrainPrefab _terrainPrefab = null;

        #endregion

        #region Private Fields

        private float _previousTickInterval = 0f;
        private Vector3 _previousTileSize = Vector3.zero;
        private Vector2Int _previousGridSize = Vector2Int.zero;

        private Plane[] _frustumPlanes = new Plane[6];
        private Vector4[] _frustumPlaneVectors = new Vector4[6];
        private Matrix4x4 _lastVP;

        private bool _needUpdate = false;
        private bool _needRebuild = false;
        private bool _needNewTickLoop = false;
        private float _lastValueChanged = 0f;

        private string _boundsExtentsIDString = "_BoundsExtents";
        private string _boundsCenterIDString = "_BoundsCenter";

        private int _boundsExtentsID = -1;
        private int _boundsCenterID = -1;

        private int _randomSeed = 0;

        private UnityEvent _onTick = new UnityEvent();
        private UnityEvent _onTickUpdate = new UnityEvent();
        private UnityEvent _onTickRebuild = new UnityEvent();
        private UnityEvent _onTickNewLoop = new UnityEvent();

        #endregion

        #region Public Properties

        public bool NeedUpdate
        {
            get => _needUpdate;
            set => _needUpdate = value;
        }

        public bool NeedRebuild
        {
            get => _needRebuild;
            set => _needRebuild = value;
        }

        public bool NeedNewTickLoop
        {
            get => _needNewTickLoop;
            set => _needNewTickLoop = value;
        }

        public float LastValueChanged
        {
            get => _lastValueChanged;
            set => _lastValueChanged = value;
        }

        public bool Live => _live;
        public bool InstantlyUpdate => _instantlyUpdate;
        public float TickInterval => _tickInterval;

        public Vector2Int GridSize => _gridSize;
        public Vector3 TileSize => _tileSize;
        public TerrainPrefab TerrainPrefab => _terrainPrefab;

        public int BoundsExtentsID => _boundsExtentsID;
        public int BoundsCenterID => _boundsCenterID;

        public int RandomSeed => _randomSeed;

        public Camera Cam => Application.isPlaying ? Camera.main : Camera.current;

        public Plane[] FrustumPlanes => _frustumPlanes;
        public Vector4[] FrustumPlaneVectors => _frustumPlaneVectors;

        public UnityEvent OnTick => _onTick;
        public UnityEvent OnTickUpdate => _onTickUpdate;
        public UnityEvent OnTickRebuild => _onTickRebuild;
        public UnityEvent OnTickNewLoop => _onTickNewLoop;

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            if (!isActiveAndEnabled) return;

            base.Awake();

            _boundsExtentsID = Shader.PropertyToID(_boundsExtentsIDString);
            _boundsCenterID = Shader.PropertyToID(_boundsCenterIDString);

            _randomSeed = (int)System.DateTime.Now.Ticks;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CancelInvoke(nameof(Tick));
            float interval = Mathf.Max(TickInterval, 0.01f);
            InvokeRepeating(nameof(Tick), 0f, interval);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CancelInvoke(nameof(Tick));
        }

        protected override void Update()
        {
            if (!isActiveAndEnabled) return;

            base.Update();

            if (TerrainGeneral.Cam == null) return;
            UpdateFrustumIfNeeded(TerrainGeneral.Cam);
        }

        #endregion

        #region Public Methods

        [Button]
        public void ButtonUpdate()
        {
            NeedUpdate = false;
            _onTickUpdate.Invoke();

            Log("Update Terrain");
        }

        [Button]
        public void ButtonRebuild()
        {
            NeedRebuild = false;
            ClearGrid();
            CreateGrid();
            _onTickRebuild.Invoke();

            Log("Rebuilt Terrain");
        }

        [Button]
        public void ButtonNewTickLoop()
        {
            NeedUpdate = false;
            NeedNewTickLoop = false;

            CancelInvoke(nameof(Tick));
            float interval = Mathf.Max(TickInterval, 0.01f);
            InvokeRepeating(nameof(Tick), 0, interval);

            _onTickNewLoop.Invoke();
            _onTickUpdate.Invoke();

            Log("Started New Tick Loop");
        }

        [Button]
        public void ButtonClear()
        {
            NeedUpdate = false;
            ClearGrid();

            Log("Cleared Terrain");
        }

        public override void Tick()
        {
            if (!Live) return;

            OnTick.Invoke();

            if (_previousTickInterval != TickInterval)
            {
                _previousTickInterval = TickInterval;
                NeedUpdate = true;
                NeedNewTickLoop = true;
                LastValueChanged = Time.time;
            }

            if (_previousTileSize != TileSize)
            {
                _previousTileSize = TileSize;
                NeedUpdate = true;
                NeedRebuild = true;
                LastValueChanged = Time.time;
            }

            if (_previousGridSize != GridSize)
            {
                _previousGridSize = GridSize;
                NeedUpdate = true;
                NeedRebuild = true;
                LastValueChanged = Time.time;
            }

            if (_needUpdate && (TerrainGeneral.InstantlyUpdate || (Time.time - _lastValueChanged) >= TerrainGeneral.TickInterval))
            {
                Log($"{_needRebuild}, {_needNewTickLoop}");
                if (_needRebuild) ButtonRebuild();
                if (_needNewTickLoop) ButtonNewTickLoop();
                if (!_needRebuild && !_needNewTickLoop) ButtonUpdate();
            }
        }

        #endregion

        #region Private Methods

        private void CreateGrid()
        {
            // Calculate total world size
            Vector3 totalSize = new Vector3(
                GridSize.x * TileSize.x,
                TileSize.y,
                GridSize.y * TileSize.z
            );

            // Offset so the grid is centered
            Vector3 offset = new Vector3(
                -totalSize.x * 0.5f + TileSize.x * 0.5f,
                0,
                -totalSize.z * 0.5f + TileSize.z * 0.5f
            );

            Type[] featureTypes = GetAllBaseFeaturesTypes(this);
            Type[] featureTileTypes = GetFeatureTileTypesForFeature(featureTypes);

            for (int i = 0; i < featureTypes.Length; i++)
            {
                Log($"Found feature of type {featureTypes[i].Name}, corresponding tile type {featureTileTypes[i].Name}");
            }

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int z = 0; z < GridSize.y; z++)
                {
                    Vector3 pos = new Vector3(
                        x * TileSize.x,
                        0,
                        z * TileSize.z
                    ) + offset;

                    TerrainPrefab tile = Instantiate(TerrainPrefab, transform);
                    tile.transform.localPosition = pos;
                    tile.transform.localScale = TileSize;

                    SetComponentOfTypeOnTile(tile, featureTileTypes, this);
                }
            }
        }

        private void ClearGrid()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        private void SetComponentOfTypeOnTile(TerrainPrefab tile, Type[] types, TerrainGeneral tg)
        {
            for (int i = 0; i < types.Length; i++)
            {
                Type tileType = types[i];
                if (tileType == null) continue;

                Component tileComp = tile.GetComponent(tileType);
                if (tileComp == null)
                    tileComp = tile.gameObject.AddComponent(tileType);

                Type baseTileType = tileComp.GetType().BaseType;
                if (!baseTileType.IsGenericType) continue;

                Type featureType = baseTileType.GetGenericArguments()[0];

                Component featureComp = tg.GetComponent(featureType);
                if (featureComp == null) continue;

#if UNITY_EDITOR
                EditorUtility.SetDirty(tileComp);
#endif
            }
        }

        private Type[] GetAllBaseFeaturesTypes(TerrainGeneral tg)
        {
            MonoBehaviour[] features = tg.GetComponents<MonoBehaviour>();
            List<Type> baseFeatureTypes = new List<Type>();

            for (int i = 0; i < features.Length; i++)
            {
                Type featureType = features[i].GetType();

                if (!features[i].isActiveAndEnabled)
                    continue;

                if (!IsSubclassOfRawGeneric(typeof(BaseFeature<>), featureType))
                    continue;

                baseFeatureTypes.Add(featureType);
            }

            return baseFeatureTypes.ToArray();
        }

        private Type[] GetFeatureTileTypesForFeature(Type[] featureTypes)
        {
            Type[] results = new Type[featureTypes.Length];
            var allTypes = typeof(BaseFeature<>).Assembly.GetTypes();

            for (int i = 0; i < featureTypes.Length; i++)
            {
                Type feature = featureTypes[i];
                Type foundTile = null;

                foreach (var t in allTypes)
                {
                    if (!t.IsClass || t.IsAbstract) continue;

                    // Must inherit BaseFeatureTile<T> - NOTE: Only 1 generic argument!
                    if (!IsSubclassOfRawGeneric(typeof(BaseFeatureTile<>), t))
                        continue;

                    // Get generic argument (T)
                    Type baseDef = t.BaseType;
                    if (!baseDef.IsGenericType) continue;

                    Type genericArg = baseDef.GetGenericArguments()[0];

                    // Check if this tile matches the feature type
                    if (genericArg == feature)
                    {
                        foundTile = t;
                        break;
                    }
                }

                if (foundTile == null)
                    Debug.LogWarning($"No tile type found for feature {feature.Name}");

                results[i] = foundTile;
            }

            return results;
        }

        private bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        private void UpdateFrustumIfNeeded(Camera cam)
        {
            Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
            if (vp == _lastVP) return;

            _lastVP = vp;
            GeometryUtility.CalculateFrustumPlanes(cam, _frustumPlanes);

            for (int i = 0; i < 6; i++)
            {
                _frustumPlaneVectors[i] = new Vector4(
                    _frustumPlanes[i].normal.x,
                    _frustumPlanes[i].normal.y,
                    _frustumPlanes[i].normal.z,
                    _frustumPlanes[i].distance + TerrainGeneral.FRUSTUM_PADDING
                );
            }
        }

        #endregion

        #region Editor Methods
        #endregion
    }
}
