using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace StemOMatiek.Services;

public class AiService
{
    private readonly IConfiguration _config;
    private readonly ApiKeyProvider _keyProvider;
    private Kernel? _kernel;
    private IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private string? _lastUsedKey;

    public AiService(IConfiguration config, ApiKeyProvider keyProvider)
    {
        _config = config;
        _keyProvider = keyProvider;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(GetApiKey());

    /// <summary>Quick validation: send a tiny chat request to verify the API key works.</summary>
    public async Task ValidateKeyAsync()
    {
        var kernel = GetKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage("Zeg enkel 'OK'.");

        try
        {
            await chat.GetChatMessageContentAsync(history);
        }
        catch (Exception ex) when (
            ex.Message.Contains("429") ||
            ex.Message.Contains("Too Many Requests") ||
            ex.InnerException?.Message.Contains("429") == true ||
            ex.InnerException?.Message.Contains("Too Many Requests") == true)
        {
            // 429 = key is valid but rate-limited. Authentication succeeded.
            return;
        }
    }

    private string? GetApiKey()
    {
        // Per-user key heeft voorrang, daarna server config
        return _keyProvider.ApiKey ?? _config["Gemini:ApiKey"];
    }

#pragma warning disable SKEXP0070
    private Kernel GetKernel()
    {
        var apiKey = GetApiKey()
            ?? throw new InvalidOperationException("Geene Gemini sleutel gevonden! Voer uwen sleutel in via de Sleutelmeester.");

        // Rebuild kernel als de key veranderd is
        if (_kernel is not null && _lastUsedKey == apiKey) return _kernel;

        var builder = Kernel.CreateBuilder();
        builder.AddGoogleAIGeminiChatCompletion("gemini-3.1-flash-lite-preview", apiKey);
        _kernel = builder.Build();
        _lastUsedKey = apiKey;
        _embeddingGenerator = null; // Reset embedding generator too
        return _kernel;
    }

    private IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator()
    {
        var apiKey = GetApiKey()
            ?? throw new InvalidOperationException("Geene Gemini sleutel gevonden!");

        if (_embeddingGenerator is not null && _lastUsedKey == apiKey) return _embeddingGenerator;

        var builder = Kernel.CreateBuilder();
        builder.AddGoogleAIEmbeddingGenerator("gemini-embedding-001", apiKey);
        var kernel = builder.Build();
        _embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        _lastUsedKey = apiKey;
        return _embeddingGenerator;
    }
#pragma warning restore SKEXP0070

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var generator = GetEmbeddingGenerator();
        var result = await generator.GenerateAsync([text]);
        return result[0].Vector.ToArray();
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(IList<string> texts)
    {
        var generator = GetEmbeddingGenerator();
        var results = await generator.GenerateAsync(texts);
        return results.Select(e => e.Vector.ToArray()).ToList();
    }

    public async Task<AnalyseResultaat> AnalyseerBeslissingAsync(
        string beslissingTitel,
        string beslissingBeschrijving,
        string partijNaam,
        List<string> relevantePassages)
    {
        var kernel = GetKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var passagesText = string.Join("\n---\n", relevantePassages);

        var prompt = $$"""
        Gij zijt de Stem-O-Matiek, eene sarcastische analysemachien die in het Oud-Vlaamsch schrijft.
        Uwe taak is om beslissingen van de Vlaamsche regeering te toetsen aen de beloften der partijen.

        BESLISSING: {{beslissingTitel}}
        BESCHRIJVING: {{beslissingBeschrijving}}

        PARTIJ: {{partijNaam}}
        RELEVANTE PASSAGES UIT HET PARTIJPROGRAMMA:
        {{passagesText}}

        Geef uwe analyse in het volgende JSON-formaat (en ENKEL JSON, geen andere tekst):
        {
            "overeenkomstScore": <getal van 0 tot 100, waarbij 100 = perfecte overeenkomst met het programma>,
            "samenvatting": "<korte samenvatting van de analyse in het Oud-Vlaamsch, max 2 zinnen>",
            "commentaar": "<sarcastisch commentaar in het Oud-Vlaamsch over hoe goed of slecht deze beslissing past bij de beloften, max 3 zinnen>"
        }

        Indien er geene relevante passages zijn, geef dan eene score van 50 en merkt op dat het programma hierover zwijgt als het graf.
        """;

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await chat.GetChatMessageContentAsync(history);
        var responseText = response.Content ?? "{}";

        // Strip markdown code fences if present
        responseText = responseText.Trim();
        if (responseText.StartsWith("```"))
        {
            var firstNewline = responseText.IndexOf('\n');
            if (firstNewline > 0) responseText = responseText[(firstNewline + 1)..];
            if (responseText.EndsWith("```")) responseText = responseText[..^3];
            responseText = responseText.Trim();
        }

        try
        {
            var result = JsonSerializer.Deserialize<AnalyseResultaat>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? new AnalyseResultaat();
        }
        catch
        {
            return new AnalyseResultaat
            {
                OvereenkomstScore = 50,
                Samenvatting = "De machien kon de analyse niet voltooien.",
                Commentaar = "Zelfs de Stem-O-Matiek heeft soms eenen slechten dag, waerde burger."
            };
        }
    }

    public async Task<AnalyseResultaat> AnalyseerStellingAsync(
        string stelling,
        string partijNaam,
        List<string> relevantePassages)
    {
        var kernel = GetKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var passagesText = string.Join("\n---\n", relevantePassages);

        var prompt = $$"""
        Gij zijt de Stem-O-Matiek, eene sarcastische analysemachien die in het Oud-Vlaamsch schrijft.
        Uwe taak is om eene stelling te toetsen aen het partijprogramma van eene Vlaamsche partij.

        STELLING: {{stelling}}

        PARTIJ: {{partijNaam}}
        RELEVANTE PASSAGES UIT HET PARTIJPROGRAMMA:
        {{passagesText}}

        Geef uwe analyse in het volgende JSON-formaat (en ENKEL JSON, geen andere tekst):
        {
            "overeenkomstScore": <getal van 0 tot 100, waarbij 100 = de partij is het hier volkomen mede eens, 0 = de partij is hier mordicus tegen>,
            "samenvatting": "<korte samenvatting: is de partij het eens of oneens met deze stelling, en waarom? Max 2 zinnen, in het Oud-Vlaamsch>",
            "commentaar": "<sarcastisch commentaar in het Oud-Vlaamsch over de positie van deze partij, max 3 zinnen>"
        }

        Indien er geene relevante passages zijn, geef dan eene score van 50 en merkt op dat het programma hierover zwijgt als het graf.
        """;

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await chat.GetChatMessageContentAsync(history);
        var responseText = response.Content ?? "{}";

        // Strip markdown code fences if present
        responseText = responseText.Trim();
        if (responseText.StartsWith("```"))
        {
            var firstNewline = responseText.IndexOf('\n');
            if (firstNewline > 0) responseText = responseText[(firstNewline + 1)..];
            if (responseText.EndsWith("```")) responseText = responseText[..^3];
            responseText = responseText.Trim();
        }

        try
        {
            var result = JsonSerializer.Deserialize<AnalyseResultaat>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? new AnalyseResultaat();
        }
        catch
        {
            return new AnalyseResultaat
            {
                OvereenkomstScore = 50,
                Samenvatting = "De machien kon de analyse niet voltooien.",
                Commentaar = "Zelfs de Stem-O-Matiek heeft soms eenen slechten dag, waerde burger."
            };
        }
    }

    public async Task<string> GenereerResultaatCommentaarAsync(
        string beslissingTitel,
        string oorspronkelijkeBeschrijving,
        string resultaatBeschrijving)
    {
        var kernel = GetKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var prompt = $$"""
        Gij zijt de Stem-O-Matiek, eene sarcastische machien die in het Oud-Vlaamsch schrijft.
        Vergelijk de oorspronkelijke beslissing met het werkelijke resultaat.

        BESLISSING: {{beslissingTitel}}
        WAT BELOOFD WERD: {{oorspronkelijkeBeschrijving}}
        WAT ER WERKELIJK GEBEURDE: {{resultaatBeschrijving}}

        Geef een sarcastisch commentaar in het Oud-Vlaamsch (max 4 zinnen) over het verschil tussen belofte en realiteit.
        Geef OOK een score van 0-100 (100 = belofte volledig nagekomen).

        Formaat (ENKEL JSON):
        {
            "score": <getal>,
            "commentaar": "<tekst>"
        }
        """;

        var history = new ChatHistory();
        history.AddUserMessage(prompt);
        var response = await chat.GetChatMessageContentAsync(history);
        return response.Content ?? "{}";
    }

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denominator == 0 ? 0 : dot / denominator;
    }
}

public class AnalyseResultaat
{
    public int OvereenkomstScore { get; set; } = 50;
    public string Samenvatting { get; set; } = string.Empty;
    public string Commentaar { get; set; } = string.Empty;
}
