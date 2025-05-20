using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using DeveMazeGeneratorMonoGame;

namespace DeveMazeGeneratorCore.MonoGame.Core
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
        
        // Control group visibility
        public bool ShowControls { get; private set; } = true;
        public bool ShowCameraControls { get; private set; } = true;
        public bool ShowMazeControls { get; private set; } = false;
        public bool ShowViewControls { get; private set; } = false;
        
        // Camera mode
        public bool IsFpsMode { get; private set; } = true;
        
        // Current control group
        private enum ControlGroup
        {
            Camera,
            Maze,
            View
        }
        private ControlGroup _activeControlGroup = ControlGroup.Camera;
        
        #region UI Rectangles
        
        // Top menu buttons
        private Rectangle _mainMenuButtonRect;
        private Rectangle _toggleControlsButtonRect;
        
        // Main menu buttons
        private Rectangle _cameraControlsButtonRect;
        private Rectangle _mazeControlsButtonRect;
        private Rectangle _viewControlsButtonRect;
        
        // Camera controls
        private Rectangle _forwardButtonRect;
        private Rectangle _backwardButtonRect;
        private Rectangle _leftButtonRect;
        private Rectangle _rightButtonRect;
        private Rectangle _upButtonRect;
        private Rectangle _downButtonRect;
        private Rectangle _lookControlRect;
        private Rectangle _toggleCameraModeButtonRect;
        
        // Maze controls
        private Rectangle _increaseSizeButtonRect;
        private Rectangle _decreaseSizeButtonRect;
        private Rectangle _prevAlgorithmButtonRect;
        private Rectangle _nextAlgorithmButtonRect;
        private Rectangle _regenerateMazeButtonRect;
        private Rectangle _mazeSizeDisplayRect;
        private Rectangle _algorithmDisplayRect;
        
        // View controls
        private Rectangle _toggleRoofButtonRect;
        private Rectangle _toggleLightingButtonRect;
        private Rectangle _togglePathButtonRect;
        private Rectangle _toggleUiButtonRect;
        private Rectangle _increaseSpeedButtonRect;
        private Rectangle _decreaseSpeedButtonRect;
        
        #endregion
        
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
            int buttonSize = (int)(Math.Min(screenWidth, screenHeight) * 0.14f);
            int smallButtonSize = (int)(buttonSize * 0.8f);
            int menuButtonSize = (int)(buttonSize * 0.7f);
            int buttonMargin = buttonSize / 7;
            int buttonRadius = buttonSize / 2;
            
            // Top menu buttons
            int topButtonWidth = (int)(screenWidth * 0.2f);
            int topButtonHeight = (int)(screenHeight * 0.06f);
            
            _mainMenuButtonRect = new Rectangle(
                buttonMargin * 2,
                buttonMargin * 2,
                topButtonWidth, topButtonHeight);
                
            _toggleControlsButtonRect = new Rectangle(
                screenWidth - topButtonWidth - buttonMargin * 2,
                buttonMargin * 2,
                topButtonWidth, topButtonHeight);
                
            // Main menu buttons (center top)
            int menuButtonGap = menuButtonSize / 5;
            int menuTotalWidth = menuButtonSize * 3 + menuButtonGap * 2;
            int menuStartX = (screenWidth - menuTotalWidth) / 2;
            int menuY = topButtonHeight + buttonMargin * 4;
            
            _cameraControlsButtonRect = new Rectangle(
                menuStartX,
                menuY,
                menuButtonSize, menuButtonSize);
                
            _mazeControlsButtonRect = new Rectangle(
                menuStartX + menuButtonSize + menuButtonGap,
                menuY,
                menuButtonSize, menuButtonSize);
                
            _viewControlsButtonRect = new Rectangle(
                menuStartX + (menuButtonSize + menuButtonGap) * 2,
                menuY,
                menuButtonSize, menuButtonSize);
                
            #region Camera Controls
            
            // Movement buttons (bottom center)
            int movementCenterX = screenWidth / 2;
            int movementCenterY = screenHeight - buttonSize * 2;
            
            _forwardButtonRect = new Rectangle(
                movementCenterX - buttonRadius, 
                movementCenterY - buttonSize - buttonMargin,
                buttonSize, buttonSize);
                
            _backwardButtonRect = new Rectangle(
                movementCenterX - buttonRadius, 
                movementCenterY,
                buttonSize, buttonSize);
                
            _leftButtonRect = new Rectangle(
                movementCenterX - buttonSize - buttonMargin - buttonRadius, 
                movementCenterY,
                buttonSize, buttonSize);
                
            _rightButtonRect = new Rectangle(
                movementCenterX + buttonMargin + buttonRadius, 
                movementCenterY,
                buttonSize, buttonSize);
                
            // Up/Down buttons (bottom left)
            _upButtonRect = new Rectangle(
                buttonMargin * 2, 
                screenHeight - buttonSize * 2,
                buttonSize, buttonSize);
                
            _downButtonRect = new Rectangle(
                buttonMargin * 2, 
                screenHeight - buttonSize,
                buttonSize, buttonSize);
                
            // Look control area (bottom right)
            int lookControlSize = (int)(buttonSize * 1.5f);
            _lookControlRect = new Rectangle(
                screenWidth - lookControlSize - buttonMargin * 2,
                screenHeight - lookControlSize - buttonMargin * 2,
                lookControlSize, lookControlSize);
                
            // Camera mode toggle (bottom right, above look control)
            _toggleCameraModeButtonRect = new Rectangle(
                screenWidth - topButtonWidth - buttonMargin * 2,
                screenHeight - lookControlSize - buttonMargin * 4 - topButtonHeight,
                topButtonWidth, topButtonHeight);
            
            #endregion
            
            #region Maze Controls
            
            // Size control buttons (center)
            int mazeControlsY = menuY + menuButtonSize + buttonMargin * 4;
            int mazeButtonWidth = (int)(buttonSize * 1.2f);
            int mazeDisplayWidth = (int)(screenWidth * 0.4f);
            
            _increaseSizeButtonRect = new Rectangle(
                (screenWidth - mazeButtonWidth) / 2,
                mazeControlsY,
                mazeButtonWidth, smallButtonSize);
                
            _mazeSizeDisplayRect = new Rectangle(
                (screenWidth - mazeDisplayWidth) / 2,
                mazeControlsY + smallButtonSize + buttonMargin,
                mazeDisplayWidth, smallButtonSize);
                
            _decreaseSizeButtonRect = new Rectangle(
                (screenWidth - mazeButtonWidth) / 2,
                mazeControlsY + smallButtonSize * 2 + buttonMargin * 2,
                mazeButtonWidth, smallButtonSize);
                
            // Algorithm control buttons
            int algorithmControlsY = mazeControlsY + smallButtonSize * 3 + buttonMargin * 4;
            
            _prevAlgorithmButtonRect = new Rectangle(
                screenWidth / 2 - mazeButtonWidth - buttonMargin,
                algorithmControlsY,
                smallButtonSize, smallButtonSize);
                
            _algorithmDisplayRect = new Rectangle(
                (screenWidth - mazeDisplayWidth) / 2,
                algorithmControlsY + smallButtonSize + buttonMargin,
                mazeDisplayWidth, smallButtonSize);
                
            _nextAlgorithmButtonRect = new Rectangle(
                screenWidth / 2 + buttonMargin,
                algorithmControlsY,
                smallButtonSize, smallButtonSize);
                
            // Regenerate button
            _regenerateMazeButtonRect = new Rectangle(
                (screenWidth - mazeButtonWidth) / 2,
                algorithmControlsY + smallButtonSize * 2 + buttonMargin * 2,
                mazeButtonWidth, smallButtonSize);
                
            #endregion
            
            #region View Controls
            
            // View control buttons arranged in a grid
            int viewControlsStartY = menuY + menuButtonSize + buttonMargin * 4;
            int viewButtonSize = smallButtonSize;
            int viewGridX = (screenWidth - viewButtonSize * 2 - buttonMargin) / 2;
            
            _toggleRoofButtonRect = new Rectangle(
                viewGridX,
                viewControlsStartY,
                viewButtonSize, viewButtonSize);
                
            _toggleLightingButtonRect = new Rectangle(
                viewGridX + viewButtonSize + buttonMargin,
                viewControlsStartY,
                viewButtonSize, viewButtonSize);
                
            _togglePathButtonRect = new Rectangle(
                viewGridX,
                viewControlsStartY + viewButtonSize + buttonMargin,
                viewButtonSize, viewButtonSize);
                
            _toggleUiButtonRect = new Rectangle(
                viewGridX + viewButtonSize + buttonMargin,
                viewControlsStartY + viewButtonSize + buttonMargin,
                viewButtonSize, viewButtonSize);
                
            // Speed controls
            int speedControlsY = viewControlsStartY + viewButtonSize * 2 + buttonMargin * 3;
            
            _increaseSpeedButtonRect = new Rectangle(
                viewGridX,
                speedControlsY,
                viewButtonSize, viewButtonSize);
                
            _decreaseSpeedButtonRect = new Rectangle(
                viewGridX + viewButtonSize + buttonMargin,
                speedControlsY,
                viewButtonSize, viewButtonSize);
                
            #endregion
        }
        
        /// <summary>
        /// Update mobile controls state
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!TouchPanel.IsGestureAvailable)
                return;
                
            // Process touch input
            TouchCollection touchCollection = TouchPanel.GetState();
            
            // Reset button states
            foreach (var key in _pressedButtons.Keys)
            {
                _pressedButtons[key] = false;
            }
            
            foreach (TouchLocation touch in touchCollection)
            {
                ProcessTouch(touch, gameTime);
            }
            
            if (touchCollection.Count == 0)
            {
                _isLookControlActive = false;
            }
            
            // Apply camera movement if any button is pressed
            if (ShowCameraControls)
            {
                ApplyCameraMovement(gameTime);
            }
        }
        
        /// <summary>
        /// Process an individual touch input
        /// </summary>
        private void ProcessTouch(TouchLocation touch, GameTime gameTime)
        {
            if (!ShowControls)
            {
                // Handle the show controls button when controls are hidden
                if (touch.State == TouchLocationState.Pressed && _toggleControlsButtonRect.Contains(touch.Position))
                {
                    ShowControls = true;
                }
                return;
            }
                
            Vector2 position = touch.Position;
            
            // Handle top menu buttons
            if (touch.State == TouchLocationState.Pressed)
            {
                if (_toggleControlsButtonRect.Contains(position))
                {
                    ShowControls = !ShowControls;
                    return;
                }
                
                if (_mainMenuButtonRect.Contains(position))
                {
                    // Toggle between showing and hiding the menu
                    ToggleMainMenu();
                    return;
                }
                
                // Handle menu selection buttons
                if (_cameraControlsButtonRect.Contains(position))
                {
                    SetActiveControlGroup(ControlGroup.Camera);
                    return;
                }
                else if (_mazeControlsButtonRect.Contains(position))
                {
                    SetActiveControlGroup(ControlGroup.Maze);
                    return;
                }
                else if (_viewControlsButtonRect.Contains(position))
                {
                    SetActiveControlGroup(ControlGroup.View);
                    return;
                }
            }
            
            // Process controls based on active group
            if (_activeControlGroup == ControlGroup.Camera && ShowCameraControls)
            {
                ProcessCameraControls(touch, gameTime, position);
            }
            else if (_activeControlGroup == ControlGroup.Maze && ShowMazeControls)
            {
                ProcessMazeControls(touch, position, gameTime);
            }
            else if (_activeControlGroup == ControlGroup.View && ShowViewControls)
            {
                ProcessViewControls(touch, position, gameTime);
            }
        }
        
        /// <summary>
        /// Handle touch events for camera controls
        /// </summary>
        private void ProcessCameraControls(TouchLocation touch, GameTime gameTime, Vector2 position)
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
            
            // Handle toggle button presses
            if (touch.State == TouchLocationState.Pressed && _toggleCameraModeButtonRect.Contains(position))
            {
                IsFpsMode = !IsFpsMode;
                
                // Update camera mode
                Basic3dExampleCamera camera = _game.GetCamera();
                if (camera != null)
                {
                    camera.CameraUi(IsFpsMode ? 
                        Basic3dExampleCamera.CAM_UI_OPTION_FPS_LAYOUT : 
                        Basic3dExampleCamera.CAM_UI_OPTION_EDIT_LAYOUT);
                }
            }
            
            // Handle touch release
            if (touch.State == TouchLocationState.Released && _lookControlRect.Contains(position))
            {
                _isLookControlActive = false;
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
            
            // Toggle UI visibility
            else if (_toggleUiButtonRect.Contains(position) && CheckActionCooldown("ui", currentTime))
            {
                _game.ToggleUI();
                _lastActionTime["ui"] = currentTime;
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
        /// Toggle the display of the main menu
        /// </summary>
        private void ToggleMainMenu()
        {
            bool allHidden = !ShowCameraControls && !ShowMazeControls && !ShowViewControls;
            
            if (allHidden)
            {
                // If all are hidden, show the last active one
                SetActiveControlGroup(_activeControlGroup);
            }
            else
            {
                // Hide all control groups
                ShowCameraControls = false;
                ShowMazeControls = false;
                ShowViewControls = false;
            }
        }
        
        /// <summary>
        /// Set the active control group and update visibility
        /// </summary>
        private void SetActiveControlGroup(ControlGroup group)
        {
            _activeControlGroup = group;
            
            // Hide all control groups first
            ShowCameraControls = false;
            ShowMazeControls = false;
            ShowViewControls = false;
            
            // Show only the active group
            switch (group)
            {
                case ControlGroup.Camera:
                    ShowCameraControls = true;
                    break;
                case ControlGroup.Maze:
                    ShowMazeControls = true;
                    break;
                case ControlGroup.View:
                    ShowViewControls = true;
                    break;
            }
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
            
            // Always draw the top menu buttons
            DrawButton(_toggleControlsButtonRect, Color.Black * 0.6f, ShowControls ? "Hide Controls" : "Show Controls", Color.White);
            
            if (!ShowControls)
            {
                _spriteBatch.End();
                return;
            }
            
            DrawButton(_mainMenuButtonRect, Color.Black * 0.6f, "Menu", Color.White);
            
            // Draw menu selection buttons
            Color cameraColor = ShowCameraControls ? Color.DarkGreen * 0.8f : Color.DarkSlateGray * 0.7f;
            Color mazeColor = ShowMazeControls ? Color.DarkBlue * 0.8f : Color.DarkSlateGray * 0.7f;
            Color viewColor = ShowViewControls ? Color.DarkRed * 0.8f : Color.DarkSlateGray * 0.7f;
            
            DrawButton(_cameraControlsButtonRect, cameraColor, "Camera", Color.White);
            DrawButton(_mazeControlsButtonRect, mazeColor, "Maze", Color.White);
            DrawButton(_viewControlsButtonRect, viewColor, "View", Color.White);
            
            // Draw specific control groups
            if (ShowCameraControls)
                DrawCameraControls();
                
            if (ShowMazeControls)
                DrawMazeControls();
                
            if (ShowViewControls)
                DrawViewControls();
            
            _spriteBatch.End();
        }
        
        /// <summary>
        /// Draw camera control buttons
        /// </summary>
        private void DrawCameraControls()
        {
            // Draw movement buttons
            DrawButton(_forwardButtonRect, Color.Black * 0.6f, "▲", Color.White);
            DrawButton(_backwardButtonRect, Color.Black * 0.6f, "▼", Color.White);
            DrawButton(_leftButtonRect, Color.Black * 0.6f, "◄", Color.White);
            DrawButton(_rightButtonRect, Color.Black * 0.6f, "►", Color.White);
            
            // Draw up/down buttons
            DrawButton(_upButtonRect, Color.Black * 0.6f, "Q", Color.White);
            DrawButton(_downButtonRect, Color.Black * 0.6f, "E", Color.White);
            
            // Draw look control area
            DrawButton(_lookControlRect, Color.Black * 0.5f, "Look", Color.White);
            
            // Draw camera mode toggle
            DrawButton(_toggleCameraModeButtonRect, Color.Black * 0.6f, IsFpsMode ? "FPS Mode" : "Edit Mode", Color.White);
        }
        
        /// <summary>
        /// Draw maze control buttons
        /// </summary>
        private void DrawMazeControls()
        {
            // Draw size controls
            DrawButton(_increaseSizeButtonRect, Color.DarkBlue * 0.7f, "Size +", Color.White);
            DrawButton(_mazeSizeDisplayRect, Color.Black * 0.5f, $"Size: {_game.MazeWidth}x{_game.MazeHeight}", Color.White);
            DrawButton(_decreaseSizeButtonRect, Color.DarkBlue * 0.7f, "Size -", Color.White);
            
            // Draw algorithm controls
            DrawButton(_prevAlgorithmButtonRect, Color.DarkGreen * 0.7f, "◄", Color.White);
            DrawButton(_algorithmDisplayRect, Color.Black * 0.5f, $"Alg: {_game.CurrentAlgorithmName}", Color.White);
            DrawButton(_nextAlgorithmButtonRect, Color.DarkGreen * 0.7f, "►", Color.White);
            
            // Draw regenerate button
            DrawButton(_regenerateMazeButtonRect, Color.DarkRed * 0.7f, "Regenerate", Color.White);
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
            Color uiColor = _game.ShowUI ? Color.Green * 0.7f : Color.DarkRed * 0.7f;
            
            DrawButton(_toggleRoofButtonRect, roofColor, "Roof", Color.White);
            DrawButton(_toggleLightingButtonRect, lightingColor, "Light", Color.White);
            DrawButton(_togglePathButtonRect, pathColor, "Path", Color.White);
            DrawButton(_toggleUiButtonRect, uiColor, "UI", Color.White);
            
            // Draw speed controls
            DrawButton(_increaseSpeedButtonRect, Color.DarkBlue * 0.7f, "Speed+", Color.White);
            DrawButton(_decreaseSpeedButtonRect, Color.DarkBlue * 0.7f, "Speed-", Color.White);
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