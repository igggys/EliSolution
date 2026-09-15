namespace Options
{
    public class DatabaseConnection
    {
        public const string SectionName = "DatabaseConnection";
        required public string ConnectionString { get; set; }
    }
}
