namespace SqlParser.Ast
{

    /// <summary>
    /// Alter column SQL operations
    /// </summary>
    public abstract class AlterColumnOperation : IWriteSql
    {
        /// <summary>
        /// Set not null column operation
        /// <exmaple>
        /// <c>
        /// SET NOT NULL
        /// </c>
        /// </exmaple>
        /// </summary>
        public class SetNotNull : AlterColumnOperation { }
        /// <summary>
        /// Drop not null column operation
        /// <exmaple>
        /// <c>
        /// DROP NOT NULL
        /// </c>
        /// </exmaple>
        /// </summary>
        public class DropNotNull : AlterColumnOperation { }
        /// <summary>
        /// Set default column operation
        /// <exmaple>
        /// <c>
        /// SET DEFAULT
        /// </c>
        /// </exmaple>
        /// </summary>
        /// <param name="Value">Expression value</param>
        public class SetDefault : AlterColumnOperation
        {
            public SetDefault(SqlExpression Value)
            {
                this.Value = Value;
            }

            public SqlExpression Value { get; set; }
        }
        /// <summary>
        /// Drop default column operation
        /// <exmaple>
        /// <c>
        /// DROP DEFAULT
        /// </c>
        /// </exmaple>
        /// </summary>
        public class DropDefault : AlterColumnOperation { }
        /// <summary>
        /// Set data type column operation
        /// <exmaple>
        /// <c>
        /// [SET DATA] TYPE data_type [USING expr]
        /// </c>
        /// </exmaple>
        /// </summary>
        /// <param name="DataType"></param>
        public class SetDataType : AlterColumnOperation, IElement
        {
            public SetDataType(DataType DataType, SqlExpression Using = null)
            {
                this.DataType = DataType;
                this.Using = Using;
            }

            public DataType DataType { get; set; }
            public SqlExpression Using { get; set; }
        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case SetNotNull:
                    writer.Write("SET NOT NULL");
                    break;

                case DropNotNull:
                    writer.Write("DROP NOT NULL");
                    break;

                case SetDefault sd:
                    writer.WriteSql($"SET DEFAULT {sd.Value}");
                    break;

                case DropDefault:
                    writer.Write("DROP DEFAULT");
                    break;

                case SetDataType sdt:

                    writer.WriteSql($"SET DATA TYPE {sdt.DataType}");

                    if (sdt.Using != null)
                    {
                        writer.WriteSql($" USING {sdt.Using}");
                    }

                    break;

            }
        }
    }
}