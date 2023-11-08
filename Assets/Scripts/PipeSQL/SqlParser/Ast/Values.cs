namespace SqlParser.Ast
{

    /// <summary>
    /// Was there an explicit ROWs keyword (MySQL)?
    /// <see href="https://dev.mysql.com/doc/refman/8.0/en/values.html"/>
    /// </summary>
    public class Values : IWriteSql, IElement
    {
        public Sequence<Sequence<SqlExpression>> Rows { get; set; }
        public bool ExplicitRow { get; set; }

        public Values(Sequence<Sequence<SqlExpression>> Rows, bool ExplicitRow = false)
        {
            this.Rows = Rows;
            this.ExplicitRow = ExplicitRow;
        }

        public void ToSql(SqlTextWriter writer)
        {
            writer.Write("VALUES ");

            var prefix = ExplicitRow ? "ROW" : string.Empty;

            for (var i = 0; i < Rows.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(", ");
                }

                writer.WriteSql($"{prefix}({Rows[i]})");
            }
        }
    }
}