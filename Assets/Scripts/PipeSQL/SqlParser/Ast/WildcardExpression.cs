namespace SqlParser.Ast
{
	
	/// <summary>
	/// Wildcard expressions
	/// </summary>
	public abstract class WildcardExpression : SqlExpression
	{
	    /// <summary>
	    /// Expression
	    /// </summary>
	    public class Expr : WildcardExpression, IElement
	    {
	        public SqlExpression Expression { get; set; }
	
	        public Expr(SqlExpression Expression)
	        {
	            this.Expression = Expression;
	        }
	    }
	
	    /// <summary>
	    /// Qualified expression
	    /// </summary>
	    public class QualifiedWildcard : WildcardExpression, IElement
	    {
	        public ObjectName Name { get; set; }
	
	        public QualifiedWildcard(ObjectName Name)
	        {
	            this.Name = Name;
	        }
	    }
	
	    /// <summary>
	    /// Wildcard expression
	    /// </summary>
	    public class Wildcard : WildcardExpression { }
	
	    public static implicit operator FunctionArgExpression(WildcardExpression expr)
	    {
	        return expr switch
	        {
	            Expr e => new FunctionArgExpression.FunctionExpression(e.Expression),
	            QualifiedWildcard q => new FunctionArgExpression.QualifiedWildcard(q.Name),
	            _ => new FunctionArgExpression.Wildcard()
	        };
	    }
	}
}