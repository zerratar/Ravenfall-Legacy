namespace SqlParser.Ast
{

    /// <summary>
    /// A single CTE (used after WITH): alias [(col1, col2, ...)] AS ( query )
    /// The names in the column list before AS, when specified, replace the names
    /// of the columns returned by the query. The parser does not validate that the
    /// number of columns in the query matches the number of columns in the query.
    /// </summary>
    /// <param name="Alias">CTE Alias</param>
    /// <param name="Query">CTE Select</param>
    /// <param name="From">Optional From identifier</param>
    public class CommonTableExpression : IWriteSql, IElement
    {
        public TableAlias Alias { get; set; }
        public Query Query { get; set; }
        public Ident? From { get; set; }

        public CommonTableExpression(TableAlias Alias, Query Query, Ident? From = null)
        {
            this.Alias = Alias;
            this.Query = Query;
            this.From = From;
        }

        public void ToSql(SqlTextWriter writer)
        {
            writer.WriteSql($"{Alias} AS ({Query.ToSql()})");

            if (From != null)
            {
                writer.WriteSql($" FROM {From}");
            }
        }
    }
}