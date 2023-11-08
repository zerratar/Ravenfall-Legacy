namespace SqlParser.Ast
{

    /// <summary>
    /// Additional options for wildcards, e.g. Snowflake EXCLUDE/RENAME and BigQuery EXCEPT.
    /// </summary>
    public class WildcardAdditionalOptions : IWriteSql, IElement
    {
        // [EXCLUDE...]
        public ExcludeSelectItem? ExcludeOption { get; set; }
        // [EXCEPT...]
        public ExceptSelectItem? ExceptOption { get; set; }
        // [RENAME ...]
        public RenameSelectItem? RenameOption { get; set; }
        // [REPLACE]
        // BigQuery syntax: <https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#select_replace>
        public ReplaceSelectItem? ReplaceOption { get; set; }

        public void ToSql(SqlTextWriter writer)
        {
            if (ExcludeOption != null)
            {
                writer.WriteSql($" {ExcludeOption}");
            }

            if (ExceptOption != null)
            {
                writer.WriteSql($" {ExceptOption}");
            }

            if (RenameOption != null)
            {
                writer.WriteSql($" {RenameOption}");
            }

            if (ReplaceOption != null)
            {
                writer.WriteSql($" {ReplaceOption}");
            }
        }
    }

    public class ReplaceSelectItem : IWriteSql, IElement
    {
        public Sequence<ReplaceSelectElement> Items { get; set; }

        public ReplaceSelectItem(Sequence<ReplaceSelectElement> Items)
        {
            this.Items = Items;
        }

        public void ToSql(SqlTextWriter writer)
        {
            writer.WriteSql($"REPLACE ({Items})");
        }
    }

    public class ReplaceSelectElement : IWriteSql, IElement
    {
        public SqlExpression Expr { get; set; }
        public Ident Name { get; set; }
        public bool AsKeyword { get; set; }

        public ReplaceSelectElement(SqlExpression Expr, Ident Name, bool AsKeyword)
        {
            this.Expr = Expr;
            this.Name = Name;
            this.AsKeyword = AsKeyword;
        }

        public void ToSql(SqlTextWriter writer)
        {
            if (AsKeyword)
            {
                writer.WriteSql($"{Expr} AS {Name}");
            }
            else
            {
                writer.WriteSql($"{Expr} {Name}");
            }
        }
    }
}