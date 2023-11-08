namespace SqlParser.Ast
{

    /// <summary>
    /// SQL column definition
    /// </summary>
    /// <param name="Name">Column name</param>
    /// <param name="DataType">Column data type</param>
    /// <param name="Collation">Collation</param>
    /// <param name="Options">Column options</param>
    public class ColumnDef : IWriteSql, IElement
    {
        public ColumnDef(Ident Name, DataType DataType, ObjectName? Collation = null, Sequence<ColumnOptionDef>? Options = null)
        {
            this.Name = Name;
            this.DataType = DataType;
            this.Collation = Collation;
            this.Options = Options;
        }

        public Ident Name { get; }
        public DataType DataType { get; }
        public ObjectName Collation { get; }
        public Sequence<ColumnOptionDef> Options { get; }

        public void ToSql(SqlTextWriter writer)
        {
            writer.WriteSql($"{Name} {DataType}");

            if (Options != null)
            {
                foreach (var option in Options)
                {
                    writer.WriteSql($" {option}");
                }
            }
        }
    }

    /// <summary>
    /// An optionally-named ColumnOption: [ CONSTRAINT name ] column-option.
    ///
    /// Note that implementations are substantially more permissive than the ANSI
    /// specification on what order column options can be presented in, and whether
    /// they are allowed to be named. The specification distinguishes between
    /// constraints (NOT NULL, UNIQUE, PRIMARY KEY, and CHECK), which can be named
    /// and can appear in any order, and other options (DEFAULT, GENERATED), which
    /// cannot be named and must appear in a fixed order. `PostgreSQL`, however,
    /// allows preceding any option with `CONSTRAINT name`, even those that are
    /// not really constraints, like NULL and DEFAULT. MSSQL is less permissive,
    /// allowing DEFAULT, UNIQUE, PRIMARY KEY and CHECK to be named, but not NULL or
    /// NOT NULL constraints (the last of which is in violation of the spec).
    ///
    /// For maximum flexibility, we don't distinguish between constraint and
    /// non-constraint options, lumping them all together under the umbrella of
    /// "column options," and we allow any column option to be named.
    /// </summary>
    /// <param name="Name">Name identifier</param>
    /// <param name="Option">Column Options</param>
    public class ColumnOptionDef : IWriteSql, IElement
    {
        public ColumnOptionDef(ColumnOption Option, Ident? Name = null)
        {
            this.Option = Option;
            this.Name = Name;
        }

        public ColumnOption Option { get; }
        public Ident Name { get; }

        public void ToSql(SqlTextWriter writer)
        {
            if (Name != null)
            {
                writer.Write($"CONSTRAINT {Name} ");
            }
            writer.WriteSql($"{Option}");
        }
    }
}