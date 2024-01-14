namespace SqlParser.Ast
{

    /// <summary>
    /// Function argument
    /// </summary>
    public abstract class FunctionArg : IWriteSql, IElement
    {
        /// <summary>
        /// Named function argument
        /// </summary>
        /// <param name="Name">Name identifier</param>
        /// <param name="Arg">Function argument expression</param>
        public class Named : FunctionArg
        {
            public Named(Ident Name, FunctionArgExpression Arg)
            {
                this.Name = Name;
                this.Arg = Arg;
            }

            public Ident Name { get; }
            public FunctionArgExpression Arg { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Name} => {Arg}");
            }
        }
        /// <summary>
        /// Unnamed function argument
        /// </summary>
        /// <param name="FunctionArgExpression">Function argument expression</param>
        public class Unnamed : FunctionArg
        {
            public Unnamed(FunctionArgExpression FunctionArgExpression)
            {
                this.FunctionArgExpression = FunctionArgExpression;
            }

            public FunctionArgExpression FunctionArgExpression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                FunctionArgExpression.ToSql(writer);
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }

    /// <summary>
    /// Function argument expression
    /// </summary>
    public abstract class FunctionArgExpression : IWriteSql, IElement
    {
        /// <summary>
        /// Function expression
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class FunctionExpression : FunctionArgExpression
        {
            public FunctionExpression(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                Expression.ToSql(writer);
            }
        }
        /// <summary>
        /// Qualified wildcard, e.g. `alias.*` or `schema.table.*`.
        /// </summary>
        public class QualifiedWildcard : FunctionArgExpression
        {
            public QualifiedWildcard(ObjectName Name)
            {
                this.Name = Name;
            }

            public ObjectName Name { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write($"{Name}.*");
            }
        }
        /// <summary>
        /// An unqualified `*`
        /// </summary>
        public class Wildcard : FunctionArgExpression
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("*");
            }
        }
        public abstract void ToSql(SqlTextWriter writer);
    }

    /// <summary>
    /// Function definition
    /// </summary>
    public abstract class FunctionDefinition : IWriteSql
    {
        /// <summary>
        /// Single quoted definition
        /// </summary>
        /// <param name="Value">String value</param>
        public class SingleQuotedDef : FunctionDefinition
        {
            public SingleQuotedDef(string Value)
            {
                this.Value = Value;
            }

            public string Value { get; }
        }
        /// <summary>
        /// Double quoted definition
        /// </summary>
        /// <param name="Value">String value</param>
        public class DoubleDollarDef : FunctionDefinition
        {
            public DoubleDollarDef(string Value)
            {
                this.Value = Value;
            }

            public string Value { get; }
        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case SingleQuotedDef s:
                    writer.WriteSql($"'{s.Value}'");
                    break;

                case DoubleDollarDef d:
                    writer.WriteSql($"$${d.Value}$$");
                    break;
            }
        }
    }

    /// <summary>
    /// Create function body
    /// </summary>
    public class CreateFunctionBody : IWriteSql, IElement
    {
        // LANGUAGE lang_name
        public Ident Language { get; internal set; }
        // IMMUTABLE | STABLE | VOLATILE
        public FunctionBehavior? Behavior { get; internal set; }
        // AS 'definition'
        // Note that Hive's `AS class_name` is also parsed here.
        public FunctionDefinition? As { get; internal set; }
        // RETURN expression
        public SqlExpression? Return { get; internal set; }
        // USING ... (Hive only)
        public CreateFunctionUsing? Using { get; internal set; }

        public void ToSql(SqlTextWriter writer)
        {
            if (Language != null)
            {
                writer.WriteSql($" LANGUAGE {Language}");
            }

            if (Behavior != null)
            {
                writer.WriteSql($" {Behavior}");
            }

            if (As != null)
            {
                writer.WriteSql($" AS {As}");
            }

            if (Return != null)
            {
                writer.WriteSql($" RETURN {Return}");
            }

            if (Using != null)
            {
                writer.WriteSql($" {Using}");
            }
        }
    }

    /// <summary>
    /// Create function using
    /// </summary>
    public abstract class CreateFunctionUsing : IWriteSql
    {
        public abstract class CreateFunctionUsingValue : CreateFunctionUsing
        {
            public CreateFunctionUsingValue(string Value)
            {
                this.Value = Value;
            }

            public string Value { get; }
        }
        /// <summary>
        /// None
        /// </summary>
        public class None : CreateFunctionUsing { }
        /// <summary>
        /// Create using Jar
        /// </summary>
        /// <param name="Value">String value</param>
        public class Jar : CreateFunctionUsingValue
        {
            public Jar(string Value) : base(Value) { }

        }
        /// <summary>
        /// Create using file
        /// </summary>
        /// <param name="Value">String value</param>
        public class File : CreateFunctionUsingValue
        {
            public File(string Value) : base(Value) { }

        }
        /// <summary>
        /// Create using archive
        /// </summary>
        /// <param name="Value">String value</param>
        public class Archive : CreateFunctionUsingValue
        {
            public Archive(string Value) : base(Value) { }

        }

        public void ToSql(SqlTextWriter writer)
        {
            writer.Write("USING ");

            switch (this)
            {
                case Jar j:
                    writer.Write($"JAR '{j.Value}'");
                    break;

                case File f:
                    writer.Write($"FILE '{f.Value}'");
                    break;

                case Archive a:
                    writer.Write($"ARCHIVE '{a.Value}'");
                    break;
            }
        }
    }

}