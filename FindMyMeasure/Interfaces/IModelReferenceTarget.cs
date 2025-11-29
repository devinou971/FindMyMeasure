namespace FindMyMeasure.Interfaces
{
    public interface IModelReferenceTarget
    {
        string Name { get; }
        string GetTargetType();
    }
}
