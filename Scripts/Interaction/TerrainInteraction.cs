#region Using Statements

using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

#endregion

namespace EarthFlow.Interaction
{
    public class TerrainInteraction : BaseFeature<TerrainInteraction>
    {
        #region Serialize Fields

        [SerializeField, BoxGroup("Interaction Settings"), Range(0, 5)] private float _interactionStrength = 1f;
        [SerializeField, BoxGroup("Interaction Settings"), Range(0, 1)] private float _interactionFalloff = 1f;
        [SerializeField, BoxGroup("Interaction Settings"), Range(0, 100)] private int _interactionMaxPoints = 10;

        #endregion

        #region Private Fields

        private string _useInteractionIDString = "_UseInteraction";
        private string _interactionAmountPointsIDString = "_InteractionAmountPoints";
        private string _interactionPointsIDString = "_InteractionPoints";
        private string _interactionStrengthIDString = "_InteractionStrength";
        private string _interactionFalloffIDString = "_InteractionFalloff";

        private int _useInteractionID = -1;
        private int _interactionAmountPointsID = -1;
        private int _interactionPointsID = -1;
        private int _interactionStrengthID = -1;
        private int _interactionFalloffID = -1;

        private List<Interactor> _validInteractors = new List<Interactor>();

        #endregion

        #region Public Properties

        public float InteractionStrength => _interactionStrength;
        public float InteractionFalloff => _interactionFalloff;
        public int InteractionMaxPoints => _interactionMaxPoints;

        public int UseInteractionID => _useInteractionID;
        public int InteractionAmountPointsID => _interactionAmountPointsID;
        public int InteractionPointsID => _interactionPointsID;
        public int InteractionStrengthID => _interactionStrengthID;
        public int InteractionFalloffID => _interactionFalloffID;

        public List<Interactor> ValidInteractors => _validInteractors;

        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            if (!isActiveAndEnabled) return;
            
            base.Awake();

            _useInteractionID = Shader.PropertyToID(_useInteractionIDString);
            _interactionAmountPointsID = Shader.PropertyToID(_interactionAmountPointsIDString);
            _interactionPointsID = Shader.PropertyToID(_interactionPointsIDString);
            _interactionStrengthID = Shader.PropertyToID(_interactionStrengthIDString);
            _interactionFalloffID = Shader.PropertyToID(_interactionFalloffIDString);
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}
