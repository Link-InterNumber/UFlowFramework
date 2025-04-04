using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PowerCellStudio
{
    
    public enum CtrlDevice
    {
        KeyboardNMouse,
        GamePad,
    }
    
    public class PlayerCtrlManager : SingletonBase<PlayerCtrlManager>, IEventModule
    {
        private List<LinkPlayerCtrl> _playerInputs = new List<LinkPlayerCtrl>();
        
        public void OnInit()
        {
#if UNITY_ANDROID || UNITY_IOS
            return;
#endif
            TryAddPlayer(CtrlDevice.GamePad, out var player);
            if (player == null)
            {
                TryAddPlayer(CtrlDevice.KeyboardNMouse, out player);
            }
        }
        
        public void RegisterEvent()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        public void UnRegisterEvent()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice arg1, InputDeviceChange arg2)
        {
            switch (arg2)
            {
                case InputDeviceChange.Removed:
                    RemovePlayer(arg1.deviceId);
                    break;
                case InputDeviceChange.Disconnected:
                    OnDisconnected(arg1.deviceId);
                    break;
                case InputDeviceChange.Reconnected:
                    OnReconnected(arg1.deviceId);
                    break;
                case InputDeviceChange.Enabled:
                case InputDeviceChange.Disabled:
                case InputDeviceChange.UsageChanged:
                case InputDeviceChange.ConfigurationChanged:
                case InputDeviceChange.SoftReset:
                case InputDeviceChange.HardReset:
                case InputDeviceChange.Added:
                default:
                    break;
            }
        }
        
        private void ReSortPlayer()
        {
            for (int i = 0; i < _playerInputs.Count; i++)
            {
                _playerInputs[i].index = i;
            }
        }

        private void OnDisconnected(int deviceId)
        {
            var matched = _playerInputs.FirstOrDefault(o => o.deviceId == deviceId);
            if (matched == default) return;
            matched.Enable(false);
            // TODO 打开设备断开界面，可以打开菜单界面

        }
        
        private void OnReconnected(int deviceId)
        {
            var matched = _playerInputs.FirstOrDefault(o => o.deviceId == deviceId);
            if (matched == default) return;
            matched.Enable(true);
        }
        
        public LinkPlayerCtrl GetPlayer(int deviceId)
        {
            return _playerInputs.FirstOrDefault(o => o.deviceId == deviceId);
        }
        
        public LinkPlayerCtrl GetPlayerByIndex(int index)
        {
            if(_playerInputs.Count <= index || index < 0) return null;
            return _playerInputs[index];
        }

        public bool TryAddPlayer(CtrlDevice ctrlDevice, out LinkPlayerCtrl playerCtrl)
        {
            var index = _playerInputs.Count;
            playerCtrl = null;
            switch (ctrlDevice)
            {
                case CtrlDevice.KeyboardNMouse:
                    var keyboard = GetDeviceNeverPaired(CtrlDevice.KeyboardNMouse);
                    if (keyboard == null) return false;
                    var inputKeyboardNMouse = new LinkPlayerCtrl(index, keyboard);
                    _playerInputs.Add(inputKeyboardNMouse);
                    playerCtrl = inputKeyboardNMouse;
                    return true;
                case CtrlDevice.GamePad:
                    var gamepad = GetDeviceNeverPaired(CtrlDevice.GamePad);
                    if (gamepad == null) return false;
                    var inputGamePad = new LinkPlayerCtrl(index, gamepad);
                    _playerInputs.Add(inputGamePad);
                    playerCtrl = inputGamePad;
                    return true;
                default:
                    return false;
            }
        }
        
        private InputDevice[] GetDeviceNeverPaired(CtrlDevice ctrlDevice)
        {
            switch (ctrlDevice)
            {
                case CtrlDevice.KeyboardNMouse:
                    return new InputDevice[] {Keyboard.current, Mouse.current};
                case CtrlDevice.GamePad:
                    foreach (var gamepad in Gamepad.all)
                    {
                        if (_playerInputs.Any(o => gamepad.deviceId == o.deviceId)) continue;
                        return new InputDevice[] {gamepad};
                    }
                    break;
                default:
                    break;
            }
            return null;
        }

        public bool RemovePlayer(int deviceId)
        {
            var player = GetPlayer(deviceId);
            if (player == null) return false;
            player.Dispose();
            _playerInputs.Remove(player);
            ReSortPlayer();
            // TODO 如果打开了手柄断联界面，则关闭手柄断联界面
            return true;
        }

        public void VibratePlayer(int index, float low, float height, float time)
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            return;
#endif
            var player = GetPlayerByIndex(index);
            if (player == null) return;
            player.Vibrate(low, height, time);
        }
    }
}