using JewelBlitzArena.Data;

namespace JewelBlitzArena.Tools.Corpus;

/// <summary>
/// Shared S5 stub catalog for headless corpus / replay scrubbing.
/// Matches Unity StreamingAssets <c>s5_stub.jba</c> content (SampleContentFactory).
/// </summary>
public static class CorpusCatalog
{
    private static readonly Lazy<ContentCatalog> S5Stub = new(
        () => ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack()));

    /// <summary>Compiled S5 stub pack (fighters + Citrine + Fulgur + items).</summary>
    public static ContentCatalog LoadS5Stub() => S5Stub.Value;
}
