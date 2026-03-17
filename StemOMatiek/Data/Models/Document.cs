namespace StemOMatiek.Data.Models;

public class Document
{
    public int Id { get; set; }
    public int PartijId { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string Inhoud { get; set; } = string.Empty;
    public string Type { get; set; } = "Partijprogramma"; // Partijprogramma, Verkiezingsbelofte, Regeerakkoord
    public DateTime DatumToegevoegd { get; set; } = DateTime.UtcNow;
    public bool IsGeindexeerd { get; set; }

    public Partij Partij { get; set; } = null!;
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
