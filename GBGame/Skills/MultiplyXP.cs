using GBGame.Entities;

namespace GBGame.Skills;

public class MultiplyXP : Skill
{
    public required Player Player { private get; set; }

    public MultiplyXP() { Name = "More XP"; }

    public override void OnActivate() => Player.XPMultiplier *= 2;
}
