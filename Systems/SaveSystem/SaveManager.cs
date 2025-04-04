using System;
using System.Collections.Generic;
using System.Linq;
using PowerCellStudio;
using UnityEngine;

namespace FrameWork.Systems.SaveSystem
{
    public class SaveManager : SingletonBase<SaveManager>
    {
        [Serializable]
        public class PlayerSave : IPersistenceData
        {
            [SerializeField] public long createTime;
            [SerializeField] public long startTime;
            [SerializeField] public string playerName;
            [SerializeField] public int slotIndex;
            [SerializeField] public string testLog;

        }
        
        [Serializable]
        public class PlayerSaveCollection : IPersistenceData
        {
            [SerializeField] public List<PlayerSave> playerSaves;
        }

        public enum SaveSlot
        {
            Auto = 0,
            Slot1,
            Slot2,
            Slot3,
            Slot4,
        }

        private PlayerSave _currentPlayer;

        public int currentId => _currentPlayer.slotIndex;

        public PlayerSave LoadSave(SaveSlot slot)
        {
            var saves = PlayerDataUtils.ReadBinary<PlayerSaveCollection>();
            if (saves == null || saves.playerSaves == null || saves.playerSaves.Count == 0)
            {
                return null;
            }
            _currentPlayer = saves.playerSaves.FirstOrDefault(o => o.slotIndex == (int) slot);
            if (_currentPlayer == null) return null;
            
            return _currentPlayer;
        }

        public PlayerSave CreatePlayerSave(SaveSlot slot, string name)
        {
            var timeTick = DateTime.Now.Ticks;
            var save = new PlayerSave()
            {
                createTime = timeTick,
                startTime = timeTick,
                playerName = name,
                slotIndex = (int) slot,
            };
            var saves = PlayerDataUtils.ReadBinary<PlayerSaveCollection>();
            if (saves == null || saves.playerSaves == null)
            {
                saves = new PlayerSaveCollection();
                saves.playerSaves = new List<PlayerSave>();
            }
            saves.playerSaves.RemoveAll(o => o.slotIndex == (int) slot);
            saves.playerSaves.Add(save);
            PlayerDataUtils.SaveDataBinaryAsync<PlayerSaveCollection>(saves, null);
            return save;
        }

        public void SavePlayer(PlayerSave save)
        {
            if(save == null) return;
            
            var saves = PlayerDataUtils.ReadBinary<PlayerSaveCollection>();
            if (saves.playerSaves == null)
            {
                saves.playerSaves = new List<PlayerSave>();
            }
            saves.playerSaves.RemoveAll(o => o.slotIndex == (int) save.slotIndex);
            saves.playerSaves.Add(save);
            PlayerDataUtils.SaveDataBinaryAsync<PlayerSaveCollection>(saves, null);
        }
        
        public void SavePlayer()
        {
            SavePlayer(_currentPlayer);
        }
    }
}