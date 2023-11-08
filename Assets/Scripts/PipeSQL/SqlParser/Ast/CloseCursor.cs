namespace SqlParser.Ast
{

    /// <summary>
    /// Close Cursor specifications
    /// </summary>
    public abstract class CloseCursor : IWriteSql
    {
        /// <summary>
        /// Close all cursors
        /// </summary>
        public class All : CloseCursor
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("ALL");
            }
        }

        /// <summary>
        /// Close specific cursors
        /// </summary>
        /// <param name="Name">Cursor name identifier</param>
        public class Specific : CloseCursor
        {
            public Specific(Ident Name)
            {
                this.Name = Name;
            }

            public Ident Name { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                Name.ToSql(writer);
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}