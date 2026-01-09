#region Using Statements

using System.Collections.Generic;
using UnityEngine;

#endregion

namespace EarthFlow.Interaction
{
    [ExecuteAlways]
    public class Interactor : MonoBehaviour
    {
        #region Serialize Fields

        [SerializeField] private float _interactionRadius = 1f;

        #endregion

        #region Private Fields
        #endregion

        #region Public Properties

        public float InteractionRadius => _interactionRadius;

        #endregion

        #region Unity Methods

        void OnEnable()
        {
            if (!isActiveAndEnabled) return;

            TerrainInteraction[] terrainInteraction = FindObjectsByType<TerrainInteraction>(FindObjectsSortMode.None);

            foreach (var interaction in terrainInteraction)
            {
                if (!interaction.ValidInteractors.Contains(this))
                {
                    interaction.ValidInteractors.Add(this);
                }
            }
        }

        void OnDisable()
        {
            if (!isActiveAndEnabled) return;

            TerrainInteraction[] terrainInteraction = FindObjectsByType<TerrainInteraction>(FindObjectsSortMode.None);
            foreach (var interaction in terrainInteraction)
            {
                if (interaction.ValidInteractors.Contains(this))
                {
                    interaction.ValidInteractors.Remove(this);
                }
            }
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion

        #region Editor Methods
#if UNITY_EDITOR

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
        }

#endif
        #endregion
    }
}
