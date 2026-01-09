#region Using Statements

using UnityEngine;
using NaughtyAttributes;

#endregion

namespace EarthFlow.Wind
{
    public class TerrainWind : BaseFeature<TerrainWind>
    {
        #region Serialize Fields

        [SerializeField, BoxGroup("Wind Settings"), Range(0f, 10f)] private float _windStrength = 1f;
        [SerializeField, BoxGroup("Wind Settings"), Range(0f, 10f)] private float _windSpeed = 0.5f;
        [SerializeField, BoxGroup("Wind Settings"), Range(0f, 10f)] private float _windFrequency = 0.2f;
        [SerializeField, BoxGroup("Wind Settings"), Range(0f, 360f)] private float _windDirection = 0f;

        #endregion

        #region Private Fields

        private string _useWindIDString = "_UseWind";
        private string _windStrengthIDString = "_WindStrength";
        private string _windSpeedIDString = "_WindSpeed";
        private string _frequencyIDString = "_WindFrequency";
        private string _windDirectionIDString = "_WindDirection";

        private int _useWindID = -1;
        private int _windStrengthID = -1;
        private int _windSpeedID = -1;
        private int _frequencyID = -1;
        private int _windDirectionID = -1;

        #endregion

        #region Public Properties

        public float WindStrength => _windStrength;
        public float WindSpeed => _windSpeed;
        public float WindFrequency => _windFrequency;
        public float WindDirection => _windDirection;

        public int UseWindID => _useWindID;
        public int WindStrengthID => _windStrengthID;
        public int WindSpeedID => _windSpeedID;
        public int FrequencyID => _frequencyID;
        public int WindDirectionID => _windDirectionID;

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            if (!isActiveAndEnabled) return;
            
            base.Awake();

            _useWindID = Shader.PropertyToID(_useWindIDString);
            _windStrengthID = Shader.PropertyToID(_windStrengthIDString);
            _windSpeedID = Shader.PropertyToID(_windSpeedIDString);
            _frequencyID = Shader.PropertyToID(_frequencyIDString);
            _windDirectionID = Shader.PropertyToID(_windDirectionIDString);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}
