using GBGame.Entities;
using GBGame.Skills;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGayme.Controllers;
using MonoGayme.States;
using MonoGayme.UI;
using MonoGayme.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace GBGame.States;

public class ControlCentreState(
    GameWindow gameWindow,
    WindowOptions windowOptions,
    ControlBindings controlBindings,
    IEnumerable<Skill> skills,
    Player player,
    CameraGB cameraGB
) : State(gameWindow)
{
    readonly List<Skill> skills = skills.ToList();

    TextButton CreateButton(Skill skill, bool first)
    {
        Vector2 measurements = spriteFont.MeasureString(skill.Name);
        Vector2 position = (windowOptions.RenderBounds.Size.ToVector2() - measurements) / 2;

        position.Y = first ? position.Y - 3 : position.Y + 6;

        TextButton btn = new(spriteFont, skill.Name, position, textColour)
        {
            OnClick = () =>
            {
                skill.OnActivate();

                player.SkillPoints--;
                if (player.SkillPoints <= 0) { ChooseSkills(true, skill); }
            }
        };

        return btn;
    }

    public void ChooseSkills(bool remove = false, Skill? skill = null)
    {
        if (remove) { this.skills.Remove(skill!); }

        this.controller.QueueRemoveAll();

        switch (this.skills.Count)
        {
            case >= 2:
                this.skills.Shuffle();
                this.controller.Add(CreateButton(this.skills[0], true));
                this.controller.Add(CreateButton(this.skills[1], false));
                break;
            case 1:
                this.controller.Add(CreateButton(this.skills[0], true));
                break;
            case 0:
                // controller.Add(CreateButton(new PlusBomb(game.Bomb), true));
                break;
        }

        TextButton returnButton = new(this.spriteFont, this.returnText, this.returnPosition, this.textColour)
        {
            OnClick = () => gameWindow.State.SwitchState(nameof(InGameState))
        };
        this.controller.Add(returnButton);
    }

    public override void Draw(GameTime time, SpriteBatch batch)
    {
        batch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: cameraGB.Transform);
        {
            batch.Draw(this.overlay, windowOptions.RenderBounds.Size.ToVector2(), this.overlayColour * 0.8f);

            if (player.SkillPoints == 0)
            {
                batch.DrawString(this.spriteFont, this.noSkillPointsText, this.noSkillPointsPos, this.textColour);
                batch.DrawString(this.spriteFont, this.returnText, this.returnPosition, this.activeTextColour);
            }
            else
            {
                batch.DrawString(this.spriteFont, $"SP: {player.SkillPoints}", new Vector2(2), this.textColour);
                this.controller.Draw(batch);
            }
        }
        batch.End();
    }

    readonly Texture2D overlay = new(gameWindow.GraphicsDevice, 1, 1);

    readonly Color overlayColour = new(40, 56, 24);
    readonly Color textColour = new(176, 192, 160);
    readonly Color activeTextColour = new(136, 152, 120);

    readonly SpriteFont spriteFont = gameWindow.Content.Load<SpriteFont>("Sprites/Fonts/File");

    readonly string noSkillPointsText = "No skill points!";
    Vector2 noSkillPointsPos;

    readonly string returnText = "Return";
    Vector2 returnPosition;

    private readonly ButtonController controller = new(true);

    public override void LoadContent()
    {
        this.overlay.SetData(new[] { this.overlayColour });
        Point noSkillPointsStringSize = this.spriteFont.MeasureString(this.noSkillPointsText).ToPoint();
        this.noSkillPointsPos = (windowOptions.RenderBounds.Size - noSkillPointsStringSize).ToVector2() / 2;

        Point returnStringMeasure = this.spriteFont.MeasureString(this.returnText).ToPoint();
        this.returnPosition = new Vector2(
            (windowOptions.RenderBounds.Width - returnStringMeasure.X) / 2,
            windowOptions.RenderBounds.Height - returnStringMeasure.Y - 1
        );

        this.controller.SetControllerButtons(
            controlBindings.ControllerInventoryUp,
            controlBindings.ControllerInventoryDown,
            controlBindings.ControllerAction
        );
        this.controller.SetKeyboardButtons(
            controlBindings.KeyboardInventoryUp,
            controlBindings.KeyboardInventoryDown,
            controlBindings.KeyboardAction
        );

        this.controller.OnActiveUpdating = (btn) => btn.Colour = this.textColour;
        this.controller.OnActiveUpdated = (btn) => btn.Colour = this.activeTextColour;
    }

    public override void Update(GameTime time)
    {
        if (player.SkillPoints is 0)
        {
            if (InputManager.IsGamePadPressed(controlBindings.ControllerAction) || InputManager.IsKeyPressed(controlBindings.KeyboardAction))
            {
                return;
            }
        }

        this.controller.Update(gameWindow.MousePosition);
    }
}