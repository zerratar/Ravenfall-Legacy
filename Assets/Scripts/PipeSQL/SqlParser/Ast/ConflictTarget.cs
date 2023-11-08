namespace SqlParser.Ast
{

    /// <summary>
    /// Conflict targets
    /// </summary>
    public abstract class ConflictTarget : IWriteSql
    {
        /// <summary>
        /// Column conflict targets
        /// </summary>
        /// <param name="Columns">Column name identifiers</param>
        public class Column : ConflictTarget
        {
            public Column(Sequence<Ident> Columns)
            {
                this.Columns = Columns;
            }

            public Sequence<Ident> Columns { get; }
        }
        /// <summary>
        /// On Constraint conflict target
        /// </summary>
        /// <param name="Name">Object name</param>
        public class OnConstraint : ConflictTarget, IElement
        {
            public OnConstraint(ObjectName Name)
            {
                this.Name = Name;
            }

            public ObjectName Name { get; }
        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case Column c:
                    writer.WriteSql($"({c.Columns})");
                    break;

                case OnConstraint oc:
                    writer.WriteSql($" ON CONSTRAINT {oc.Name}");
                    break;
            }
        }
    }
}