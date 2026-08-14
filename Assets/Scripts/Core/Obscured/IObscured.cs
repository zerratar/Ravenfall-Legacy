namespace Obscured
{
    public interface IObscured
    {
        void RandomizeKey();
        void SetRaw(object value);
        object GetRaw();
    }
}
