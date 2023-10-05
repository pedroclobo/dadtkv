namespace Utils;

public class DadInteger
{
    public string Key { get; set; }
    public int Value { get; set; }

    public DadInteger(string key, int value)
    {
        Key = key;
        Value = value;
    }

    public static DadInteger Parse(string s)
    {
        string[] kv = s.TrimStart('<').TrimEnd('>').Split(',');

        return new DadInteger(kv[0].Trim('"'), int.Parse(kv[1]));
    }
    public static DadInteger FromProtobuf(DadInt dadInt)
    {
        return new DadInteger(dadInt.Key, dadInt.Value);
    }

    public DadInt ToProtobuf()
    {
        return new DadInt
        {
            Key = Key,
            Value = Value
        };
    }
    public override string ToString()
    {
        return $"<{Key}, {Value}>";
    }
}