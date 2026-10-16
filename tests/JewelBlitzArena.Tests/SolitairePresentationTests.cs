using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class SolitairePresentationTests
{
    private const ulong Seed = 0x534F4C49505245UL; // SOLIPRE

    [Test]
    public void MatchSession_TryUseSolitaire_requires_pocket_in_config()
    {
        var session = new MatchSession();
        session.ResetDefault(Seed, actorCount: 4);
        Assert.That(session.SolitaireAvailable, Is.False);
        Assert.That(session.TryUseSolitaire(0), Is.False);

        MatchConfig config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 4);
        config.SetPocketItems(new[] { MatchRules.SolitaireItemId });
        session.Reset(config);
        Assert.That(session.SolitaireAvailable, Is.True);
        int before = session.Current.Actor0.JewelsHeld;
        Assert.That(session.TryUseSolitaire(0), Is.True);
        Assert.That(session.Current.Actor0.JewelsHeld, Is.EqualTo(before + 1));
    }

    [Test]
    public void MetaGreyboxFlow_Custom_PocketItems_wires_Solitaire_use()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        string path = MetaGreyboxFlow.DefaultTempProfilePath("jba-sol-pres-");
        try
        {
            var flow = new MetaGreyboxFlow(path);
            flow.SetCatalog(catalog);
            flow.BootProfile();
            flow.Profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
            Assert.That(flow.TryPocketItem(SampleContentFactory.SolitaireItemId), Is.True);

            var request = new ModeRequest
            {
                Mode = ModeId.Custom,
                Seed = Seed,
                ActorCount = 4,
                HouseRules = new HouseRulesSettings
                {
                    PocketItems = true,
                    LusterTimer = true,
                    JewelCap = 0,
                    BlitzedDamageTakenPercent = 50,
                    BlitzedAttackPercent = 100,
                    RallySeconds = 9,
                    PhaseClockPercent = 100,
                },
            };

            MatchConfig config = flow.StartCustom(request);
            Assert.That(config.ContainsPocketItem(SampleContentFactory.SolitaireItemId), Is.True);
            Assert.That(flow.SolitaireAvailable, Is.True);
            int before = flow.Session.Current.Actor0.JewelsHeld;
            Assert.That(flow.TryUseSolitaire(0), Is.True);
            Assert.That(flow.Session.Current.Actor0.JewelsHeld, Is.EqualTo(before + 1));
            flow.CompleteCustom();
            Assert.That(flow.TryUseSolitaire(0), Is.False, "no use after match ends");
        }
        finally
        {
            string? dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
