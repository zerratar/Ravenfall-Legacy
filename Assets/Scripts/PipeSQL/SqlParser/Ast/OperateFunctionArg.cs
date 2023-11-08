namespace SqlParser.Ast
{

    /// <summary>
    /// Operate function argument
    /// </summary>
    /// <param name="Mode">Argument mode</param>
    public class OperateFunctionArg : IWriteSql, IElement
    {
        public OperateFunctionArg(ArgMode Mode)
        {
            this.Mode = Mode;
        }
        public Ident? Name { get; set; }
        public DataType? DataType { get; set; }
        public SqlExpression? DefaultExpr { get; set; }
        public ArgMode Mode { get; }

        public void ToSql(SqlTextWriter writer)
        {
            if (Mode != ArgMode.None)
            {
                writer.WriteSql($"{Mode} ");
            }

            if (Name != null)
            {
                writer.WriteSql($"{Name} ");
            }

            if (DataType != null)
            {
                writer.WriteSql($"{DataType}");
            }

            if (DefaultExpr != null)
            {
                writer.WriteSql($" = {DefaultExpr}");
            }
        }
    }
}