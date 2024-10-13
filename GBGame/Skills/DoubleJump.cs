using GBGame.Components;
using GBGame.Entities;

namespace GBGame.Skills;

public class DoubleJump : Skill
{
    readonly Player player;

    public DoubleJump(Player player)
    {
        Name = "Double Jump";
        this.player = player;
    }

    public override void OnActivate()
    {
        Jump? jump = this.player.Components.GetComponent<Jump>();
        if (jump is null) return;

        jump.BaseCount = 2;
        jump.Count = 2;
    }
}
