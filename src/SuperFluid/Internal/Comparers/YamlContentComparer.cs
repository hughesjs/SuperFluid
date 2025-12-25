namespace SuperFluid.Internal.Comparers;

internal class YamlContentComparer : IEqualityComparer<(string Name, string Content)>
{
    public bool Equals((string Name, string Content) x, (string Name, string Content) y)
    {
        return x.Name == y.Name && x.Content == y.Content;
    }

    public int GetHashCode((string Name, string Content) obj)
    {
        return HashCode.Combine(obj.Name, obj.Content);
    }
}
