namespace SqlParser.Ast
{

    /// <summary>
    /// With statement
    /// </summary>
    /// <param name="Recursive">True if recursive</param>
    /// <param name="CteTables">Common expression tables</param>
    public class With : IWriteSql, IElement
    {
        public bool Recursive { get; set; }
        public Sequence<CommonTableExpression> CteTables { get; set; }

        public With(bool Recursive, Sequence<CommonTableExpression> CteTables)
        {
            this.Recursive = Recursive;
            this.CteTables = CteTables;
        }

        public void ToSql(SqlTextWriter writer)
        {
            var recursive = Recursive ? "RECURSIVE " : string.Empty;
            writer.WriteSql($"WITH {recursive}{CteTables}");
        }
    }
}