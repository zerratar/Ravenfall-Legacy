using SqlParser.Tokens;

namespace SqlParser.Ast
{

    /// <summary>
    /// ColumnOptions are modifiers that follow a column definition in a CREATE TABLE statement.
    /// </summary>
    public abstract class ColumnOption : IWriteSql
    {
        /// <summary>
        /// Null column option
        /// <example>
        /// <c>NULL</c>
        /// </example>
        /// </summary>
        public class Null : ColumnOption
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("NULL");
            }
        }
        /// <summary>
        /// Not Null column option
        /// <example>
        /// <c>NOT NULL</c>
        /// </example>
        /// </summary>
        public class NotNull : ColumnOption
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("NOT NULL");
            }
        }
        /// <summary>
        /// Default column option
        /// <example>
        /// <c>{ PRIMARY KEY | UNIQUE }</c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class Default : ColumnOption, IElement
        {
            public Default(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"DEFAULT {Expression}");
            }
        }
        /// <summary>
        /// Unique column option
        /// <example>
        /// <c>{ PRIMARY KEY | UNIQUE }</c>
        /// </example>
        /// </summary>
        /// <param name="IsPrimary">True if primary</param>
        public class Unique : ColumnOption
        {
            public Unique(bool IsPrimary)
            {
                this.IsPrimary = IsPrimary;
            }

            public bool IsPrimary { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write(IsPrimary ? "PRIMARY KEY" : "UNIQUE");
            }
        }
        /// <summary>
        /// Referential integrity constraint column options
        /// <example>
        /// <c>
        /// ([FOREIGN KEY REFERENCES
        /// foreign_table (referred_columns)
        /// { [ON DELETE referential_action] [ON UPDATE referential_action] |
        ///   [ON UPDATE referential_action] [ON DELETE referential_action]
        /// })
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name">Name</param>
        /// <param name="ReferredColumns">Referred Columns</param>
        /// <param name="OnDeleteAction">On Delete Action</param>
        /// <param name="OnUpdateAction">On DoUpdate Action</param>
        public class ForeignKey : ColumnOption, IElement
        {
            public ForeignKey(ObjectName Name, Sequence<Ident> ReferredColumns = null,
                ReferentialAction OnDeleteAction = ReferentialAction.None, ReferentialAction OnUpdateAction = ReferentialAction.None)
            {
                this.Name = Name;
                this.ReferredColumns = ReferredColumns;
                this.OnDeleteAction = OnDeleteAction;
                this.OnUpdateAction = OnUpdateAction;
            }

            public ObjectName Name { get; }
            public Sequence<Ident> ReferredColumns { get; }
            public ReferentialAction OnDeleteAction { get; }
            public ReferentialAction OnUpdateAction { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"REFERENCES {Name}");
                if (ReferredColumns.SafeAny())
                {
                    writer.WriteSql($" ({ReferredColumns})");
                }

                if (OnDeleteAction != ReferentialAction.None)
                {
                    writer.WriteSql($" ON DELETE {OnDeleteAction}");
                }

                if (OnUpdateAction != ReferentialAction.None)
                {
                    writer.WriteSql($" ON UPDATE {OnUpdateAction}");
                }
            }
        }
        /// <summary>
        /// Check expression column options
        /// <example>
        /// <c>
        /// CHECK (expr)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class Check : ColumnOption, IElement
        {
            public Check(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"CHECK ({Expression})");
            }
        }
        /// <summary>
        ///  Dialect-specific options, such as:
        ///  MySQL's AUTO_INCREMENT or SQLite's `AUTOINCREMENT`
        /// </summary>
        /// <param name="Tokens">Tokens</param>
        public class DialectSpecific : ColumnOption
        {
            public DialectSpecific(Sequence<Token> Tokens)
            {
                this.Tokens = Tokens;
            }

            public Sequence<Token> Tokens { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                for (var i = 0; i < Tokens.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(" ");
                    }

                    writer.Write(Tokens[i]);
                }
            }
        }
        /// <summary>
        /// Character set options
        /// </summary>
        /// <param name="Name"></param>
        public class CharacterSet : ColumnOption, IElement
        {
            public CharacterSet(ObjectName Name)
            {
                this.Name = Name;
            }

            public ObjectName Name { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"CHARACTER SET {Name}");
            }
        }
        /// <summary>
        /// Comment column option
        /// </summary>
        /// <param name="Value">Comment value</param>
        public class Comment : ColumnOption
        {
            public Comment(string Value)
            {
                this.Value = Value;
            }

            public string Value { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write($"COMMENT '{Value.EscapeSingleQuoteString()}'");
            }
        }
        /// <summary>
        /// On Update column options
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class OnUpdate : ColumnOption, IElement
        {
            public OnUpdate(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"ON UPDATE {Expression}");
            }
        }
        /// <summary>
        /// Generated are modifiers that follow a column definition in a CREATE TABLE statement.
        /// </summary>
        /// <param name="GeneratedAs">Generated as</param>
        /// <param name="SequenceOptions">Sequence options</param>
        /// <param name="GenerationExpr">Generation expression</param>
        public class Generated : ColumnOption, IElement
        {
            public Generated(GeneratedAs GeneratedAs, Sequence<SequenceOptions> SequenceOptions = null, SqlExpression? GenerationExpr = null)
            {
                this.GeneratedAs = GeneratedAs;
                this.SequenceOptions = SequenceOptions;
                this.GenerationExpr = GenerationExpr;
            }

            public GeneratedAs GeneratedAs { get; }
            public Sequence<SequenceOptions> SequenceOptions { get; }
            public SqlExpression GenerationExpr { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                switch (GeneratedAs)
                {
                    case GeneratedAs.Always:
                        {
                            writer.Write("GENERATED ALWAYS AS IDENTITY");

                            if (SequenceOptions.SafeAny())
                            {
                                writer.Write(" (");

                                foreach (var option in SequenceOptions!)
                                {
                                    option.ToSql(writer);
                                }

                                writer.Write(" )");
                            }

                            break;
                        }
                    case GeneratedAs.ByDefault:
                        {
                            writer.Write("GENERATED BY DEFAULT AS IDENTITY");

                            if (SequenceOptions.SafeAny())
                            {
                                writer.Write(" (");

                                foreach (var option in SequenceOptions!)
                                {
                                    option.ToSql(writer);
                                }

                                writer.Write(" )");
                            }

                            break;
                        }
                    case GeneratedAs.ExpStored:
                        writer.WriteSql($"GENERATED ALWAYS AS ({GenerationExpr}) STORED");
                        break;
                }
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}