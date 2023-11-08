namespace SqlParser.Ast
{
	
	/// <summary>
	/// Do Update action
	/// </summary>
	/// <param name="Assignments">Update assignments</param>
	/// <param name="Selection">Selection expression</param>
	public class DoUpdateAction : IElement
	{
	    public Sequence<Statement.Assignment> Assignments { get; set; }
	    public SqlExpression? Selection { get; set; }
	
	    public DoUpdateAction(Sequence<Statement.Assignment> Assignments, SqlExpression? Selection = null)
	    {
	        this.Assignments = Assignments;
	        this.Selection = Selection;
	    }
	}
	
}