namespace PowerCellStudio
{
    public class AttributeString : AttributeValue<string>
    {
        public AttributeString(string initValue) : base(initValue)
        {
            // Init(initValue);
        }

        public static string ToString(AttributeString i)
        {
            return i.value;
        }
    }
}