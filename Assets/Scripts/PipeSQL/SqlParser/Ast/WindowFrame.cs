namespace SqlParser.Ast
{

    /// <summary>
    /// Framed boundary
    /// </summary>
    /// <param name="Units">Window unit flag</param>
    /// <param name="StartBound">Boundary start</param>
    /// <param name="EndBound">Boundary end</param>
    public class WindowFrame : IElement
    {
        public WindowFrameUnit Units { get; set; }
        public WindowFrameBound? StartBound { get; set; }
        public WindowFrameBound? EndBound { get; set; }

        public WindowFrame(WindowFrameUnit Units, WindowFrameBound? StartBound, WindowFrameBound? EndBound)
        {
            this.Units = Units;
            this.StartBound = StartBound;
            this.EndBound = EndBound;
        }
    }

    public abstract class WindowFrameBound : IWriteSql, IElement
    {
        public class CurrentRow : WindowFrameBound
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("CURRENT ROW");
            }
        }
        public class Preceding : WindowFrameBound
        {
            public Preceding(SqlExpression? Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Expression == null)
                {
                    writer.Write("UNBOUNDED PRECEDING");
                }
                else
                {
                    writer.WriteSql($"{Expression} PRECEDING");
                }
            }
        }
        public class Following : WindowFrameBound
        {
            public Following(SqlExpression? Expression)
            {
                this.Expression = Expression;
            }

            public SqlExpression Expression { get; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Expression == null)
                {
                    writer.Write("UNBOUNDED FOLLOWING");
                }
                else
                {
                    writer.WriteSql($"{Expression} FOLLOWING");
                }
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
    public class WindowSpec : IWriteSql, IElement
    {
        public Sequence<SqlExpression> PartitionBy { get; set; }
        public Sequence<OrderByExpression> OrderBy { get; set; }
        public WindowFrame? WindowFrame { get; set; }

        public WindowSpec(Sequence<SqlExpression>? PartitionBy = null, Sequence<OrderByExpression> OrderBy = null, WindowFrame? WindowFrame = null)
        {
            this.PartitionBy = PartitionBy;
            this.OrderBy = OrderBy;
            this.WindowFrame = WindowFrame;
        }

        public void ToSql(SqlTextWriter writer)
        {
            var delimiter = string.Empty;
            if (PartitionBy?.SafeAny() == true)
            {
                delimiter = " ";
                writer.WriteSql($"PARTITION BY {PartitionBy}");
            }

            if (OrderBy != null)
            {
                writer.Write(delimiter);
                delimiter = " ";
                writer.WriteSql($"ORDER BY {OrderBy}");
            }

            if (WindowFrame != null)
            {
                writer.Write(delimiter);
                if (WindowFrame.EndBound != null)
                {
                    writer.WriteSql($"{WindowFrame.Units} BETWEEN {WindowFrame.StartBound} AND {WindowFrame.EndBound}");
                }
                else
                {
                    writer.WriteSql($"{WindowFrame.Units} {WindowFrame.StartBound}");
                }
            }
        }
    }
}