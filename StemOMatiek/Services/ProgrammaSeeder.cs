using Microsoft.EntityFrameworkCore;
using StemOMatiek.Data;
using StemOMatiek.Data.Models;
using UglyToad.PdfPig;

namespace StemOMatiek.Services;

/// <summary>
/// Importeert partijprogramma's uit de /programmas directory bij opstarten.
/// Documenten worden opgeslagen maar niet geïndexeerd (dat vereist de API-sleutel).
/// </summary>
public static class ProgrammaSeeder
{
    // Mapping: bestandsnaam (lowercase) → Partij Id
    private static readonly Dictionary<string, int> FileToPartij = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nva.pdf"] = 1,                                    // N-VA
        ["Vooruit_programma_2024.pdf"] = 2,                 // Vooruit
        ["cdnv.pdf"] = 3,                                   // CD&V
        ["vlaamsbehang.pdf"] = 4,                           // VB
        ["Programma_Groen.pdf"] = 7,                        // Groen
        ["Programma-2024-Team-Fouad-Ahidar-NL.pdf"] = 8,   // TFA
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Zoek de programmas directory naast het project
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "programmas");
        if (!Directory.Exists(basePath))
        {
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "programmas");
        }
        if (!Directory.Exists(basePath)) return;

        // Partijen die al een document hebben
        var partijIdsMetDoc = await db.Documenten
            .Select(d => d.PartijId)
            .Distinct()
            .ToListAsync();

        foreach (var (filename, partijId) in FileToPartij)
        {
            if (partijIdsMetDoc.Contains(partijId)) continue;

            var filepath = Path.Combine(basePath, filename);
            if (!File.Exists(filepath)) continue;

            try
            {
                var text = ExtractTextFromPdf(filepath);
                if (string.IsNullOrWhiteSpace(text)) continue;

                var partij = await db.Partijen.FindAsync(partijId);
                if (partij is null) continue;

                var doc = new Document
                {
                    PartijId = partijId,
                    Titel = $"Verkiezingsprogramma {partij.Afkorting} 2024",
                    Inhoud = text,
                    Type = "Partijprogramma",
                    DatumToegevoegd = DateTime.UtcNow,
                    IsGeindexeerd = false  // Indexeren vereist API-sleutel
                };

                db.Documenten.Add(doc);
                Console.WriteLine($"[Seeder] Geïmporteerd: {filename} → {partij.Afkorting} ({text.Length:N0} tekens)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seeder] Fout bij {filename}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();
    }

    private static string ExtractTextFromPdf(string path)
    {
        using var document = PdfDocument.Open(path);
        var sb = new System.Text.StringBuilder();
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }
}
