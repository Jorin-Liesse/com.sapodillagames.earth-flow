#region Using Statements

using System.Linq;
using UnityEngine;

#endregion

namespace EarthFlow.Grass
{
    [RequireComponent(typeof(LODGroup))]
    public class GrassPrefab : MonoBehaviour
    {
        #region Serialize Fields

        [SerializeField] private LODGroup _lodGroup = null;

        #endregion

        #region Private Fields

        private LOD[] _lods = null;
        private Mesh[] _meshes = null;
        private Material[] _materials = null;
        private float[] _lodsTransitionDistances = null;

        #endregion

        #region Public Properties

        public LODGroup LodGroup => _lodGroup;
        public int LodsCount => _lodGroup.lodCount;
        public LOD[] Lods
        {
            get
            {
                if (_lods != null && _lods.Count() == _lodGroup.lodCount) return _lods;
                _lods = _lodGroup.GetLODs();
                return _lods;
            }
        }
        public Mesh[] LodsMeshes
        {
            get
            {
                if (_meshes != null && _meshes.Count() == _lodGroup.lodCount) return _meshes;

                Mesh[] meshes = new Mesh[_lodGroup.lodCount];

                LOD[] lods = _lodGroup.GetLODs();

                for (int i = 0; i < _lodGroup.lodCount; i++)
                {
                    if (lods[i].renderers.Length > 0)
                    {
                        MeshFilter meshFilter = lods[i].renderers[0].GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshes[i] = meshFilter.sharedMesh;
                        }
                    }
                }
                return meshes;
            }
        }
        public Material[] LodsMaterials
        {
            get
            {
                if (_materials != null && _materials.Count() == _lodGroup.lodCount) return _materials;

                Material[] materials = new Material[_lodGroup.lodCount];

                LOD[] lods = _lodGroup.GetLODs();

                for (int i = 0; i < _lodGroup.lodCount; i++)
                {
                    if (lods[i].renderers.Length > 0)
                    {
                        MeshRenderer meshRenderer = lods[i].renderers[0].GetComponent<MeshRenderer>();
                        if (meshRenderer != null)
                        {
                            materials[i] = meshRenderer.sharedMaterial;
                        }
                    }
                }
                return materials;
            }
        }
        public float[] LodsTransitionDistances
        {
            get
            {
                if (_lodsTransitionDistances != null && _lodsTransitionDistances.Count() == _lodGroup.lodCount) return _lodsTransitionDistances;

                LOD[] lods = _lodGroup.GetLODs();
                float[] distances = new float[_lodGroup.lodCount];

                for (int i = 0; i < _lodGroup.lodCount; i++)
                {
                    distances[i] = lods[i].screenRelativeTransitionHeight;
                }

                _lodsTransitionDistances = distances;
                return _lodsTransitionDistances;
            }
        }

        #endregion

        #region Unity Methods
        #endregion

        #region Public Methods

        public int GetCurrentLOD(Camera cam, Bounds bounds)
        {
            float distance = Vector3.Distance(cam.transform.position, bounds.center);

            float relativeHeight =
                bounds.size.y /
                (distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f);

            LOD[] lods = _lodGroup.GetLODs();

            // CULLED
            if (relativeHeight < lods[lods.Length - 1].screenRelativeTransitionHeight)
                return -1;
            

            for (int i = 0; i < lods.Length; i++)
            {
                if (relativeHeight >= lods[i].screenRelativeTransitionHeight)
                    return i;
            }

            return lods.Length - 1;
        }


        #endregion

        #region Private Methods
        #endregion

        #region Editor

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_lodGroup == null) _lodGroup = GetComponent<LODGroup>();
        }
#endif

        #endregion
    }
}
