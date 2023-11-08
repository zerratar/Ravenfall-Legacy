namespace SqlParser.Ast
{
	
	public class Join : IWriteSql, IElement
	{
	    public Join(TableFactor? Relation = null, JoinOperator? JoinOperator = null)
	    {
	        this.Relation = Relation;
	        this.JoinOperator = JoinOperator;
	    }
	
	    public TableFactor? Relation { get; set; }
	    public JoinOperator? JoinOperator { get; set; }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        string? Prefix(JoinConstraint constraint)
	        {
	            return constraint is JoinConstraint.Natural ? "NATURAL " : null;
	        }
	
	        string? Suffix(JoinConstraint constraint)
	        {
	            if (constraint is JoinConstraint.On on)
	            {
	                return $" ON {on.Expression.ToSql()}";
	            }
	
	            if (constraint is JoinConstraint.Using @using)
	            {
	                return $" USING({@using.Idents.ToSqlDelimited()})";
	            }
	            return null;
	        }
	
	
	
	        //switch (JoinOperator)
	        //{
	        //    case JoinOperator.CrossApply:
	        //        writer.WriteSql($" CROSS APPLY {Relation}");
	        //        return;
	        //    case JoinOperator.OuterApply:
	        //        writer.WriteSql($" OUTER APPLY {Relation}");
	        //        return;
	        //    case JoinOperator.CrossJoin:
	        //        writer.WriteSql($" CROSS JOIN {Relation}");
	        //        return;
	        //}
	
	        //string joinText = null!;
	        //JoinConstraint constraint = null!;
	        //switch (JoinOperator)
	        //{
	        //    case JoinOperator.Inner inner:
	        //        joinText = "JOIN";
	        //        constraint = inner.JoinConstraint;
	        //        break;
	        //    case JoinOperator.LeftOuter left:
	        //        joinText = "LEFT JOIN";
	        //        constraint = left.JoinConstraint;
	        //        break;
	        //    case JoinOperator.RightOuter right:
	        //        joinText = "RIGHT JOIN";
	        //        constraint = right.JoinConstraint;
	        //        break;
	        //    case JoinOperator.FullOuter full:
	        //        joinText = "FULL JOIN";
	        //        constraint = full.JoinConstraint;
	        //        break;
	        //    case JoinOperator.LeftSemi leftSemi:
	        //        joinText = "LEFT SEMI JOIN";
	        //        constraint = leftSemi.JoinConstraint;
	        //        break;
	        //    case JoinOperator.RightSemi rightSemi:
	        //        joinText = "RIGHT SEMI JOIN";
	        //        constraint = rightSemi.JoinConstraint;
	        //        break;
	        //    case JoinOperator.LeftAnti leftAnti:
	        //        joinText = "LEFT ANTI JOIN";
	        //        constraint = leftAnti.JoinConstraint;
	        //        break;
	        //    case JoinOperator.RightAnti rightAnti:
	        //        joinText = "RIGHT ANTI JOIN";
	        //        constraint = rightAnti.JoinConstraint;
	        //        break;
	        //}
	
	        //writer.WriteSql($" {Prefix(constraint)}{joinText} {Relation}{Suffix(constraint)}");
	    }
	}
	
	/// <summary>
	/// Join operator
	/// </summary>
	public abstract class JoinOperator : IElement
	{
	    public abstract class ConstrainedJoinOperator : JoinOperator
	    {
	        public ConstrainedJoinOperator(JoinConstraint JoinConstraint)
	        {
	            this.JoinConstraint = JoinConstraint;
	        }
	
	        public JoinConstraint JoinConstraint { get; set; }
	    }
	    /// <summary>
	    /// Inner join
	    /// </summary>
	    /// <param name="JoinConstraint">Join constraint</param>
	    public class Inner : ConstrainedJoinOperator
	    {
	        public Inner(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Left join
	         /// </summary>
	         /// <param name="JoinConstraint">Join constraint</param>
	    public class LeftOuter : ConstrainedJoinOperator
	    {
	        public LeftOuter(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Right outer join
	         /// </summary>
	         /// <param name="JoinConstraint">Join constraint</param>
	    public class RightOuter : ConstrainedJoinOperator
	    {
	        public RightOuter(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Full outer join
	         /// </summary>
	         /// <param name="JoinConstraint">Join constraint</param>
	    public class FullOuter : ConstrainedJoinOperator
	    {
	        public FullOuter(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Cross join
	         /// </summary>
	    public class CrossJoin : JoinOperator { }
	    /// <summary>
	    /// Left semi join
	    /// </summary>
	    /// <param name="JoinConstraint">Join constraint</param>
	    public class LeftSemi : ConstrainedJoinOperator
	    {
	        public LeftSemi(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Right semi join
	         /// </summary>
	         /// <param name="JoinConstraint">Join constraint</param>
	    public class RightSemi : ConstrainedJoinOperator
	    {
	        public RightSemi(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Left anti join
	         /// </summary>
	         /// <param name="JoinConstraint">Join constraint</param>
	    public class LeftAnti : ConstrainedJoinOperator
	    {
	        public LeftAnti(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Right anti join
	         /// </summary>
	         /// <param name="JoinConstraint">Join constraint</param>
	    public class RightAnti : ConstrainedJoinOperator
	    {
	        public RightAnti(JoinConstraint JoinConstraint) : base(JoinConstraint) { }
	    }    /// <summary>
	         /// Cross apply join
	         /// </summary>
	    public class CrossApply : JoinOperator { }
	    /// <summary>
	    /// 
	    /// </summary>
	    public class OuterApply : JoinOperator { }
	}
	
	/// <summary>
	/// Join constraint
	/// </summary>
	public abstract class JoinConstraint : IElement
	{
	    /// <summary>
	    /// On join constraint
	    /// </summary>
	    /// <param name="Expression">Constraint expression</param>
	    public class On : JoinConstraint
	    {
	        public On(SqlExpression Expression)
	        {
	            this.Expression = Expression;
	        }
	
	        public SqlExpression Expression { get; set; }
	    }    /// <summary>
	         /// Using join constraint
	         /// </summary>
	         /// <param name="Idents">Name identifiers</param>
	    public class Using : JoinConstraint
	    {
	        public Using(Sequence<Ident> Idents)
	        {
	            this.Idents = Idents;
	        }
	
	        public Sequence<Ident> Idents { get; set; }
	    }    /// <summary>
	         /// Natural join constraint
	         /// </summary>
	    public class Natural : JoinConstraint { }
	    /// <summary>
	    /// No join constraint
	    /// </summary>
	    public class None : JoinConstraint { }
	}
}