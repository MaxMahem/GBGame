using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.Components;
using MonoGayme.Controllers;
using MonoGayme.UI;

namespace GBGame.Components;

public class Pause
{
    public bool Paused = false;

    private Texture2D _overlay;
    private Color _overlayColour = new(40, 56, 24);
    private Color _textColour = new(176, 192, 160);

    private Vector2 _titlePos;

    private SpriteFont _fontBig;
    private SpriteFont _font;

    private GameWindow _window;

    private ButtonController _controller = new ButtonController(true);

    public Pause(GameWindow window, WindowOptions windowOptions, ControlBindings controlBindings)
    {
        _overlay = new Texture2D(window.GraphicsDevice, 1, 1);
        _overlay.SetData(new[] { _overlayColour });

        _fontBig = window.Content.Load<SpriteFont>("Sprites/Fonts/FontBig");
        _font = window.Content.Load<SpriteFont>("Sprites/Fonts/File");

        Vector2 titleSize = _fontBig.MeasureString("PAUSED");
        _titlePos = new Vector2(
            (windowOptions.RenderBounds.Width - titleSize.X) / 2,
            10
        );

        _window = window;

        TextButton resume = new TextButton(_font, "resume", new Vector2((windowOptions.RenderBounds.Width - _font.MeasureString("resume").X) / 2, 40), _textColour) {
            OnClick = () => { 
                Paused = !Paused;
            }
        };

        TextButton mainMenu = new TextButton(_font, "main menu", new Vector2((windowOptions.RenderBounds.Width - _font.MeasureString("main menu").X) / 2, 50), _textColour) {
            OnClick = () => {
                Console.WriteLine("Meow meow");
            }
        };

        TextButton quit = new TextButton(_font, "quit game", new Vector2((windowOptions.RenderBounds.Width - _font.MeasureString("quit game").X) / 2, 60), _textColour) {
            OnClick = window.Exit
        };

        _controller.SetControllerButtons(
            controlBindings.ControllerInventoryUp, 
            controlBindings.ControllerInventoryDown, 
            controlBindings.ControllerAction
        );
        
        _controller.SetKeyboardButtons(
            controlBindings.KeyboardInventoryUp, 
            controlBindings.KeyboardInventoryDown, 
            controlBindings.KeyboardAction
        );

        _controller.OnActiveUpdating = (btn) => {
            btn.Colour = _textColour;
        };

        _controller.OnActiveUpdated = (btn) => {
            btn.Colour = _overlayColour;
        };

        _controller.Add(resume);
        _controller.Add(mainMenu);
        _controller.Add(quit);
    }

    public void Update() 
    {
        _controller.Update(_window.MousePosition);
    }

    public void Draw(SpriteBatch batch, Camera2D camera)
    {
        batch.Draw(_overlay, camera.ScreenToWorld(Vector2.Zero), Color.White * 0.6f);

        batch.DrawString(_fontBig, "PAUSED", camera.ScreenToWorld(_titlePos), _textColour);

        _controller.Draw(batch, camera);
    }
}
