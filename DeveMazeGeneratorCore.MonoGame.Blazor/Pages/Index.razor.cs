using DeveMazeGeneratorCore.MonoGame.Core;
using DeveMazeGeneratorCore.MonoGame.Core.HelperObjects;
using DeveMazeGeneratorMonoGame;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Collections.Generic;

namespace DeveMazeGeneratorCore.MonoGame.Blazor.Pages
{
    public partial class Index
    {
        TheGame _game;
        
        // Flag to show/hide camera controls
        public bool ShowCameraControls { get; set; } = true;
        
        // Dictionary to track which movement buttons are currently pressed
        private Dictionary<string, bool> _pressedButtons = new Dictionary<string, bool>
        {
            { "Forward", false },
            { "Left", false },
            { "Right", false },
            { "Down", false },
            { "Up", false },
            { "DownVert", false },
        };
        
        // Look control state
        private bool _isLookControlActive = false;
        private float _lastTouchX;
        private float _lastTouchY;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet()
        {
            // init game
            if (_game == null)
            {
                //TODO: Remove this fix once the MonoGame Blazor library has fixed it's _isRunningOnNetCore detection
                var field = typeof(Microsoft.Xna.Framework.Content.ContentTypeReaderManager).GetField("_isRunningOnNetCore",
                    BindingFlags.Static |
                    BindingFlags.NonPublic);
                field.SetValue(null, true);

                _game = new TheGame(new CustomEmbeddedResourceLoader(), new IntSize(400, 800), Platform.Blazor);
                _game.Run();
            }

            // Process camera movement from touch controls
            ProcessCameraControls();

            // run gameloop
            _game.Tick();
        }

        // Process camera control inputs
        private void ProcessCameraControls()
        {
            if (_game == null || !ShowCameraControls)
                return;

            Basic3dExampleCamera camera = _game.GetCamera();
            if (camera == null)
                return;

            // Create a dummy game time for control methods
            var gameTime = new GameTime(System.TimeSpan.Zero, System.TimeSpan.FromMilliseconds(16));

            // Process movement buttons
            if (_pressedButtons["Forward"])
                camera.MoveForward(gameTime);
            if (_pressedButtons["Down"])
                camera.MoveBackward(gameTime);
            if (_pressedButtons["Left"])
                camera.MoveLeft(gameTime);
            if (_pressedButtons["Right"])
                camera.MoveRight(gameTime);
            if (_pressedButtons["Up"])
                camera.MoveUp(gameTime);
            if (_pressedButtons["DownVert"])
                camera.MoveDown(gameTime);
        }

        // Button event handlers
        public void MoveButtonPressed(string button)
        {
            if (_pressedButtons.ContainsKey(button))
                _pressedButtons[button] = true;
        }

        public void MoveButtonReleased(string button)
        {
            if (_pressedButtons.ContainsKey(button))
                _pressedButtons[button] = false;
        }

        // Look control event handlers
        public void OnLookControlTouchStart(TouchEventArgs e)
        {
            if (e.ChangedTouches.Length > 0)
            {
                _isLookControlActive = true;
                _lastTouchX = (float)e.ChangedTouches[0].ClientX;
                _lastTouchY = (float)e.ChangedTouches[0].ClientY;
            }
        }

        public void OnLookControlTouchMove(TouchEventArgs e)
        {
            if (_isLookControlActive && e.ChangedTouches.Length > 0 && _game != null)
            {
                Basic3dExampleCamera camera = _game.GetCamera();
                if (camera == null)
                    return;

                float currentX = (float)e.ChangedTouches[0].ClientX;
                float currentY = (float)e.ChangedTouches[0].ClientY;
                
                float deltaX = currentX - _lastTouchX;
                float deltaY = currentY - _lastTouchY;
                
                // Create a dummy game time for rotation
                var gameTime = new GameTime(System.TimeSpan.Zero, System.TimeSpan.FromMilliseconds(16));
                
                // Apply rotation based on touch movement
                if (deltaX != 0)
                    camera.RotateLeftOrRight(gameTime, -deltaX * 0.5f);
                
                if (deltaY != 0)
                    camera.RotateUpOrDown(gameTime, -deltaY * 0.5f);
                
                _lastTouchX = currentX;
                _lastTouchY = currentY;
            }
        }

        public void OnLookControlTouchEnd(TouchEventArgs e)
        {
            _isLookControlActive = false;
        }

        // Toggle camera controls visibility
        public void ToggleCameraControls()
        {
            ShowCameraControls = !ShowCameraControls;
        }

        // Original touch handlers for the canvas
        public void OnTouchStart(TouchEventArgs e)
        {
            // Keep these general canvas touch handlers for future use
        }

        public void OnTouchMove(TouchEventArgs e)
        {
            // Keep these general canvas touch handlers for future use
        }

        public void OnTouchEnd(TouchEventArgs e)
        {
            // Keep these general canvas touch handlers for future use
        }
    }
}