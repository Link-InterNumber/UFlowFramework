using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace PowerCellStudio
{
    [Serializable]
    public partial class PlayerSave: IPersistenceData
    {
        public long slotIndex;
        public string playerName;
        public long createTime;
        public List<RItem> items;

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("PlayerSave: {\n");
            stringBuilder.Append($"\t\"slotIndex\": {slotIndex},\n");
            stringBuilder.Append($"\t\"playerName\": {playerName},\n");
            // stringBuilder.Append($"\t\"Weapons\": [\n");
            // foreach (var weapon in allWeapons)
            // {
            //     stringBuilder.Append($"\t\t{weapon},\n");
            // }
            // stringBuilder.Append($"\t],\n");
            // stringBuilder.Append($"\t\"CurrentWeapons\": [\n");
            // foreach (var weapon in currentWeapons)
            // {
            //     stringBuilder.Append($"\t\t{weapon},\n");
            // }
            // stringBuilder.Append($"\t],\n");
            stringBuilder.Append($"\t\"items\": [\n");
            foreach (var item in items)
            {
                stringBuilder.Append($"\t\t{item},\n");
            }
            stringBuilder.Append($"\t]\n");
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }
    }
}