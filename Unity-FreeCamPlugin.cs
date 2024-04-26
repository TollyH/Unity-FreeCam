using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string Version = "1.1.0";

        private static new ManualLogSource Logger;

        private static ConfigEntry<KeyCode> configToggleFreecamKey;
        private static ConfigEntry<KeyCode> configSelectCameraKey;
        private static ConfigEntry<KeyCode> configListCamerasKey;
        private static ConfigEntry<KeyCode> configToggleGameFreezeKey;
        private static ConfigEntry<KeyCode> configToggleUIVisibilityKey;
        private static ConfigEntry<KeyCode> configResetPositionKey;
        private static ConfigEntry<KeyCode> configResetRotationKey;
        private static ConfigEntry<KeyCode> configResetViewKey;

        private static ConfigEntry<KeyCode> configMoveForwardKey;
        private static ConfigEntry<KeyCode> configMoveBackwardKey;
        private static ConfigEntry<KeyCode> configMoveLeftKey;
        private static ConfigEntry<KeyCode> configMoveRightKey;
        private static ConfigEntry<KeyCode> configMoveUpKey;
        private static ConfigEntry<KeyCode> configMoveDownKey;

        private static ConfigEntry<KeyCode> configRotatePitchForwardKey;
        private static ConfigEntry<KeyCode> configRotatePitchBackwardKey;
        private static ConfigEntry<KeyCode> configRotateYawLeftKey;
        private static ConfigEntry<KeyCode> configRotateYawRightKey;
        private static ConfigEntry<KeyCode> configRotateRollCounterClockwiseKey;
        private static ConfigEntry<KeyCode> configRotateRollClockwiseKey;

        private static ConfigEntry<KeyCode> configIncreaseFovKey;
        private static ConfigEntry<KeyCode> configDecreaseFovKey;
        private static ConfigEntry<KeyCode> configIncreaseNearClipKey;
        private static ConfigEntry<KeyCode> configDecreaseNearClipKey;
        private static ConfigEntry<KeyCode> configIncreaseFarClipKey;
        private static ConfigEntry<KeyCode> configDecreaseFarClipKey;

        private static ConfigEntry<KeyCode> configIncreaseMoveSpeedKey;
        private static ConfigEntry<KeyCode> configDecreaseMoveSpeedKey;
        private static ConfigEntry<KeyCode> configIncreaseRotationSpeedKey;
        private static ConfigEntry<KeyCode> configDecreaseRotationSpeedKey;

        private static int selectedCameraIndex = 0;

        private static float moveSpeed = 5.0f;
        private static float rotationSpeed = 75.0f;

        private static bool freecamActive = false;
        private static bool gameFrozen = false;
        private static bool hideUI = false;

        private static float oldTimeScale = 1.0f;

        private static bool processingInput = false;

        private static readonly Dictionary<Camera, Vector3> originalCameraPositions = new Dictionary<Camera, Vector3>();
        private static readonly Dictionary<Camera, Quaternion> originalCameraRotations = new Dictionary<Camera, Quaternion>();
        private static readonly Dictionary<Camera, float> originalCameraFovs = new Dictionary<Camera, float>();
        private static readonly Dictionary<Camera, float> originalCameraNearClips = new Dictionary<Camera, float>();
        private static readonly Dictionary<Camera, float> originalCameraFarClips = new Dictionary<Camera, float>();

        private static readonly Dictionary<Camera, Vector3?> overrideCameraPositions = new Dictionary<Camera, Vector3?>();
        private static readonly Dictionary<Camera, Quaternion?> overrideCameraRotations = new Dictionary<Camera, Quaternion?>();
        private static readonly Dictionary<Camera, float?> overrideCameraFovs = new Dictionary<Camera, float?>();
        private static readonly Dictionary<Camera, float?> overrideCameraNearClips = new Dictionary<Camera, float?>();
        private static readonly Dictionary<Camera, float?> overrideCameraFarClips = new Dictionary<Camera, float?>();

        private static List<Canvas> disabledCanvases = new List<Canvas>();

        public void Start()
        {
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(FreeCamPlugin));

            // The methods for the new input system must only be patched if the Assembly is loaded, otherwise they won't be found
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Unity.InputSystem"))
            {
                Logger.LogDebug("New input system present - patching additional methods");
                harmony.PatchAll(typeof(NewInputSystemPatches));
            }
        }

        public void Awake()
        {
            Logger = base.Logger;

            configToggleFreecamKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Toggle FreeCam", KeyCode.KeypadMultiply);
            configSelectCameraKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Select Camera", KeyCode.KeypadMinus);
            configListCamerasKey = Config.Bind("Keyboard Shortcuts - Plugin State", "List Cameras", KeyCode.Keypad5);
            configToggleGameFreezeKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Toggle Game Freeze", KeyCode.KeypadPeriod);
            configToggleUIVisibilityKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Toggle UI Visibility", KeyCode.KeypadPlus);
            configResetPositionKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera Position", KeyCode.KeypadDivide);
            configResetRotationKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera Rotation", KeyCode.KeypadDivide);
            configResetViewKey = Config.Bind("Keyboard Shortcuts - Plugin State", "Reset Camera View", KeyCode.KeypadDivide);

            configMoveForwardKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Forward", KeyCode.W);
            configMoveBackwardKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Backward", KeyCode.S);
            configMoveLeftKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Left", KeyCode.A);
            configMoveRightKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Right", KeyCode.D);
            configMoveUpKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Up", KeyCode.E);
            configMoveDownKey = Config.Bind("Keyboard Shortcuts - Camera Movement", "Move Down", KeyCode.Q);

            configRotatePitchForwardKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Pitch Forward", KeyCode.Keypad8);
            configRotatePitchBackwardKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Pitch Backward", KeyCode.Keypad2);
            configRotateYawLeftKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Yaw Left", KeyCode.Keypad4);
            configRotateYawRightKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Yaw Right", KeyCode.Keypad6);
            configRotateRollCounterClockwiseKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Roll Counter-clockwise", KeyCode.Keypad7);
            configRotateRollClockwiseKey = Config.Bind("Keyboard Shortcuts - Camera Rotation", "Roll Clockwise", KeyCode.Keypad9);

            configIncreaseFovKey = Config.Bind("Keyboard Shortcuts - Camera View", "Increase FOV", KeyCode.Alpha3);
            configDecreaseFovKey = Config.Bind("Keyboard Shortcuts - Camera View", "Decrease FOV", KeyCode.Alpha1);
            configIncreaseNearClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Increase Near Clip Plane", KeyCode.X);
            configDecreaseNearClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Decrease Near Clip Plane", KeyCode.Z);
            configIncreaseFarClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Increase Far Clip Plane", KeyCode.V);
            configDecreaseFarClipKey = Config.Bind("Keyboard Shortcuts - Camera View", "Decrease Far Clip Plane", KeyCode.C);

            configIncreaseMoveSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Increase Movement Speed", KeyCode.R);
            configDecreaseMoveSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Decrease Movement Speed", KeyCode.F);
            configIncreaseRotationSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Increase Rotation Speed", KeyCode.Keypad3);
            configDecreaseRotationSpeedKey = Config.Bind("Keyboard Shortcuts - Speed Control", "Decrease Rotation Speed", KeyCode.Keypad1);
        }

        public static void LogCameraList()
        {
            string message = $"Current cameras ({Camera.allCamerasCount}):";
            for (int i = 0; i < Camera.allCamerasCount; i++)
            {
                Camera camera = Camera.allCameras[i];
                message += $"\n    {i + 1}. {GetFullHierarchyPath(camera.gameObject)}";
                if (i == selectedCameraIndex)
                {
                    message += " (selected)";
                }
                if (overrideCameraPositions.TryGetValue(camera, out Vector3? position) && position != null)
                {
                    message += " (overriding position)";
                }
                if (overrideCameraRotations.TryGetValue(camera, out Quaternion? rotation) && rotation != null)
                {
                    message += " (overriding rotation)";
                }
                if (overrideCameraFovs.TryGetValue(camera, out float? fov) && fov != null)
                {
                    message += " (overriding fov)";
                }
                if (overrideCameraNearClips.TryGetValue(camera, out float? nearClip) && nearClip != null)
                {
                    message += " (overriding near clip plane)";
                }
                if (overrideCameraFarClips.TryGetValue(camera, out float? farClip) && farClip != null)
                {
                    message += " (overriding far clip plane)";
                }
            }
            Logger.LogMessage(message);
            if (selectedCameraIndex >= Camera.allCamerasCount)
            {
                Logger.LogWarning("No camera is selected as the selected index is greater than the number of cameras! Pressing the select camera key should fix this.");
            }
        }

        private static void StartPositionControl(Camera selectedCamera)
        {
            if (!overrideCameraPositions.ContainsKey(selectedCamera) || overrideCameraPositions[selectedCamera] == null)
            {
                originalCameraPositions[selectedCamera] = selectedCamera.transform.position;
                overrideCameraPositions[selectedCamera] = selectedCamera.transform.position;
            }
        }

        private static void StartRotationControl(Camera selectedCamera)
        {
            if (!overrideCameraRotations.ContainsKey(selectedCamera) || overrideCameraRotations[selectedCamera] == null)
            {
                originalCameraRotations[selectedCamera] = selectedCamera.transform.rotation;
                overrideCameraRotations[selectedCamera] = selectedCamera.transform.rotation;
            }
        }

        private static void StartViewControl(Camera selectedCamera)
        {
            if (!overrideCameraFovs.ContainsKey(selectedCamera) || overrideCameraFovs[selectedCamera] == null)
            {
                originalCameraFovs[selectedCamera] = selectedCamera.fieldOfView;
                overrideCameraFovs[selectedCamera] = selectedCamera.fieldOfView;
            }
            if (!overrideCameraNearClips.ContainsKey(selectedCamera) || overrideCameraNearClips[selectedCamera] == null)
            {
                originalCameraNearClips[selectedCamera] = selectedCamera.nearClipPlane;
                overrideCameraNearClips[selectedCamera] = selectedCamera.nearClipPlane;
            }
            if (!overrideCameraFarClips.ContainsKey(selectedCamera) || overrideCameraFarClips[selectedCamera] == null)
            {
                originalCameraFarClips[selectedCamera] = selectedCamera.farClipPlane;
                overrideCameraFarClips[selectedCamera] = selectedCamera.farClipPlane;
            }
        }

        private static void StopPositionControl(Camera selectedCamera)
        {
            overrideCameraPositions[selectedCamera] = null;
            if (originalCameraPositions.TryGetValue(selectedCamera, out Vector3 position))
            {
                selectedCamera.transform.position = position;
            }
        }

        private static void StopRotationControl(Camera selectedCamera)
        {
            overrideCameraRotations[selectedCamera] = null;
            if (originalCameraRotations.TryGetValue(selectedCamera, out Quaternion rotation))
            {
                selectedCamera.transform.rotation = rotation;
            }
        }

        private static void StopViewControl(Camera selectedCamera)
        {
            overrideCameraFovs[selectedCamera] = null;
            overrideCameraNearClips[selectedCamera] = null;
            overrideCameraFarClips[selectedCamera] = null;
            if (originalCameraFovs.TryGetValue(selectedCamera, out float fov))
            {
                selectedCamera.fieldOfView = fov;
            }
            if (originalCameraNearClips.TryGetValue(selectedCamera, out float nearClip))
            {
                selectedCamera.nearClipPlane = nearClip;
            }
            if (originalCameraFarClips.TryGetValue(selectedCamera, out float farClip))
            {
                selectedCamera.farClipPlane = farClip;
            }
        }

        private static void ProcessInput()
        {
            // Flag that our input is being processed to stop it being overridden
            processingInput = true;
            try
            {
                if (UnityInput.Current.GetKeyDown(configToggleFreecamKey.Value))
                {
                    freecamActive = !freecamActive;
                    Logger.LogMessage($"FreeCam is now {(freecamActive ? "enabled" : "disabled")}");
                }

                if (!freecamActive)
                {
                    return;
                }

                if (UnityInput.Current.GetKeyDown(configSelectCameraKey.Value))
                {
                    if (Camera.allCamerasCount == 0)
                    {
                        Logger.LogWarning("There are no cameras to select");
                    }
                    else
                    {
                        selectedCameraIndex = (selectedCameraIndex + 1) % Camera.allCamerasCount;
                        Logger.LogMessage($"Switched to camera {selectedCameraIndex + 1}/{Camera.allCamerasCount} at {GetFullHierarchyPath(Camera.allCameras[selectedCameraIndex].gameObject)}");
                    }
                }

                if (UnityInput.Current.GetKeyDown(configListCamerasKey.Value))
                {
                    LogCameraList();
                }

                if (UnityInput.Current.GetKeyDown(configToggleGameFreezeKey.Value))
                {
                    gameFrozen = !gameFrozen;
                    if (!gameFrozen)
                    {
                        Time.timeScale = oldTimeScale;
                    }
                    else
                    {
                        oldTimeScale = Time.timeScale;
                    }
                    Logger.LogMessage($"Game is now {(gameFrozen ? "frozen" : "un-frozen")}");
                }

                if (UnityInput.Current.GetKeyDown(configToggleUIVisibilityKey.Value))
                {
                    hideUI = !hideUI;
                    if (hideUI)
                    {
                        disabledCanvases = FindObjectsOfType<Canvas>().Where(c => c.isActiveAndEnabled).ToList();
                    }
                    else
                    {
                        foreach (Canvas canvas in disabledCanvases)
                        {
                            if (canvas == null)
                            {
                                continue;
                            }
                            canvas.enabled = true;
                        }
                    }
                    Logger.LogMessage($"UI is now {(hideUI ? "hidden" : "visible")}");
                }

                if (UnityInput.Current.GetKey(configIncreaseMoveSpeedKey.Value))
                {
                    moveSpeed += moveSpeed * Time.unscaledDeltaTime;
                }
                if (UnityInput.Current.GetKey(configDecreaseMoveSpeedKey.Value))
                {
                    moveSpeed -= moveSpeed * Time.unscaledDeltaTime;
                }
                if (UnityInput.Current.GetKey(configIncreaseRotationSpeedKey.Value))
                {
                    rotationSpeed += rotationSpeed * Time.unscaledDeltaTime;
                }
                if (UnityInput.Current.GetKey(configDecreaseRotationSpeedKey.Value))
                {
                    rotationSpeed -= rotationSpeed * Time.unscaledDeltaTime;
                }

                // Following key-binds need an existing selected camera
                if (selectedCameraIndex < Camera.allCamerasCount)
                {
                    Camera selectedCamera = Camera.allCameras[selectedCameraIndex];

                    if (UnityInput.Current.GetKeyDown(configResetPositionKey.Value))
                    {
                        StopPositionControl(selectedCamera);
                    }
                    if (UnityInput.Current.GetKeyDown(configResetRotationKey.Value))
                    {
                        StopRotationControl(selectedCamera);
                    }
                    if (UnityInput.Current.GetKeyDown(configResetViewKey.Value))
                    {
                        StopViewControl(selectedCamera);
                    }

                    if (UnityInput.Current.GetKey(configMoveForwardKey.Value))
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.forward * Time.unscaledDeltaTime * moveSpeed);
                    }
                    if (UnityInput.Current.GetKey(configMoveBackwardKey.Value))
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.back * Time.unscaledDeltaTime * moveSpeed);
                    }
                    if (UnityInput.Current.GetKey(configMoveLeftKey.Value))
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.left * Time.unscaledDeltaTime * moveSpeed);
                    }
                    if (UnityInput.Current.GetKey(configMoveRightKey.Value))
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.right * Time.unscaledDeltaTime * moveSpeed);
                    }
                    if (UnityInput.Current.GetKey(configMoveUpKey.Value))
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.up * Time.unscaledDeltaTime * moveSpeed);
                    }
                    if (UnityInput.Current.GetKey(configMoveDownKey.Value))
                    {
                        StartPositionControl(selectedCamera);
                        overrideCameraPositions[selectedCamera] += selectedCamera.transform.TransformDirection(Vector3.down * Time.unscaledDeltaTime * moveSpeed);
                    }

                    if (UnityInput.Current.GetKey(configRotatePitchForwardKey.Value))
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x - (Time.unscaledDeltaTime * rotationSpeed), currentEuler.y, currentEuler.z);
                    }
                    if (UnityInput.Current.GetKey(configRotatePitchBackwardKey.Value))
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x + (Time.unscaledDeltaTime * rotationSpeed), currentEuler.y, currentEuler.z);
                    }
                    if (UnityInput.Current.GetKey(configRotateYawLeftKey.Value))
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y - (Time.unscaledDeltaTime * rotationSpeed), currentEuler.z);
                    }
                    if (UnityInput.Current.GetKey(configRotateYawRightKey.Value))
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y + (Time.unscaledDeltaTime * rotationSpeed), currentEuler.z);
                    }
                    if (UnityInput.Current.GetKey(configRotateRollCounterClockwiseKey.Value))
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z + (Time.unscaledDeltaTime * rotationSpeed));
                    }
                    if (UnityInput.Current.GetKey(configRotateRollClockwiseKey.Value))
                    {
                        StartRotationControl(selectedCamera);
                        Vector3 currentEuler = overrideCameraRotations[selectedCamera].Value.eulerAngles;
                        overrideCameraRotations[selectedCamera] = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z - (Time.unscaledDeltaTime * rotationSpeed));
                    }

                    if (UnityInput.Current.GetKey(configIncreaseFovKey.Value))
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFovs[selectedCamera] += Time.unscaledDeltaTime * rotationSpeed;
                    }
                    if (UnityInput.Current.GetKey(configDecreaseFovKey.Value))
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFovs[selectedCamera] -= Time.unscaledDeltaTime * rotationSpeed;
                    }
                    if (UnityInput.Current.GetKey(configIncreaseNearClipKey.Value))
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraNearClips[selectedCamera] += Time.unscaledDeltaTime * moveSpeed;
                    }
                    if (UnityInput.Current.GetKey(configDecreaseNearClipKey.Value))
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraNearClips[selectedCamera] -= Time.unscaledDeltaTime * moveSpeed;
                        if (overrideCameraNearClips[selectedCamera] < 0.01f)
                        {
                            overrideCameraNearClips[selectedCamera] = 0.01f;
                        }
                    }
                    if (UnityInput.Current.GetKey(configIncreaseFarClipKey.Value))
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFarClips[selectedCamera] += Time.unscaledDeltaTime * moveSpeed;
                    }
                    if (UnityInput.Current.GetKey(configDecreaseFarClipKey.Value))
                    {
                        StartViewControl(selectedCamera);
                        overrideCameraFarClips[selectedCamera] -= Time.unscaledDeltaTime * moveSpeed;
                        if (overrideCameraFarClips[selectedCamera] < 0.01f)
                        {
                            overrideCameraFarClips[selectedCamera] = 0.01f;
                        }
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

        public void LateUpdate()
        {
            if (gameFrozen)
            {
                Time.timeScale = 0;
            }

            if (hideUI)
            {
                foreach (Canvas canvas in disabledCanvases)
                {
                    if (canvas == null)
                    {
                        continue;
                    }
                    canvas.enabled = false;
                }
            }

            foreach (Camera camera in Camera.allCameras)
            {
                if (overrideCameraPositions.ContainsKey(camera) && overrideCameraPositions[camera] != null)
                {
                    camera.transform.position = overrideCameraPositions[camera].Value;
                }
                if (overrideCameraRotations.ContainsKey(camera) && overrideCameraRotations[camera] != null)
                {
                    camera.transform.rotation = overrideCameraRotations[camera].Value;
                }
                if (overrideCameraFovs.ContainsKey(camera) && overrideCameraFovs[camera] != null)
                {
                    camera.fieldOfView = overrideCameraFovs[camera].Value;
                }
                if (overrideCameraNearClips.ContainsKey(camera) && overrideCameraNearClips[camera] != null)
                {
                    camera.nearClipPlane = overrideCameraNearClips[camera].Value;
                }
                if (overrideCameraFarClips.ContainsKey(camera) && overrideCameraFarClips[camera] != null)
                {
                    camera.farClipPlane = overrideCameraFarClips[camera].Value;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(string))]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(KeyCode))]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(string))]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(KeyCode))]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(string))]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(KeyCode))]
        public static bool OverrideKeybinds(ref bool __result)
        {
            if (freecamActive && !processingInput)
            {
                __result = false;
                return false;
            }
            return true;
        }

        public static class NewInputSystemPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem", "IsValueConsideredPressed")]
            [HarmonyPatch("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem", "isPressed", MethodType.Getter)]
            [HarmonyPatch("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem", "wasPressedThisFrame", MethodType.Getter)]
            [HarmonyPatch("UnityEngine.InputSystem.InputAction, Unity.InputSystem", "IsPressed")]
            [HarmonyPatch("UnityEngine.InputSystem.InputAction, Unity.InputSystem", "IsInProgress")]
            [HarmonyPatch("UnityEngine.InputSystem.InputAction, Unity.InputSystem", "WasPressedThisFrame")]
            [HarmonyPatch("UnityEngine.InputSystem.InputAction, Unity.InputSystem", "WasPerformedThisFrame")]
            public static bool OverrideNewKeybinds(ref bool __result)
            {
                if (freecamActive && !processingInput)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        public static string GetFullHierarchyPath(GameObject gameObject)
        {
            StringBuilder sb = new StringBuilder(gameObject.name);
            GameObject currentObject = gameObject.transform.parent?.gameObject;
            while (currentObject != null)
            {
                _ = sb.Insert(0, '/').Insert(0, currentObject.name);
                currentObject = currentObject.transform.parent?.gameObject;
            }
            return sb.ToString();
        }
    }
}
