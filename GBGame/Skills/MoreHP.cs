using GBGame.Components;
using GBGame.Entities;

namespace GBGame.Skills;

public class MoreHP : Skill
{
    readonly Player player;

    public MoreHP(Player player)
    {
        this.player = player;
        Name = "one more life";
    }
    
    public override void OnActivate() => this.player.AddHealth();
}
