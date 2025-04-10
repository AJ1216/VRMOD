using System;
using System.Collections.Generic;
using System.Numerics;

namespace VRGameConverter.OpenWorld
{
    /// <summary>
    /// Specialized mapping system for complex open-world games
    /// </summary>
    public class OpenWorldVRMapper
    {
        private GameProfile gameProfile;
        private CameraManager cameraManager;
        private MovementSystem movementSystem;
        private InteractionSystem interactionSystem;
        private VehicleHandler vehicleHandler;
        private CombatSystem combatSystem;
        private UIManager uiManager;
        
        public OpenWorldVRMapper(GameProfile profile)
        {
            this.gameProfile = profile;
            
            // Initialize subsystems based on game type
            cameraManager = new CameraManager(profile.GameType);
            movementSystem = new MovementSystem(profile.GameType);
            interactionSystem = new InteractionSystem(profile.GameType);
            vehicleHandler = new VehicleHandler(profile.GameType);
            combatSystem = new CombatSystem(profile.GameType);
            uiManager = new UIManager(profile.GameType);
        }
        
        public void Initialize()
        {
            // Configure each system
            cameraManager.Configure(gameProfile.CameraSettings);
            movementSystem.Configure(gameProfile.MovementSettings);
            interactionSystem.Configure(gameProfile.InteractionSettings);
            vehicleHandler.Configure(gameProfile.VehicleSettings);
            combatSystem.Configure(gameProfile.CombatSettings);
            uiManager.Configure(gameProfile.UISettings);
            
            // Memory scanning to find critical game functions
            ScanGameMemoryForHooks();
        }
        
        private void ScanGameMemoryForHooks()
        {
            // Scan game memory to find key functions and data structures
            var scanner = new MemoryScanner(gameProfile.ExecutablePath);
            
            // Scan for camera control functions
            var cameraFunctions = scanner.FindFunctions(gameProfile.CameraSignatures);
            cameraManager.SetHookTargets(cameraFunctions);
            
            // Scan for movement functions
            var movementFunctions = scanner.FindFunctions(gameProfile.MovementSignatures);
            movementSystem.SetHookTargets(movementFunctions);
            
            // Scan for interaction functions
            var interactionFunctions = scanner.FindFunctions(gameProfile.InteractionSignatures);
            interactionSystem.SetHookTargets(interactionFunctions);
            
            // Scan for vehicle control functions (for GTA, etc.)
            var vehicleFunctions = scanner.FindFunctions(gameProfile.VehicleSignatures);
            vehicleHandler.SetHookTargets(vehicleFunctions);
            
            // Scan for combat functions
            var combatFunctions = scanner.FindFunctions(gameProfile.CombatSignatures);
            combatSystem.SetHookTargets(combatFunctions);
            
            // Scan for UI rendering functions
            var uiFunctions = scanner.FindFunctions(gameProfile.UISignatures);
            uiManager.SetHookTargets(uiFunctions);
        }
        
        public void Start()
        {
            // Activate all subsystems
            cameraManager.Activate();
            movementSystem.Activate();
            interactionSystem.Activate();
            vehicleHandler.Activate();
            combatSystem.Activate();
            uiManager.Activate();
        }
        
        public void Update(HeadPose headPose, ControllerState leftController, ControllerState rightController)
        {
            // Update all subsystems with the latest VR input
            cameraManager.Update(headPose);
            movementSystem.Update(headPose, leftController, rightController);
            interactionSystem.Update(headPose, leftController, rightController);
            vehicleHandler.Update(headPose, leftController, rightController);
            combatSystem.Update(headPose, leftController, rightController);
            uiManager.Update(headPose);
        }
    }
    
    /// <summary>
    /// Manages camera conversion from third-person to first-person VR
    /// </summary>
    public class CameraManager
    {
        private GameType gameType;
        private CameraSettings settings;
        private Dictionary<string, IntPtr> hookTargets;
        private bool isActive = false;
        
        // Special settings for different perspective modes
        private bool isFirstPerson = false;
        private Vector3 thirdPersonOffset = new Vector3(0, 1.7f, -0.5f);
        
        public CameraManager(GameType gameType)
        {
            this.gameType = gameType;
            
            // Set default camera behavior based on game type
            switch (gameType)
            {
                case GameType.GTA5:
                    // GTA has both first and third person modes
                    thirdPersonOffset = new Vector3(0, 1.6f, -0.5f);
                    break;
                case GameType.SpiderMan:
                    // Spider-Man is primarily third-person
                    thirdPersonOffset = new Vector3(0, 1.7f, -0.8f);
                    break;
                case GameType.HogwartsLegacy:
                    // Hogwarts Legacy has adjustable camera distance
                    thirdPersonOffset = new Vector3(0, 1.6f, -0.6f);
                    break;
            }
        }
        
        public void Configure(CameraSettings settings)
        {
            this.settings = settings;
            
            // Apply custom offsets if provided
            if (settings.CustomThirdPersonOffset != Vector3.Zero)
            {
                thirdPersonOffset = settings.CustomThirdPersonOffset;
            }
        }
        
        public void SetHookTargets(Dictionary<string, IntPtr> targets)
        {
            this.hookTargets = targets;
        }
        
        public void Activate()
        {
            if (isActive) return;
            
            // Hook into game camera control functions
            if (hookTargets.TryGetValue("UpdateCamera", out var updateCameraFunc))
            {
                // Install hook for camera update function
                InstallHook(updateCameraFunc, UpdateCameraHook);
            }
            
            if (hookTargets.TryGetValue("SetCameraMode", out var setCameraModeFunc))
            {
                // Install hook for camera mode switching
                InstallHook(setCameraModeFunc, SetCameraModeHook);
            }
            
            isActive = true;
        }
        
        private void InstallHook(IntPtr targetFunction, Delegate hookFunction)
        {
            // Implementation would use a library like EasyHook to install function hooks
            Console.WriteLine($"Installing hook at {targetFunction} with function {hookFunction.Method.Name}");
        }
        
        private void UpdateCameraHook(IntPtr gameCamera, float deltaTime)
        {
            // This would be called instead of the game's camera update function
            // We would modify the camera parameters for VR
            
            // Example implementation (pseudocode):
            if (isFirstPerson)
            {
                // In first-person mode, directly use the HMD orientation
                // gameCamera->orientation = headPose.Rotation;
            }
            else
            {
                // In third-person mode, position the camera at a fixed offset from character
                // Vector3 characterPosition = GetCharacterPosition();
                // gameCamera->position = characterPosition + thirdPersonOffset;
                
                // Orient based on a combination of character direction and HMD rotation
                // gameCamera->orientation = characterOrientation * headPose.Rotation;
            }
        }
        
        private void SetCameraModeHook(IntPtr gameCamera, int mode)
        {
            // Intercept camera mode changes
            // mode: 0 = third-person, 1 = first-person, etc. (game-specific)
            
            isFirstPerson = (mode == 1);
            
            // Call original function or apply our own camera mode
            // originalSetCameraMode(gameCamera, mode);
        }
        
        public void Update(HeadPose headPose)
        {
            // Update camera based on head tracking
            // This is called from our main update loop
            
            // Implementation will depend on the specific game and how we're hooking into it
        }
        
        public void TogglePerspective()
        {
            // Toggle between first and third person
            isFirstPerson = !isFirstPerson;
        }
    }
    
    /// <summary>
    /// Handles character movement conversion for VR
    /// </summary>
    public class MovementSystem
    {
        private GameType gameType;
        private MovementSettings settings;
        private Dictionary<string, IntPtr> hookTargets;
        private bool isActive = false;
        
        // Movement control variables
        private Vector3 movementDirection = Vector3.Zero;
        private bool isSprinting = false;
        private bool isCrouching = false;
        private bool isJumping = false;
        
        // For games like Spider-Man or other traversal-heavy titles
        private bool isClimbing = false;
        private bool isSwinging = false;
        private bool isGliding = false;
        
        public MovementSystem(GameType gameType)
        {
            this.gameType = gameType;
        }
        
        public void Configure(MovementSettings settings)
        {
            this.settings = settings;
        }
        
        public void SetHookTargets(Dictionary<string, IntPtr> targets)
        {
            this.hookTargets = targets;
        }
        
        public void Activate()
        {
            if (isActive) return;
            
            // Hook character movement functions
            if (hookTargets.TryGetValue("UpdateCharacterMovement", out var moveFunc))
            {
                InstallHook(moveFunc, UpdateMovementHook);
            }
            
            // Hook special movement abilities
            if (gameType == GameType.SpiderMan)
            {
                if (hookTargets.TryGetValue("WebSwing", out var swingFunc))
                {
                    InstallHook(swingFunc, WebSwingHook);
                }
            }
            else if (gameType == GameType.HogwartsLegacy)
            {
                if (hookTargets.TryGetValue("BroomFlight", out var broomFunc))
                {
                    InstallHook(broomFunc, BroomFlightHook);
                }
            }
            
            isActive = true;
        }
        
        private void InstallHook(IntPtr targetFunction, Delegate hookFunction)
        {
            // Implementation would use a library like EasyHook
            Console.WriteLine($"Installing hook at {targetFunction} with function {hookFunction.Method.Name}");
        }
        
        private void UpdateMovementHook(IntPtr character, Vector3 direction, float speed)
        {
            // Called instead of the game's character movement function
            
            // Apply our VR-derived movement direction and speed
            // character->moveDirection = movementDirection;
            // character->moveSpeed = isSprinting ? settings.SprintSpeed : settings.WalkSpeed;
            
            // Apply jumping/crouching if active
            // if (isJumping) character->Jump();
            // if (isCrouching) character->Crouch();
        }
        
        private void WebSwingHook(IntPtr character, Vector3 direction, float speed)
        {
            // Special handler for Spider-Man web swinging
            if (isSwinging)
            {
                // Modify web swinging based on VR controller input
                // Vector3 swingDirection = CalculateSwingDirectionFromControllers();
                // character->WebSwingDirection = swingDirection;
            }
        }
        
        private void BroomFlightHook(IntPtr character, Vector3 direction, float speed)
        {
            // Special handler for Hogwarts Legacy broom flight
            // Similar to web swinging but with different physics
        }
        
        public void Update(HeadPose headPose, ControllerState leftController, ControllerState rightController)
        {
            // Map VR controller input to character movement
            
            // Get movement direction from left thumbstick
            movementDirection = new Vector3(
                leftController.ThumbstickPosition.X,
                0,
                leftController.ThumbstickPosition.Y
            );
            
            // Normalize and apply HMD orientation to movement direction
            if (movementDirection != Vector3.Zero)
            {
                movementDirection = Vector3.Normalize(movementDirection);
                
                // Rotate movement direction based on head rotation (but only yaw component)
                Quaternion headYaw = ExtractYawRotation(headPose.Rotation);
                movementDirection = Vector3.Transform(movementDirection, headYaw);
            }
            
            // Map other controller inputs to movement actions
            isSprinting = leftController.ThumbstickPressed;
            isJumping = rightController.TriggerPressed;
            isCrouching = rightController.GripPressed;
            
            // Special movement for specific games
            if (gameType == GameType.SpiderMan)
            {
                isSwinging = leftController.TriggerPressed && rightController.TriggerPressed;
            }
        }
        
        private Quaternion ExtractYawRotation(Quaternion rotation)
        {
            // Extract just the yaw component (rotation around Y axis) from a quaternion
            
            // Convert to euler angles
            Vector3 euler = QuaternionToEuler(rotation);
            
            // Create a new quaternion with only the yaw component
            return CreateFromYaw(euler.Y);
        }
        
        private Vector3 QuaternionToEuler(Quaternion q)
        {
            // Convert quaternion to euler angles (roll, pitch, yaw)
            
            float roll = MathF.Atan2(2 * (q.W * q.X + q.Y * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y));
            float pitch = MathF.Asin(2 * (q.W * q.Y - q.Z * q.X));
            float yaw = MathF.Atan2(2 * (q.W * q.Z + q.X * q.Y), 1 - 2 * (q.Y * q.Y + q.Z * q.Z));
            
            return new Vector3(roll, pitch, yaw);
        }
        
        private Quaternion CreateFromYaw(float yaw)
        {
            // Create quaternion from yaw angle
            return Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        }
    }
    
    /// <summary>
    /// Handles interactions with the game world
    /// </summary>
    public class InteractionSystem
    {
        private GameType gameType;
        private InteractionSettings settings;
        private Dictionary<string, IntPtr> hookTargets;
        private bool isActive = false;
        
        public InteractionSystem(GameType gameType)
        {
            this.gameType = gameType;
        }
        
        public void Configure(InteractionSettings settings)
        {
            this.settings = settings;
        }
        
        public void SetHookTargets(Dictionary<string, IntPtr> targets)
        {
            this.hookTargets = targets;
        }
        
        public void Activate()
        {
            if (isActive) return;
            
            // Hook interaction functions
            if (hookTargets.TryGetValue("InteractWithObject", out var interactFunc))
            {
                InstallHook(interactFunc, InteractionHook);
            }
            
            isActive = true;
        }
        
        private void InstallHook(IntPtr targetFunction, Delegate hookFunction)
        {
            // Implementation using hooking library
        }
        
        private void InteractionHook(IntPtr character, IntPtr targetObject)
        {
            // Called instead of the game's interaction function
            
            // Implement VR-specific interaction logic
            // if (IsPointingAtObject(targetObject)) {
            //     TriggerInteraction(character, targetObject);
            // }
        }
        
        public void Update(HeadPose headPose, ControllerState leftController, ControllerState rightController)
        {
            // Handle interaction inputs from controllers
            
            // Ray casting from controllers for pointing-based interaction
            var leftRay = CalculateRayFromController(headPose, "left");
            var rightRay = CalculateRayFromController(headPose, "right");
            
            // Check for interactions
            bool leftInteracting = leftController.TriggerPressed;
            bool rightInteracting = rightController.TriggerPressed;
            
            if (leftInteracting)
            {
                // Trigger interaction with object hit by left ray
                // TriggerInteractionAtRay(leftRay);
            }
            
            if (rightInteracting)
            {
                // Trigger interaction with object hit by right ray
                // TriggerInteractionAtRay(rightRay);
            }
        }
        
        private Ray CalculateRayFromController(HeadPose headPose, string controller)
        {
            // Calculate a ray originating from the controller position in the direction it's pointing
            // Implementation depends on how we're tracking controllers
            
            // Placeholder
            return new Ray();
        }
    }
    
    /// <summary>
    /// Special handler for vehicle control in games like GTA
    /// </summary>
    public class VehicleHandler
    {
        private GameType gameType;
        private VehicleSettings settings;
        private Dictionary<string, IntPtr> hookTargets;
        private bool isActive = false;
        
        // Vehicle state
        private bool isInVehicle = false;
        private VehicleType currentVehicleType = VehicleType.None;
        
        public VehicleHandler(GameType gameType)
        {
            this.gameType = gameType;
        }
        
        public void Configure(VehicleSettings settings)
        {
            this.settings = settings;
        }
        
        public void SetHookTargets(Dictionary<string, IntPtr> targets)
        {
            this.hookTargets = targets;
        }
        
        public void Activate()
        {
            if (isActive) return;
            
            // Only relevant for games with vehicles
            if (gameType == GameType.GTA5 || gameType == GameType.GTA4)
            {
                if (hookTargets.TryGetValue("DriveVehicle", out var driveFunc))
                {
                    InstallHook(driveFunc, DriveVehicleHook);
                }
                
                if (hookTargets.TryGetValue("EnterVehicle", out var enterFunc))
                {
                    InstallHook(enterFunc, EnterVehicleHook);
                }
                
                if (hookTargets.TryGetValue("ExitVehicle", out var exitFunc))
                {
                    InstallHook(exitFunc, ExitVehicleHook);
                }
            }
            
            isActive = true;
        }
        
        private void InstallHook(IntPtr targetFunction, Delegate hookFunction)
        {
            // Implementation using hooking library
        }
        
        private void DriveVehicleHook(IntPtr vehicle, float throttle, float brake, float steering)
        {
            // Replace the game's vehicle control function
            
            // Apply VR-derived controls
            // vehicle->throttle = vrThrottle;
            // vehicle->brake = vrBrake;
            // vehicle->steering = vrSteering;
        }
        
        private void EnterVehicleHook(IntPtr character, IntPtr vehicle, int seat)
        {
            // Called when character enters a vehicle
            
            isInVehicle = true;
            
            // Determine vehicle type
            // currentVehicleType = DetermineVehicleType(vehicle);
            
            // Apply appropriate VR camera positioning
            // AdjustCameraForVehicle(currentVehicleType);
        }
        
        private void ExitVehicleHook(IntPtr character, IntPtr vehicle)
        {
            // Called when character exits a vehicle
            
            isInVehicle = false;
            currentVehicleType = VehicleType.None;
            
            // Reset VR camera positioning
            // ResetCameraPosition();
        }
        
        public void Update(HeadPose headPose, ControllerState leftController, ControllerState rightController)
        {
            if (!isInVehicle) return;
            
            // Map controller inputs to vehicle controls
            float throttle = 0, brake = 0, steering = 0;
            
            // Different control schemes based on vehicle type
            switch (currentVehicleType)
            {
                case VehicleType.Car:
                    // Use triggers for throttle/brake
                    throttle = rightController.TriggerValue;
                    brake = leftController.TriggerValue;
                    // Use joystick for steering
                    steering = leftController.ThumbstickPosition.X;
                    break;
                    
                case VehicleType.Motorcycle:
                    // Similar to car but with different ergonomics
                    throttle = rightController.TriggerValue;
                    brake = leftController.TriggerValue;
                    // Use controller rotation for steering
                    // steering = CalculateSteeringFromControllerRotation();
                    break;
                    
                case VehicleType.Aircraft:
                    // More complex control scheme for aircraft
                    // Use joysticks for pitch/roll/yaw
                    break;
            }
            
            // Update the vehicle controls
            // ApplyVehicleControls(throttle, brake, steering);
        }
    }
    
    /// <summary>
    /// Handles combat mechanics for VR
    /// </summary>
    public class CombatSystem
    {
        private GameType gameType;
        private CombatSettings settings;
        private Dictionary<string, IntPtr> hookTargets;
        private bool isActive = false;
        
        public CombatSystem(GameType gameType)
        {
            this.gameType = gameType;
        }
        
        public void Configure(CombatSettings settings)
        {
            this.settings = settings;
        }
        
        public void SetHookTargets(Dictionary<string, IntPtr> targets)
        {
            this.hookTargets = targets;
        }
        
        public void Activate()
        {
            if (isActive) return;
            
            // Hook combat functions
            if (hookTargets.TryGetValue("MeleeAttack", out var meleeFunc))
            {
                InstallHook(meleeFunc, MeleeAttackHook);
            }
            
            if (hookTargets.TryGetValue("RangedAttack", out var rangedFunc))
            {
                InstallHook(rangedFunc, RangedAttackHook);
            }
            
            // For Hogwarts Legacy
            if (gameType == GameType.HogwartsLegacy)
            {
                if (hookTargets.TryGetValue("CastSpell", out var spellFunc))
                {
                    InstallHook(spellFunc, CastSpellHook);
                }
            }
            
            isActive = true;
        }
        
        private void InstallHook(IntPtr targetFunction, Delegate hookFunction)
        {
            // Implementation using hooking library
        }
        
        private void MeleeAttackHook(IntPtr character, int attackType)
        {
            // Replace the game's melee attack function
            
            // Use VR controller motion to determine attack type
            // int vrAttackType = DetermineAttackFromControllerMotion();
            
            // Trigger the appropriate attack
            // character->PerformAttack(vrAttackType);
        }
        
        private void RangedAttackHook(IntPtr character, Vector3 targetDirection, float power)
        {
            // Replace the game's ranged attack function
            
            // Use controller pointing direction
            // Vector3 vrTargetDirection = CalculateDirectionFromController();
            
            // Use trigger pressure for power
            // float vrPower = rightController.TriggerValue;
            
            // Fire the ranged attack
            // character->FireRangedAttack(vrTargetDirection, vrPower);
        }
        
        private void CastSpellHook(IntPtr character, int spellType, Vector3 targetDirection)
        {
            // Special handler for Hogwarts Legacy spell casting
            
            // Use controller gesture to determine spell
            // int vrSpellType = DetermineSpellFromControllerGesture();
            
            // Use controller pointing for targeting
            // Vector3 vrTargetDirection = CalculateDirectionFromController();
            
            // Cast the spell
            // character->CastSpell(vrSpellType, vrTargetDirection);
        }
        
        public void Update(HeadPose headPose, ControllerState leftController, ControllerState rightController)
        {
            // Track controller movements for gesture recognition
            // UpdateControllerHistory(leftController, rightController);
            
            // Check for attack triggers
            bool meleeAttackTriggered = leftController.GripPressed || rightController.GripPressed;
            bool rangedAttackTriggered = leftController.TriggerPressed || rightController.TriggerPressed;
            
            if (meleeAttackTriggered)
            {
                // Determine attack type from controller motion
                // int attackType = RecognizeMeleeAttackGesture();
                // TriggerMeleeAttack(attackType);
            }
            
            if (rangedAttackTriggered)
            {
                // Get aiming direction from controller
                // Vector3 aimDirection = GetControllerAimDirection();
                // TriggerRangedAttack(aimDirection);
            }
            
            // Special handling for Hogwarts Legacy
            if (gameType == GameType.HogwartsLegacy)
            {
                // Check for spell casting gestures
                // int spellType = RecognizeSpellGesture();
                // if (spellType != SpellType.None) {
                //     TriggerSpellCast(spellType);
                // }
            }
        }
    }
    
    /// <summary>
    /// Manages the UI adaptation for VR
    /// </summary>
    public class UIManager
    {
        private GameType gameType;
        private UISettings settings;
        private Dictionary<string, IntPtr> hookTargets;
        private bool isActive = false;
        
        public UIManager(GameType gameType)
        {
            this.gameType = gameType;
        }
        
        public void Configure(UISettings settings)
        {
            this.settings = settings;
        }
        
        public void SetHookTargets(Dictionary<string, IntPtr> targets)
        {
            this.hookTargets = targets;
        }
        
        public void Activate()
        {
            if (isActive) return;
            
            // Hook UI rendering functions
            if (hookTargets.TryGetValue("RenderUI", out var renderUIFunc))
            {
                InstallHook(renderUIFunc, RenderUIHook);
            }
            
            if (hookTargets.TryGetValue("ShowMenu", out var showMenuFunc))
            {
                InstallHook(showMenuFunc, ShowMenuHook);
            }
            
            isActive = true;
        }
        
        private void InstallHook(IntPtr targetFunction, Delegate hookFunction)
        {
            // Implementation using hooking library
        }
        
        private void RenderUIHook(IntPtr uiContext)
        {
            // Replace the game's UI rendering function
            
            // Modify UI layout for VR
            // AdjustUIForVR(uiContext);
            
            // Render UI elements at appropriate depth
            // RenderUIElements(uiContext, settings.HUDDistance);
        }
        
        private void ShowMenuHook(IntPtr uiContext, int menuType)
        {
            // Replace the game's menu display function
            
            // Create a 3D VR-friendly version of the menu
            // ShowVRMenu(uiContext, menuType);
        }
        
        public void Update(HeadPose headPose)
        {
            // Update UI based on head position
            // Position UI elements to follow the user's gaze
            
            // Implementation depends on specific game UI system
        }
    }
    
    // Support classes
    
    public class MemoryScanner
    {
        private string executablePath;
        
        public MemoryScanner(string executablePath)
        {
            this.executablePath = executablePath;
        }
        
        public Dictionary<string, IntPtr> FindFunctions(Dictionary<string, byte[]> signatures)
        {
            // Scan the game's memory space to find functions matching the provided signatures
            var results = new Dictionary<string, IntPtr>();
            
            foreach (var signature in signatures)
            {
                // Placeholder - would use memory scanning techniques to find the function
                results[signature.Key] = IntPtr.Zero;
            }
            
            return results;
        }
    }
    
    public class GameProfile
    {
        public string GameName { get; set; }
        public string ExecutablePath { get; set; }
        public GameType GameType { get; set; }
        public GraphicsAPI GraphicsAPI { get; set; }
        
        // Settings for different subsystems
        public RenderSettings RenderSettings { get; set; } = new RenderSettings();
        public CameraSettings CameraSettings { get; set; } = new CameraSettings();
        public MovementSettings MovementSettings { get; set; } = new MovementSettings();
        public InteractionSettings InteractionSettings { get; set; } = new InteractionSettings();
        public VehicleSettings VehicleSettings { get; set; } = new VehicleSettings();
        public CombatSettings CombatSettings { get; set; } = new CombatSettings();
        public UISettings UISettings { get; set; } = new UISettings();
        
        // Memory signatures for hooking
        public Dictionary<string, byte[]> CameraSignatures { get; set; } = new Dictionary<string, byte[]>();
        public Dictionary<string, byte[]> MovementSignatures { get; set; } = new Dictionary<string, byte[]>();
        public Dictionary<string, byte[]> InteractionSignatures { get; set; } = new Dictionary<string, byte[]>();
        public Dictionary<string, byte[]> VehicleSignatures { get; set; } = new Dictionary<string, byte[]>();
        public Dictionary<string, byte[]> CombatSignatures { get; set; } = new Dictionary<string, byte[]>();
        public Dictionary<string, byte[]> UISignatures { get; set; } = new Dictionary<string, byte[]>();
        
        // Factory methods for popular games
        public static GameProfile CreateForGTA5()
        {
            var profile = new GameProfile
            {
                GameName = "Grand Theft Auto V",
                ExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V\GTA5.exe",
                GameType = GameType.GTA5,
                GraphicsAPI = GraphicsAPI.DirectX11
            };
            
            // Set GTA5-specific signatures
            profile.CameraSignatures["UpdateCamera"] = new byte[] { 0x48, 0x89, 0x5C, 0x24, 0x08, 0x57, 0x48, 0x83, 0xEC, 0x20, 0x48, 0x8B, 0xD9, 0x48 };
            profile.MovementSignatures["UpdateCharacterMovement"] = new byte[] { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x20, 0x48, 0x8B, 0xD9, 0xE8 };
            profile.VehicleSignatures["DriveVehicle"] = new byte[] { 0x48, 0x89, 0x5