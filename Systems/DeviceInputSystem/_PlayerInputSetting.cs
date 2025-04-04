// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// namespace PowerCellStudio
// {
//     [Serializable]
//     public class PlayerInputSetting : IPersistenceData
//     {
//         [SerializeField]
//         public bool isDefault;
//         [SerializeField]
//         public string deviceName;
//         [SerializeField]
//         public List<PlayerInputSettingData> playerInputSettingDatas = new List<PlayerInputSettingData>();
//     }
//     
//     [Serializable]
//     public class PlayerInputSettingData : IPersistenceData
//     {
//         public EPlayerInput playerInput;
//         public string Name;
//     }
// }