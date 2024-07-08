namespace EAVFW.Extensions.Manifest.SDK
{
    public struct StringOrBoolean
    {
        public StringOrBoolean(string stringValue)
        {
            StringValue = stringValue;
            IsBool = false;
            BooleanValue = default;
        }

        public StringOrBoolean(bool booleanValue)
        {
            StringValue = default;
            IsBool = false;
            BooleanValue = booleanValue;
        }

        public string StringValue { get; }

        public bool BooleanValue { get; }

        public bool IsBool { get; }

        public override string ToString()
        {
            return IsBool ? BooleanValue.ToString() : StringValue;
        }
    }
}
