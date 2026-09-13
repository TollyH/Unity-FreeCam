using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity_FreeCam
{
    public sealed partial class FreeCamPlugin
    {
        private class FreeCamIMGUI : MonoBehaviour
        {
            public bool ShowWindow = true;

            // By default, place the window in the bottom-left of the screen with a 10px margin, at 270x500px in size.
            private Rect windowArea = new Rect(10, Screen.currentResolution.height - 510f, 270f, 500f);

            private Vector2 windowScrollPosition;

            private int windowId;

            public void Awake()
            {
                // Use this class instance's unique hash code as the window ID
                // to ensure all instances of this class represent a separate window.
                windowId = RuntimeHelpers.GetHashCode(this);
            }

            public void OnGUI()
            {
                if (ShowWindow)
                {
                    windowArea = GUILayout.Window(windowId, windowArea, DrawWindow, $"Unity-FreeCam {Version}");
                }
            }

            private void DrawWindow(int _)
            {
                windowScrollPosition = GUILayout.BeginScrollView(windowScrollPosition);
                GUILayout.BeginVertical(GUI.skin.box);

                if (GUILayout.Button($"Hide Window (Re-Open with {configPluginUIShowKey.Value})"))
                {
                    ShowWindow = false;
                }

                GUILayout.Label($"Plugin controls: {(freecamActive ? "Active" : "Inactive")}");
                GUILayout.Label($"Game freeze: {(gameFrozen ? "Active" : "Inactive")}");
                GUILayout.Label($"Game UI hidden: {(hideUI ? "Yes" : "No")}");
                GUILayout.Space(10);
                GUILayout.Label($"Movement speed: {moveSpeed:N2}");
                GUILayout.Label($"Rotation speed: {rotationSpeed:N2}");

                GUILayout.Label($"Active cameras ({Camera.allCamerasCount}):");
                for (int i = 0; i < Camera.allCamerasCount; i++)
                {
                    Camera camera = Camera.allCameras[i];

                    GUILayout.BeginHorizontal(GUI.skin.box);

                    string cameraPath = GetFullHierarchyPath(camera.gameObject);
                    if (i == selectedCameraIndex)
                    {
                        cameraPath = "(*) " + cameraPath;
                    }
                    GUILayout.Label(cameraPath);

                    if (GUILayout.Button("Select"))
                    {
                        selectedCameraIndex = i;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
                GUILayout.EndScrollView();

                GUILayout.Label("https://github.com/TollyH/Unity-FreeCam");

                GUI.DragWindow();
            }
        }
    }
}
