namespace SqlParser.Ast
{

    /// <summary>
    /// Actions such as create, execute, select, etc.
    /// </summary>
    public abstract class Action : IWriteSql
    {
        public abstract class ColumnAction : Action
        {
            public ColumnAction(Sequence<Ident> Columns = null)
            {
                this.Columns = Columns;
            }

            public Sequence<Ident>? Columns { get; }
        }

        /// <summary>
        /// Connect action
        /// </summary>
        public class Connect : Action { }
        /// <summary>
        /// Create action
        /// </summary>
        public class Create : Action { }
        /// <summary>
        /// Delete action
        /// </summary>
        public class Delete : Action { }
        /// <summary>
        /// Execute action
        /// </summary>
        public class Execute : Action { }
        /// <summary>
        /// Insert action
        /// </summary>
        public class Insert : ColumnAction
        {
            public Insert(Sequence<Ident> Columns = null) : base(Columns) { }

        }
        /// <summary>
        /// References action
        /// </summary>
        public class References : ColumnAction
        {
            public References(Sequence<Ident> Columns = null) : base(Columns) { }

        }
        /// <summary>
        /// Select action
        /// </summary>
        public class Select : ColumnAction
        {
            public Select(Sequence<Ident> Columns = null) : base(Columns) { }

        }
        /// <summary>
        /// Temporary action
        /// </summary>
        public class Temporary : Action { }
        /// <summary>
        /// Trigger action
        /// </summary>
        public class Trigger : Action { }
        /// <summary>
        /// Truncate action
        /// </summary>
        public class Truncate : Action { }
        /// <summary>
        /// Update action
        /// </summary>
        public class Update : ColumnAction
        {
            public Update(Sequence<Ident> Columns) : base(Columns) { }

        }
        /// <summary>
        /// Usage action
        /// </summary>
        public class Usage : Action { }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case Connect:
                    writer.Write(value: "CONNECT");
                    break;

                case Create:
                    writer.Write(value: "CREATE");
                    break;

                case Delete:
                    writer.Write(value: "DELETE");
                    break;

                case Execute:
                    writer.Write(value: "EXECUTE");
                    break;

                case Insert:
                    writer.Write(value: "INSERT");
                    break;

                case References:
                    writer.Write(value: "REFERENCES");
                    break;

                case Select:
                    writer.Write(value: "SELECT");
                    break;

                case Temporary:
                    writer.Write(value: "TEMPORARY");
                    break;

                case Trigger:
                    writer.Write(value: "TRIGGER");
                    break;

                case Truncate:
                    writer.Write(value: "TRUNCATE");
                    break;

                case Update:
                    writer.Write(value: "UPDATE");
                    break;

                case Usage:
                    writer.Write(value: "USAGE");
                    break;
            }

            if (this is not ColumnAction c)
            {
                return;
            }

            if (c.Columns.SafeAny())
            {
                writer.WriteSql($" ({c.Columns})");
            }
        }
    }
}