namespace Models;

public sealed class BlogImage
{
    public int Id { get; set; }

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public required byte[] Content { get; set; }
}
