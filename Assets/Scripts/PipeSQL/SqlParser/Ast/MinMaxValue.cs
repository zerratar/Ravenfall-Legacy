namespace SqlParser.Ast
{

    /// <summary>
    /// Min or max value
    /// </summary>
    public abstract class MinMaxValue
    {
        /// <summary>
        /// Clause is not specified
        /// </summary>
        public class Empty : MinMaxValue { }
        /// <summary>
        /// NO minvalue, no maxvalue
        /// </summary>
        public class None : MinMaxValue { }
        /// <summary>
        /// Minimum or maximum value
        /// <example>
        /// <c>
        /// MINVALUE Expression / MAXVALUE expr
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Min/Max value expression</param>
        public class Some : MinMaxValue, IWriteSql, IElement
        {
            public Some(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public void ToSql(SqlTextWriter writer)
            {
                Expression.ToSql(writer);
            }
        }
    }
}