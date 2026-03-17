using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StemOMatiek.Data;
using StemOMatiek.Data.Models;

namespace StemOMatiek.Services;

public class DocumentService
{
    private readonly AppDbContext _db;
    private readonly AiService _ai;
    private const int ChunkSize = 500; // ~500 woorden per chunk
    private const int ChunkOverlap = 50;

    public DocumentService(AppDbContext db, AiService ai)
    {
        _db = db;
        _ai = ai;
    }

    public async Task<Document> VoegDocumentToeAsync(int partijId, string titel, string inhoud, string type = "Partijprogramma")
    {
        var doc = new Document
        {
            PartijId = partijId,
            Titel = titel,
            Inhoud = inhoud,
            Type = type,
            DatumToegevoegd = DateTime.UtcNow
        };

        _db.Documenten.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }

    public async Task IndexeerDocumentAsync(int documentId)
    {
        var doc = await _db.Documenten
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (doc is null) return;

        // Verwijder bestaande chunks
        _db.DocumentChunks.RemoveRange(doc.Chunks);

        // Split in chunks
        var chunks = SplitInChunks(doc.Inhoud);
        var chunkEntities = new List<DocumentChunk>();

        for (int i = 0; i < chunks.Count; i++)
        {
            chunkEntities.Add(new DocumentChunk
            {
                DocumentId = doc.Id,
                Inhoud = chunks[i].inhoud,
                SectieNaam = chunks[i].sectie,
                Volgnummer = i
            });
        }

        // Genereer embeddings in batches (max 20 per call om rate limits te vermijden)
        if (_ai.IsConfigured)
        {
            const int batchSize = 20;
            try
            {
                for (int batch = 0; batch < chunkEntities.Count; batch += batchSize)
                {
                    var batchChunks = chunkEntities.Skip(batch).Take(batchSize).ToList();
                    var texts = batchChunks.Select(c => c.Inhoud).ToList();
                    var embeddings = await _ai.GetEmbeddingsAsync(texts);

                    for (int i = 0; i < batchChunks.Count; i++)
                    {
                        batchChunks[i].EmbeddingJson = JsonSerializer.Serialize(embeddings[i]);
                    }

                    // Kleine pauze tussen batches om rate limits te vermijden
                    if (batch + batchSize < chunkEntities.Count)
                        await Task.Delay(500);
                }
            }
            catch (Exception)
            {
                // Embeddings gefaald — chunks worden toch opgeslagen zonder embeddings
            }
        }

        _db.DocumentChunks.AddRange(chunkEntities);
        doc.IsGeindexeerd = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<(DocumentChunk chunk, float score)>> ZoekRelevanteChunksAsync(
        string query, int partijId, int topK = 5)
    {
        // Probeer eerst met embeddings
        if (_ai.IsConfigured)
        {
            var chunksMetEmbedding = await _db.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document.PartijId == partijId && c.EmbeddingJson != null)
                .ToListAsync();

            if (chunksMetEmbedding.Count > 0)
            {
                try
                {
                    var queryEmbedding = await _ai.GetEmbeddingAsync(query);

                    var scored = chunksMetEmbedding
                        .Select(c =>
                        {
                            var embedding = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson!);
                            var score = embedding is not null ? AiService.CosineSimilarity(queryEmbedding, embedding) : 0f;
                            return (chunk: c, score);
                        })
                        .OrderByDescending(x => x.score)
                        .Take(topK)
                        .Where(x => x.score > 0.3f)
                        .ToList();

                    if (scored.Count > 0) return scored;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZoekChunks] Embedding zoeken mislukt, val terug op tekst: {ex.Message}");
                }
            }
        }

        // Terugval: eenvoudige tekszoek op trefwoorden
        Console.WriteLine($"[ZoekChunks] Terugval op trefwoord-zoeken voor partij {partijId}");
        var queryWords = query.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .ToArray();

        var alleChunks = await _db.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.Document.PartijId == partijId)
            .ToListAsync();

        if (alleChunks.Count == 0) return new();

        // Score op basis van trefwoord-overlap
        var textScored = alleChunks
            .Select(c =>
            {
                var lower = c.Inhoud.ToLowerInvariant();
                var hits = queryWords.Count(w => lower.Contains(w));
                var score = queryWords.Length > 0 ? (float)hits / queryWords.Length : 0f;
                return (chunk: c, score);
            })
            .OrderByDescending(x => x.score)
            .Take(topK)
            .ToList();

        return textScored;
    }

    private static List<(string inhoud, string? sectie)> SplitInChunks(string text)
    {
        var result = new List<(string inhoud, string? sectie)>();
        var lines = text.Split('\n', StringSplitOptions.None);
        string? currentSection = null;
        var currentChunk = new List<string>();
        int wordCount = 0;

        foreach (var line in lines)
        {
            // Detect section headers (lines starting with # or all caps lines)
            if (line.TrimStart().StartsWith('#') ||
                (line.Length > 3 && line.Length < 100 && line == line.ToUpperInvariant() && line.Any(char.IsLetter)))
            {
                // Save current chunk if we have content
                if (currentChunk.Count > 0)
                {
                    result.Add((string.Join('\n', currentChunk), currentSection));
                    currentChunk.Clear();
                    wordCount = 0;
                }
                currentSection = line.Trim().TrimStart('#').Trim();
            }

            currentChunk.Add(line);
            wordCount += line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            if (wordCount >= ChunkSize)
            {
                result.Add((string.Join('\n', currentChunk), currentSection));

                // Keep overlap
                var overlapLines = currentChunk.TakeLast(3).ToList();
                currentChunk.Clear();
                currentChunk.AddRange(overlapLines);
                wordCount = overlapLines.Sum(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            }
        }

        if (currentChunk.Count > 0)
        {
            result.Add((string.Join('\n', currentChunk), currentSection));
        }

        return result;
    }
}
