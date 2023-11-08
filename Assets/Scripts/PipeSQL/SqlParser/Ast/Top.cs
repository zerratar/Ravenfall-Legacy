namespace SqlParser.Ast
{

    /// <summary>
    /// Top query qualifier
    /// </summary>
    /// <param name="Quantity">Quantity expression</param>
    /// <param name="WithTies">True if with ties</param>
    /// <param name="Percent">True if percentage</param>
    public class Top : IWriteSql, IElement
    {
        public Top(SqlExpression? Quantity, bool WithTies, bool Percent)
        {
            this.Quantity = Quantity;
            this.WithTies = WithTies;
            this.Percent = Percent;
        }

        public SqlExpression Quantity { get; }
        public bool WithTies { get; }
        public bool Percent { get; }

        public void ToSql(SqlTextWriter writer)
        {
            var extension = WithTies ? " WITH TIES" : null;

            if (Quantity != null)
            {
                var percent = Percent ? " PERCENT" : null;
                writer.WriteSql($"TOP ({Quantity}){percent}{extension}");
            }
            else
            {
                writer.WriteSql($"TOP{extension}");
            }
        }
    }
}