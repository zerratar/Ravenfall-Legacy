namespace SqlParser.Ast
{

    public abstract class SqlExpression : IWriteSql, IElement
    {

        // ReSharper disable CommentTypo
        public interface INegated
        {
            bool Negated { get; set; }

            string NegatedText { get; }//=>Negated ? "NOT " : null;
        }

        //public string NegatedText => Negated ? "NOT " : null;

        /// <summary>
        /// Case-based expression and data type
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="DataType">Data type</param>
        public abstract class CastBase : SqlExpression
        {
            public CastBase(SqlExpression Expression, DataType DataType)
            {
                this.Expression = Expression;
                this.DataType = DataType;
            }

            public SqlExpression Expression { get; set; }
            public DataType DataType { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                var cast = "";
                if (this is Cast) cast = "CAST";
                if (this is SafeCast) cast = "SAFE_CAST";
                if (this is TryCast) cast = "TRY_CAST";
                writer.WriteSql($"{cast}({Expression} AS {DataType})");
            }
        }

        /// <summary>
        /// Aggregate function with filter
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Filter">Filter</param>
        public class AggregateExpressionWithFilter : SqlExpression
        {
            public AggregateExpressionWithFilter(SqlExpression Expression, SqlExpression Filter)
            {
                this.Expression = Expression;
                this.Filter = Filter;
            }

            public SqlExpression Expression { get; set; }
            public SqlExpression Filter { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} FILTER (WHERE {Filter})");
            }
        }
        /// <summary>
        /// ALL operation e.g. `1 ALL (1)` or `foo > ALL(bar)`, It will be wrapped in the right side of BinaryExpr
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class AllOp : SqlExpression
        {
            public AllOp(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"ALL({Expression})");
            }
        }
        /// <summary>
        /// Any operation e.g. `1 ANY (1)` or `foo > ANY(bar)`, It will be wrapped in the right side of BinaryExpr
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class AnyOp : SqlExpression
        {
            public AnyOp(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"ANY({Expression})");
            }
        }
        /// <summary>
        /// An array expression e.g. ARRAY[1, 2]
        /// </summary>
        /// <param name="Arr"></param>
        public class Array : SqlExpression
        {
            public Array(ArrayExpression Arr)
            {
                this.Arr = Arr;
            }

            public ArrayExpression Arr { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Arr}");
            }
        }
        /// <summary>
        /// ARRAY_AGG function
        /// <example>
        /// <c>
        /// SELECT ARRAY_AGG(... ORDER BY ...)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="ArrayAggregate">Array aggregation</param>
        public class ArrayAgg : SqlExpression
        {
            public ArrayAgg(ArrayAggregate ArrayAggregate)
            {
                this.ArrayAggregate = ArrayAggregate;
            }

            public ArrayAggregate ArrayAggregate { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{ArrayAggregate}");
            }
        }
        /// <summary>
        /// An array index expression
        /// <example>
        /// <c>
        /// (ARRAY[1, 2])[1] or (current_schemas(FALSE))[1]
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Obj"></param>
        /// <param name="Indexes"></param>
        public class ArrayIndex : SqlExpression
        {
            public ArrayIndex(SqlExpression Obj, Sequence<SqlExpression> Indexes)
            {
                this.Obj = Obj;
                this.Indexes = Indexes;
            }

            public SqlExpression Obj { get; set; }
            public Sequence<SqlExpression> Indexes { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Obj}");

                foreach (var index in Indexes)
                {
                    writer.WriteSql($"[{index}]");
                }
            }
        }
        /// <summary>
        /// An array subquery constructor
        /// <example>
        /// <c>
        /// SELECT ARRAY(SELECT 1 UNION SELECT 2)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Query">Subquery</param>
        public class ArraySubquery : SqlExpression
        {
            public ArraySubquery(Query Query)
            {
                this.Query = Query;
            }

            public Query Query { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"ARRAY({Query})");
            }
        }
        /// <summary>
        /// AT a timestamp to a different timezone
        /// <example>
        /// <c>
        /// FROM_UNIXTIME(0) AT TIME ZONE 'UTC-06:00'
        /// </c>
        /// </example>
        /// </summary>
        public class AtTimeZone : SqlExpression
        {
            public AtTimeZone(SqlExpression Timestamp, string? TimeZone)
            {
                this.Timestamp = Timestamp;
                this.TimeZone = TimeZone;
            }

            public SqlExpression Timestamp { get; set; }
            public string? TimeZone { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Timestamp} AT TIME ZONE '{TimeZone}'");
            }
        }
        /// <summary>
        /// Between expression
        /// <example>
        /// <c>
        /// Expression [ NOT ] BETWEEN low> AND high
        /// </c>
        /// </example>
        /// </summary>
        public class Between : SqlExpression, INegated
        {
            public Between(SqlExpression Expression, bool Negated, SqlExpression Low, SqlExpression High)
            {
                this.Expression = Expression;
                this.Negated = Negated;
                this.Low = Low;
                this.High = High;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public SqlExpression Expression { get; set; }
            public bool Negated { get; set; }
            public SqlExpression Low { get; set; }
            public SqlExpression High { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} {AsNegated.NegatedText}BETWEEN {Low} AND {High}");
            }
        }
        /// <summary>
        /// Binary operation
        /// <example>
        /// <c>
        /// 1 + 1 or foo > bar
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Left">Operation left hand expression</param>
        /// <param name="Op">Binary operator</param>
        /// <param name="Right">Operation right hand expression</param>
        public class BinaryOp : SqlExpression
        {
            public BinaryOp(SqlExpression Left, BinaryOperator Op, SqlExpression Right)
            {
                this.Left = Left;
                this.Op = Op;
                this.Right = Right;
            }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Op != BinaryOperator.PGCustomBinaryOperator)
                {
                    writer.WriteSql($"{Left} {Op} {Right}");
                }
                else
                {
                    writer.WriteSql($"{Left}");

                    if (PgOptions != null)
                    {
                        writer.Write(" OPERATOR(");

                        for (var i = 0; i < PgOptions?.Count; i++)
                        {
                            if (i > 0)
                            {
                                writer.Write(Symbols.Dot);
                            }

                            writer.Write(PgOptions[i]);
                        }
                        writer.Write(")");
                    }

                    writer.WriteSql($" {Right}");
                }
            }

            public Sequence<string?>? PgOptions { get; set; }
            public SqlExpression Left { get; set; }
            public BinaryOperator Op { get; set; }
            public SqlExpression Right { get; set; }
        }
        /// <summary>
        /// `CASE [operand] WHEN condition THEN result ... [ELSE result] END`
        ///
        /// Note we only recognize a complete single expression as `condition`,
        /// not ` 0` nor `1, 2, 3` as allowed in a `simple_when_clause` per
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2011-foundation-grammar.html#simple-when-clause"/>
        /// </summary>
        /// <param name="Results">Case results</param>
        public class Case : SqlExpression
        {
            public Case(Sequence<SqlExpression> Conditions, Sequence<SqlExpression> Results)
            {
                this.Conditions = Conditions;
                this.Results = Results;
            }
            public SqlExpression? Operand { get; set; }
            //public Sequence<Increment>? Conditions { get; set; }
            public SqlExpression? ElseResult { get; set; }
            public Sequence<SqlExpression> Conditions { get; set; }
            public Sequence<SqlExpression> Results { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("CASE");

                if (Operand != null)
                {
                    writer.WriteSql($" {Operand}");
                }

                if (Conditions.SafeAny())
                {
                    for (var i = 0; i < Conditions.Count; i++)
                    {
                        writer.WriteSql($" WHEN {Conditions[i]} THEN {Results[i]}");
                    }
                }

                if (ElseResult != null)
                {
                    writer.WriteSql($" ELSE {ElseResult}");
                }

                writer.Write(" END");
            }
        }
        /// <summary>
        /// CAST an expression to a different data type e.g. `CAST(foo AS VARCHAR(123))`
        /// </summary>
        public class Cast : CastBase        {
            public Cast(SqlExpression Expression, DataType DataType) : base(Expression, DataType)
            {
            }
        }

        /// <summary>
        /// CEIL(Expression [TO DateTimeField])
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Field">Date time field</param>
        public class Ceil : SqlExpression
        {
            public Ceil(SqlExpression Expression, DateTimeField Field)
            {
                this.Expression = Expression;
                this.Field = Field;
            }

            public SqlExpression Expression { get; set; }
            public DateTimeField Field { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Field == DateTimeField.NoDateTime)
                {
                    writer.WriteSql($"CEIL({Expression})");
                }
                else
                {
                    writer.WriteSql($"CEIL({Expression} TO {Field})");
                }
            }
        }
        /// <summary>
        /// Collate expression
        /// <example>
        /// <c>
        /// Expression COLLATE collation
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Collation">Collation</param>
        public class Collate : SqlExpression
        {
            public Collate(SqlExpression Expression, ObjectName Collation)
            {
                this.Expression = Expression;
                this.Collation = Collation;
            }

            public SqlExpression Expression { get; set; }
            public ObjectName Collation { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} COLLATE {Collation}");
            }
        }
        /// <summary>
        /// Multi-part identifier, e.g. 
        /// <example>
        /// <c>
        /// table_alias.column or schema.table.col
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Idents">Name identifiers</param>
        public class CompoundIdentifier : SqlExpression
        {
            public CompoundIdentifier(Sequence<Ident> Idents)
            {
                this.Idents = Idents;
            }

            public Sequence<Ident> Idents { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteDelimited(Idents, ".");
            }
        }
        /// <summary>
        /// Composite Access Postgres
        /// <example>
        /// <c>
        /// SELECT (information_schema._pg_expandarray(array['i','i'])).n
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Key">Key identifier</param>
        public class CompositeAccess : SqlExpression
        {
            public CompositeAccess(SqlExpression Expression, Ident Key)
            {
                this.Expression = Expression;
                this.Key = Key;
            }

            public SqlExpression Expression { get; set; }
            public Ident Key { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression}.{Key}");
            }
        }
        /// <summary>
        /// CUBE expresion.
        /// </summary>
        /// <param name="Sets">Sets</param>
        public class Cube : SqlExpression
        {
            public Cube(Sequence<Sequence<SqlExpression>> Sets)
            {
                this.Sets = Sets;
            }

            public Sequence<Sequence<SqlExpression>> Sets { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("CUBE (");

                for (var i = 0; i < Sets.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }

                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (Sets[i].Count == 1)
                    {
                        writer.WriteSql($"{Sets[i]}");
                    }
                    else
                    {
                        writer.WriteSql($"({Sets[i]})");
                    }
                }

                writer.Write(")");
            }
        }
        /// <summary>
        /// CAST an expression to a different data type
        /// <example>
        /// <c>
        /// CAST(foo AS VARCHAR(123))
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="SubQuery">Subquery</param>
        /// <param name="Negated">Exists negated</param>
        public class Exists : SqlExpression, INegated
        {
            public Exists(Query SubQuery, bool Negated = false)
            {
                this.SubQuery = SubQuery;
                this.Negated = Negated;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public Query SubQuery { get; set; }
            public bool Negated { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{AsNegated.NegatedText}EXISTS ({SubQuery})");
            }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// EXTRACT(DateTimeField FROM expr)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Field">Date time field</param>
        public class Extract : SqlExpression
        {
            public Extract(SqlExpression Expression, DateTimeField Field)
            {
                this.Expression = Expression;
                this.Field = Field;
            }

            public SqlExpression Expression { get; set; }
            public DateTimeField Field { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"EXTRACT({Field} FROM {Expression})");
            }
        }
        /// <summary>
        /// Floor expression
        /// <example>
        /// <c>
        /// FLOOR(Expression [TO DateTimeField])
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Field">Date time field</param>
        public class Floor : SqlExpression
        {
            public Floor(SqlExpression Expression, DateTimeField Field)
            {
                this.Expression = Expression;
                this.Field = Field;
            }

            public SqlExpression Expression { get; set; }
            public DateTimeField Field { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Field == DateTimeField.NoDateTime)
                {
                    writer.WriteSql($"FLOOR({Expression})");
                }
                else
                {
                    writer.WriteSql($"FLOOR({Expression} TO {Field})");
                }
            }
        }
        /// <summary>
        /// Scalar function call
        /// <example>
        /// <c>
        /// LEFT(foo, 5)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name">Function object name</param>
        public class Function : SqlExpression
        {
            public Function(ObjectName Name)
            {
                this.Name = Name;
            }
            /// <summary>
            /// Sequence function call
            /// </summary>
            public Sequence<FunctionArg>? Args { get; internal set; }
            /// <summary>
            /// Window spec
            /// </summary>
            public WindowSpec? Over { get; set; }
            /// <summary>
            /// Aggregate functions may specify eg `COUNT(DISTINCT x)`
            /// </summary>
            public bool Distinct { get; set; }
            /// <summary>
            /// Some functions must be called without trailing parentheses, for example Postgres\
            /// do it for current_catalog, current_schema, etc. This flags is used for formatting.
            /// </summary>
            public bool Special { get; set; }
            public ObjectName Name { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Special)
                {
                    Name.ToSql(writer);
                }
                else
                {
                    var distinct = Distinct ? "DISTINCT " : null;
                    writer.WriteSql($"{Name}({distinct}{Args})");

                    if (Over != null)
                    {
                        writer.WriteSql($" OVER ({Over})");
                    }
                }
            }
        }
        /// <summary>
        /// GROUPING SETS expression.
        /// </summary>
        /// <param name="Expressions">Sets</param>
        public class GroupingSets : SqlExpression
        {
            public GroupingSets(Sequence<Sequence<SqlExpression>> Expressions)
            {
                this.Expressions = Expressions;
            }

            public Sequence<Sequence<SqlExpression>> Expressions { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("GROUPING SETS (");

                for (var i = 0; i < Expressions.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }

                    writer.WriteSql($"({Expressions[i]})");
                }
                writer.Write(")");
            }
        }
        /// <summary>
        /// Identifier e.g. table name or column name
        /// </summary>
        /// <param name="Ident">Identifier name</param>
        public class Identifier : SqlExpression
        {
            public Identifier(Ident Ident)
            {
                this.Ident = Ident;
            }

            public Ident Ident { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                Ident.ToSql(writer);
            }
        }
        /// <summary>
        /// ILIKE (case-insensitive LIKE)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public class ILike : SqlExpression, INegated
        {
            public ILike(SqlExpression Expression, bool Negated, SqlExpression Pattern, char? EscapeChar = null)
            {
                this.Expression = Expression;
                this.Negated = Negated;
                this.Pattern = Pattern;
                this.EscapeChar = EscapeChar;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public SqlExpression Expression { get; set; }
            public bool Negated { get; set; }
            public SqlExpression Pattern { get; set; }
            public char? EscapeChar { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (EscapeChar != null)
                {
                    writer.WriteSql($"{Expression} {AsNegated.NegatedText}ILIKE {Pattern} ESCAPE '{EscapeChar}'");
                }
                else
                {
                    writer.WriteSql($"{Expression} {AsNegated.NegatedText}ILIKE {Pattern}");
                }
            }
        }
        /// <summary>
        /// In List expression
        /// <example>
        /// <c>
        /// [ NOT ] IN (val1, val2, ...)
        /// </c>
        /// </example>
        /// </summary>
        public class InList : SqlExpression, INegated
        {
            public InList(SqlExpression Expression, Sequence<SqlExpression> List, bool Negated)
            {
                this.Expression = Expression;
                this.List = List;
                this.Negated = Negated;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public SqlExpression Expression { get; set; }
            public Sequence<SqlExpression> List { get; set; }
            public bool Negated { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} {AsNegated.NegatedText}IN ({List})");
            }
        }
        /// <summary>
        /// In Subqery expression
        /// <example>
        /// <c>
        ///`[ NOT ] IN (SELECT ...)
        /// </c>
        /// </example>
        /// </summary>
        public class InSubquery : SqlExpression, INegated
        {
            public InSubquery(Query SubQuery, bool Negated, SqlExpression? Expression = null)
            {
                this.SubQuery = SubQuery;
                this.Negated = Negated;
                this.Expression = Expression;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public Query SubQuery { get; set; }
            public bool Negated { get; set; }
            public SqlExpression? Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} {AsNegated.NegatedText}IN ({SubQuery})");
            }
        }
        /// <summary>
        /// INTERVAL literals, roughly in the following format:
        ///
        /// INTERVAL 'value' [ leading_field [ (leading_precision) ] ]
        /// [ TO last_field [ (fractional_seconds_precision) ] ],
        ///
        /// The parser does not validate the `value`, nor does it ensure
        /// that the `leading_field` units >= the units in `last_field`,
        /// so the user will have to reject intervals like `HOUR TO YEAR`
        ///
        /// <example>
        /// <c>
        /// INTERVAL '123:45.67' MINUTE(3) TO SECOND(2)
        /// </c>
        /// </example>.
        /// </summary>
        /// <param name="Value">Value</param>
        /// <param name="LeadingField">Date time leading field</param>
        /// <param name="LastField">Date time last field</param>
        public class Interval : SqlExpression
        {
            public Interval(SqlExpression Value, DateTimeField LeadingField = DateTimeField.None, DateTimeField LastField = DateTimeField.None)
            {
                this.Value = Value;
                this.LeadingField = LeadingField;
                this.LastField = LastField;
            }
            public ulong? LeadingPrecision { get; set; }
            /// The seconds precision can be specified in SQL source as
            /// `INTERVAL '__' SECOND(_, x)` (in which case the `leading_field`
            /// will be `Second` and the `last_field` will be `None`),
            /// or as `__ TO SECOND(x)`.
            public ulong? FractionalSecondsPrecision { get; set; }
            public SqlExpression Value { get; set; }
            public DateTimeField LeadingField { get; set; }
            public DateTimeField LastField { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                // Length the leading field is SECOND, the parser guarantees that the last field is None.
                if (LeadingField == DateTimeField.Second && LeadingPrecision != null)
                {
                    writer.WriteSql($"INTERVAL {Value} SECOND ({LeadingPrecision}, {FractionalSecondsPrecision})");
                }
                else
                {
                    writer.WriteSql($"INTERVAL {Value}");

                    if (LeadingField != DateTimeField.None)
                    {
                        writer.WriteSql($" {LeadingField}");
                    }

                    if (LeadingPrecision != null)
                    {
                        writer.WriteSql($" ({LeadingPrecision})");
                    }

                    if (LastField != DateTimeField.None)
                    {
                        writer.WriteSql($" TO {LastField}");
                    }

                    if (FractionalSecondsPrecision != null)
                    {
                        writer.WriteSql($" ({FractionalSecondsPrecision})");
                    }
                }
            }
        }
        /// <summary>
        /// Introduced string
        /// 
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/charset-introducer.html"/>
        /// </summary>
        public class IntroducedString : SqlExpression
        {
            public IntroducedString(string Introducer, Value Value)
            {
                this.Introducer = Introducer;
                this.Value = Value;
            }

            public string Introducer { get; set; }
            public Value Value { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Introducer} {Value}");
            }
        }
        /// <summary>
        /// In Unnest expression
        /// <example>
        /// <c>
        /// [ NOT ] IN UNNEST(array_expression)
        /// </c>
        /// </example>
        /// </summary>
        public class InUnnest : SqlExpression, INegated
        {
            public InUnnest(SqlExpression Expression, SqlExpression ArrayExpression, bool Negated)
            {
                this.Expression = Expression;
                this.ArrayExpression = ArrayExpression;
                this.Negated = Negated;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public SqlExpression Expression { get; set; }
            public SqlExpression ArrayExpression { get; set; }
            public bool Negated { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} {AsNegated.NegatedText}IN UNNEST({ArrayExpression})");
            }
        }
        /// <summary>
        /// IS DISTINCT FROM operator
        /// </summary>
        /// <param name="Expression1">Expresison 1</param>
        /// <param name="Expression2">Expressoin 2</param>
        public class IsDistinctFrom : SqlExpression
        {
            public IsDistinctFrom(SqlExpression Expression1, SqlExpression Expression2)
            {
                this.Expression1 = Expression1;
                this.Expression2 = Expression2;
            }

            public SqlExpression Expression1 { get; set; }
            public SqlExpression Expression2 { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression1} IS DISTINCT FROM {Expression2}");
            }
        }
        /// <summary>
        /// IS FALSE operator
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class IsFalse : SqlExpression
        {
            public IsFalse(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS FALSE");
            }
        }
        /// <summary>
        /// IS NOT DISTINCT FROM operator
        /// </summary>
        /// <param name="Expression1">Expresison 1</param>
        /// <param name="Expression2">Expressoin 1</param>
        public class IsNotDistinctFrom : SqlExpression
        {
            public IsNotDistinctFrom(SqlExpression Expression1, SqlExpression Expression2)
            {
                this.Expression1 = Expression1;
                this.Expression2 = Expression2;
            }

            public SqlExpression Expression1 { get; set; }
            public SqlExpression Expression2 { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression1} IS NOT DISTINCT FROM {Expression2}");
            }
        }
        /// <summary>
        /// IS NOT FALSE operator
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class IsNotFalse : SqlExpression
        {
            public IsNotFalse(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS NOT FALSE");
            }
        }
        /// <summary>
        /// IS NOT NULL operator
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class IsNotNull : SqlExpression
        {
            public IsNotNull(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS NOT NULL");
            }
        }
        /// <summary>
        /// IS NOT UNKNOWN operator
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class IsNotUnknown : SqlExpression
        {
            public IsNotUnknown(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS NOT UNKNOWN");
            }
        }
        /// <summary>
        /// IS NULL operator
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class IsNull : SqlExpression
        {
            public IsNull(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS NULL");
            }
        }
        /// <summary>
        /// IS NOT TRUE operator
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class IsNotTrue : SqlExpression
        {
            public IsNotTrue(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS NOT TRUE");
            }
        }
        /// <summary>
        /// IS TRUE operator
        /// </summary>
        public class IsTrue : SqlExpression
        {
            public IsTrue(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS TRUE");
            }
        }
        /// <summary>
        /// IS UNKNOWN operator
        /// </summary>
        /// <param name="Expression">Expression</param>
        public class IsUnknown : SqlExpression
        {
            public IsUnknown(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"{Expression} IS UNKNOWN");
            }
        }
        /// <summary>
        /// JSON access (Postgres)  eg: data->'tags'
        /// </summary>
        /// <param name="Left">Left hand expression</param>
        /// <param name="Operator">Json Operator</param>
        /// <param name="Right">Right hand expression</param>
        public class JsonAccess : SqlExpression
        {
            public JsonAccess(SqlExpression Left, JsonOperator Operator, SqlExpression Right)
            {
                this.Left = Left;
                this.Operator = Operator;
                this.Right = Right;
            }

            public SqlExpression Left { get; set; }
            public JsonOperator Operator { get; set; }
            public SqlExpression Right { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Operator == JsonOperator.Colon)
                {
                    writer.WriteSql($"{Left}{Operator}{Right}");
                }
                else
                {
                    writer.WriteSql($"{Left} {Operator} {Right}");
                }
            }
        }
        /// <summary>
        /// LIKE expression
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Negated">Negated</param>
        /// <param name="Pattern">pattern expression</param>
        /// <param name="EscapeChar">Escape character</param>
        public class Like : SqlExpression, INegated
        {
            public Like(SqlExpression? Expression, bool Negated, SqlExpression Pattern, char? EscapeChar = null)
            {
                this.Expression = Expression;
                this.Negated = Negated;
                this.Pattern = Pattern;
                this.EscapeChar = EscapeChar;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public SqlExpression? Expression { get; set; }
            public bool Negated { get; set; }
            public SqlExpression Pattern { get; set; }
            public char? EscapeChar { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (EscapeChar != null)
                {
                    writer.WriteSql($"{Expression} {AsNegated.NegatedText}LIKE {Pattern} ESCAPE '{EscapeChar}'");
                }
                else
                {
                    writer.WriteSql($"{Expression} {AsNegated.NegatedText}LIKE {Pattern}");
                }
            }
        }
        /// <summary>
        /// LISTAGG function
        /// <example>
        /// <c>
        /// SELECT LISTAGG(...) WITHIN GROUP (ORDER BY ...)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="ListAggregate">List aggregate</param>
        public class ListAgg : SqlExpression
        {
            public ListAgg(ListAggregate ListAggregate)
            {
                this.ListAggregate = ListAggregate;
            }

            public ListAggregate ListAggregate { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                ListAggregate.ToSql(writer);
            }
        }
        /// <summary>
        /// Literal value e.g. '5'
        /// </summary>
        /// <param name="Value">Value</param>
        public class LiteralValue : SqlExpression
        {
            public LiteralValue(Value Value)
            {
                this.Value = Value;
            }

            public Value Value { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                Value.ToSql(writer);
            }
        }
        /// Access a map-like object by field
        /// 
        /// Note that depending on the dialect, struct like accesses may be
        /// parsed as [ArrayIndex](Self::ArrayIndex) or `MapAcces`](Self::MapAccess)
        ///
        /// <see href="https://clickhouse.com/docs/en/sql-reference/data-types/map/"/>
        ///
        /// <example>
        /// <c>
        /// column['field'] or column[4]
        /// </c>
        /// </example>
        public class MapAccess : SqlExpression
        {
            public MapAccess(SqlExpression Column, Sequence<SqlExpression> Keys)
            {
                this.Column = Column;
                this.Keys = Keys;
            }

            public SqlExpression Column { get; set; }
            public Sequence<SqlExpression> Keys { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                Column.ToSql(writer);

                foreach (var key in Keys)
                {
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (key is LiteralValue { Value: Value.SingleQuotedString s })
                    {
                        writer.WriteSql($"[\"{s.Value}\"]");
                    }
                    else
                    {
                        writer.WriteSql($"[{key}]");
                    }
                }
            }
        }
        /// <summary>
        /// MySQL specific text search function
        ///
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/fulltext-search.html#function_match"/>
        ///
        /// <example>
        /// <c>
        /// MARCH (col, col, ...) AGAINST (Expression [search modifier])
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Columns">Columns</param>
        /// <param name="MatchValue">Match Value</param>
        /// <param name="OptSearchModifier">Search Modifier</param>
        public class MatchAgainst : SqlExpression
        {
            public MatchAgainst(Sequence<Ident> Columns, Value MatchValue, SearchModifier OptSearchModifier)
            {
                this.Columns = Columns;
                this.MatchValue = MatchValue;
                this.OptSearchModifier = OptSearchModifier;
            }

            public Sequence<Ident> Columns { get; set; }
            public Value MatchValue { get; set; }
            public SearchModifier OptSearchModifier { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"MATCH ({Columns}) AGAINST ");

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (OptSearchModifier != SearchModifier.None)
                {
                    writer.WriteSql($"({MatchValue} {OptSearchModifier})");
                }
                else
                {
                    writer.WriteSql($"({MatchValue})");
                }
            }
        }
        /// <summary>
        /// Nested expression
        /// <example>
        /// <c>
        /// (foo > bar) or (1)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression"></param>
        public class Nested : SqlExpression
        {
            public Nested(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"({Expression})");
            }
        }
        /// <summary>
        /// Overlay expression
        /// <example>
        /// <c>
        /// OVERLAY(Expression PLACING Expression FROM expr[ FOR Expression ]
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="OverlayWhat">Overlay what expression</param>
        /// <param name="OverlayFrom">Overlay from expression</param>
        /// <param name="OverlayFor">Overlay for expression</param>
        public class Overlay : SqlExpression
        {
            public Overlay(SqlExpression Expression, SqlExpression OverlayWhat, SqlExpression OverlayFrom, SqlExpression? OverlayFor = null)
            {
                this.Expression = Expression;
                this.OverlayWhat = OverlayWhat;
                this.OverlayFrom = OverlayFrom;
                this.OverlayFor = OverlayFor;
            }

            public SqlExpression Expression { get; set; }
            public SqlExpression OverlayWhat { get; set; }
            public SqlExpression OverlayFrom { get; set; }
            public SqlExpression? OverlayFor { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"OVERLAY({Expression} PLACING {OverlayWhat} FROM {OverlayFrom}");

                if (OverlayFor != null)
                {
                    writer.WriteSql($" FOR {OverlayFor}");
                }

                writer.WriteSql($")");
            }
        }
        /// <summary>
        /// Position expression
        /// <example>
        /// <c>
        /// (Expression in expr)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="In">In expression</param>
        public class Position : SqlExpression
        {
            public Position(SqlExpression Expression, SqlExpression In)
            {
                this.Expression = Expression;
                this.In = In;
            }

            public SqlExpression Expression { get; set; }
            public SqlExpression In { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"POSITION({Expression} IN {In})");
            }
        }
        /// <summary>
        /// Rollup expression
        /// <example>
        /// <c>
        /// ROLLUP expr
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expressions">Sets</param>
        public class Rollup : SqlExpression
        {
            public Rollup(Sequence<Sequence<SqlExpression>> Expressions)
            {
                this.Expressions = Expressions;
            }

            public Sequence<Sequence<SqlExpression>> Expressions { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("ROLLUP (");

                for (var i = 0; i < Expressions.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }

                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (Expressions[i].Count == 1)
                    {
                        writer.WriteSql($"{Expressions[i]}");
                    }
                    else
                    {
                        writer.WriteSql($"({Expressions[i]})");
                    }
                }

                writer.Write(")");
            }
        }
        /// <summary>
        /// SAFE_CAST an expression to a different data type
        ///
        /// only available for BigQuery: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators#safe_casting"/>
        /// Works the same as `TRY_CAST`
        /// <example>
        /// <c>
        /// SAFE_CAST(foo AS FLOAT64)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="DataType"></param>
        public class SafeCast : CastBase
        {
            public SafeCast(SqlExpression Expression, DataType DataType) : base(Expression, DataType) { }

        }
        /// <summary>
        /// SimilarTo regex
        /// </summary>
        public class SimilarTo : SqlExpression, INegated
        {
            public SimilarTo(SqlExpression Expression, bool Negated, SqlExpression Pattern, char? EscapeChar = null)
            {
                this.Expression = Expression;
                this.Negated = Negated;
                this.Pattern = Pattern;
                this.EscapeChar = EscapeChar;
            }
            public string NegatedText => Negated ? "NOT " : null;
            public SqlExpression Expression { get; set; }
            public bool Negated { get; set; }
            public SqlExpression Pattern { get; set; }
            public char? EscapeChar { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (EscapeChar != null)
                {
                    writer.WriteSql($"{Expression} {AsNegated.NegatedText}SIMILAR TO {Pattern} ESCAPE '{EscapeChar}'");
                }
                else
                {
                    writer.WriteSql($"{Expression} {AsNegated.NegatedText}SIMILAR TO {Pattern}");
                }
            }
        }
        /// <summary>
        /// Substring expression
        /// <example>
        /// <c>
        /// SUBSTRING(Expression [FROM expr] [FOR expr])
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="SubstringFrom">From expression</param>
        /// <param name="SubstringFor">For expression</param>
        public class Substring : SqlExpression
        {
            public Substring(SqlExpression Expression, SqlExpression? SubstringFrom = null, SqlExpression? SubstringFor = null)
            {
                this.Expression = Expression;
                this.SubstringFrom = SubstringFrom;
                this.SubstringFor = SubstringFor;
            }

            public SqlExpression Expression { get; set; }
            public SqlExpression? SubstringFrom { get; set; }
            public SqlExpression? SubstringFor { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"SUBSTRING({Expression}");

                if (SubstringFrom != null)
                {
                    writer.WriteSql($" FROM {SubstringFrom}");
                }

                if (SubstringFor != null)
                {
                    writer.WriteSql($" FOR {SubstringFor}");
                }

                writer.WriteSql($")");
            }
        }
        /// <summary>
        /// A parenthesized subquery `(SELECT ...)`, used in expression like
        /// <example>
        /// <c>
        /// SELECT (subquery) AS x` or `WHERE (subquery) = x
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Query">Select</param>
        public class Subquery : SqlExpression
        {
            public Subquery(Query Query)
            {
                this.Query = Query;
            }

            public Query Query { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"({Query})");
            }
        }
        /// <summary>
        /// Trim expression
        /// <example>
        /// <c>
        /// TRIM([BOTH | LEADING | TRAILING] [expr FROM] expr)
        ///
        /// TRIM(expr)
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="TrimWhere">Trim where field</param>
        /// <param name="TrimWhat">What to trip expression</param>
        public class Trim : SqlExpression
        {
            public Trim(SqlExpression Expression, TrimWhereField TrimWhere, SqlExpression? TrimWhat = null)
            {
                this.Expression = Expression;
                this.TrimWhere = TrimWhere;
                this.TrimWhat = TrimWhat;
            }

            public SqlExpression Expression { get; set; }
            public TrimWhereField TrimWhere { get; set; }
            public SqlExpression? TrimWhat { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("TRIM(");

                if (TrimWhere != TrimWhereField.None)
                {
                    writer.WriteSql($"{TrimWhere} ");
                }

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (TrimWhat != null)
                {
                    writer.WriteSql($"{TrimWhat} FROM {Expression}");
                }
                else
                {
                    writer.WriteSql($"{Expression}");
                }

                writer.Write(")");
            }
        }
        /// <summary>
        /// TRY_CAST an expression to a different data type
        ///
        /// this differs from CAST in the choice of how to implement invalid conversions
        /// <example>
        /// <c>
        /// TRY_CAST(foo AS VARCHAR(123))
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="DataType">Cast data type</param>
        public class TryCast : CastBase
        {
            public TryCast(SqlExpression Expression, DataType DataType) : base(Expression, DataType) { }

        }
        /// <summary>
        /// ROW / TUPLE a single value, such as `SELECT (1, 2)`
        /// </summary>
        /// <param name="Expressions">Sets</param>
        public class Tuple : SqlExpression
        {
            public Tuple(Sequence<SqlExpression> Expressions)
            {
                this.Expressions = Expressions;
            }

            public Sequence<SqlExpression> Expressions { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"({Expressions})");
            }
        }
        /// <summary>
        /// A constant of form data_type 'value'.
        /// This can represent ANSI SQL DATE, TIME, and TIMESTAMP literals (such as DATE '2020-01-01'),
        /// as well as constants of other types (a non-standard PostgreSQL extension).
        /// </summary>
        /// <param name="Value">Value</param>
        /// <param name="DataType">Optional data type</param>
        public class TypedString : SqlExpression
        {
            public TypedString(string Value, DataType DataType)
            {
                this.Value = Value;
                this.DataType = DataType;
            }

            public string Value { get; set; }
            public DataType DataType { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                DataType.ToSql(writer);
                writer.Write($" '{Value.EscapeSingleQuoteString()}'");
            }
        }
        /// <summary>
        /// Unary operation e.g. `NOT foo`
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="Op"></param>
        public class UnaryOp : SqlExpression
        {
            public UnaryOp(SqlExpression Expression, UnaryOperator Op)
            {
                this.Expression = Expression;
                this.Op = Op;
            }

            public SqlExpression Expression { get; set; }
            public UnaryOperator Op { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Op == UnaryOperator.PGPostfixFactorial)
                {
                    writer.WriteSql($"{Expression}{Op}");
                }
                else if (Op == UnaryOperator.Not)
                {
                    writer.WriteSql($"{Op} {Expression}");
                }
                else
                {
                    writer.WriteSql($"{Op}{Expression}");
                }
            }
        }
        public virtual void ToSql(SqlTextWriter writer) { }

        internal INegated AsNegated => (INegated)this;

        public T As<T>() where T : SqlExpression
        {
            return (T)this;
        }

        public BinaryOp AsBinaryOp()
        {
            return As<BinaryOp>();
        }

        public UnaryOp AsUnaryOp()
        {
            return As<UnaryOp>();
        }

        public Identifier AsIdentifier()
        {
            return As<Identifier>();
        }

        public LiteralValue AsLiteral()
        {
            return As<LiteralValue>();
        }
    }
    // ReSharper restore CommentTypo
}