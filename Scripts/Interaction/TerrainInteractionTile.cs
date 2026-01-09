#region Using Statements

using UnityEngine;
using NaughtyAttributes;

#endregion

namespace EarthFlow.Interaction
{
    public class TerrainInteractionTile : BaseFeatureTile<TerrainInteraction>
    {
        #region Serialize Fields
        #endregion

        #region Private Fields

        private GraphicsBuffer _interactionPointsBuffer = null;

        #endregion

        #region Public Properties
        #endregion

        #region Unity Methods

        protected override void Update()
        {
            if (!isActiveAndEnabled) return;
            if (!TerrainGeneralTile.IsDisplayed) return;

            base.Update();

            if (_interactionPointsBuffer != null && _interactionPointsBuffer.count != BaseFeature.InteractionMaxPoints)
            {
                _interactionPointsBuffer.Release();
                _interactionPointsBuffer = null;
            }

            if (_interactionPointsBuffer == null)
            {
                _interactionPointsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, BaseFeature.InteractionMaxPoints, sizeof(float) * 4);
            }

            int validCount = 0;
            Vector4[] points = new Vector4[BaseFeature.InteractionMaxPoints];

            for (int i = 0; i < BaseFeature.ValidInteractors.Count && validCount < BaseFeature.InteractionMaxPoints; i++)
            {
                var interactor = BaseFeature.ValidInteractors[i];
        
                Vector3 interactorPos = interactor.transform.position;
                float radius = interactor.InteractionRadius;

                if (IsPointInExpandedBounds(interactorPos, radius))
                {
                    points[validCount] = new Vector4(interactorPos.x, interactorPos.y, interactorPos.z, radius);
                    validCount++;
                }
            }

            if (validCount <= 0)
            {
                _interactionPointsBuffer.SetData(new Vector4[] { Vector4.zero }, 0, 0, 1);

                TerrainGeneralTile.MPB.SetFloat(BaseFeature.UseInteractionID, 1f);
                TerrainGeneralTile.MPB.SetBuffer(BaseFeature.InteractionPointsID, _interactionPointsBuffer);
                TerrainGeneralTile.MPB.SetInt(BaseFeature.InteractionAmountPointsID, 0);
                return;
            }

            _interactionPointsBuffer.SetData(points, 0, 0, validCount);

            TerrainGeneralTile.MPB.SetFloat(BaseFeature.UseInteractionID, 1f);
            TerrainGeneralTile.MPB.SetBuffer(BaseFeature.InteractionPointsID, _interactionPointsBuffer);
            TerrainGeneralTile.MPB.SetInt(BaseFeature.InteractionAmountPointsID, validCount);
            TerrainGeneralTile.MPB.SetFloat(BaseFeature.InteractionStrengthID, BaseFeature.InteractionStrength);
            TerrainGeneralTile.MPB.SetFloat(BaseFeature.InteractionFalloffID, BaseFeature.InteractionFalloff);
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            Update();
        }

        public override void CleanUp()
        {
            base.CleanUp();

            _interactionPointsBuffer?.Release();
        }

        #endregion

        #region Private Methods

        private bool IsPointInExpandedBounds(Vector3 point, float expansionRadius)
        {
            Vector3 closestPoint = TerrainGeneralTile.Bounds.ClosestPoint(point);

            float sqrDistance = (point - closestPoint).sqrMagnitude;
            float sqrRadius = expansionRadius * expansionRadius;

            return sqrDistance <= sqrRadius;
        }


        #endregion
    }
}
