namespace SqlParser.Ast
{

    /// <summary>
    /// Schema name
    /// </summary>
    public abstract class SchemaName : Statement
    {
        /// <summary>
        /// Only schema name specified: schema name
        /// </summary>
        /// <param name="Name"></param>
        public class Simple : SchemaName
        {
            public Simple(ObjectName Name)
            {
                this.Name = Name;
            }

            public ObjectName Name { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write(Name);
            }
        }

        /// <summary>
        /// Only authorization identifier specified: `AUTHORIZATION schema authorization identifier`
        /// </summary>
        /// <param name="Value"></param>
        public class UnnamedAuthorization : SchemaName
        {
            public UnnamedAuthorization(Ident Value)
            {
                this.Value = Value;
            }

            public Ident Value { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write($"AUTHORIZATION {Value}");
            }
        }

        /// <summary>
        /// Both schema name and authorization identifier specified: `schema name  AUTHORIZATION schema authorization identifier`
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public class NamedAuthorization : SchemaName
        {
            public NamedAuthorization(ObjectName Name, Ident Value)
            {
                this.Name = Name;
                this.Value = Value;
            }

            public ObjectName Name { get; }
            public Ident Value { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write($"{Name} AUTHORIZATION {Value}");
            }
        }
    }
}