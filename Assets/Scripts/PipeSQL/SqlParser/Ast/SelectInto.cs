namespace SqlParser.Ast
{

    /// <summary>
    /// Select into operation
    /// </summary>
    /// <param name="Name">Object name identifier</param>
    public class SelectInto : IWriteSql, IElement
    {
        public ObjectName Name { get; set; }
        public bool Temporary { get; set; }
        public bool Unlogged { get; set; }
        public bool Table { get; set; }

        public SelectInto(ObjectName Name)
        {
            this.Name = Name;
        }

        public void ToSql(SqlTextWriter writer)
        {
            var temp = Temporary ? " TEMPORARY" : string.Empty;
            var unlogged = Unlogged ? " UNLOGGED" : string.Empty;
            var table = Table ? " TABLE" : string.Empty;

            writer.Write($"INTO{temp}{unlogged}{table} {Name}");
        }
    }

}