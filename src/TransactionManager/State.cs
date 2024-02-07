namespace TransactionManager
{
    public class State
    {
        private Dictionary<string, int> _data;
        public State()
        {
            _data = new Dictionary<string, int>();
        }
        public void Set(string key, int value)
        {
            Console.WriteLine("Setting key {0} with value {1}", key, value);
            if (!_data.ContainsKey(key))
            {
                _data.Add(key, value);
            }
            else
            {
                _data[key] = value;
            }
        }
        public int Get(string key)
        {
            if (!_data.ContainsKey(key))
            {
                throw new Exception("Key not found: " + key);
            }
            Console.WriteLine("Retrieved key {0} with value {1}", key, _data[key]);
            return _data[key];
        }

        public List<DadInt> ToDadInt()
        {
            List<DadInt> pairs = new List<DadInt>();

            foreach (var pair in _data)
            {
                pairs.Add(new DadInt
                {
                    Key = pair.Key,
                    Value = pair.Value,
                });
            }

            return pairs;
        }
    }
}
