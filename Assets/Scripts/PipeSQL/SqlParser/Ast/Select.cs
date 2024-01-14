namespace SqlParser.Ast
{
	
	/// <summary>
	/// A restricted variant of SELECT (without CTEs/ORDER BY), which may
	/// appear either as the only body item of a Select, or as an operand
	/// to a set operation like UNION.
	/// </summary>
	/// <param name="Projection">Select projections</param>
	public class Select : IWriteSql, IElement
	{
	    public Select([Visit(1)] Sequence<SelectItem> Projection)
	    {
	        this.Projection = Projection;
	    }
	
	    public bool Distinct { get; set; }
	    [Visit(0)] public Top? Top { get; set; }
	    [Visit(2)] public SelectInto? Into { get; set; }
	    [Visit(3)] public Sequence<TableWithJoins> From { get; set; }
	    [Visit(4)] public Sequence<LateralView> LateralViews { get; set; }
	    [Visit(5)] public SqlExpression? Selection { get; set; }
	    [Visit(6)] public Sequence<SqlExpression> GroupBy { get; set; }
	    [Visit(7)] public Sequence<SqlExpression> ClusterBy { get; set; }
	    [Visit(8)] public Sequence<SqlExpression> DistributeBy { get; set; }
	    [Visit(9)] public Sequence<SqlExpression> SortBy { get; set; }
	    [Visit(10)] public SqlExpression? Having { get; set; }
	    [Visit(11)] public SqlExpression? QualifyBy { get; set; }
	    public Sequence<SelectItem> Projection { get; set; }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        var distinct = Distinct ? " DISTINCT" : null;
	        writer.Write($"SELECT{distinct}");
	
	        if (Top != null)
	        {
	            writer.WriteSql($" {Top}");
	        }
	
	        writer.WriteSql($" {Projection}");
	
	        if (Into != null)
	        {
	            writer.WriteSql($" {Into}");
	        }
	
	        if (From != null)
	        {
	            writer.WriteSql($" FROM {From}");
	        }
	
	        if (LateralViews.SafeAny())
	        {
	            foreach (var view in LateralViews!)
	            {
	                view.ToSql(writer);
	            }
	        }
	
	        if (Selection != null)
	        {
	            writer.WriteSql($" WHERE {Selection}");
	        }
	
	        if (GroupBy != null)
	        {
	            writer.WriteSql($" GROUP BY {GroupBy}");
	        }
	
	        if (ClusterBy != null)
	        {
	            writer.WriteSql($" CLUSTER BY {ClusterBy}");
	        }
	
	        if (DistributeBy != null)
	        {
	            writer.WriteSql($" DISTRIBUTE BY {DistributeBy}");
	        }
	
	        if (SortBy != null)
	        {
	            writer.WriteSql($" SORT BY {SortBy}");
	        }
	
	        if (Having != null)
	        {
	            writer.WriteSql($" HAVING {Having}");
	        }
	
	        if (QualifyBy != null)
	        {
	            writer.WriteSql($" QUALIFY {QualifyBy}");
	        }
	    }
	}
}