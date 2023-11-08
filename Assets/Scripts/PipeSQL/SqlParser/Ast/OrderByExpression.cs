namespace SqlParser.Ast
{
	
	/// <summary>
	/// Order by expression
	/// </summary>
	/// <param name="Expression">Expression</param>
	/// <param name="Asc">Ascending if true; descending if false</param>
	/// <param name="NullsFirst">Nulls first if true; Nulls last if false</param>
	public class OrderByExpression : IWriteSql, IElement
	{
	    public OrderByExpression(SqlExpression Expression, bool? Asc = null, bool? NullsFirst = null)
	    {
	        this.Expression = Expression;
	        this.Asc = Asc;
	        this.NullsFirst = NullsFirst;
	    }
	
	    public SqlExpression Expression { get; }
	    public bool? Asc { get; }
	    public bool? NullsFirst { get; }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        Expression.ToSql(writer);
	
	        if (Asc.HasValue)
	        {
	            writer.Write(Asc.Value ? " ASC" : " DESC");
	        }
	
	        if (NullsFirst.HasValue)
	        {
	            writer.Write(NullsFirst.Value ? " NULLS FIRST" : " NULLS LAST");
	        }
	    }
	}
}