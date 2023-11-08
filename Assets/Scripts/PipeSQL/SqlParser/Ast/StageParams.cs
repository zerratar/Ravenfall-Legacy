namespace SqlParser.Ast
{

    /// <summary>
    /// Snowflake stage params
    /// </summary>
    public class StageParams : IWriteSql
    {
        public string? Url { get; set; }
        public string? Endpoint { get; set; }
        public string? StorageIntegration { get; set; }
        public Sequence<DataLoadingOption>? Credentials { get; set; }
        public Sequence<DataLoadingOption>? Encryption { get; set; }

        public void ToSql(SqlTextWriter writer)
        {
            if (Url != null)
            {
                writer.Write($" URL='{Url}'");
            }

            if (StorageIntegration != null)
            {
                writer.Write($" STORAGE_INTEGRATION={StorageIntegration}");
            }

            if (Endpoint != null)
            {
                writer.Write($" ENDPOINT='{Endpoint}'");
            }

            if (Credentials != null)
            {
                writer.WriteSql($" CREDENTIALS=({Credentials.ToSqlDelimited(" ")})");
            }

            if (Encryption != null)
            {
                writer.WriteSql($" ENCRYPTION=({Encryption.ToSqlDelimited(" ")})");
            }
        }
    }

    /// <summary>
    /// Snowflake data loading options
    /// </summary>
    public class DataLoadingOption : IWriteSql
    {
        public string Name { get; set; }
        public DataLoadingOptionType OptionType { get; set; }
        public string Value { get; set; }

        public DataLoadingOption(string Name, DataLoadingOptionType OptionType, string Value)
        {
            this.Name = Name;
            this.OptionType = OptionType;
            this.Value = Value;
        }

        public void ToSql(SqlTextWriter writer)
        {
            writer.Write(OptionType == DataLoadingOptionType.String
                ? $"{Name}='{Value}'"
                : $"{Name}={Value}");
        }
    }
}