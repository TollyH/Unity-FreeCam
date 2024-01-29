using System.Collections.Generic;
using System.Text;
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
        private static ConfigEntry<KeyboardShortcut> configToggleGameFreezeKey;
        private static ConfigEntry<KeyboardShortcut> configResetPositionKey;
        private static ConfigEntry<KeyboardShortcut> configResetRotationKey;
        private static ConfigEntry<KeyboardShortcut> configResetViewKey;

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

        private static bool processingInput = false;

        private static readonly Dictionary<Camera, Vector3> originalCameraPositions;
        private static readonly Dictionary<Camera, Quaternion> originalCameraRotations;
        private static readonly Dictionary<Camera, float> originalCameraFovs;
        private static readonly Dictionary<Camera, float> originalCameraNearClips;
        private static readonly Dictionary<Camera, float> originalCameraFarClips;

        private static readonly Dictionary<Camera, Vector3?> overrideCameraPositions;
        private static readonly Dictionary<Camera, Quaternion?> overrideCameraRotations;
        private static readonly Dictionary<Camera, float?> overrideCameraFovs;
        private static readonly Dictionary<Camera, float?> overrideCameraNearClips;
        private static readonly Dictionary<Camera, float?> overrideCameraFarClips;


        public void Start()
        {
            _ = Harmony.CreateAndPatchAll(typeof(FreeCamPlugin));
        }

        public void Awake()
        {
            Logger = base.Logger;

            configToggleFreecamKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Toggle FreeCam", new KeyboardShortcut(KeyCode.KeypadMultiply));
            configSelectCameraKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Select Camera", new KeyboardShortcut(KeyCode.KeypadMinus));
            configToggleGameFreezeKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Toggle Game Freeze", new KeyboardShortcut(KeyCode.KeypadPeriod));
            configResetPositionKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera Position", new KeyboardShortcut(KeyCode.KeypadDivide));
            configResetRotationKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera Rotation", new KeyboardShortcut(KeyCode.KeypadDivide));
            configResetViewKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera View", new KeyboardShortcut(KeyCode.KeypadDivide));

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

            configIncreaseFovKey = Config.Bind("Keyboard Shortcuts - Camera View", "Increase FOV", new KeyboardShortcut(KeyCode.E));
            configDecreaseFovKey = Config.Bind("Keyboard Shortcuts - Camera View", "Decrease FOV", new KeyboardShortcut(KeyCode.Q));
            configIncreaseNearClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Increase Near Clip Plane", new KeyboardShortcut(KeyCode.X));
            configDecreaseNearClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Decrease Near Clip Plane", new KeyboardShortcut(KeyCode.Z));
            configIncreaseFarClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Increase Far Clip Plane", new KeyboardShortcut(KeyCode.V));
            configDecreaseFarClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Decrease Far Clip Plane", new KeyboardShortcut(KeyCode.C));

            configIncreaseMoveSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Increase Movement Speed", new KeyboardShortcut(KeyCode.R));
            configDecreaseMoveSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Decrease Movement Speed", new KeyboardShortcut(KeyCode.F));
            configIncreaseRotationSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Increase Rotation Speed", new KeyboardShortcut(KeyCode.R));
            configDecreaseRotationSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Decrease Rotation Speed", new KeyboardShortcut(KeyCode.F));
        }

        private static void StartPositionControl(Camera selectedCamera)
        {
            if (overrideCameraPositions[selectedCamera] == null)
            {
                originalCameraPositions[selectedCamera] = selectedCamera.transform.position;
                overrideCameraPositions[selectedCamera] = selectedCamera.transform.position;
            }
        }

        private static void StartRotationControl(Camera selectedCamera)
        {
            if (overrideCameraRotations[selectedCamera] == null)
            {
                originalCameraRotations[selectedCamera] = selectedCamera.transform.rotation;
                overrideCameraRotations[selectedCamera] = selectedCamera.transform.rotation;
            }
        }

        private static void StartViewControl(Camera selectedCamera)
        {
            if (overrideCameraFovs[selectedCamera] == null)
            {
                originalCameraFovs[selectedCamera] = selectedCamera.fieldOfView;
                overrideCameraFovs[selectedCamera] = selectedCamera.fieldOfView;
            }
            if (overrideCameraNearClips[selectedCamera] == null)
            {
                originalCameraNearClips[selectedCamera] = selectedCamera.nearClipPlane;
                overrideCameraNearClips[selectedCamera] = selectedCamera.nearClipPlane;
            }
            if (overrideCameraFarClips[selectedCamera] == null)
            {
                originalCameraFarClips[selectedCamera] = selectedCamera.farClipPlane;
                overrideCameraFarClips[selectedCamera] = selectedCamera.farClipPlane;
            }
        }

        private static void StopPositionControl(Camera selectedCamera)
        {
            overrideCameraPositions[selectedCamera] = null;
            selectedCamera.transform.position = originalCameraPositions[selectedCamera];
        }

        private static void StopRotationControl(Camera selectedCamera)
        {
            overrideCameraRotations[selectedCamera] = null;
            selectedCamera.transform.rotation = originalCameraRotations[selectedCamera];
        }

        private static void StopViewControl(Camera selectedCamera)
        {
            overrideCameraFovs[selectedCamera] = null;
            overrideCameraNearClips[selectedCamera] = null;
            overrideCameraFarClips[selectedCamera] = null;
            selectedCamera.fieldOfView = originalCameraFovs[selectedCamera];
            selectedCamera.nearClipPlane = originalCameraNearClips[selectedCamera];
            selectedCamera.farClipPlane = originalCameraFarClips[selectedCamera];
        }

        private static void ProcessInput()
        {
            // Flag that our input is being processed to stop it being overridden
            processingInput = true;
            try
            {
                if (configToggleFreecamKey.Value.IsDown())
                {
                    freecamActive = !freecamActive;
                    Logger.LogMessage($"FreeCam is now {(freecamActive ? "enabled" : "disabled")}");
                }

                if (configSelectCameraKey.Value.IsDown())
                {
                    if (loadedCameras.Count == 0)
                    {
                        Logger.LogWarning("There are no cameras to select");
                    }
                    else
                    {
                        selectedCameraIndex = (selectedCameraIndex + 1) % loadedCameras.Count;
                        Logger.LogMessage($"Switched to camera {selectedCameraIndex + 1}/{loadedCameras.Count} at {GetFullHierarchyPath(loadedCameras[selectedCameraIndex].gameObject)}");
                        if (!loadedCameras[selectedCameraIndex].isActiveAndEnabled)
                        {
                            Logger.LogMessage("The selected camera is NOT active");
                        }
                    }
                }

                if (configToggleGameFreezeKey.Value.IsDown())
                {
                    gameFrozen = !gameFrozen;
                    Logger.LogMessage($"Game is now {(gameFrozen ? "frozen" : "un-frozen")}");
                }

                if (configIncreaseMoveSpeedKey.Value.IsPressed())
                {
                    moveSpeed += moveSpeed * Time.deltaTime;
                }
                if (configDecreaseMoveSpeedKey.Value.IsPressed())
                {
                    moveSpeed -= moveSpeed * Time.deltaTime;
                }
                if (configIncreaseRotationSpeedKey.Value.IsPressed())
                {
                    rotationSpeed += rotationSpeed * Time.deltaTime;
                }
                if (configDecreaseRotationSpeedKey.Value.IsPressed())
                {
                    rotationSpeed -= rotationSpeed * Time.deltaTime;
                }

                // Following key-binds need an existing selected camera
                if (selectedCameraIndex < loadedCameras.Count)
                {
                    Camera selectedCamera = loadedCameras[selectedCameraIndex];

                    if (configResetPositionKey.Value.IsDown())
                    {
                        StopPositionControl(selectedCamera);
                    }
                    if (configResetRotationKey.Value.IsDown())
                    {
                        StopRotationControl(selectedCamera);
                    }
                    if (configResetViewKey.Value.IsDown())
                    {
                        StopViewControl(selectedCamera);
                    }

                    if (configMoveForwardKey.Value.IsPressed())
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.forward * Time.deltaTime * moveSpeed);
                    }
                    if (configMoveBackwardKey.Value.IsPressed())
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.back * Time.deltaTime * moveSpeed);
                    }
                    if (configMoveLeftKey.Value.IsPressed())
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.left * Time.deltaTime * moveSpeed);
                    }
                    if (configMoveRightKey.Value.IsPressed())
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.right * Time.deltaTime * moveSpeed);
                    }
                    if (configMoveUpKey.Value.IsPressed())
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.up * Time.deltaTime * moveSpeed);
                    }
                    if (configMoveDownKey.Value.IsPressed())
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.down * Time.deltaTime * moveSpeed);
                    }

                    if (configRotatePitchForwardKey.Value.IsPressed())
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x + (Time.deltaTime * rotationSpeed), currentEuler.y, currentEuler.z);
                    }
                    if (configRotatePitchBackwardKey.Value.IsPressed())
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x - (Time.deltaTime * rotationSpeed), currentEuler.y, currentEuler.z);
                    }
                    if (configRotateYawLeftKey.Value.IsPressed())
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y - (Time.deltaTime * rotationSpeed), currentEuler.z);
                    }
                    if (configRotateYawRightKey.Value.IsPressed())
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y + (Time.deltaTime * rotationSpeed), currentEuler.z);
                    }
                    if (configRotateRollCounterClockwiseKey.Value.IsPressed())
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z - (Time.deltaTime * rotationSpeed));
                    }
                    if (configRotateRollClockwiseKey.Value.IsPressed())
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z + (Time.deltaTime * rotationSpeed));
                    }

                    if (configIncreaseFovKey.Value.IsPressed())
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFovs[selectedCamera] += Time.deltaTime * rotationSpeed;
                    }
                    if (configDecreaseFovKey.Value.IsPressed())
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFovs[selectedCamera] -= Time.deltaTime * rotationSpeed;
                    }
                    if (configIncreaseNearClipKey.Value.IsPressed())
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraNearClips[selectedCamera] += Time.deltaTime * moveSpeed;
                    }
                    if (configDecreaseNearClipKey.Value.IsPressed())
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraNearClips[selectedCamera] -= Time.deltaTime * moveSpeed;
                    }
                    if (configIncreaseFarClipKey.Value.IsPressed())
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFarClips[selectedCamera] += Time.deltaTime * moveSpeed;
                    }
                    if (configDecreaseFarClipKey.Value.IsPressed())
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFarClips[selectedCamera] -= Time.deltaTime * moveSpeed;
                    }
                }
            }
            finally
            {
                processingInput = false;
            }
        }

        public void Update()
        {
            ProcessInput();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey))]
        public static bool OverrideGetKey(ref bool __result)
        {
            if (freecamActive && !processingInput)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown))]
        public static bool OverrideGetKeyDown(ref bool __result)
        {
            if (freecamActive && !processingInput)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp))]
        public static bool OverrideGetKeyUp(ref bool __result)
        {
            if (freecamActive && !processingInput)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static string GetFullHierarchyPath(GameObject gameObject)
        {
            StringBuilder sb = new StringBuilder(gameObject.name);
            GameObject currentObject = gameObject.transform.parent.gameObject;
            while (currentObject != null)
            {
                _ = sb.Insert(0, gameObject.name).Insert(0, '/');
                currentObject = currentObject.transform.parent.gameObject;
            }
            return sb.ToString();
        }
    }
}
