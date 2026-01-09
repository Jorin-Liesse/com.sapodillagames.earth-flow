#region Using Statements

using UnityEngine;
using NaughtyAttributes;

#endregion

namespace EarthFlow.Wind
{
    public class TerrainWindTile : BaseFeatureTile<TerrainWind>
    {
        #region Serialize Fields
        #endregion

        #region Private Fields
        #endregion

        #region Public Properties
        #endregion

        #region Unity Methods
        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            TerrainGeneralTile.MPB.SetFloat(BaseFeature.UseWindID, 1f);
            TerrainGeneralTile.MPB.SetFloat(BaseFeature.WindStrengthID, BaseFeature.WindStrength);
            TerrainGeneralTile.MPB.SetFloat(BaseFeature.WindSpeedID, BaseFeature.WindSpeed);
            TerrainGeneralTile.MPB.SetFloat(BaseFeature.FrequencyID, BaseFeature.WindFrequency);
            TerrainGeneralTile.MPB.SetVector(BaseFeature.WindDirectionID, new Vector2(Mathf.Cos(BaseFeature.WindDirection * Mathf.Deg2Rad), Mathf.Sin(BaseFeature.WindDirection * Mathf.Deg2Rad)));
        }

        public override void CleanUp()
        {
            base.CleanUp();
        }

        #endregion

        #region Private Methods
        #endregion
    }
}
