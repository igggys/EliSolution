namespace Models.AdministratorSite
{
    public class Language
    {
        public byte Id { get; set; }

        /// <summary>ISO 639-1 language code (he, en, fr, ru).</summary>
        public required string Code { get; set; }

        /// <summary>English name for admin UI.</summary>
        public required string Name { get; set; }

        /// <summary>Name in the language itself.</summary>
        public required string NativeName { get; set; }

        public bool IsRtl { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }
    }

}
