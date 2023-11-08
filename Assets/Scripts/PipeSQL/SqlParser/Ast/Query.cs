using System.Linq;

namespace SqlParser.Ast
{
	
	/// <summary>
	/// The most complete variant of a SELECT select expression, optionally
	/// including WITH, UNION / other set operations, and ORDER BY.
	/// </summary>
	public class Query : IWriteSql, IElement
	{
	    public Query([Visit(1)] SetExpression Body)
	    {
	        this.Body = Body;
	    }
	    [Visit(0)] public With? With { get; set; }
	    [Visit(2)] public Sequence<OrderByExpression>? OrderBy { get; set; }
	    [Visit(3)] public SqlExpression? Limit { get; set; }
	    [Visit(4)] public Offset? Offset { get; set; }
	    [Visit(5)] public Fetch? Fetch { get; set; }
	    [Visit(6)] public Sequence<LockClause>? Locks { get; set; }
	    public SetExpression Body { get; set; }
	
	    public static implicit operator Query(Statement.Select select)
	    {
	        return select.Query;
	    }
	
	    public static implicit operator Statement.Select(Query query)
	    {
	        return new Statement.Select(query);
	    }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        if (With != null)
	        {
	            writer.WriteSql($"{With} ");
	        }
	
	        Body.ToSql(writer);
	
	        if (OrderBy != null)
	        {
	            writer.WriteSql($" ORDER BY {OrderBy}");
	        }
	
	        if (Limit != null)
	        {
	            writer.WriteSql($" LIMIT {Limit}");
	        }
	
	        if (Offset != null)
	        {
	            writer.WriteSql($" {Offset}");
	        }
	
	        if (Fetch != null)
	        {
	            writer.WriteSql($" {Fetch}");
	        }
	
	        if (Locks != null && Locks.Any())
	        {
	            writer.WriteSql($" {Locks.ToSqlDelimited(Symbols.Space.ToString())}");
	        }
	    }
	}
}