using DeveMazeGeneratorCore.MonoGame.Core;
using DeveMazeGeneratorCore.MonoGame.Core.HelperObjects;
using DeveMazeGeneratorMonoGame;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace DeveMazeGeneratorCore.MonoGame.Blazor.Pages
{
    public partial class Index
    {
        TheGame _game;

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
                
                // Enable the new camera by default for Blazor
                _game.ToggleNewCamera();
            }

            // run gameloop
            _game.Tick();
        }

        // Original touch handlers for the canvas - retained for future use
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