namespace SqlParser.Ast
{

    /// <summary>
    /// Lock clause
    /// </summary>
    /// <param name="LockType">Lock type</param>
    /// <param name="Of">Lock object Name</param>
    /// <param name="NonBlock">Non-block flag</param>
    public class LockClause : IWriteSql, IElement
    {
        public LockClause(LockType LockType, NonBlock NonBlock, ObjectName? Of = null)
        {
            this.LockType = LockType;
            this.NonBlock = NonBlock;
            this.Of = Of;
        }

        public LockType LockType { get; }
        public NonBlock NonBlock { get; }
        public ObjectName Of { get; }

        public void ToSql(SqlTextWriter writer)
        {
            writer.WriteSql($"FOR {LockType}");

            if (Of != null)
            {
                writer.Write($" OF {Of}");
            }

            if (NonBlock != NonBlock.None)
            {
                writer.WriteSql($" {NonBlock}");
            }
        }
    }
}