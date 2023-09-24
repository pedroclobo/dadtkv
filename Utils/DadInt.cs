namespace Utils;

public class DadInt
{
    public string Key { get; set; }
    public int Value { get; set; }

    public DadInt(string key, int value)
    {
        Key = key;
        Value = value;
    }
    public override string ToString()
    {
        return $"<{Key}, {Value}>";
    }
}