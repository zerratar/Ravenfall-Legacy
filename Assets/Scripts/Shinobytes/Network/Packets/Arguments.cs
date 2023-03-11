using System;

public class Arguments
{
    public Arguments(object[] values)
    {
        this.Values = values;
    }
    public object[] Values { get; }

    public object this[int index] => Values[index];
    public int Count => Values.Length;

    internal T GetArg<T>(int index)
    {
        var value = this[index];
        if (value != null && value is not T)
        {
            // is this a json obj?
        }
        return (T)this[index];
    }
}