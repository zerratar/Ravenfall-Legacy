using System.Linq;

namespace SqlParser.Ast
{

    /// <summary>
    /// Alter table operations
    /// </summary>
    public abstract class AlterTableOperation : IWriteSql
    {
        /// <summary>
        /// Add table constraint operation
        /// <example>
        /// <c>
        /// ADD table_constraint
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="TableConstraint">Table Constraint</param>
        public class AddConstraint : AlterTableOperation, IElement
        {
            public AddConstraint(TableConstraint TableConstraint)
            {
                this.TableConstraint = TableConstraint;
            }

            public TableConstraint TableConstraint { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"ADD {TableConstraint}");
            }
        }
        /// <summary>
        ///  Add column operation
        /// <example>
        /// <c>
        /// ADD [COLUMN] [IF NOT EXISTS] column_def
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="ColumnKeyword">Contains column keyword</param>
        /// <param name="IfNotExists">Contains If Not Exists</param>
        /// <param name="ColumnDef">Column Definition</param>
        public class AddColumn : AlterTableOperation, IIfNotExists, IElement
        {
            public AddColumn(bool ColumnKeyword, bool IfNotExists, ColumnDef ColumnDef)
            {
                this.ColumnKeyword = ColumnKeyword;
                this.IfNotExists = IfNotExists;
                this.ColumnDef = ColumnDef;
            }
            public string IfNotExistsText => IfNotExists ? "IF NOT EXISTS" : null;
            public bool ColumnKeyword { get; set; }
            public bool IfNotExists { get; set; }
            public ColumnDef ColumnDef { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("ADD");

                if (ColumnKeyword)
                {
                    writer.Write(" COLUMN");
                }

                if (IfNotExists)
                {
                    writer.Write($" {IfNotExistsText}");
                }

                writer.WriteSql($" {ColumnDef}");
            }
        }
        /// <summary>
        /// Drop constraint table operation
        /// <example>
        /// <c>
        /// DROP CONSTRAINT [ IF EXISTS ] name
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name">Name identifier</param>
        /// <param name="IfExists">Contains If Exists</param>
        /// <param name="Cascade">Cascade</param>
        public class DropConstraint : AlterTableOperation
        {
            public DropConstraint(Ident Name, bool IfExists, bool Cascade)
            {
                this.Name = Name;
                this.IfExists = IfExists;
                this.Cascade = Cascade;
            }

            public Ident Name { get; set; }
            public bool IfExists { get; set; }
            public bool Cascade { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {

                var ifExists = IfExists ? "IF EXISTS " : null;
                var cascade = Cascade ? " CASCADE" : null;

                writer.WriteSql($"DROP CONSTRAINT {ifExists}{Name}{cascade}");
            }
        }
        /// <summary>
        /// Drop column table operation
        /// <example>
        /// <c>
        ///  DROP [ COLUMN ] [ IF EXISTS ] column_name [ CASCADE ]
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="IfExists"></param>
        /// <param name="Cascade"></param>
        public class DropColumn : AlterTableOperation
        {
            public DropColumn(Ident Name, bool IfExists, bool Cascade)
            {
                this.Name = Name;
                this.IfExists = IfExists;
                this.Cascade = Cascade;
            }

            public Ident Name { get; set; }
            public bool IfExists { get; set; }
            public bool Cascade { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {

                var ifExists = IfExists ? "IF EXISTS " : null;
                var cascade = Cascade ? " CASCADE" : null;

                writer.WriteSql($"DROP COLUMN {ifExists}{Name}{cascade}");
            }
        }
        /// <summary>
        /// Drop primary key table operation
        /// 
        /// Note: this is a MySQL-specific operation.
        /// <example>
        /// <c>
        /// DROP PRIMARY KEY
        /// </c>
        /// </example>
        /// </summary>
        public class DropPrimaryKey : AlterTableOperation
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("DROP PRIMARY KEY");
            }
        }
        /// <summary>
        /// Rename partitions table operation
        /// <example>
        /// <c>
        /// RENAME TO PARTITION (partition=val)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="OldPartitions">Old partitions</param>
        /// <param name="NewPartitions">New partitions</param>
        public class RenamePartitions : AlterTableOperation, IElement
        {
            public RenamePartitions(Sequence<SqlExpression> OldPartitions, Sequence<SqlExpression> NewPartitions)
            {
                this.OldPartitions = OldPartitions;
                this.NewPartitions = NewPartitions;
            }

            public Sequence<SqlExpression> OldPartitions { get; set; }
            public Sequence<SqlExpression> NewPartitions { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"PARTITION ({OldPartitions}) RENAME TO PARTITION ({NewPartitions})");
            }
        }
        /// <summary>
        /// Add partitions table operation
        /// <example>
        /// <c>
        /// ADD PARTITION
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="IfNotExists"></param>
        /// <param name="NewPartitions"></param>
        public class AddPartitions : AlterTableOperation, IIfNotExists, IElement
        {
            public AddPartitions(bool IfNotExists, Sequence<SqlExpression> NewPartitions)
            {
                this.IfNotExists = IfNotExists;
                this.NewPartitions = NewPartitions;
            }
            public string IfNotExistsText => IfNotExists ? "IF NOT EXISTS" : null;
            public bool IfNotExists { get; set; }
            public Sequence<SqlExpression> NewPartitions { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                var ifNot = IfNotExists ? $" {IfNotExistsText}" : null;
                writer.WriteSql($"ADD{ifNot} PARTITION ({NewPartitions})");
            }
        }
        /// <summary>
        /// Drop partitions table operation
        /// <example>
        /// <c>
        /// DROP PARTITION
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Partitions">Partitions sto drop</param>
        /// <param name="IfExists">Contains If Not Exists</param>
        public class DropPartitions : AlterTableOperation, IElement
        {
            public DropPartitions(Sequence<SqlExpression> Partitions, bool IfExists)
            {
                this.Partitions = Partitions;
                this.IfExists = IfExists;
            }

            public Sequence<SqlExpression> Partitions { get; set; }
            public bool IfExists { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                var ie = IfExists ? " IF EXISTS" : null;
                writer.WriteSql($"DROP{ie} PARTITION ({Partitions})");
            }
        }
        /// <summary>
        /// Rename column table operation
        /// <example>
        /// <c>
        ///  RENAME [ COLUMN ] old_column_name TO new_column_name
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="OldColumnName">Old column names</param>
        /// <param name="NewColumnName">New column names</param>
        public class RenameColumn : AlterTableOperation
        {
            public RenameColumn(Ident OldColumnName, Ident NewColumnName)
            {
                this.OldColumnName = OldColumnName;
                this.NewColumnName = NewColumnName;
            }

            public Ident OldColumnName { get; set; }
            public Ident NewColumnName { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"RENAME COLUMN {OldColumnName} TO {NewColumnName}");
            }
        }
        /// <summary>
        /// Rename table table operation
        /// <example>
        /// <c>
        /// RENAME TO table_name
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name">Table name</param>
        public class RenameTable : AlterTableOperation, IElement
        {
            public RenameTable(ObjectName Name)
            {
                this.Name = Name;
            }

            public ObjectName Name { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"RENAME TO {Name}");
            }
        }
        /// <summary>
        /// Change column  table operation
        /// <example>
        /// <c>
        /// CHANGE [ COLUMN ] old_name new_name data_type [ options ]
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="OldName">Old name</param>
        /// <param name="NewName">New name</param>
        /// <param name="DataType">Data type</param>
        /// <param name="Options">Rename options</param>
        public class ChangeColumn : AlterTableOperation, IElement
        {
            public ChangeColumn(Ident OldName, Ident NewName, DataType DataType, Sequence<ColumnOption> Options)
            {
                this.OldName = OldName;
                this.NewName = NewName;
                this.DataType = DataType;
                this.Options = Options;
            }

            public Ident OldName { get; set; }
            public Ident NewName { get; set; }
            public DataType DataType { get; set; }
            public Sequence<ColumnOption> Options { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"CHANGE COLUMN {OldName} {NewName} {DataType}");
                if (Options.Any())
                {
                    writer.WriteSql($" {Options.ToSqlDelimited(" ")}");
                }
            }
        }
        /// <summary>
        /// Rename Constraint table operation
        ///
        ///  Note: this is a PostgreSQL-specific operation.
        /// <example>
        /// <c>
        /// RENAME CONSTRAINT old_constraint_name TO new_constraint_name
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="OldName"></param>
        /// <param name="NewName"></param>
        public class RenameConstraint : AlterTableOperation
        {
            public RenameConstraint(Ident OldName, Ident NewName)
            {
                this.OldName = OldName;
                this.NewName = NewName;
            }

            public Ident OldName { get; set; }
            public Ident NewName { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"RENAME CONSTRAINT {OldName} TO {NewName}");
            }
        }
        /// <summary>
        /// Alter column table operation
        /// <example>
        /// <c>
        /// ALTER [ COLUMN ]
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="ColumnName">Column Name</param>
        /// <param name="Operation">Alter column operation</param>
        public class AlterColumn : AlterTableOperation, IElement
        {
            public AlterColumn(Ident ColumnName, AlterColumnOperation Operation)
            {
                this.ColumnName = ColumnName;
                this.Operation = Operation;
            }

            public Ident ColumnName { get; set; }
            public AlterColumnOperation Operation { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"ALTER COLUMN {ColumnName} {Operation}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        public class SwapWith : AlterTableOperation, IElement
        {
            public SwapWith(ObjectName Name)
            {
                this.Name = Name;
            }

            public ObjectName Name { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"SWAP WITH {Name}");
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}