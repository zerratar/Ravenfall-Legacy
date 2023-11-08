namespace SqlParser.Ast
{	
	
	/// <summary>
	/// On conflict statement
	/// </summary>
	/// <param name="OnConflictAction">On conflict action</param>
	/// <param name="ConflictTarget">Conflict target</param>
	public class OnConflict : IWriteSql, IElement
	{
	    public OnConflictAction OnConflictAction { get; set; }
	    public ConflictTarget? ConflictTarget { get; set; }
	
	    public OnConflict(OnConflictAction OnConflictAction, ConflictTarget? ConflictTarget = null)
	    {
	        this.OnConflictAction = OnConflictAction;
	        this.ConflictTarget = ConflictTarget;
	    }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        writer.Write(" ON CONFLICT");
	        if (ConflictTarget != null)
	        {
	            ConflictTarget.ToSql(writer);
	        }
	        writer.WriteSql($" {OnConflictAction}");
	    }
	}
	
	/// <summary>
	/// On conflict action
	/// </summary>
	public abstract class OnConflictAction : IWriteSql
	{
	    /// <summary>
	    /// Do nothing on conflict
	    /// </summary>
	    public class DoNothing : OnConflictAction { }
	    /// <summary>
	    /// Update on conflict
	    /// </summary>
	    /// <param name="DoUpdateAction">Do update instruction</param>
	    public class DoUpdate : OnConflictAction, IElement
	    {
	        public DoUpdateAction DoUpdateAction { get; set; }
	
	        public DoUpdate(DoUpdateAction DoUpdateAction)
	        {
	            this.DoUpdateAction = DoUpdateAction;
	        }
	    }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        switch (this)
	        {
	            case DoNothing:
	                writer.Write("DO NOTHING");
	                break;
	
	            case DoUpdate d:
	                writer.Write("DO UPDATE");
	                if (d.DoUpdateAction.Assignments.SafeAny())
	                {
	                    writer.WriteSql($" SET {d.DoUpdateAction.Assignments}");
	                }
	
	                if (d.DoUpdateAction.Selection != null)
	                {
	                    writer.WriteSql($" WHERE {d.DoUpdateAction.Selection}");
	                }
	                break;
	        }
	    }
	}
}