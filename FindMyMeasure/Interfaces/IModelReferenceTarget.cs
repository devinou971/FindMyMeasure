namespace FindMyMeasure.Interfaces
{
    public interface IModelReferenceTarget
    {
        string Name { get; }
        string Type { get; }
        bool Equals(object obj);
    }
}
