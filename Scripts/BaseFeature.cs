#region Using Statements

using UnityEngine;
using EarthFlow.General;
using NaughtyAttributes;

#endregion

namespace EarthFlow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TerrainGeneral))]
    public abstract class BaseFeature<T> : MonoBehaviour where T : BaseFeature<T>
    {
        #region Serialize Fields

        [SerializeField, BoxGroup("Editor Settings")] private bool _debugLogs = false;

        [SerializeField, BoxGroup("Feature Settings")] private TerrainGeneral _terrainGeneral;

        #endregion

        #region Private Fields

        private bool _isStarted = false;

        #endregion

        #region Public Properties

        public TerrainGeneral TerrainGeneral
        {
            get
            {
                _terrainGeneral ??= GetComponent<TerrainGeneral>();
                return _terrainGeneral;
            }
        }

        public bool DebugLogs => _debugLogs;

        public bool IsStarted => _isStarted;

        #endregion

        #region Unity Methods

        protected virtual void Awake()
        {
            if (!isActiveAndEnabled) return;
        }

        protected virtual void Start()
        {
            if (!isActiveAndEnabled) return;
            _isStarted = true;
        }

        protected virtual void OnEnable()
        {
            if (!isActiveAndEnabled) return;
            TerrainGeneral.NeedUpdate = true;
            TerrainGeneral.NeedRebuild = true;
            TerrainGeneral.LastValueChanged = Time.time;
        }

        protected virtual void OnDisable()
        {
            if (!isActiveAndEnabled) return;
            TerrainGeneral.NeedUpdate = true;
            TerrainGeneral.NeedRebuild = true;
            TerrainGeneral.LastValueChanged = Time.time;
        }

        protected virtual void Update()
        {
            if (!isActiveAndEnabled) return;
        }

        protected virtual void FixedUpdate()
        {
            if (!isActiveAndEnabled) return;
        }

        protected virtual void LateUpdate()
        {
            if (!isActiveAndEnabled) return;
        }

        protected virtual void OnDestroy()
        {
            if (!isActiveAndEnabled) return;
        }

        protected virtual void OnValidate()
        {
            if (!isActiveAndEnabled) return;

            TerrainGeneral.NeedUpdate = true;
            TerrainGeneral.LastValueChanged = Time.time;
        }

        #endregion

        #region Public Methods

        public virtual void Tick()
        {
        }

        #endregion

        #region Private Methods
        #endregion

        #region Editor Methods

        public void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_debugLogs) return;
            Debug.Log($"[{typeof(T).Name}] {message}");
#endif
        }

        #endregion
    }
}
