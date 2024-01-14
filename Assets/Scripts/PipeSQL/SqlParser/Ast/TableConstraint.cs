namespace SqlParser.Ast
{

    /// <summary>
    /// A table-level constraint, specified in a CREATE TABLE or an
    /// ALTER TABLE ADD constraint statement.
    /// </summary>
    public abstract class TableConstraint : IWriteSql, IElement
    {
        /// <summary>
        /// Unique table constraint
        /// <example>
        /// <c>
        /// [ CONSTRAINT name ] { PRIMARY KEY | UNIQUE } (columns)
        /// </c>
        /// </example>
        /// </summary>
        public class Unique : TableConstraint
        {
            public Unique(Sequence<Ident> Columns)
            {
                this.Columns = Columns;
            }
            public Ident Name { get; set; }
            public bool IsPrimaryKey { get; set; }
            public Sequence<Ident> Columns { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                var primary = IsPrimaryKey ? "PRIMARY KEY" : "UNIQUE";
                writer.WriteConstraint(Name);
                writer.WriteSql($"{primary} ({Columns})");
            }
        }
        /// <summary>
        /// A referential integrity constraint
        /// <example>
        /// <c>
        /// REFERENCES foreign_table (referred_columns)
        /// { [ON DELETE referential_action] [ON UPDATE referential_action] |
        ///   [ON UPDATE referential_action] [ON DELETE referential_action]
        /// }`).
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="ForeignTable">Foreign table object name</param>
        /// <param name="Columns">Column identifiers</param>
        public class ForeignKey : TableConstraint
        {
            public ForeignKey(ObjectName ForeignTable, Sequence<Ident> Columns)
            {
                this.ForeignTable = ForeignTable;
                this.Columns = Columns;
            }
            public Ident Name { get; set; }
            public Sequence<Ident> ReferredColumns { get; set; }
            public ReferentialAction OnDelete { get; set; }
            public ReferentialAction OnUpdate { get; set; }
            public ObjectName ForeignTable { get; }
            public Sequence<Ident> Columns { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteConstraint(Name);
                writer.WriteSql($"FOREIGN KEY ({Columns}) REFERENCES {ForeignTable}({ReferredColumns})");

                if (OnDelete != ReferentialAction.None)
                {
                    writer.WriteSql($" ON DELETE {OnDelete}");
                }

                if (OnUpdate != ReferentialAction.None)
                {
                    writer.WriteSql($" ON UPDATE {OnUpdate}");
                }
            }
        }

        /// <summary>
        /// Check constraint
        /// <example>
        /// <c>
        /// [ CONSTRAINT name ] CHECK (expr)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name">Name identifier</param>
        /// <param name="Expression">Check expression</param>
        public class Check : TableConstraint
        {
            public Check(SqlExpression Expression, Ident Name)
            {
                this.Expression = Expression;
                this.Name = Name;
            }

            public SqlExpression Expression { get; }
            public Ident Name { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteConstraint(Name);
                writer.WriteSql($"CHECK ({Expression})");
            }
        }

        /// <summary>
        /// MySQLs [index definition][1] for index creation. Not present on ANSI so, for now, the usage
        /// is restricted to MySQL, as no other dialects that support this syntax were found.
        ///
        /// `{INDEX | KEY} [index_name] [index_type] (key_part,...) [index_option]...`
        ///
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/create-table.html"/>
        /// </summary>
        public class Index : TableConstraint
        {
            public Index(Sequence<Ident> Columns)
            {
                this.Columns = Columns;
            }
            public bool DisplayAsKey { get; set; }
            public Ident Name { get; set; }
            public IndexType IndexType { get; set; }
            public Sequence<Ident> Columns { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                var displayAs = DisplayAsKey ? "KEY" : "INDEX";
                writer.Write(displayAs);

                if (Name != null)
                {
                    writer.WriteSql($" {Name}");
                }

                if (IndexType != IndexType.None)
                {
                    writer.WriteSql($" USING {IndexType}");
                }

                writer.WriteSql($" ({Columns})");
            }
        }

        /// <summary>
        /// MySQLs [fulltext][1] definition. Since the [`SPATIAL`][2] definition is exactly the same,
        /// and MySQL displays both the same way, it is part of this definition as well.
        ///
        /// Supported syntax:
        ///
        /// {FULLTEXT | SPATIAL} [INDEX | KEY] [index_name] (key_part,...)
        ///
        /// key_part: col_name
        ///
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/fulltext-natural-language.html"/>
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/spatial-types.html"/>
        /// </summary>
        public class FulltextOrSpatial : TableConstraint
        {
            public FulltextOrSpatial(Sequence<Ident> Columns)
            {
                this.Columns = Columns;
            }
            public bool FullText { get; set; }
            public KeyOrIndexDisplay IndexTypeDisplay { get; set; }
            public Ident OptIndexName { get; set; }
            public Sequence<Ident> Columns { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write(FullText ? "FULLTEXT" : "SPATIAL");

                if (IndexTypeDisplay != KeyOrIndexDisplay.None)
                {
                    writer.WriteSql($" {IndexTypeDisplay}");
                }

                if (OptIndexName != null)
                {
                    writer.WriteSql($" {OptIndexName}");
                }

                writer.WriteSql($" ({Columns})");
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}