namespace Options
{
    public class AdministratorDatabaseConnection
    {
        public const string SectionName = "AdministratorDatabaseConnection";

        required public string ConnectionString { get; set; }
    }
}
