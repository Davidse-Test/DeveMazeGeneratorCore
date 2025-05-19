using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;

namespace DeveMazeGeneratorCore.MonoGame.Core
{
    /// <summary>
    /// Handles mobile UI controls for camera movement and rotation
    /// </summary>
    public class MobileControls
    {
        private readonly TheGame _game;
        private SpriteBatch _spriteBatch;
        
        // Textures
        private Texture2D _buttonTexture;
        private Texture2D _panelTexture;
        
        // Control visibility
        public bool ShowControls { get; private set; } = true;
        
        // Camera mode
        public bool IsFpsMode { get; private set; } = true;
        
        // UI rectangles
        private Rectangle _forwardButtonRect;
        private Rectangle _backwardButtonRect;
        private Rectangle _leftButtonRect;
        private Rectangle _rightButtonRect;
        private Rectangle _upButtonRect;
        private Rectangle _downButtonRect;
        private Rectangle _lookControlRect;
        private Rectangle _toggleControlsButtonRect;
        private Rectangle _toggleCameraModeButtonRect;
        
        // Button states
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
            
            _panelTexture = new Texture2D(graphicsDevice, 1, 1);
            _panelTexture.SetData(new[] { Color.White });
            
            // Position UI elements
            UpdateControlPositions();
        }
        
        /// <summary>
        /// Updates control positions based on screen size
        /// </summary>
        public void UpdateControlPositions()
        {
            int screenWidth = _game.ScreenWidth;
            int screenHeight = _game.ScreenHeight;
            
            // Button size - adjust based on screen size
            int buttonSize = (int)(Math.Min(screenWidth, screenHeight) * 0.14f);
            int buttonMargin = buttonSize / 7;
            int buttonRadius = buttonSize / 2;
            
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
                
            // Top buttons
            int topButtonWidth = (int)(screenWidth * 0.25f);
            int topButtonHeight = (int)(screenHeight * 0.06f);
            
            _toggleCameraModeButtonRect = new Rectangle(
                screenWidth - topButtonWidth - buttonMargin * 2 - topButtonWidth - buttonMargin * 2,
                buttonMargin * 2,
                topButtonWidth, topButtonHeight);
                
            _toggleControlsButtonRect = new Rectangle(
                screenWidth - topButtonWidth - buttonMargin * 2,
                buttonMargin * 2,
                topButtonWidth, topButtonHeight);
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
            ApplyCameraMovement(gameTime);
        }
        
        /// <summary>
        /// Process an individual touch input
        /// </summary>
        private void ProcessTouch(TouchLocation touch, GameTime gameTime)
        {
            if (!ShowControls)
                return;
                
            Vector2 position = touch.Position;
            
            // Handle button touches
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
            if (touch.State == TouchLocationState.Pressed)
            {
                if (_toggleControlsButtonRect.Contains(position))
                {
                    ShowControls = !ShowControls;
                }
                else if (_toggleCameraModeButtonRect.Contains(position))
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
            }
            
            // Handle touch release
            if (touch.State == TouchLocationState.Released)
            {
                if (_lookControlRect.Contains(position))
                {
                    _isLookControlActive = false;
                }
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
            if (!ShowControls)
            {
                // When controls are hidden, still draw the show controls button
                _spriteBatch.Begin();
                DrawButton(_toggleControlsButtonRect, Color.Black * 0.6f, "Show Controls", Color.White);
                _spriteBatch.End();
                return;
            }
            
            _spriteBatch.Begin();
            
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
            
            // Draw top buttons
            DrawButton(_toggleCameraModeButtonRect, Color.Black * 0.6f, IsFpsMode ? "FPS Mode" : "Edit Mode", Color.White);
            DrawButton(_toggleControlsButtonRect, Color.Black * 0.6f, "Hide Controls", Color.White);
            
            _spriteBatch.End();
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