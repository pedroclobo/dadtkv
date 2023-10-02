namespace TransactionManager
{
    public class State
    {
        private Dictionary<string, int> _data;
        public State()
        {
            _data = new Dictionary<string, int>();
        }
        public void Add(string key, int value)
        {
            Console.WriteLine("Added {0} with value {1}", key, value);
            _data.Add(key, value);
        }
        public int Get(string key)
        {
            if (!_data.ContainsKey(key))
            {
                throw new Exception("Key not found: " + key);
            }
            Console.WriteLine("Got {0} with value {1}", key, _data[key]);
            return _data[key];
        }
    }
}
