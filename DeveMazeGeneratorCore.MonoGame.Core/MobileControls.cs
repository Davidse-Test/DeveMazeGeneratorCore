using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using DeveMazeGeneratorCore.MonoGame.Core;

namespace DeveMazeGeneratorMonoGame
{
    /// <summary>
    /// Handles mobile UI controls for camera movement, maze settings, and view options
    /// </summary>
    public class MobileControls
    {
        private readonly TheGame _game;
        private SpriteBatch _spriteBatch;

        // Textures
        private Texture2D _buttonTexture;
        private Texture2D _iconTexture;

        // Control visibility
        public bool ShowControls { get; private set; } = false;



        #region UI Rectangles

        // Main hamburger button
        private Rectangle _hamburgerButtonRect;
        private Rectangle _debugUiButtonRect;

        // Movement controls (always visible when ShowControls is true)
        private Rectangle _forwardButtonRect;
        private Rectangle _backwardButtonRect;
        private Rectangle _leftButtonRect;
        private Rectangle _rightButtonRect;
        private Rectangle _upButtonRect;
        private Rectangle _downButtonRect;
        private Rectangle _lookControlRect;

        // Camera mode buttons
        private Rectangle _followCameraModeButtonRect;
        private Rectangle _freeCameraModeButtonRect;
        private Rectangle _fromAboveCameraModeButtonRect;
        private Rectangle _chaseCameraModeButtonRect;

        // Camera rotation controls
        private Rectangle _rotateCameraUpButtonRect;
        private Rectangle _rotateCameraDownButtonRect;
        private Rectangle _rotateCameraLeftButtonRect;
        private Rectangle _rotateCameraRightButtonRect;

        // Maze controls
        private Rectangle _increaseSizeButtonRect;
        private Rectangle _decreaseSizeButtonRect;
        private Rectangle _prevAlgorithmButtonRect;
        private Rectangle _nextAlgorithmButtonRect;
        private Rectangle _regenerateMazeButtonRect;

        // View controls
        private Rectangle _toggleRoofButtonRect;
        private Rectangle _toggleLightingButtonRect;
        private Rectangle _togglePathButtonRect;
        private Rectangle _increaseSpeedButtonRect;
        private Rectangle _decreaseSpeedButtonRect;

        #endregion

        // Control group panels
        private Rectangle _cameraModesPanel;
        private Rectangle _mazeControlsPanel;
        private Rectangle _viewControlsPanel;

        // Button states for camera movement
        private Dictionary<string, bool> _pressedButtons = new Dictionary<string, bool>
        {
            { "Forward", false },
            { "Backward", false },
            { "Left", false },
            { "Right", false },
            { "Up", false },
            { "Down", false },
        };

        // Look control state
        private bool _isLookControlActive = false;
        private Vector2 _lastTouchPosition;
        private bool _isMouseActive = false;
        private Vector2 _lastMousePosition;

        // Action timestamps to prevent rapid pressing
        private Dictionary<string, TimeSpan> _lastActionTime = new Dictionary<string, TimeSpan>();
        private const double ACTION_COOLDOWN_SECONDS = 0.3;

        public MobileControls(TheGame game)
        {
            _game = game;
        }

        /// <summary>
        /// Initialize controls, textures and positions
        /// </summary>
        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _spriteBatch = new SpriteBatch(graphicsDevice);

            // Create textures
            _buttonTexture = new Texture2D(graphicsDevice, 1, 1);
            _buttonTexture.SetData(new[] { Color.White });

            // Create icon texture for buttons
            _iconTexture = CreateIconTexture(graphicsDevice);

            // Position UI elements
            UpdateControlPositions();
        }

        /// <summary>
        /// Creates a simple icon texture with multiple colors
        /// </summary>
        private Texture2D CreateIconTexture(GraphicsDevice graphicsDevice)
        {
            // Create a small texture with a few colors for various icons
            Texture2D texture = new Texture2D(graphicsDevice, 16, 16);
            Color[] data = new Color[16 * 16];

            // Fill with white
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.White;
            }

            // Add some color patterns that we can use for different icons
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x == 0 || y == 0 || x == 15 || y == 15)
                    {
                        data[y * 16 + x] = Color.Black; // Border
                    }
                    else if ((x + y) % 2 == 0)
                    {
                        data[y * 16 + x] = new Color(200, 200, 200); // Subtle pattern
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Updates control positions based on screen size
        /// </summary>
        public void UpdateControlPositions()
        {
            int screenWidth = _game.ScreenWidth;
            int screenHeight = _game.ScreenHeight;

            // Sizing
            int buttonSize = (int)(Math.Min(screenWidth, screenHeight) * 0.13f);
            int smallButtonSize = (int)(buttonSize * 0.8f);
            int buttonMargin = buttonSize / 7;

            // Hamburger menu button (top left)
            int hamburgerSize = (int)(buttonSize * 0.8f);
            _hamburgerButtonRect = new Rectangle(
                buttonMargin,
                buttonMargin,
                hamburgerSize, hamburgerSize);

            // Debug UI button (top right)
            _debugUiButtonRect = new Rectangle(
                screenWidth - hamburgerSize - buttonMargin,
                buttonMargin,
                hamburgerSize, hamburgerSize);

            #region Movement Controls

            // Movement buttons (bottom left, like a D-pad)
            int movementCenterX = (int)(buttonSize * 1.5f) + buttonMargin * 3;
            int movementCenterY = screenHeight - buttonSize * 2;

            _forwardButtonRect = new Rectangle(
                movementCenterX,
                movementCenterY - buttonSize - buttonMargin,
                buttonSize, buttonSize);

            _backwardButtonRect = new Rectangle(
                movementCenterX,
                movementCenterY,
                buttonSize, buttonSize);

            _leftButtonRect = new Rectangle(
                movementCenterX - buttonSize - buttonMargin,
                movementCenterY,
                buttonSize, buttonSize);

            _rightButtonRect = new Rectangle(
                movementCenterX + buttonSize + buttonMargin,
                movementCenterY,
                buttonSize, buttonSize);

            // Up/Down buttons (to the left of d-pad)
            _upButtonRect = new Rectangle(
                buttonMargin,
                screenHeight - buttonSize * 2,
                buttonSize, buttonSize);

            _downButtonRect = new Rectangle(
                buttonMargin,
                screenHeight - buttonSize,
                buttonSize, buttonSize);

            // Look control area (bottom right)
            int lookControlSize = (int)(buttonSize * 1.7f);
            _lookControlRect = new Rectangle(
                screenWidth - lookControlSize - buttonMargin,
                screenHeight - lookControlSize - buttonMargin,
                lookControlSize, lookControlSize);

            // Camera rotation buttons (right side, above look control)
            int rotationButtonY = screenHeight - lookControlSize - buttonMargin * 2 - buttonSize * 2;
            int rotationCenterX = screenWidth - buttonSize - buttonMargin - buttonSize / 2;

            _rotateCameraUpButtonRect = new Rectangle(
                rotationCenterX,
                rotationButtonY - buttonSize - buttonMargin,
                buttonSize, buttonSize);

            _rotateCameraDownButtonRect = new Rectangle(
                rotationCenterX,
                rotationButtonY,
                buttonSize, buttonSize);

            _rotateCameraLeftButtonRect = new Rectangle(
                rotationCenterX - buttonSize - buttonMargin,
                rotationButtonY,
                buttonSize, buttonSize);

            _rotateCameraRightButtonRect = new Rectangle(
                rotationCenterX + buttonSize + buttonMargin,
                rotationButtonY,
                buttonSize, buttonSize);

            // Camera mode buttons (top of screen, center)
            int cameraModeButtonY = buttonMargin * 2;
            int cameraModeButtonWidth = buttonSize * 3;
            int cameraModeSpacing = buttonMargin * 2;
            int totalCameraModeWidth = cameraModeButtonWidth * 3 + cameraModeSpacing * 2;
            int cameraModeStartX = (screenWidth - totalCameraModeWidth) / 2;

            _followCameraModeButtonRect = new Rectangle(
                cameraModeStartX,
                cameraModeButtonY,
                cameraModeButtonWidth, buttonSize);

            _freeCameraModeButtonRect = new Rectangle(
                cameraModeStartX + cameraModeButtonWidth + cameraModeSpacing,
                cameraModeButtonY,
                cameraModeButtonWidth, buttonSize);

            _fromAboveCameraModeButtonRect = new Rectangle(
                cameraModeStartX + (cameraModeButtonWidth + cameraModeSpacing) * 2,
                cameraModeButtonY,
                cameraModeButtonWidth, buttonSize);

            _chaseCameraModeButtonRect = new Rectangle(
                cameraModeStartX + (cameraModeButtonWidth + cameraModeSpacing) * 3,
                cameraModeButtonY,
                cameraModeButtonWidth, buttonSize);

            #endregion

            #region Control Panels

            // Calculate panel positions (centered when UI is shown)
            int panelWidth = (int)(screenWidth * 0.9f);
            int panelHeight = (int)(screenHeight * 0.5f);
            int panelX = (screenWidth - panelWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 3;

            // Camera modes panel - no longer needed as buttons are now always visible
            _cameraModesPanel = new Rectangle(0, 0, 0, 0);

            // Maze controls panel
            _mazeControlsPanel = new Rectangle(
                panelX,
                panelY,
                panelWidth, smallButtonSize * 2 + buttonMargin * 3);

            // Maze control buttons
            int mazeButtonWidth = (panelWidth - buttonMargin * 6) / 5;

            _decreaseSizeButtonRect = new Rectangle(
                panelX + buttonMargin,
                panelY + buttonMargin,
                mazeButtonWidth, smallButtonSize);

            _increaseSizeButtonRect = new Rectangle(
                panelX + mazeButtonWidth + buttonMargin * 2,
                panelY + buttonMargin,
                mazeButtonWidth, smallButtonSize);

            _prevAlgorithmButtonRect = new Rectangle(
                panelX + mazeButtonWidth * 2 + buttonMargin * 3,
                panelY + buttonMargin,
                mazeButtonWidth, smallButtonSize);

            _nextAlgorithmButtonRect = new Rectangle(
                panelX + mazeButtonWidth * 3 + buttonMargin * 4,
                panelY + buttonMargin,
                mazeButtonWidth, smallButtonSize);

            _regenerateMazeButtonRect = new Rectangle(
                panelX + mazeButtonWidth * 4 + buttonMargin * 5,
                panelY + buttonMargin,
                mazeButtonWidth, smallButtonSize);

            // View controls panel
            _viewControlsPanel = new Rectangle(
                panelX,
                panelY + _mazeControlsPanel.Height + buttonMargin,
                panelWidth, smallButtonSize * 2 + buttonMargin * 3);

            // View control buttons
            int viewButtonWidth = (panelWidth - buttonMargin * 6) / 5;

            _toggleRoofButtonRect = new Rectangle(
                panelX + buttonMargin,
                panelY + _mazeControlsPanel.Height + buttonMargin * 2,
                viewButtonWidth, smallButtonSize);

            _toggleLightingButtonRect = new Rectangle(
                panelX + viewButtonWidth + buttonMargin * 2,
                panelY + _mazeControlsPanel.Height + buttonMargin * 2,
                viewButtonWidth, smallButtonSize);

            _togglePathButtonRect = new Rectangle(
                panelX + viewButtonWidth * 2 + buttonMargin * 3,
                panelY + _mazeControlsPanel.Height + buttonMargin * 2,
                viewButtonWidth, smallButtonSize);

            _decreaseSpeedButtonRect = new Rectangle(
                panelX + viewButtonWidth * 3 + buttonMargin * 4,
                panelY + _mazeControlsPanel.Height + buttonMargin * 2,
                viewButtonWidth, smallButtonSize);

            _increaseSpeedButtonRect = new Rectangle(
                panelX + viewButtonWidth * 4 + buttonMargin * 5,
                panelY + _mazeControlsPanel.Height + buttonMargin * 2,
                viewButtonWidth, smallButtonSize);

            #endregion
        }

        /// <summary>
        /// Update mobile controls state
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Reset button states
            foreach (var key in _pressedButtons.Keys)
            {
                _pressedButtons[key] = false;
            }

            // Process touch input
            TouchCollection touchCollection = TouchPanel.GetState();

            foreach (TouchLocation touch in touchCollection)
            {
                ProcessTouch(touch, gameTime);
            }

            // Process mouse input
            ProcessMouseInput(gameTime);

            if (touchCollection.Count == 0 && !_isMouseActive)
            {
                _isLookControlActive = false;
            }

            // Apply camera movement if any buttons are pressed
            ApplyCameraMovement(gameTime);
        }

        /// <summary>
        /// Process mouse input for all controls
        /// </summary>
        private void ProcessMouseInput(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();
            Vector2 mousePosition = new Vector2(mouseState.X, mouseState.Y);

            // Hamburger menu button to toggle all controls
            if (InputDing.TouchedOrMouseClickedInRect(_hamburgerButtonRect))
            {
                ShowControls = !ShowControls;
                return;
            }

            // Debug UI button
            if (InputDing.TouchedOrMouseClickedInRect(_debugUiButtonRect))
            {
                _game.ToggleUI();
                return;
            }

            // Process camera mode buttons (always visible)
            if (InputDing.TouchedOrMouseClickedInRect(_followCameraModeButtonRect))
            {
                _game.SetCameraMode(ActiveCameraMode.FollowCamera);
                return;
            }
            if (InputDing.TouchedOrMouseClickedInRect(_freeCameraModeButtonRect))
            {
                _game.SetCameraMode(ActiveCameraMode.FreeCamera);
                return;
            }
            if (InputDing.TouchedOrMouseClickedInRect(_fromAboveCameraModeButtonRect))
            {
                _game.SetCameraMode(ActiveCameraMode.FromAboveCamera);
                return;
            }
            if (InputDing.TouchedOrMouseClickedInRect(_chaseCameraModeButtonRect))
            {
                _game.SetCameraMode(ActiveCameraMode.ChaseCamera);
                return;
            }

            // Movement buttons are always active
            ProcessMovementControlsMouse(mouseState, gameTime, mousePosition);

            // Camera rotation buttons are always active
            ProcessCameraRotationMouse(mouseState, gameTime, mousePosition);

            // If controls are hidden, don't process other UI
            if (!ShowControls)
                return;

            // Process maze control buttons
            ProcessMazeControlsMouse(mousePosition, gameTime);

            // Process view control buttons
            ProcessViewControlsMouse(mousePosition, gameTime);
        }

        /// <summary>
        /// Process mouse input for movement controls
        /// </summary>
        private void ProcessMovementControlsMouse(MouseState mouseState, GameTime gameTime, Vector2 position)
        {
            // Handle button clicks for movement
            _pressedButtons["Forward"] |= _forwardButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed;
            _pressedButtons["Backward"] |= _backwardButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed;
            _pressedButtons["Left"] |= _leftButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed;
            _pressedButtons["Right"] |= _rightButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed;
            _pressedButtons["Up"] |= _upButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed;
            _pressedButtons["Down"] |= _downButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed;

            // Look control area
            if (_lookControlRect.Contains(position))
            {
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (!_isMouseActive)
                    {
                        _isMouseActive = true;
                        _lastMousePosition = position;
                    }
                    else
                    {
                        // Calculate delta from last position
                        Vector2 delta = position - _lastMousePosition;

                        // Apply rotation based on mouse movement
                        Basic3dExampleCamera camera = _game.GetCamera();
                        if (camera != null)
                        {
                            if (delta.X != 0)
                                camera.RotateLeftOrRight(gameTime, -delta.X * 0.5f);

                            if (delta.Y != 0)
                                camera.RotateUpOrDown(gameTime, -delta.Y * 0.5f);
                        }

                        _lastMousePosition = position;
                    }
                }
                else
                {
                    _isMouseActive = false;
                }
            }
        }

        /// <summary>
        /// Process mouse input for camera rotation controls
        /// </summary>
        private void ProcessCameraRotationMouse(MouseState mouseState, GameTime gameTime, Vector2 position)
        {
            Basic3dExampleCamera camera = _game.GetCamera();
            if (camera == null)
                return;

            // Camera rotation up
            if (_rotateCameraUpButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed)
            {
                camera.RotateUp(gameTime);
            }

            // Camera rotation down
            if (_rotateCameraDownButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed)
            {
                camera.RotateDown(gameTime);
            }

            // Camera rotation left
            if (_rotateCameraLeftButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed)
            {
                camera.RotateLeft(gameTime);
            }

            // Camera rotation right
            if (_rotateCameraRightButtonRect.Contains(position) && mouseState.LeftButton == ButtonState.Pressed)
            {
                camera.RotateRight(gameTime);
            }
        }

        /// <summary>
        /// Process mouse events for maze controls
        /// </summary>
        private void ProcessMazeControlsMouse(Vector2 position, GameTime gameTime)
        {
            // Check cooldown to prevent rapid button presses
            TimeSpan currentTime = gameTime.TotalGameTime;

            // Increase maze size
            if (InputDing.TouchedOrMouseClickedInRect(_increaseSizeButtonRect) && CheckActionCooldown("size", currentTime))
            {
                _game.IncreaseMazeSize();
                _lastActionTime["size"] = currentTime;
            }

            // Decrease maze size
            else if (InputDing.TouchedOrMouseClickedInRect(_decreaseSizeButtonRect) && CheckActionCooldown("size", currentTime))
            {
                _game.DecreaseMazeSize();
                _lastActionTime["size"] = currentTime;
            }

            // Previous algorithm
            else if (InputDing.TouchedOrMouseClickedInRect(_prevAlgorithmButtonRect) && CheckActionCooldown("alg", currentTime))
            {
                _game.PreviousAlgorithm();
                _lastActionTime["alg"] = currentTime;
            }

            // Next algorithm
            else if (InputDing.TouchedOrMouseClickedInRect(_nextAlgorithmButtonRect) && CheckActionCooldown("alg", currentTime))
            {
                _game.NextAlgorithm();
                _lastActionTime["alg"] = currentTime;
            }

            // Regenerate maze
            else if (InputDing.TouchedOrMouseClickedInRect(_regenerateMazeButtonRect) && CheckActionCooldown("regen", currentTime))
            {
                _game.RegenerateMaze();
                _lastActionTime["regen"] = currentTime;
            }
        }

        /// <summary>
        /// Process mouse events for view controls
        /// </summary>
        private void ProcessViewControlsMouse(Vector2 position, GameTime gameTime)
        {
            // Check cooldown to prevent rapid button presses
            TimeSpan currentTime = gameTime.TotalGameTime;

            // Toggle roof visibility
            if (InputDing.TouchedOrMouseClickedInRect(_toggleRoofButtonRect) && CheckActionCooldown("roof", currentTime))
            {
                _game.ToggleRoof();
                _lastActionTime["roof"] = currentTime;
            }

            // Toggle lighting
            else if (InputDing.TouchedOrMouseClickedInRect(_toggleLightingButtonRect) && CheckActionCooldown("light", currentTime))
            {
                _game.ToggleLighting();
                _lastActionTime["light"] = currentTime;
            }

            // Toggle path visibility
            else if (InputDing.TouchedOrMouseClickedInRect(_togglePathButtonRect) && CheckActionCooldown("path", currentTime))
            {
                _game.TogglePath();
                _lastActionTime["path"] = currentTime;
            }

            // Increase speed
            else if (InputDing.TouchedOrMouseClickedInRect(_increaseSpeedButtonRect) && CheckActionCooldown("speed", currentTime))
            {
                _game.IncreaseSpeed();
                _lastActionTime["speed"] = currentTime;
            }

            // Decrease speed
            else if (InputDing.TouchedOrMouseClickedInRect(_decreaseSpeedButtonRect) && CheckActionCooldown("speed", currentTime))
            {
                _game.DecreaseSpeed();
                _lastActionTime["speed"] = currentTime;
            }
        }

        /// <summary>
        /// Process an individual touch input
        /// </summary>
        private void ProcessTouch(TouchLocation touch, GameTime gameTime)
        {
            Vector2 position = touch.Position;

            // Hamburger menu button to toggle all controls
            if (touch.State == TouchLocationState.Pressed && _hamburgerButtonRect.Contains(position))
            {
                ShowControls = !ShowControls;
                return;
            }

            // Debug UI button
            if (touch.State == TouchLocationState.Pressed && _debugUiButtonRect.Contains(position))
            {
                _game.ToggleUI();
                return;
            }

          

            // Movement buttons are always active
            ProcessMovementTouch(touch, gameTime, position);

            // Camera rotation buttons are always active
            ProcessCameraRotationTouch(touch, gameTime, position);

            // If controls are hidden, don't process other UI
            if (!ShowControls)
                return;

            // Process maze control buttons
            if (touch.State == TouchLocationState.Pressed)
            {
                // Process maze control buttons
                ProcessMazeControls(touch, position, gameTime);

                // Process view control buttons
                ProcessViewControls(touch, position, gameTime);
            }
        }

        /// <summary>
        /// Process touch for movement controls
        /// </summary>
        private void ProcessMovementTouch(TouchLocation touch, GameTime gameTime, Vector2 position)
        {
            // Handle button touches for movement
            if ((touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved))
            {
                // Movement buttons
                if (_forwardButtonRect.Contains(position))
                    _pressedButtons["Forward"] = true;
                else if (_backwardButtonRect.Contains(position))
                    _pressedButtons["Backward"] = true;
                else if (_leftButtonRect.Contains(position))
                    _pressedButtons["Left"] = true;
                else if (_rightButtonRect.Contains(position))
                    _pressedButtons["Right"] = true;
                else if (_upButtonRect.Contains(position))
                    _pressedButtons["Up"] = true;
                else if (_downButtonRect.Contains(position))
                    _pressedButtons["Down"] = true;

                // Look control area
                if (_lookControlRect.Contains(position))
                {
                    if (!_isLookControlActive || touch.State == TouchLocationState.Pressed)
                    {
                        _isLookControlActive = true;
                        _lastTouchPosition = position;
                    }
                    else if (_isLookControlActive && touch.State == TouchLocationState.Moved)
                    {
                        // Calculate delta from last position
                        Vector2 delta = position - _lastTouchPosition;

                        // Apply rotation based on touch movement
                        Basic3dExampleCamera camera = _game.GetCamera();
                        if (camera != null)
                        {
                            if (delta.X != 0)
                                camera.RotateLeftOrRight(gameTime, -delta.X * 0.5f);

                            if (delta.Y != 0)
                                camera.RotateUpOrDown(gameTime, -delta.Y * 0.5f);
                        }

                        _lastTouchPosition = position;
                    }
                }
            }

            // Handle touch release
            if (touch.State == TouchLocationState.Released && _lookControlRect.Contains(position))
            {
                _isLookControlActive = false;
            }
        }

        /// <summary>
        /// Process touch for camera rotation controls
        /// </summary>
        private void ProcessCameraRotationTouch(TouchLocation touch, GameTime gameTime, Vector2 position)
        {
            Basic3dExampleCamera camera = _game.GetCamera();
            if (camera == null)
                return;

            if (touch.State == TouchLocationState.Pressed || touch.State == TouchLocationState.Moved)
            {
                // Camera rotation up
                if (_rotateCameraUpButtonRect.Contains(position))
                {
                    camera.RotateUp(gameTime);
                }

                // Camera rotation down
                else if (_rotateCameraDownButtonRect.Contains(position))
                {
                    camera.RotateDown(gameTime);
                }

                // Camera rotation left
                else if (_rotateCameraLeftButtonRect.Contains(position))
                {
                    camera.RotateLeft(gameTime);
                }

                // Camera rotation right
                else if (_rotateCameraRightButtonRect.Contains(position))
                {
                    camera.RotateRight(gameTime);
                }
            }
        }

        /// <summary>
        /// Handle touch events for maze controls
        /// </summary>
        private void ProcessMazeControls(TouchLocation touch, Vector2 position, GameTime gameTime)
        {
            if (touch.State != TouchLocationState.Pressed)
                return;

            // Check cooldown to prevent rapid button presses
            TimeSpan currentTime = gameTime.TotalGameTime;

            // Increase maze size
            if (_increaseSizeButtonRect.Contains(position) && CheckActionCooldown("size", currentTime))
            {
                _game.IncreaseMazeSize();
                _lastActionTime["size"] = currentTime;
            }

            // Decrease maze size
            else if (_decreaseSizeButtonRect.Contains(position) && CheckActionCooldown("size", currentTime))
            {
                _game.DecreaseMazeSize();
                _lastActionTime["size"] = currentTime;
            }

            // Previous algorithm
            else if (_prevAlgorithmButtonRect.Contains(position) && CheckActionCooldown("alg", currentTime))
            {
                _game.PreviousAlgorithm();
                _lastActionTime["alg"] = currentTime;
            }

            // Next algorithm
            else if (_nextAlgorithmButtonRect.Contains(position) && CheckActionCooldown("alg", currentTime))
            {
                _game.NextAlgorithm();
                _lastActionTime["alg"] = currentTime;
            }

            // Regenerate maze
            else if (_regenerateMazeButtonRect.Contains(position) && CheckActionCooldown("regen", currentTime))
            {
                _game.RegenerateMaze();
                _lastActionTime["regen"] = currentTime;
            }
        }

        /// <summary>
        /// Handle touch events for view controls
        /// </summary>
        private void ProcessViewControls(TouchLocation touch, Vector2 position, GameTime gameTime)
        {
            if (touch.State != TouchLocationState.Pressed)
                return;

            // Check cooldown to prevent rapid button presses
            TimeSpan currentTime = gameTime.TotalGameTime;

            // Toggle roof visibility
            if (_toggleRoofButtonRect.Contains(position) && CheckActionCooldown("roof", currentTime))
            {
                _game.ToggleRoof();
                _lastActionTime["roof"] = currentTime;
            }

            // Toggle lighting
            else if (_toggleLightingButtonRect.Contains(position) && CheckActionCooldown("light", currentTime))
            {
                _game.ToggleLighting();
                _lastActionTime["light"] = currentTime;
            }

            // Toggle path visibility
            else if (_togglePathButtonRect.Contains(position) && CheckActionCooldown("path", currentTime))
            {
                _game.TogglePath();
                _lastActionTime["path"] = currentTime;
            }

            // Increase speed
            else if (_increaseSpeedButtonRect.Contains(position) && CheckActionCooldown("speed", currentTime))
            {
                _game.IncreaseSpeed();
                _lastActionTime["speed"] = currentTime;
            }

            // Decrease speed
            else if (_decreaseSpeedButtonRect.Contains(position) && CheckActionCooldown("speed", currentTime))
            {
                _game.DecreaseSpeed();
                _lastActionTime["speed"] = currentTime;
            }
        }

        /// <summary>
        /// Check if enough time has passed since the last action of this type
        /// </summary>
        private bool CheckActionCooldown(string actionType, TimeSpan currentTime)
        {
            if (!_lastActionTime.ContainsKey(actionType))
            {
                return true;
            }

            return (currentTime - _lastActionTime[actionType]).TotalSeconds >= ACTION_COOLDOWN_SECONDS;
        }

        /// <summary>
        /// Apply camera movement based on button states
        /// </summary>
        private void ApplyCameraMovement(GameTime gameTime)
        {
            Basic3dExampleCamera camera = _game.GetCamera();
            if (camera == null)
                return;

            // Apply movement based on pressed buttons
            if (_pressedButtons["Forward"])
                camera.MoveForward(gameTime);
            if (_pressedButtons["Backward"])
                camera.MoveBackward(gameTime);
            if (_pressedButtons["Left"])
                camera.MoveLeft(gameTime);
            if (_pressedButtons["Right"])
                camera.MoveRight(gameTime);
            if (_pressedButtons["Up"])
                camera.MoveUp(gameTime);
            if (_pressedButtons["Down"])
                camera.MoveDown(gameTime);
        }

        /// <summary>
        /// Draw mobile UI controls
        /// </summary>
        public void Draw()
        {
            _spriteBatch.Begin();

            // Always draw the hamburger and debug buttons
            DrawButton(_hamburgerButtonRect, Color.Black * 0.7f, "≡", Color.White);
            DrawButton(_debugUiButtonRect, Color.DarkSlateGray * 0.8f, "⚙", Color.White);

            // Always draw camera mode buttons at the top
            DrawCameraModeButtons();

            // Always draw movement controls
            DrawMovementControls();

            // Always draw camera rotation controls
            DrawCameraRotationControls();

            // Draw additional UI panels if controls are visible
            if (ShowControls)
            {
                DrawPanel(_mazeControlsPanel, Color.Black * 0.5f);
                DrawMazeControls();

                DrawPanel(_viewControlsPanel, Color.Black * 0.5f);
                DrawViewControls();
            }

            _spriteBatch.End();
        }

        /// <summary>
        /// Draw a panel with background
        /// </summary>
        private void DrawPanel(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_buttonTexture, rect, color);

            // Draw border
            int thickness = 2;
            Rectangle borderRect = new Rectangle(rect.X, rect.Y, rect.Width, thickness);
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.6f); // Top

            borderRect.Y = rect.Y + rect.Height - thickness;
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.6f); // Bottom

            borderRect = new Rectangle(rect.X, rect.Y, thickness, rect.Height);
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.6f); // Left

            borderRect.X = rect.X + rect.Width - thickness;
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.6f); // Right
        }

        /// <summary>
        /// Draw movement control buttons
        /// </summary>
        private void DrawMovementControls()
        {
            // Draw movement buttons
            DrawButton(_forwardButtonRect, Color.Black * 0.6f, "▲", Color.White);
            DrawButton(_backwardButtonRect, Color.Black * 0.6f, "▼", Color.White);
            DrawButton(_leftButtonRect, Color.Black * 0.6f, "◄", Color.White);
            DrawButton(_rightButtonRect, Color.Black * 0.6f, "►", Color.White);

            // Draw up/down buttons
            DrawButton(_upButtonRect, Color.Black * 0.6f, "Q/Up", Color.White);
            DrawButton(_downButtonRect, Color.Black * 0.6f, "E/Down", Color.White);

            // Draw look control area
            DrawButton(_lookControlRect, Color.Black * 0.5f, "Look", Color.White);
        }

        /// <summary>
        /// Draw camera mode buttons
        /// </summary>
        private void DrawCameraModeButtons()
        {
            Color followColor = _game.ActiveCameraMode == ActiveCameraMode.FollowCamera ? Color.Green * 0.7f : Color.DarkSlateGray * 0.7f;
            Color freeColor = _game.ActiveCameraMode == ActiveCameraMode.FreeCamera ? Color.Green * 0.7f : Color.DarkSlateGray * 0.7f;
            Color fromAboveColor = _game.ActiveCameraMode == ActiveCameraMode.FromAboveCamera ? Color.Green * 0.7f : Color.DarkSlateGray * 0.7f;
            Color chaseColor = _game.ActiveCameraMode == ActiveCameraMode.ChaseCamera ? Color.Green * 0.7f : Color.DarkSlateGray * 0.7f;

            DrawButton(_followCameraModeButtonRect, followColor, "Follow", Color.White);
            DrawButton(_freeCameraModeButtonRect, freeColor, "Free", Color.White);
            DrawButton(_fromAboveCameraModeButtonRect, fromAboveColor, "Top", Color.White);
            DrawButton(_fromAboveCameraModeButtonRect, chaseColor, "Chase", Color.White);
        }

        /// <summary>
        /// Draw camera rotation controls
        /// </summary>
        private void DrawCameraRotationControls()
        {
            // Draw camera rotation buttons
            DrawButton(_rotateCameraUpButtonRect, Color.Black * 0.6f, "▲", Color.White);
            DrawButton(_rotateCameraDownButtonRect, Color.Black * 0.6f, "▼", Color.White);
            DrawButton(_rotateCameraLeftButtonRect, Color.Black * 0.6f, "◄", Color.White);
            DrawButton(_rotateCameraRightButtonRect, Color.Black * 0.6f, "►", Color.White);
        }

        /// <summary>
        /// Draw maze control buttons
        /// </summary>
        private void DrawMazeControls()
        {
            DrawButton(_decreaseSizeButtonRect, Color.DarkBlue * 0.7f, "Size-", Color.White);
            DrawButton(_increaseSizeButtonRect, Color.DarkBlue * 0.7f, "Size+", Color.White);
            DrawButton(_prevAlgorithmButtonRect, Color.DarkGreen * 0.7f, "Alg◄", Color.White);
            DrawButton(_nextAlgorithmButtonRect, Color.DarkGreen * 0.7f, "Alg►", Color.White);
            DrawButton(_regenerateMazeButtonRect, Color.DarkRed * 0.7f, "Regen", Color.White);
        }

        /// <summary>
        /// Draw view control buttons
        /// </summary>
        private void DrawViewControls()
        {
            // Draw toggle buttons
            Color roofColor = _game.DrawRoof ? Color.Green * 0.7f : Color.DarkRed * 0.7f;
            Color lightingColor = _game.Lighting ? Color.Green * 0.7f : Color.DarkRed * 0.7f;
            Color pathColor = _game.DrawPath ? Color.Green * 0.7f : Color.DarkRed * 0.7f;

            DrawButton(_toggleRoofButtonRect, roofColor, "Roof", Color.White);
            DrawButton(_toggleLightingButtonRect, lightingColor, "Light", Color.White);
            DrawButton(_togglePathButtonRect, pathColor, "Path", Color.White);

            // Draw speed controls
            DrawButton(_decreaseSpeedButtonRect, Color.DarkBlue * 0.7f, "Spd-", Color.White);
            DrawButton(_increaseSpeedButtonRect, Color.DarkBlue * 0.7f, "Spd+", Color.White);
        }

        /// <summary>
        /// Helper to draw a button with text
        /// </summary>
        private void DrawButton(Rectangle rect, Color backgroundColor, string text, Color textColor)
        {
            // Draw button background
            _spriteBatch.Draw(_buttonTexture, rect, backgroundColor);

            // Draw button border
            int borderThickness = Math.Max(1, rect.Width / 35);
            Rectangle borderRect = new Rectangle(rect.X, rect.Y, rect.Width, borderThickness);
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.5f); // Top

            borderRect.Y = rect.Y + rect.Height - borderThickness;
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.5f); // Bottom

            borderRect = new Rectangle(rect.X, rect.Y, borderThickness, rect.Height);
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.5f); // Left

            borderRect.X = rect.X + rect.Width - borderThickness;
            _spriteBatch.Draw(_buttonTexture, borderRect, Color.White * 0.5f); // Right

            // Draw text (if SpriteFont is available)
            if (ContentDing.spriteFont != null)
            {
                Vector2 textSize = ContentDing.spriteFont.MeasureString(text);
                Vector2 textPosition = new Vector2(
                    rect.X + (rect.Width - textSize.X) / 2,
                    rect.Y + (rect.Height - textSize.Y) / 2
                );
                _spriteBatch.DrawString(ContentDing.spriteFont, text, textPosition, textColor);
            }
        }
    }
}