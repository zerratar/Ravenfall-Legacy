namespace SqlParser.Ast
{

    /// <summary>
    /// Grand objects
    /// </summary>
    public abstract class GrantObjects : IWriteSql, IElement
    {
        public Sequence<ObjectName> Schemas { get; }

        public GrantObjects(Sequence<ObjectName> Schemas)
        {
            this.Schemas = Schemas;
        }
        /// <summary>
        /// Grant privileges on ALL SEQUENCES IN SCHEMA schema_name [, ...]
        /// </summary>
        /// <param name="Schemas">Schemas</param>
        public class AllSequencesInSchema : GrantObjects
        {
            public AllSequencesInSchema(Sequence<ObjectName> Schemas) : base(Schemas) { }

        }
        /// <summary>
        /// Grant privileges on ALL TABLES IN SCHEMA schema_name [, ...]
        /// </summary>
        /// <param name="Schemas">Schemas</param>
        public class AllTablesInSchema : GrantObjects
        {
            public AllTablesInSchema(Sequence<ObjectName> Schemas) : base(Schemas) { }

        }
        /// <summary>
        /// Grant privileges on specific schemas
        /// </summary>
        /// <param name="Schemas">Schemas</param>
        public class Schema : GrantObjects
        {
            public Schema(Sequence<ObjectName> Schemas) : base(Schemas) { }

        }
        /// <summary>
        /// Grant privileges on specific sequences
        /// </summary>
        /// <param name="Schemas">Schemas</param>
        public class Sequences : GrantObjects
        {
            public Sequences(Sequence<ObjectName> Schemas) : base(Schemas) { }

        }
        /// <summary>
        /// Grant privileges on specific tables
        /// </summary>
        /// <param name="Schemas">Schemas</param>
        public class Tables : GrantObjects
        {
            public Tables(Sequence<ObjectName> Schemas) : base(Schemas) { }

        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case Sequences:
                    writer.WriteSql($"SEQUENCE {Schemas}");
                    break;

                case Schema:
                    writer.WriteSql($"SCHEMA {Schemas}");
                    break;

                case Tables:
                    writer.WriteSql($"{Schemas}");
                    break;

                case AllSequencesInSchema:
                    writer.WriteSql($"ALL SEQUENCES IN SCHEMA {Schemas}");
                    break;

                case AllTablesInSchema:
                    writer.WriteSql($"ALL TABLES IN SCHEMA {Schemas}");
                    break;
            }
        }
    }
}