namespace AJS_VRMOD
{
    public enum VRAction
    {
        None,
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        LookUp,
        LookDown,
        LookLeft,
        LookRight,
        PrimaryAction, // e.g., Shoot, Interact
        SecondaryAction, // e.g., Aim, Use Item
        Jump,
        Sprint,
        Crouch,
        Menu,
        Map,
        Inventory,

        // Hogwarts Legacy Flying Actions
        FlyForward,
        FlyBackward,
        FlyAscend,
        FlyDescend,
        FlySteerLeft,
        FlySteerRight,
        CastSpellWhileFlying,
        BroomBoost,

        // Spider-Man Web-Swinging Actions
        WebShootLeft,
        WebShootRight,
        WebRelease,
        SwingForward,
        SwingBackward,
        WallRun,
        ZipToPoint,

        // General Vehicle Actions
        VehicleAccelerate,
        VehicleBrake,
        VehicleSteerLeft,
        VehicleSteerRight,
        VehicleLookLeft,
        VehicleLookRight,
        VehicleLookUp,
        VehicleLookDown,
        VehicleChangeGearUp,
        VehicleChangeGearDown,
        VehicleHandbrake,
        VehicleHorn,

        // Airplane Specific
        AirplaneThrottleUp,
        AirplaneThrottleDown,
        AirplanePitchUp,
        AirplanePitchDown,
        AirplaneRollLeft,
        AirplaneRollRight,
        AirplaneYawLeft,
        AirplaneYawRight,
        AirplaneLandingGear,
        AirplaneWeapons,

        // Helicopter Specific
        HelicopterCollectiveUp,
        HelicopterCollectiveDown,
        HelicopterCyclicForward,
        HelicopterCyclicBackward,
        HelicopterCyclicLeft,
        HelicopterCyclicRight,
        HelicopterTailRotorLeft,
        HelicopterTailRotorRight,
        HelicopterWeapons,

        // Motorcycle Specific
        MotorcycleLeanLeft,
        MotorcycleLeanRight,
        MotorcycleWheelie,

        // Boat Specific (if applicable)
        BoatThrottleUp,
        BoatThrottleDown,
        BoatSteerLeft,
        BoatSteerRight,
        BoatWeapons
    }
}