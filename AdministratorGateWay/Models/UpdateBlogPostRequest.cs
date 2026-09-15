using System.ComponentModel.DataAnnotations;

namespace AdministratorGateWay.Models;

public class UpdateBlogPostRequest
{
    [Range(1, 255)]
    public byte LanguageId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public int? SortOrder { get; set; }

    public DateTime? PublishedAt { get; set; }

    [MaxLength(200)]
    public string? SeoTitle { get; set; }

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    [MaxLength(100)]
    public string? MetaRobots { get; set; }

    [MaxLength(200)]
    public string? OgTitle { get; set; }

    [MaxLength(500)]
    public string? OgDescription { get; set; }

    [MaxLength(500)]
    public string? OgImage { get; set; }

    [MaxLength(50)]
    public string? OgType { get; set; }

    [MaxLength(500)]
    public string? OgUrl { get; set; }
}
