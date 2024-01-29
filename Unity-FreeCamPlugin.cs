using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Unity_FreeCam
{
    [BepInPlugin(GUID, "Unity-FreeCam", Version)]
    public sealed class FreeCamPlugin : BaseUnityPlugin
    { 
        public const string GUID = "TollyH.Unity-FreeCam";
        public const string Version = "1.0.0";

        private static new ManualLogSource Logger;

        private static ConfigEntry<KeyboardShortcut> configToggleFreecamKey;
        private static ConfigEntry<KeyboardShortcut> configSelectCameraKey;
        private static ConfigEntry<KeyboardShortcut> configResetPositionKey;
        private static ConfigEntry<KeyboardShortcut> configResetRotationKey;
        private static ConfigEntry<KeyboardShortcut> configToggleGameFreezeKey;

        private static ConfigEntry<KeyboardShortcut> configMoveForwardKey;
        private static ConfigEntry<KeyboardShortcut> configMoveBackwardKey;
        private static ConfigEntry<KeyboardShortcut> configMoveLeftKey;
        private static ConfigEntry<KeyboardShortcut> configMoveRightKey;
        private static ConfigEntry<KeyboardShortcut> configMoveUpKey;
        private static ConfigEntry<KeyboardShortcut> configMoveDownKey;

        private static ConfigEntry<KeyboardShortcut> configRotatePitchForwardKey;
        private static ConfigEntry<KeyboardShortcut> configRotatePitchBackwardKey;
        private static ConfigEntry<KeyboardShortcut> configRotateYawLeftKey;
        private static ConfigEntry<KeyboardShortcut> configRotateYawRightKey;
        private static ConfigEntry<KeyboardShortcut> configRotateRollCounterClockwiseKey;
        private static ConfigEntry<KeyboardShortcut> configRotateRollClockwiseKey;

        private static ConfigEntry<KeyboardShortcut> configIncreaseFovKey;
        private static ConfigEntry<KeyboardShortcut> configDecreaseFovKey;
        private static ConfigEntry<KeyboardShortcut> configIncreaseNearClipKey;
        private static ConfigEntry<KeyboardShortcut> configDecreaseNearClipKey;
        private static ConfigEntry<KeyboardShortcut> configIncreaseFarClipKey;
        private static ConfigEntry<KeyboardShortcut> configDecreaseFarClipKey;

        private static ConfigEntry<KeyboardShortcut> configIncreaseMoveSpeedKey;
        private static ConfigEntry<KeyboardShortcut> configDecreaseMoveSpeedKey;
        private static ConfigEntry<KeyboardShortcut> configIncreaseRotationSpeedKey;
        private static ConfigEntry<KeyboardShortcut> configDecreaseRotationSpeedKey;

        private static readonly List<Camera> loadedCameras = new List<Camera>();
        private static int selectedCameraIndex = 0;

        private static float moveSpeed = 1.0f;
        private static float rotationSpeed = 1.0f;

        private static bool freecamActive = false;
        private static bool gameFrozen = false;

        private static readonly Dictionary<Camera, Vector3> originalCameraPositions;
        private static readonly Dictionary<Camera, Quaternion> originalCameraRotations;
        private static readonly Dictionary<Camera, float> originalCameraFovs;
        private static readonly Dictionary<Camera, float> originalCameraNearClips;
        private static readonly Dictionary<Camera, float> originalCameraFarClips;

        private static readonly Dictionary<Camera, Vector3> overrideCameraPositions;
        private static readonly Dictionary<Camera, Quaternion> overrideCameraRotations;
        private static readonly Dictionary<Camera, float> overrideCameraFovs;
        private static readonly Dictionary<Camera, float> overrideCameraNearClips;
        private static readonly Dictionary<Camera, float> overrideCameraFarClips;


        public void Start()
        {
            _ = Harmony.CreateAndPatchAll(typeof(FreeCamPlugin));
        }

        public void Awake()
        {
            Logger = base.Logger;

            configToggleFreecamKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Toggle FreeCam", new KeyboardShortcut(KeyCode.KeypadMultiply));
            configSelectCameraKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Select Camera", new KeyboardShortcut(KeyCode.KeypadMinus));
            configResetPositionKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera Position", new KeyboardShortcut(KeyCode.KeypadDivide));
            configResetRotationKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera Rotation", new KeyboardShortcut(KeyCode.KeypadDivide));
            configToggleGameFreezeKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Toggle Game Freeze", new KeyboardShortcut(KeyCode.KeypadPeriod));

            configMoveForwardKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Forward", new KeyboardShortcut(KeyCode.W));
            configMoveBackwardKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Backward", new KeyboardShortcut(KeyCode.S));
            configMoveLeftKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Left", new KeyboardShortcut(KeyCode.A));
            configMoveRightKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Right", new KeyboardShortcut(KeyCode.D));
            configMoveUpKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Up", new KeyboardShortcut(KeyCode.E));
            configMoveDownKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Down", new KeyboardShortcut(KeyCode.Q));

            configRotatePitchForwardKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Pitch Forward", new KeyboardShortcut(KeyCode.Keypad8));
            configRotatePitchBackwardKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Pitch Backward", new KeyboardShortcut(KeyCode.Keypad2));
            configRotateYawLeftKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Yaw Left", new KeyboardShortcut(KeyCode.Keypad4));
            configRotateYawRightKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Yaw Right", new KeyboardShortcut(KeyCode.Keypad6));
            configRotateRollCounterClockwiseKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Roll Counter-clockwise", new KeyboardShortcut(KeyCode.Keypad7));
            configRotateRollClockwiseKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Roll Clockwise", new KeyboardShortcut(KeyCode.Keypad9));

            configIncreaseFovKey = Config.Bind("Keyboard Shortcuts - Camera Viewport", "Increase FOV", new KeyboardShortcut(KeyCode.E));
            configDecreaseFovKey = Config.Bind("Keyboard Shortcuts - Camera Viewport", "Decrease FOV", new KeyboardShortcut(KeyCode.Q));
            configIncreaseNearClipKey = Config.Bind("Keyboard Shortcuts - Camera Viewport", "Increase Near Clip Plane", new KeyboardShortcut(KeyCode.X));
            configDecreaseNearClipKey = Config.Bind("Keyboard Shortcuts - Camera Viewport", "Decrease Near Clip Plane", new KeyboardShortcut(KeyCode.Z));
            configIncreaseFarClipKey = Config.Bind("Keyboard Shortcuts - Camera Viewport", "Increase Far Clip Plane", new KeyboardShortcut(KeyCode.V));
            configDecreaseFarClipKey = Config.Bind("Keyboard Shortcuts - Camera Viewport", "Decrease Far Clip Plane", new KeyboardShortcut(KeyCode.C));

            configIncreaseMoveSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Increase Movement Speed", new KeyboardShortcut(KeyCode.R));
            configDecreaseMoveSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Decrease Movement Speed", new KeyboardShortcut(KeyCode.F));
            configIncreaseRotationSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Increase Rotation Speed", new KeyboardShortcut(KeyCode.R));
            configDecreaseRotationSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Decrease Rotation Speed", new KeyboardShortcut(KeyCode.F));
        }

        public void Update()
        {

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey))]
        public static void OverrideGetKey()
        {

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown))]
        public static void OverrideGetKeyDown()
        {

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp))]
        public static void OverrideGetKeyUp()
        {

        }
    }
}
