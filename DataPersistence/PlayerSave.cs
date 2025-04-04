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
        public long Id;
        public string Name;
        // public List<RWeapon> allWeapons;
        // public List<RWeapon> currentWeapons;
        public List<RItem> Items;

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("PlayerSave: {\n");
            stringBuilder.Append($"\t\"Id\": {Id},\n");
            stringBuilder.Append($"\t\"Name\": {Name},\n");
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
            stringBuilder.Append($"\t\"Items\": [\n");
            foreach (var item in Items)
            {
                stringBuilder.Append($"\t\t{item},\n");
            }
            stringBuilder.Append($"\t]\n");
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }
    }
}