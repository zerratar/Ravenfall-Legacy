namespace SqlParser.Tokens
{
	
	/// <summary>
	/// string concatenation `||`
	/// </summary>
	public class StringConcat : StringToken
	{
	    public StringConcat() : base("||")
	    {
	    }
	}
}