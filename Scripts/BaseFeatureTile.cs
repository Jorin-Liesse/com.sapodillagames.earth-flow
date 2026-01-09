#region Using Statements

using UnityEngine;
using EarthFlow.General;
using NaughtyAttributes;
using System.Collections;

#endregion

namespace EarthFlow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TerrainGeneralTile))]
    public abstract class BaseFeatureTile<T> : MonoBehaviour where T : BaseFeature<T>
    {
        #region Serialize Fields
        #endregion

        #region Private Fields

        private bool _isStarted = false;

        private TerrainGeneralTile _terrainGeneralTile;
        private TerrainGeneral _terrainGeneral;
        private T _baseFeature;

        #endregion

        #region Public Properties

        public TerrainGeneralTile TerrainGeneralTile
        {
            get
            {
                _terrainGeneralTile ??= GetComponent<TerrainGeneralTile>();
                return _terrainGeneralTile;
            }
        }

        public TerrainGeneral TerrainGeneral
        {
            get
            {
                _terrainGeneral ??= transform.parent.GetComponent<TerrainGeneral>();
                return _terrainGeneral;
            }
        }

        public T BaseFeature
        {
            get
            {
                _baseFeature ??= transform.parent.GetComponent<T>();
                return _baseFeature;
            }
        }

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
            
            StartCoroutine(DelayedInitialize());

            TerrainGeneral.OnTick.AddListener(Tick);
            TerrainGeneral.OnTickUpdate.AddListener(Initialize);
        }

        protected virtual void OnDisable()
        {
            if (!isActiveAndEnabled) return;

            CleanUp();

            TerrainGeneral.OnTick.RemoveListener(Tick);
            TerrainGeneral.OnTickUpdate.RemoveListener(Initialize);
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

        public virtual void Initialize()
        {
        }

        public virtual void CleanUp()
        {
        }

        public virtual void Tick()
        {
        }

        #endregion

        #region Private Methods

        private IEnumerator DelayedInitialize()
        {
            yield return null;
            Initialize();
        }

        #endregion

        #region Editor Methods

        public void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_baseFeature.DebugLogs) return;
            Debug.Log($"[{typeof(T).Name}] {message}");
#endif
        }


        #endregion
    }
}
