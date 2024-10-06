using Microsoft.Xna.Framework.Input;

namespace GBGame;

public class ControlBindings
{
    public Keys KeyboardRight { get; set; } = Keys.Right;
    public Keys KeyboardLeft { get; set; } = Keys.Left;
    public Keys KeyboardInventoryUp { get; set; } = Keys.Up;
    public Keys KeyboardInventoryDown { get; set; } = Keys.Down;

    public Keys KeyboardJump { get; set; } = Keys.Space;
    public Keys KeyboardAction { get; set; } = Keys.LeftShift;

    public Keys FullScreen { get; set; } = Keys.F;

    public Buttons ControllerRight { get; set; } = Buttons.DPadRight;
    public Buttons ControllerLeft { get; set; } = Buttons.DPadLeft;
    public Buttons ControllerInventoryUp { get; set; } = Buttons.DPadUp;
    public Buttons ControllerInventoryDown { get; set; } = Buttons.DPadDown;

    public Buttons ControllerJump { get; set; } = Buttons.B;
    public Buttons ControllerAction { get; set; } = Buttons.A;
}
