namespace SqlParser.Ast
{

    /// <summary>
    /// Alter index operations
    /// </summary>
    public abstract class AlterIndexOperation : IWriteSql
    {
        /// <summary>
        /// Rename index alter operation
        /// </summary>
        /// <param name="Name">Object name</param>
        public class RenameIndex : AlterIndexOperation, IElement
        {
            public ObjectName Name { get; set; }

            public RenameIndex(ObjectName Name)
            {
                this.Name = Name;
            }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write($"RENAME TO {Name}");
            }
        }


        public abstract void ToSql(SqlTextWriter writer);
    }
}