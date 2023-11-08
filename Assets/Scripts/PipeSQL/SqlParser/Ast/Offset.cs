namespace SqlParser.Ast
{

    /// <summary>
    /// Offset expression
    /// </summary>
    /// <param name="Value">Expression</param>
    /// <param name="Rows">Offset rows type</param>
    public class Offset : IWriteSql, IElement
    {
        public Offset(SqlExpression Value, OffsetRows Rows)
        {
            this.Value = Value;
            this.Rows = Rows;
        }

        public SqlExpression Value { get; }
        public OffsetRows Rows { get; }

        public void ToSql(SqlTextWriter writer)
        {
            writer.WriteSql($"OFFSET {Value}");

            if (Rows != OffsetRows.None)
            {
                writer.WriteSql($" {Rows}");
            }
        }
    }
}