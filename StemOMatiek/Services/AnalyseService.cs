using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StemOMatiek.Data;
using StemOMatiek.Data.Models;

namespace StemOMatiek.Services;

public class AnalyseService
{
    private readonly AppDbContext _db;
    private readonly AiService _ai;
    private readonly DocumentService _docService;

    public AnalyseService(AppDbContext db, AiService ai, DocumentService docService)
    {
        _db = db;
        _ai = ai;
        _docService = docService;
    }

    public async Task<Analyse?> AnalyseerVoorPartijAsync(int beslissingId, int partijId)
    {
        var beslissing = await _db.Beslissingen.FindAsync(beslissingId);
        var partij = await _db.Partijen.FindAsync(partijId);

        if (beslissing is null || partij is null) return null;

        // Zoek relevante passages uit het partijprogramma
        var query = $"{beslissing.Titel} {beslissing.Beschrijving}";
        var relevanteChunks = await _docService.ZoekRelevanteChunksAsync(query, partijId);

        var passages = relevanteChunks.Select(c => c.chunk.Inhoud).ToList();

        // AI analyse
        var resultaat = await _ai.AnalyseerBeslissingAsync(
            beslissing.Titel,
            beslissing.Beschrijving,
            partij.Naam,
            passages
        );

        // Sla analyse op
        var analyse = new Analyse
        {
            BeslissingId = beslissingId,
            PartijId = partijId,
            OvereenkomstScore = resultaat.OvereenkomstScore,
            Commentaar = resultaat.Commentaar,
            Samenvatting = resultaat.Samenvatting,
            RelevanteChunkIds = string.Join(",", relevanteChunks.Select(c => c.chunk.Id)),
            DatumAnalyse = DateTime.UtcNow
        };

        _db.Analyses.Add(analyse);
        await _db.SaveChangesAsync();

        return analyse;
    }

    public async Task<List<Analyse>> AnalyseerVoorAllePartijen(int beslissingId)
    {
        var partijen = await _db.Partijen
            .Where(p => p.Documenten.Any(d => d.IsGeindexeerd))
            .ToListAsync();

        var analyses = new List<Analyse>();

        foreach (var partij in partijen)
        {
            var analyse = await AnalyseerVoorPartijAsync(beslissingId, partij.Id);
            if (analyse is not null)
                analyses.Add(analyse);
        }

        return analyses;
    }

    public async Task<Resultaat?> VoegResultaatToeAsync(int beslissingId, string beschrijving)
    {
        var beslissing = await _db.Beslissingen
            .Include(b => b.Resultaat)
            .FirstOrDefaultAsync(b => b.Id == beslissingId);

        if (beslissing is null) return null;

        // Genereer AI commentaar
        var aiResponse = await _ai.GenereerResultaatCommentaarAsync(
            beslissing.Titel,
            beslissing.Beschrijving,
            beschrijving
        );

        int score = 50;
        string? commentaar = null;

        try
        {
            var responseText = aiResponse.Trim();
            if (responseText.StartsWith("```"))
            {
                var firstNewline = responseText.IndexOf('\n');
                if (firstNewline > 0) responseText = responseText[(firstNewline + 1)..];
                if (responseText.EndsWith("```")) responseText = responseText[..^3];
                responseText = responseText.Trim();
            }

            var parsed = JsonSerializer.Deserialize<JsonElement>(responseText);
            score = parsed.GetProperty("score").GetInt32();
            commentaar = parsed.GetProperty("commentaar").GetString();
        }
        catch { /* Use defaults */ }

        if (beslissing.Resultaat is not null)
        {
            beslissing.Resultaat.Beschrijving = beschrijving;
            beslissing.Resultaat.BelofteBarometerScore = score;
            beslissing.Resultaat.AiCommentaar = commentaar;
            beslissing.Resultaat.DatumIngevoerd = DateTime.UtcNow;
        }
        else
        {
            beslissing.Resultaat = new Resultaat
            {
                BeslissingId = beslissingId,
                Beschrijving = beschrijving,
                BelofteBarometerScore = score,
                AiCommentaar = commentaar
            };
        }

        await _db.SaveChangesAsync();
        return beslissing.Resultaat;
    }
}
