namespace SqlParser.Ast
{
    /// <summary>
    /// Table object
    /// </summary>
    public class Table : IWriteSql
    {
        public string Name { get; set; }
        public string? SchemaName { get; set; }

        public Table(string Name, string? SchemaName = null)
        {
            this.Name = Name;
            this.SchemaName = SchemaName;
        }

        public void ToSql(SqlTextWriter writer)
        {
            if (SchemaName != null)
            {
                writer.WriteSql($"TABLE {SchemaName}.{Name}");
            }
            else
            {
                writer.Write($"TABLE {Name}");
            }
        }
    }

    /// <summary>
    /// Table alias
    /// </summary>
    public class TableAlias : IWriteSql
    {
        public Ident Name { get; set; }
        public Sequence<Ident>? Columns { get; set; }

        public TableAlias(Ident Name, Sequence<Ident>? Columns = null)
        {
            this.Name = Name;
            this.Columns = Columns;
        }

        public void ToSql(SqlTextWriter writer)
        {
            Name.ToSql(writer);

            if (Columns?.SafeAny() == true)
            {
                writer.WriteSql($" ({Columns})");
            }
        }
    }
}