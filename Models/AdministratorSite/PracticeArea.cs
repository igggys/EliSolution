namespace Models.AdministratorSite
{
    public class PracticeArea
    {
        public int Id { get; set; }

        public int MenuItemId { get; set; }

        public string? Url { get; set; }

        public string? ImageUrl { get; set; }

        public string? IconUrl { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }

        public byte LanguageId { get; set; }

        public required string Name { get; set; }

        public string? Body { get; set; }

        public string? SeoTitle { get; set; }

        public string? MetaDescription { get; set; }

        public required string MetaRobots { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public required string OgType { get; set; }

        public string? OgUrl { get; set; }
    }
}
