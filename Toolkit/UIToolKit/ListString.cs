using UnityEngine.UI;

namespace PowerCellStudio
{
    public class ListString : ListItem
    {
        public Text txt;
        
        public override void UpdateContent(int index, object data)
        {
            base.UpdateContent(index, data);
            if (data is string str)
            {
                txt.text = str;
            }
            else
            {
                txt.text = data?.ToString() ?? string.Empty;
            }
        }
    }
}