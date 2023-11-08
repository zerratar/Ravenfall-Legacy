namespace SqlParser.Ast
{

    /// <summary>
    /// An ARRAY_AGG invocation
    /// 
    /// ORDER BY position is defined differently for BigQuery, Postgres and Snowflake.
    /// <example>
    /// <c>
    /// ARRAY_AGG( [ DISTINCT ] Expression [ORDER BY expr] [LIMIT n] )
    /// Or
    /// ARRAY_AGG( [ DISTINCT ] Expression ) [ WITHIN GROUP ( ORDER BY Expression ) ]
    /// </c>
    /// </example>
    /// </summary>
    /// <param name="Expression"></param>
    public class ArrayAggregate : IWriteSql, IElement
    {
        public ArrayAggregate(SqlExpression Expression)
        {
            this.Expression = Expression;
        }

        [Visit(1)]
        public OrderByExpression? OrderBy { get; set; }
        [Visit(2)]
        public SqlExpression? Limit { get; set; }
        public bool Distinct { get; set; }
        public bool WithinGroup { get; set; }
        [Visit(0)] public SqlExpression Expression { get; }

        public void ToSql(SqlTextWriter writer)
        {
            var distinct = Distinct ? "DISTINCT " : null;
            writer.WriteSql($"ARRAY_AGG({distinct}{Expression}");

            if (!WithinGroup)
            {
                if (OrderBy != null)
                {
                    writer.WriteSql($" ORDER BY {OrderBy}");
                }
                if (Limit != null)
                {
                    writer.WriteSql($" LIMIT {Limit}");
                }
            }

            writer.Write(")");

            if (WithinGroup)
            {
                if (OrderBy != null)
                {
                    writer.WriteSql($" WITHIN GROUP (ORDER BY {OrderBy})");
                }
            }
        }
    }

    /// <summary>
    /// Represents an Array Expression
    /// <example>
    /// <c>
    /// `ARRAY[..]`, or `[..]`
    /// </c>
    /// </example>
    /// </summary>
    /// <param name="Element">The list of expressions between brackets</param>
    /// <param name="Named">true for ARRAY[..], false for [..]</param>
    public class ArrayExpression : IWriteSql, IElement
    {
        public ArrayExpression(Sequence<SqlExpression> Element, bool Named = false)
        {
            this.Element = Element;
            this.Named = Named;
        }

        public Sequence<SqlExpression> Element { get; }
        public bool Named { get; }

        public void ToSql(SqlTextWriter writer)
        {
            var named = Named ? "ARRAY" : null;

            writer.WriteSql($"{named}[{Element}]");
        }
    }
}