using System;

namespace PowerCellStudio
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestButtonAttribute : Attribute
    {
        public string Text;

        public TestButtonAttribute() { }
        
        public TestButtonAttribute(string text)
        {
            Text = text;
        }
    }
}