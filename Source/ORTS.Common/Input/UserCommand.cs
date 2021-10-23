﻿namespace ORTS.Common.Input
{
    /// <summary>
    /// Specifies game commands.
    /// </summary>
    /// <remarks>
    /// <para>The ordering and naming of these commands is important. They are listed in the UI in the order they are defined in the code, and the first word of each command is the "group" to which it belongs.</para>
    /// </remarks>
    public enum UserCommand
    {
        [GetString("Game Pause Menu")] GamePauseMenu,
        [GetString("Game Save")] GameSave,
        [GetString("Game Quit")] GameQuit,
        [GetString("Game Pause")] GamePause,
        [GetString("Game Screenshot")] GameScreenshot,
        [GetString("Game Fullscreen")] GameFullscreen,
        [GetString("Game External Controller (RailDriver)")] GameExternalCabController,
//        [GetString("Game Switch Ahead")] GameSwitchAhead,
//        [GetString("Game Switch Behind")] GameSwitchBehind,
        [GetString("Game Facing Switch Ahead")] GameFacingSwitchAhead,
        [GetString("Game Facing Switch Behind")] GameFacingSwitchBehind,
        [GetString("Game Switch Picked")] GameSwitchPicked,
        [GetString("Game Signal Picked")] GameSignalPicked,
        [GetString("Game Trailing Switch Ahead")] GameTrailingSwitchAhead,
        [GetString("Game Trailing Switch Behind")] GameTrailingSwitchBehind,
        [GetString("Game Switch With Mouse")] GameSwitchWithMouse,
        [GetString("Game Uncouple With Mouse")] GameUncoupleWithMouse,
        [GetString("Game Change Cab")] GameChangeCab,
        [GetString("Game Request Control")] GameRequestControl,
        [GetString("Game Multi Player Dispatcher")] GameMultiPlayerDispatcher,
        [GetString("Game Multi Player Texting")] GameMultiPlayerTexting,
        [GetString("Game Switch Manual Mode")] GameSwitchManualMode,
        [GetString("Game Reset Out Of Control Mode")] GameResetOutOfControlMode,
        [GetString("Game Clear Signal Forward")] GameClearSignalForward,
        [GetString("Game Clear Signal Backward")] GameClearSignalBackward,
        [GetString("Game Reset Signal Forward")] GameResetSignalForward,
        [GetString("Game Reset Signal Backward")] GameResetSignalBackward,
        [GetString("Game Autopilot Mode")] GameAutopilotMode,
        [GetString("Game Suspend Old Player")] GameSuspendOldPlayer,

        [GetString("Display Next Window Tab")] DisplayNextWindowTab,
        [GetString("Display Help Window")] DisplayHelpWindow,
        [GetString("Display Track Monitor Window")] DisplayTrackMonitorWindow,
        [GetString("Display HUD")] DisplayHUD,
        [GetString("Display Train Driving Window")] DisplayTrainDrivingWindow,
        [GetString("Display Multi Player Window")] DisplayMultiPlayerWindow,
        [GetString("Display Car Labels")] DisplayCarLabels,
        [GetString("Display Station Labels")] DisplayStationLabels,
        [GetString("Display Switch Window")] DisplaySwitchWindow,
        [GetString("Display Train Operations Window")] DisplayTrainOperationsWindow,
        [GetString("Display Next Station Window")] DisplayNextStationWindow,
        [GetString("Display Compass Window")] DisplayCompassWindow,
        [GetString("Display Train List Window")] DisplayTrainListWindow,

        [GetString("Debug Speed Up")] DebugSpeedUp,
        [GetString("Debug Speed Down")] DebugSpeedDown,
        [GetString("Debug Speed Reset")] DebugSpeedReset,
        [GetString("Debug Overcast Increase")] DebugOvercastIncrease,
        [GetString("Debug Overcast Decrease")] DebugOvercastDecrease,
        [GetString("Debug Fog Increase")] DebugFogIncrease,
        [GetString("Debug Fog Decrease")] DebugFogDecrease,
        [GetString("Debug Precipitation Increase")] DebugPrecipitationIncrease,
        [GetString("Debug Precipitation Decrease")] DebugPrecipitationDecrease,
        [GetString("Debug Precipitation Liquidity Increase")] DebugPrecipitationLiquidityIncrease,
        [GetString("Debug Precipitation Liquidity Decrease")] DebugPrecipitationLiquidityDecrease,
        [GetString("Debug Weather Change")] DebugWeatherChange,
        [GetString("Debug Clock Forwards")] DebugClockForwards,
        [GetString("Debug Clock Backwards")] DebugClockBackwards,
        [GetString("Debug Logger")] DebugLogger,
        [GetString("Debug Lock Shadows")] DebugLockShadows,
        [GetString("Debug Dump Keyboard Map")] DebugDumpKeymap,
        [GetString("Debug Log Render Frame")] DebugLogRenderFrame,
        [GetString("Debug Tracks")] DebugTracks,
        [GetString("Debug Signalling")] DebugSignalling,
        [GetString("Debug Reset Wheel Slip")] DebugResetWheelSlip,
        [GetString("Debug Toggle Advanced Adhesion")] DebugToggleAdvancedAdhesion,
        [GetString("Debug Sound Form")] DebugSoundForm,
        [GetString("Debug Physics Form")] DebugPhysicsForm,
        [GetString("Debug Toggle Confirmations")] DebugToggleConfirmations,

        [GetString("Camera Cab")] CameraCab,
        [GetString("Camera Change Passenger Viewpoint")] CameraChangePassengerViewPoint,
        [GetString("Camera Toggle 3D Cab")] CameraToggleThreeDimensionalCab,
        [GetString("Camera Toggle Show Cab")] CameraToggleShowCab,
        [GetString("Camera Toggle Letterbox Cab")] CameraToggleLetterboxCab,
        [GetString("Camera Head Out Forward")] CameraHeadOutForward,
        [GetString("Camera Head Out Backward")] CameraHeadOutBackward,
        [GetString("Camera Outside Front")] CameraOutsideFront,
        [GetString("Camera Outside Rear")] CameraOutsideRear,
        [GetString("Camera Trackside")] CameraTrackside,
        [GetString("Camera SpecialTracksidePoint")] CameraSpecialTracksidePoint,
        [GetString("Camera Passenger")] CameraPassenger,
        [GetString("Camera Brakeman")] CameraBrakeman,
        [GetString("Camera Change Brakeman Viewpoint")] CameraChangeBrakemanViewPoint,
        [GetString("Camera Free")] CameraFree,
        [GetString("Camera Previous Free")] CameraPreviousFree,
        [GetString("Camera Reset")] CameraReset,
        [GetString("Camera Move Fast")] CameraMoveFast,
        [GetString("Camera Move Slow")] CameraMoveSlow,
        [GetString("Camera Pan (Rotate) Left")] CameraPanLeft,
        [GetString("Camera Pan (Rotate) Right")] CameraPanRight,
        [GetString("Camera Pan (Rotate) Up")] CameraPanUp,
        [GetString("Camera Pan (Rotate) Down")] CameraPanDown,
        [GetString("Camera Zoom In (Move Z)")] CameraZoomIn,
        [GetString("Camera Zoom Out (Move Z)")] CameraZoomOut,
        [GetString("Camera Rotate (Pan) Left")] CameraRotateLeft,
        [GetString("Camera Rotate (Pan) Right")] CameraRotateRight,
        [GetString("Camera Rotate (Pan) Up")] CameraRotateUp,
        [GetString("Camera Rotate (Pan) Down")] CameraRotateDown,
        [GetString("Camera Car Next")] CameraCarNext,
        [GetString("Camera Car Previous")] CameraCarPrevious,
        [GetString("Camera Car First")] CameraCarFirst,
        [GetString("Camera Car Last")] CameraCarLast,
        [GetString("Camera Jumping Trains")] CameraJumpingTrains,
        [GetString("Camera Jump Back Player")] CameraJumpBackPlayer,
        [GetString("Camera Jump See Switch")] CameraJumpSeeSwitch,
        [GetString("Camera Vibrate")] CameraVibrate,
        [GetString("Camera Scroll Right")] CameraScrollRight,
        [GetString("Camera Scroll Left")] CameraScrollLeft,
        [GetString("Camera Browse Backwards")] CameraBrowseBackwards,
        [GetString("Camera Browse Forwards")] CameraBrowseForwards,
        [GetString("Camera Follow Cut Car")] CameraFollowCutCar,

        [GetString("Control Forwards")] ControlForwards,
        [GetString("Control Backwards")] ControlBackwards,
        [GetString("Control Throttle Increase")] ControlThrottleIncrease,
        [GetString("Control Throttle Decrease")] ControlThrottleDecrease,
        [GetString("Control Throttle Zero")] ControlThrottleZero,
        [GetString("Control Gear Up")] ControlGearUp,
        [GetString("Control Gear Down")] ControlGearDown,
        [GetString("Control Train Brake Increase")] ControlTrainBrakeIncrease,
        [GetString("Control Train Brake Decrease")] ControlTrainBrakeDecrease,
        [GetString("Control Train Brake Zero")] ControlTrainBrakeZero,
        [GetString("Control Engine Brake Increase")] ControlEngineBrakeIncrease,
        [GetString("Control Engine Brake Decrease")] ControlEngineBrakeDecrease,
        [GetString("Control Brakeman Brake Increase")] ControlBrakemanBrakeIncrease,
        [GetString("Control Brakeman Brake Decrease")] ControlBrakemanBrakeDecrease,
        [GetString("Control Dynamic Brake Increase")] ControlDynamicBrakeIncrease,
        [GetString("Control Dynamic Brake Decrease")] ControlDynamicBrakeDecrease,
        [GetString("Control Bail Off")] ControlBailOff,
        [GetString("Control Initialize Brakes")] ControlInitializeBrakes,
        [GetString("Control Handbrake Full")] ControlHandbrakeFull,
        [GetString("Control Handbrake None")] ControlHandbrakeNone,
        [GetString("Control Odometer Show/Hide")] ControlOdoMeterShowHide,
        [GetString("Control Odometer Reset")] ControlOdoMeterReset,
        [GetString("Control Odometer Direction")] ControlOdoMeterDirection,
        [GetString("Control Retainers On")] ControlRetainersOn,
        [GetString("Control Retainers Off")] ControlRetainersOff,
        [GetString("Control Brake Hose Connect")] ControlBrakeHoseConnect,
        [GetString("Control Brake Hose Disconnect")] ControlBrakeHoseDisconnect,
        [GetString("Control Alerter")] ControlAlerter,
        [GetString("Control Emergency Push Button")] ControlEmergencyPushButton,
        [GetString("Control Sander")] ControlSander,
        [GetString("Control Sander Toggle")] ControlSanderToggle,
        [GetString("Control Wiper")] ControlWiper,
        [GetString("Control Horn")] ControlHorn,
        [GetString("Control Bell")] ControlBell,
        [GetString("Control Bell Toggle")] ControlBellToggle,
        [GetString("Control Door Left")] ControlDoorLeft,
        [GetString("Control Door Right")] ControlDoorRight,
        [GetString("Control Mirror")] ControlMirror,
        [GetString("Control Light")] ControlLight,
        [GetString("Control Pantograph 1")] ControlPantograph1,
        [GetString("Control Pantograph 2")] ControlPantograph2,
        [GetString("Control Pantograph 3")] ControlPantograph3,
        [GetString("Control Pantograph 4")] ControlPantograph4,
        [GetString("Control Battery Close")] ControlBatterySwitchClose,
        [GetString("Control Battery Open")] ControlBatterySwitchOpen,
        [GetString("Control Master Key")] ControlMasterKey,
        [GetString("Control Service Retention")] ControlServiceRetention,
        [GetString("Control Service Retention Cancellation")] ControlServiceRetentionCancellation,
        [GetString("Control Circuit Breaker Closing Order")] ControlCircuitBreakerClosingOrder,
        [GetString("Control Circuit Breaker Opening Order")] ControlCircuitBreakerOpeningOrder,
        [GetString("Control Circuit Breaker Closing Authorization")] ControlCircuitBreakerClosingAuthorization,
        [GetString("Control Traction Cut-Off Relay Closing Order")] ControlTractionCutOffRelayClosingOrder,
        [GetString("Control Traction Cut-Off Relay Opening Order")] ControlTractionCutOffRelayOpeningOrder,
        [GetString("Control Traction Cut-Off Relay Closing Authorization")] ControlTractionCutOffRelayClosingAuthorization,
        [GetString("Control Electric Train Supply")] ControlElectricTrainSupply,
        [GetString("Control Diesel Player")] ControlDieselPlayer,
        [GetString("Control Diesel Helper")] ControlDieselHelper,
        [GetString("Control Headlight Increase")] ControlHeadlightIncrease,
        [GetString("Control Headlight Decrease")] ControlHeadlightDecrease,
        [GetString("Control Injector 1 Increase")] ControlInjector1Increase,
        [GetString("Control Injector 1 Decrease")] ControlInjector1Decrease,
        [GetString("Control Injector 1")] ControlInjector1,
        [GetString("Control Injector 2 Increase")] ControlInjector2Increase,
        [GetString("Control Injector 2 Decrease")] ControlInjector2Decrease,
        [GetString("Control Injector 2")] ControlInjector2,
        [GetString("Control Blowdown Valve")] ControlBlowdownValve,
        [GetString("Control Blower Increase")] ControlBlowerIncrease,
        [GetString("Control Blower Decrease")] ControlBlowerDecrease,
        [GetString("Control Steam Heat Increase")] ControlSteamHeatIncrease,
        [GetString("Control Steam Heat Decrease")] ControlSteamHeatDecrease,
        [GetString("Control Damper Increase")] ControlDamperIncrease,
        [GetString("Control Damper Decrease")] ControlDamperDecrease,
        [GetString("Control Firebox Open")] ControlFireboxOpen,
        [GetString("Control Firebox Close")] ControlFireboxClose,
        [GetString("Control Firing Rate Increase")] ControlFiringRateIncrease,
        [GetString("Control Firing Rate Decrease")] ControlFiringRateDecrease,
        [GetString("Control Fire Shovel Full")] ControlFireShovelFull,
        [GetString("Control Cylinder Cocks")] ControlCylinderCocks,
        [GetString("Control Large Ejector Increase")] ControlLargeEjectorIncrease,
        [GetString("Control Large Ejector Decrease")] ControlLargeEjectorDecrease,
        [GetString("Control Small Ejector Increase")] ControlSmallEjectorIncrease,
        [GetString("Control Small Ejector Decrease")] ControlSmallEjectorDecrease,
        [GetString("Control Vacuum Exhauster")] ControlVacuumExhausterPressed,
        [GetString("Control Cylinder Compound")] ControlCylinderCompound,
        [GetString("Control Firing")] ControlFiring,
        [GetString("Control Refill")] ControlRefill,
        [GetString("Control Water Scoop")] ControlWaterScoop,
        [GetString("Control ImmediateRefill")] ControlImmediateRefill,
        [GetString("Control Turntable Clockwise")] ControlTurntableClockwise,
        [GetString("Control Turntable Counterclockwise")] ControlTurntableCounterclockwise,
        [GetString("Control Generic Item 1")] ControlGenericItem1,
        [GetString("Control Generic Item 2")] ControlGenericItem2,
        [GetString("Control TCS Generic 1")] ControlTCSGeneric1,
        [GetString("Control TCS Generic 2")] ControlTCSGeneric2,
        [GetString("Control Cab Radio")] ControlCabRadio,
        [GetString("Control AI Fire On")] ControlAIFireOn,
        [GetString("Control AI Fire Off")] ControlAIFireOff,
        [GetString("Control AI Fire Reset")] ControlAIFireReset,
        [GetString("Control Battery")] ControlBattery,
        [GetString("Control PowerKey")] ControlPowerKey,
        [GetString("Control Speed Regulator Mode Increase")] ControlSpeedRegulatorModeIncrease,
        [GetString("Control Speed Regulator Mode Descrease")] ControlSpeedRegulatorModeDecrease,
        [GetString("Control Selected Speed Increase")] ControlSpeedRegulatorSelectedSpeedIncrease,
        [GetString("Control Selected Speed Decrease")] ControlSpeedRegulatorSelectedSpeedDecrease,
        [GetString("Control Speed Regulator Max Acceleration Increase")] ControlSpeedRegulatorMaxAccelerationIncrease,
        [GetString("Control Speed Regulator Max Acceleration Decrease")] ControlSpeedRegulatorMaxAccelerationDecrease,
        [GetString("Control Number Of Axles Increase")] ControlNumberOfAxlesIncrease,
        [GetString("Control Number Of Axles Decrease")] ControlNumberOfAxlesDecrease,
        [GetString("Control Restricted Speed Zone Active")] ControlRestrictedSpeedZoneActive,
        [GetString("Control Cruise Control Mode Increase")] ControlCruiseControlModeIncrease,
        [GetString("Control Cruise Control Mode Decrease")] ControlCruiseControlModeDecrease,
        [GetString("Control Train Type Change (Passenger/Cargo)")] ControlTrainTypePaxCargo,
        [GetString("Control Select Speed 10 kph/mph")] ControlSelectSpeed10,
        [GetString("Control Select Speed 20 kph/mph")] ControlSelectSpeed20,
        [GetString("Control Select Speed 30 kph/mph")] ControlSelectSpeed30,
        [GetString("Control Select Speed 40 kph/mph")] ControlSelectSpeed40,
        [GetString("Control Select Speed 50 kph/mph")] ControlSelectSpeed50,
        [GetString("Control Select Speed 60 kph/mph")] ControlSelectSpeed60,
        [GetString("Control Select Speed 70 kph/mph")] ControlSelectSpeed70,
        [GetString("Control Select Speed 80 kph/mph")] ControlSelectSpeed80,
        [GetString("Control Select Speed 90 kph/mph")] ControlSelectSpeed90,
        [GetString("Control Select Speed 100 kph/mph")] ControlSelectSpeed100,
        [GetString("Control Select Speed 110 kph/mph")] ControlSelectSpeed110,
        [GetString("Control Select Speed 120 kph/mph")] ControlSelectSpeed120,
        [GetString("Control Select Speed 130 kph/mph")] ControlSelectSpeed130,
        [GetString("Control Select Speed 140 kph/mph")] ControlSelectSpeed140,
        [GetString("Control Select Speed 150 kph/mph")] ControlSelectSpeed150,
        [GetString("Control Select Speed 160 kph/mph")] ControlSelectSpeed160,
        [GetString("Control Select Speed 170 kph/mph")] ControlSelectSpeed170,
        [GetString("Control Select Speed 180 kph/mph")] ControlSelectSpeed180,
        [GetString("Control Select Speed 190 kph/mph")] ControlSelectSpeed190,
        [GetString("Control Select Speed 200 kph/mph")] ControlSelectSpeed200,
        [GetString("Control Selected Speed To Zero")] ControlSpeedRegulatorSelectedSpeedToZero,
    }
}
