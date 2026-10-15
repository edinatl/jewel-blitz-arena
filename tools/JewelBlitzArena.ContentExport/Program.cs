using JewelBlitzArena.Data;

static int Usage()
{
    Console.Error.WriteLine(
        "Usage:\n" +
        "  JewelBlitzArena.ContentExport stub <out.jba> [authored.json]\n" +
        "  JewelBlitzArena.ContentExport validate <in.jba>\n" +
        "  JewelBlitzArena.ContentExport from-json <authored.json> <out.jba>");
    return 2;
}

if (args.Length < 2)
{
    return Usage();
}

string command = args[0];
try
{
    switch (command)
    {
        case "stub":
        {
            AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
            ValidationResult validation = ContentValidator.Validate(pack);
            if (!validation.IsOk)
            {
                foreach (ValidationIssue issue in validation.Errors)
                {
                    Console.Error.WriteLine(issue);
                }

                return 1;
            }

            string outPath = args[1];
            ContentCatalog catalog = ContentCompiler.Compile(pack);
            byte[] blob = ContentBlobWriter.Write(catalog);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllBytes(outPath, blob);

            if (args.Length >= 3)
            {
                AuthoredJson.WriteToFile(pack, args[2]);
                Console.WriteLine($"Wrote authored JSON {args[2]}");
            }

            Console.WriteLine($"Wrote {outPath} ({blob.Length} bytes, hash=0x{catalog.ContentHash:X16})");
            Console.WriteLine(
                $"Fighters={catalog.Fighters.Length} Stages={catalog.Stages.Length} Items={catalog.Items.Length} Always={catalog.AlwaysEligibleItems().Count}");
            return 0;
        }

        case "from-json":
        {
            if (args.Length < 3)
            {
                return Usage();
            }

            AuthoredContentPack pack = AuthoredJson.ReadFromFile(args[1]);
            ValidationResult validation = ContentValidator.Validate(pack);
            foreach (ValidationIssue w in validation.Warnings)
            {
                Console.WriteLine("WARN " + w);
            }

            if (!validation.IsOk)
            {
                foreach (ValidationIssue issue in validation.Errors)
                {
                    Console.Error.WriteLine(issue);
                }

                return 1;
            }

            ContentCatalog catalog = ContentCompiler.Compile(pack);
            byte[] blob = ContentBlobWriter.Write(catalog);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
            File.WriteAllBytes(args[2], blob);
            Console.WriteLine($"Wrote {args[2]} hash=0x{catalog.ContentHash:X16}");
            return 0;
        }

        case "validate":
        {
            byte[] blob = File.ReadAllBytes(args[1]);
            ContentCatalog catalog = ContentBlobReader.Read(blob);
            Console.WriteLine(
                $"OK schema={catalog.SchemaVersion} hash=0x{catalog.ContentHash:X16} " +
                $"fighters={catalog.Fighters.Length} stages={catalog.Stages.Length} items={catalog.Items.Length}");
            return 0;
        }

        default:
            return Usage();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
