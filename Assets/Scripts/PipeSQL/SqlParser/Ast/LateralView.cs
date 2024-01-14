namespace SqlParser.Ast
{

    /// <summary>
    /// A Hive LATERAL VIEW with potential column aliases
    /// </summary>
    /// <param name="Expression">Vie expression</param>
    public class LateralView : IWriteSql, IElement
    {
        public LateralView(SqlExpression Expression)
        {
            this.Expression = Expression;
        }

        [Visit(1)]
        public ObjectName? LateralViewName { get; set; }
        public Sequence<Ident?> LateralColAlias { get; set; }
        public bool Outer { get; set; }
        [Visit(0)] public SqlExpression Expression { get; }

        public void ToSql(SqlTextWriter writer)
        {
            var outer = Outer ? " OUTER" : null;
            writer.WriteSql($" LATERAL VIEW{outer} {Expression} {LateralViewName}");

            if (LateralColAlias.SafeAny())
            {
                writer.WriteSql($" AS {LateralColAlias}");
            }
        }
    }
}