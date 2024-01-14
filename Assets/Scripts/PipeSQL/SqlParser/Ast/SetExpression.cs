namespace SqlParser.Ast
{
	
	/// <summary>
	/// A node in a tree, representing a "query body" expression, roughly:
	/// SELECT ... [ {UNION|EXCEPT|INTERSECT} SELECT ...]
	/// </summary>
	public abstract class SetExpression : IWriteSql, IElement
	{
	    /// <summary>
	    /// Insert query bdy
	    /// </summary>
	    /// <param name="Statement">Statement</param>
	    public class Insert : SetExpression
	    {
	        public Insert(Statement Statement)
	        {
	            this.Statement = Statement;
	        }
	
	        public Statement Statement { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            Statement.ToSql(writer);
	        }
	    }
	    /// <summary>
	    /// Select expression body
	    /// </summary>
	    /// <param name="Query"></param>
	    public class QueryExpression : SetExpression
	    {
	        public QueryExpression(Query Query)
	        {
	            this.Query = Query;
	        }
	
	        public Query Query { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"({Query})");
	        }
	    }
	
	    //public class QueryExpression([Visit(1)] SetExpression Body) : IWriteSql, IElement
	    //{
	    //    [Visit(0)] public With? With { get; set; }
	    //    [Visit(2)] public Sequence<OrderByExpression> OrderBy { get; set; }
	    //    [Visit(3)] public Expression? Limit { get; set; }
	    //    [Visit(4)] public Offset? Offset { get; set; }
	    //    [Visit(5)] public Fetch? Fetch { get; set; }
	    //    [Visit(6)] public Sequence<LockClause> Locks { get; set; }
	
	    //    //public static implicit operator Query(Statement.Select select)
	    //    //{
	    //    //    return select.Query;
	    //    //}
	
	    //    //public static implicit operator Statement.Select(Query query)
	    //    //{
	    //    //    return new Statement.Select(query);
	    //    //}
	
	    //    public void ToSql(SqlTextWriter writer)
	    //    {
	    //        if (With != null)
	    //        {
	    //            writer.WriteSql($"{With} ");
	    //        }
	
	    //        Body.ToSql(writer);
	
	    //        if (OrderBy != null)
	    //        {
	    //            writer.WriteSql($" ORDER BY {OrderBy}");
	    //        }
	
	    //        if (Limit != null)
	    //        {
	    //            writer.WriteSql($" LIMIT {Limit}");
	    //        }
	
	    //        if (Offset != null)
	    //        {
	    //            writer.WriteSql($" {Offset}");
	    //        }
	
	    //        if (Fetch != null)
	    //        {
	    //            writer.WriteSql($" {Fetch}");
	    //        }
	
	    //        if (Locks != null && Locks.Any())
	    //        {
	    //            writer.WriteSql($" {Locks.ToSqlDelimited(Symbols.Space.ToString())}");
	    //        }
	    //    }
	    //}
	
	    /// <summary>
	    /// Select expression body
	    /// </summary>
	    /// <param name="Select"></param>
	    public class SelectExpression : SetExpression
	    {
	        public SelectExpression(Select Select)
	        {
	            this.Select = Select;
	        }
	
	        public Select Select { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            Select.ToSql(writer);
	        }
	    }
	    /// <summary>
	    /// Set operation body
	    /// </summary>
	    /// <param name="Left">Left hand expression</param>
	    /// <param name="Op">Set operator</param>
	    /// <param name="Right">Right hand expression</param>
	    /// <param name="SetQuantifier">Set quantifier</param>
	    public class SetOperation : SetExpression
	    {
	        public SetOperation(SetExpression Left, SetOperator Op, SetExpression Right, SetQuantifier SetQuantifier)
	        {
	            this.Left = Left;
	            this.Op = Op;
	            this.Right = Right;
	            this.SetQuantifier = SetQuantifier;
	        }
	
	        public SetExpression Left { get; }
	        public SetOperator Op { get; }
	        public SetExpression Right { get; }
	        public SetQuantifier SetQuantifier { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"{Left} {Op}");
	
	            if (SetQuantifier != SetQuantifier.None)
	            {
	                writer.WriteSql($" {SetQuantifier}");
	            }
	
	            writer.WriteSql($" {Right}");
	        }
	    }
	    /// <summary>
	    /// Table expression
	    /// </summary>
	    /// <param name="Table">Table</param>
	    public class TableExpression : SetExpression
	    {
	        public TableExpression(Table Table)
	        {
	            this.Table = Table;
	        }
	
	        public Table Table { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            Table.ToSql(writer);
	        }
	    }
	    /// <summary>
	    /// Values expression
	    /// </summary>
	    /// <param name="Values">Values</param>
	    public class ValuesExpression : SetExpression
	    {
	        public ValuesExpression(Values Values)
	        {
	            this.Values = Values;
	        }
	
	        public Values Values { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            Values.ToSql(writer);
	        }
	    }
	
	    public abstract void ToSql(SqlTextWriter writer);
	
	    public T As<T>() where T : SetExpression
	    {
	        return (T)this;
	    }
	
	    public SelectExpression AsSelectExpression()
	    {
	        return As<SelectExpression>();
	    }
	
	    public Ast.Select AsSelect()
	    {
	        return AsSelectExpression().Select;
	    }
	}
}