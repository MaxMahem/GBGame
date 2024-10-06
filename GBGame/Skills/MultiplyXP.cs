namespace GBGame.Skills;

public class MultiplyXP : Skill
{
    public required InGameOptions GameOptions { private get; set; }

    public MultiplyXP() { Name = "More XP"; }

    public override void OnActivate() => GameOptions.XPMultiplier *= 2;
}
