namespace SqlParser.Ast
{

    /// <summary>
    /// Can use to describe options in create sequence or table column type identity
    /// <example>
    /// <c>
    /// [ INCREMENT [ BY ] increment ]
    /// [ MINVALUE minvalue | NO MINVALUE ] [ MAXVALUE maxvalue | NO MAXVALUE ]
    /// [ START [ WITH ] start ] [ CACHE cache ] [ [ NO ] CYCLE ]
    /// </c>
    /// </example>
    /// </summary>
    public abstract class SequenceOptions : IWriteSql, IElement
    {
        /// <summary>
        /// By by sequence
        /// </summary>
        /// <param name="Increment">Expression</param>
        /// <param name="By">True to increment</param>
        public class IncrementBy : SequenceOptions
        {
            public IncrementBy(SqlExpression Increment, bool By)
            {
                this.Increment = Increment;
                this.By = By;
            }

            public SqlExpression Increment { get; }
            public bool By { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                var by = By ? " BY" : null;

                writer.WriteSql($" INCREMENT{by} {Increment}");
            }
        }

        /// <summary>
        /// Min value sequence
        /// </summary>
        /// <param name="Value">Min value</param>
        public class MinValue : SequenceOptions
        {
            public MinValue(MinMaxValue Value)
            {
                this.Value = Value;
            }

            public MinMaxValue Value { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Value is MinMaxValue.Some)
                {
                    writer.WriteSql($" MINVALUE {Value}");
                }
                else if (Value is MinMaxValue.None)
                {
                    writer.Write(" NO MINVALUE");
                }
            }
        }
        /// <summary>
        /// Max value sequence
        /// </summary>
        /// <param name="Value">Max value</param>
        public class MaxValue : SequenceOptions
        {
            public MaxValue(MinMaxValue Value)
            {
                this.Value = Value;
            }

            public MinMaxValue Value { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Value is MinMaxValue.Some)
                {
                    writer.WriteSql($" MAXVALUE {Value}");
                }
                else if (Value is MinMaxValue.None)
                {
                    writer.Write(" NO MAXVALUE");
                }
            }
        }
        /// <summary>
        /// Starts with sequence
        /// </summary>
        /// <param name="Expression">Expression</param>
        /// <param name="With">True if start</param>
        public class StartWith : SequenceOptions
        {
            public StartWith(SqlExpression Expression, bool With)
            {
                this.Expression = Expression;
                this.With = With;
            }

            public SqlExpression Expression { get; }
            public bool With { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                var with = With ? " WITH" : null;
                writer.WriteSql($" START{with} {Expression}");
            }
        }
        /// <summary>
        /// Cache sequence
        /// </summary>
        /// <param name="Expression">Cycle expression</param>
        public class Cache : SequenceOptions
        {
            public Cache(SqlExpression Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($" CACHE {Expression}");
            }
        }
        /// <summary>
        /// Cycle sequence
        /// </summary>
        /// <param name="ShouldCycle">True if cycling</param>
        public class Cycle : SequenceOptions
        {
            public Cycle(bool ShouldCycle)
            {
                this.ShouldCycle = ShouldCycle;
            }

            public bool ShouldCycle { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                var cycle = ShouldCycle ? "NO " : null;
                writer.WriteSql($" {cycle}CYCLE");
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}