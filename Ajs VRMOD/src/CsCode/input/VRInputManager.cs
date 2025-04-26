using System;
using System.Numerics;
using System.Collections.Generic;
using AJS_VRMOD.Controllers;
using AJS_VRMOD.Models; // Assuming GameType is in this namespace

namespace VRGameConverter
{
    /// <summary>
    /// Handles integration with VR headsets for head tracking and controllers
    /// </summary>
    public class VRInputManager // Renamed from HeadTracker to reflect broader scope
    {
        private CameraSettings cameraSettings;
        private IVRSystem vrSystem;
        private GameControllerMapping currentControllerMapping;
        private GameType currentGameType;

        public void Configure(CameraSettings settings, DetectedGame game) // Added DetectedGame
        {
            this.cameraSettings = settings;
            this.currentGameType = game.GameType;

            // Initialize VR system based on available hardware
            vrSystem = DetectAndInitializeVRSystem();

            // Initialize controller mapping based on game type
            InitializeControllerMapping(game.GameType);
        }

        private void InitializeControllerMapping(GameType gameType)
        {
            if (gameType == GameType.GTA5)
            {
                currentControllerMapping = new GTA5ControllerMapping();
            }
            else if (gameType == GameType.GTA4)
            {
                // Create a GTA4ControllerMapping class
                // currentControllerMapping = new GTA4ControllerMapping();
            }
            else if (gameType == GameType.HogwartsLegacy)
            {
                // Create a HogwartsLegacyControllerMapping class
                // currentControllerMapping = new HogwartsLegacyControllerMapping();
            }
            else if (gameType == GameType.SpiderMan)
            {
                // Create a SpiderManControllerMapping class
                // currentControllerMapping = new SpiderManControllerMapping();
            }
            else if (gameType == GameType.Cyberpunk2077)
            {
                // Create a Cyberpunk2077ControllerMapping class
                // currentControllerMapping = new Cyberpunk2077ControllerMapping();
            }
            else if (gameType == GameType.RedDeadRedemption2)
            {
                // Create a RedDeadRedemption2ControllerMapping class
                // currentControllerMapping = new RedDeadRedemption2ControllerMapping();
            }
            else if (gameType == GameType.CallOfDutyBlackOps1)
            {
                // Create a CallOfDutyBlackOps1ControllerMapping class
                // currentControllerMapping = new CallOfDutyBlackOps1ControllerMapping();
            }
            else if (gameType == GameType.CallOfDutyBlackOps2)
            {
                // Create a CallOfDutyBlackOps2ControllerMapping class
                // currentControllerMapping = new CallOfDutyBlackOps2ControllerMapping();
            }
            else if (gameType == GameType.CallOfDutyBlackOps3)
            {
                // Create a CallOfDutyBlackOps3ControllerMapping class
                // currentControllerMapping = new CallOfDutyBlackOps3ControllerMapping();
            }
            else if (gameType == GameType.WalkingDeadSaintsSinners)
            {
                // Create a WalkingDeadSaintsSinnersControllerMapping class
                // currentControllerMapping = new WalkingDeadSaintsSinnersControllerMapping();
            }
            else if (gameType == GameType.BatmanArkhamKnight)
            {
                // Create a BatmanArkhamKnightControllerMapping class
                // currentControllerMapping = new BatmanArkhamKnightControllerMapping();
            }
            else if (gameType == GameType.WatchDogs2)
            {
                // Create a WatchDogs2ControllerMapping class
                // currentControllerMapping = new WatchDogs2ControllerMapping();
            }
            else
            {
                currentControllerMapping = null; // Or a default mapping
            }
        }

        private IVRSystem DetectAndInitializeVRSystem()
        {
            // Try to initialize different VR systems in order of preference

            // Try OpenVR (SteamVR)
            try
            {
                return new OpenVRSystem();
            }
            catch (VRInitializationException)
            {
                // Failed to initialize OpenVR, try Oculus
            }

            // Try Oculus
            try
            {
                return new OculusSystem();
            }
            catch (VRInitializationException)
            {
                // Failed to initialize Oculus, try Windows Mixed Reality
            }

            // Try Windows Mixed Reality
            try
            {
                return new WMRSystem();
            }
            catch (VRInitializationException)
            {
                // No VR system available
                throw new VRInitializationException("No compatible VR system found");
            }
        }

        public HeadPose GetHeadPose()
        {
            // Get raw tracking data from VR system
            var rawPose = vrSystem.GetHeadsetPose();

            // Apply calibration and transformation based on game-specific settings
            var transformedPose = TransformRawPose(rawPose);

            return transformedPose;
        }

        private HeadPose TransformRawPose(RawVRPose rawPose)
        {
            // Transform from VR space to game space
            Vector3 position = rawPose.Position;
            Quaternion rotation = rawPose.Rotation;

            // Apply scaling
            position *= cameraSettings.PositionScale;

            // Apply offset
            position += cameraSettings.PositionOffset;

            // Apply rotation adjustment
            rotation = cameraSettings.RotationOffset * rotation;

            return new HeadPose
            {
                Position = position,
                Rotation = rotation
            };
        }

        public Dictionary<VRAction, bool> GetDigitalActionStates(ControllerHand hand)
        {
            if (vrSystem != null && currentControllerMapping != null)
            {
                var controllerState = vrSystem.GetControllerState(hand);
                return currentControllerMapping.GetDigitalActionStates(hand, controllerState);
            }
            return new Dictionary<VRAction, bool>();
        }

        public Dictionary<VRAction, float> GetAnalogActionValues(ControllerHand hand)
        {
            if (vrSystem != null && currentControllerMapping != null)
            {
                var controllerState = vrSystem.GetControllerState(hand);
                return currentControllerMapping.GetAnalogActionValues(hand, controllerState);
            }
            return new Dictionary<VRAction, float>();
        }

        public Dictionary<VRAction, Vector2> GetVector2ActionValues(ControllerHand hand)
        {
            if (vrSystem != null && currentControllerMapping != null)
            {
                var controllerState = vrSystem.GetControllerState(hand);
                return currentControllerMapping.GetVector2ActionValues(hand, controllerState);
            }
            return new Dictionary<VRAction, Vector2>();
        }
    }
}