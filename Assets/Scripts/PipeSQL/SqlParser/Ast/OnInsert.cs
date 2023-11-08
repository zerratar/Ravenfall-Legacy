
namespace SqlParser.Ast
{

    /// <summary>
    /// On insert statement
    /// </summary>
    public abstract class OnInsert : IWriteSql, IElement
    {
        /// <summary>
        /// MySQL when the key already exists, then execute an update instead
        /// <example>
        /// <c>
        /// ON DUPLICATE KEY UPDATE 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Assignments"></param>
        public class DuplicateKeyUpdate : OnInsert
        {
            public DuplicateKeyUpdate(Sequence<Statement.Assignment> Assignments)
            {
                this.Assignments = Assignments;
            }

            public Sequence<Statement.Assignment> Assignments { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($" ON DUPLICATE KEY UPDATE {Assignments}");
            }
        }
        /// <summary>
        /// This is a PostgreSQL and Sqlite extension
        /// <example>
        /// <c>
        /// ON CONFLICT 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="OnConflict"></param>
        public class Conflict : OnInsert
        {
            public Conflict(OnConflict OnConflict)
            {
                this.OnConflict = OnConflict;
            }

            public OnConflict OnConflict { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{OnConflict}");
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}