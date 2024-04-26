# Unity FreeCam

Unity BepInEx plugin for enabling free control over in-game cameras, with support for both the legacy and new input system.

## Contents

- [Unity FreeCam](#unity-freecam)
  - [Contents](#contents)
  - [How to Use](#how-to-use)
    - [Toggling the Plugin](#toggling-the-plugin)
    - [Selecting a Camera](#selecting-a-camera)
    - [Manipulating the Camera](#manipulating-the-camera)
    - [Freezing the Game](#freezing-the-game)
    - [Hiding the UI](#hiding-the-ui)
  - [Default Key Map](#default-key-map)

## How to Use

This plugin requires [BepInEx](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.22) (ideally version `5.4.22`) to be installed in the target game. The default bindings for the keys mentioned in this section can be found below.

### Toggling the Plugin

The plugin can be toggled on and off with the `Toggle FreeCam` key. When the plugin is on, input to the game will be disabled and input to the plugin will be enabled. When off, the game can be controlled as normal, however the plugin will only respond to the `Toggle FreeCam` key. Repositioned cameras will retain their new position even when toggling the plugin off, allowing you to go back to controlling the game whilst keeping the camera in your desired position.

### Selecting a Camera

Some games may use multiple cameras. When this is the case, the `Select Camera` key can be used to cycle through every currently active camera. The camera control keys will only affect the currently selected camera. Selecting a new camera does not cause the current one to lose its position.

### Manipulating the Camera

Once the plugin has been toggled on and the desired camera has been selected, you can reposition and rotate the camera with the relevant keys as desired. The game will continue to have control over the camera's position and rotation until you start changing them, at which point the plugin will take control of the respective aspect of the camera from the game. If you wish to return the camera to its position or rotation prior to taking control of it, and give control of the camera back to the game, press the corresponding `Reset` key.

> [!TIP]
> If the expected position of a camera has moved since you took control of it, simply resetting the position may not always work as expected in some games. For this reason, it can be beneficial to enable the game freeze feature *before* taking control of a camera, especially in games where you know overriding the camera can cause issues.

### Freezing the Game

The plugin also has a feature which allows you to freeze the game whilst retaining control over the game's cameras. This allows you to move the camera around while keeping everything else in the scene stationary. You can toggle this on and off with the `Toggle Game Freeze` key.

> [!WARNING]
> Freezing may not work in every game, and in some games can introduce bugs. A game must be programmed to be fully framerate independent for it to work correctly.

### Hiding the UI

Pressing the `Toggle UI Visibility` key will hide all currently active `Canvas` components, which in most games will hide all UI elements. Pressing the key again will restore the visibility of the UI. UI hiding is not specific to any particular camera, all UIs visible to any and all cameras will be disabled.

> [!NOTE]
> If the game creates a new Canvas or enables a previously hidden Canvas while the Hide UI mode is already enabled, the new Canvas will **not** be hidden until the feature is toggled off and on again. Similarly, if the game attempts to hide a Canvas that has already been hidden by the plugin, the plugin may inadvertently re-enable the Canvas upon toggling the feature back off. For this reason, it can be a good idea to enable the game freeze feature before using the Hide UI feature.

## Default Key Map

| Action                             | Key                 |
|------------------------------------|---------------------|
| **Plugin State**                   |                     |
| Toggle FreeCam                     | Numpad Multiply (*) |
| Select Camera                      | Numpad Minus (-)    |
| List Current Cameras               | Numpad 5            |
| Toggle Game Freeze                 | Numpad Decimal (.)  |
| Toggle UI Visibility               | Numpad Plus (+)     |
| Reset Position                     | Numpad Divide (/)   |
| Reset Rotation                     | Numpad Divide (/)   |
| Reset View (FOV & Clipping Planes) | Numpad Divide (/)   |
| **Movement**                       |                     |
| Move Forward                       | W                   |
| Move Backward                      | S                   |
| Move Left                          | A                   |
| Move Right                         | D                   |
| Move Up                            | E                   |
| Move Down                          | Q                   |
| **Rotation**                       |                     |
| Pitch Forward                      | Numpad 8            |
| Pitch Backward                     | Numpad 2            |
| Yaw Left                           | Numpad 4            |
| Yaw Right                          | Numpad 6            |
| Roll Counter-clockwise             | Numpad 7            |
| Roll Clockwise                     | Numpad 9            |
| **View**                           |                     |
| Increase FOV                       | 3                   |
| Decrease FOV                       | 1                   |
| Increase Near Clip Plane           | X                   |
| Decrease Near Clip Plane           | Z                   |
| Increase Far Clip Plane            | V                   |
| Decrease Far Clip Plane            | C                   |
| **Speed**                          |                     |
| Increase Movement Speed            | R                   |
| Decrease Movement Speed            | F                   |
| Increase Rotation Speed            | Numpad 3            |
| Decrease Rotation Speed            | Numpad 1            |
