namespace StemOMatiek.Data.Models;

public class DocumentChunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string Inhoud { get; set; } = string.Empty;
    public string? SectieNaam { get; set; }
    public int Volgnummer { get; set; }

    /// <summary>
    /// JSON-serialized float[] embedding vector from OpenAI text-embedding-3-small
    /// </summary>
    public string? EmbeddingJson { get; set; }

    public Document Document { get; set; } = null!;
}
