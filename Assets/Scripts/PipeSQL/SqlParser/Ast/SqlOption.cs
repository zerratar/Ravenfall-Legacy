namespace SqlParser.Ast
{

    /// <summary>
    /// Sql option
    /// </summary>
    /// <param name="Name">Name identifier</param>
    /// <param name="Value">Value</param>
    public class SqlOption : IWriteSql
    {
        public Ident Name { get; set; }
        public Value Value { get; set; }

        public SqlOption(Ident Name, Value Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public void ToSql(SqlTextWriter writer)
        {
            writer.WriteSql($"{Name} = {Value}");
        }
    }
}