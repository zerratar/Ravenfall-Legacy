namespace SqlParser.Ast
{

    /// <summary>
    /// Merge clause
    /// </summary>
    public abstract class MergeClause : IWriteSql, IElement
    {
        /// <summary>
        /// Matched update clause
        /// </summary>
        /// <param name="Predicate">Expression predicate</param>
        /// <param name="Assignments">Merge update assignments</param>
        public class MatchedUpdate : MergeClause
        {
            public MatchedUpdate(Sequence<Statement.Assignment> Assignments, SqlExpression? Predicate = null)
            {
                this.Assignments = Assignments;
                this.Predicate = Predicate;
            }

            public Sequence<Statement.Assignment> Assignments { get; }
            public SqlExpression Predicate { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("WHEN MATCHED");

                if (Predicate != null)
                {
                    writer.WriteSql($" AND {Predicate}");
                }

                writer.WriteSql($" THEN UPDATE SET {Assignments}");
            }
        }
        /// <summary>
        /// Matched delete clause
        /// </summary>
        /// <param name="Predicate">Delete expression</param>
        public class MatchedDelete : MergeClause
        {
            public MatchedDelete(SqlExpression? Predicate = null)
            {
                this.Predicate = Predicate;
            }

            public SqlExpression Predicate { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("WHEN MATCHED");

                if (Predicate != null)
                {
                    writer.WriteSql($" AND {Predicate}");
                }

                writer.Write(" THEN DELETE");
            }
        }
        /// <summary>
        /// Not matched update clause
        /// </summary>
        /// <param name="Predicate">Expression predicate</param>
        /// <param name="Columns">Columns</param>
        /// <param name="Values">Values</param>
        public class NotMatched : MergeClause
        {
            public NotMatched(Sequence<Ident> Columns, Values Values, SqlExpression? Predicate = null)
            {
                this.Columns = Columns;
                this.Values = Values;
                this.Predicate = Predicate;
            }

            public Sequence<Ident> Columns { get; }
            public Values Values { get; }
            public SqlExpression Predicate { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("WHEN NOT MATCHED");
                if (Predicate != null)
                {
                    writer.WriteSql($" AND {Predicate}");
                }

                writer.WriteSql($" THEN INSERT ({Columns}) {Values}");
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}