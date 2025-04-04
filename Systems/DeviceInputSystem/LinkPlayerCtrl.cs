using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace PowerCellStudio
{
    public class LinkPlayerCtrl : IDisposable
    {
        public int index;
        private PlayerInputBase _user;
        public int deviceId;
        private CtrlDevice _ctrlDevice;
        private InputDevice[] _allDevices;
        
        public PlayerInputBase.PlayerActions Player => _user.Player;
        public PlayerInputBase.UIActions UI => _user.UI;

        public LinkPlayerCtrl(int index, params InputDevice[] devices)
        {
            this.index = index;
            _user = new PlayerInputBase();
            ChangeDevice(devices);
            EventManager.instance.onUIInputEnable.AddListener(EnableUICtrl);
        }
        
        public void ChangeDevice(params InputDevice[] devices)
        {
            if (devices == null || devices.Length <= 0) return;
            _user.devices = devices;
            deviceId = devices[0].deviceId;
            _ctrlDevice = devices[0].device is Keyboard ? CtrlDevice.KeyboardNMouse : CtrlDevice.GamePad;
            _allDevices = devices;
        }

        public void EnablePlayerCtrl(bool enable)
        {
            if (enable)
            {
                _user.Enable();
                _user.Player.Enable();
            }
            else
            {
                _user.Player.Disable();
            }
        }
        
        public void EnableUICtrl(bool enable)
        {
            if (enable)
            {
                _user.Enable();
                _user.UI.Enable();
            }
            else
            {
                _user.UI.Disable();
            }
        }
        
        public void Enable(bool enable)
        {
            if (enable)
            {
                _user.Enable();
            }
            else
            {
                _user.Disable();
            }
        }
        
        private void OnDisconnect()
        {
            _user.Disable();
        }

        public void Dispose()
        {
            EventManager.instance.onUIInputEnable.RemoveListener(EnableUICtrl);
            _user?.Dispose();
        }
        
        public void Vibrate(float low, float height, float time)
        {
            if (_ctrlDevice != CtrlDevice.GamePad) return;
            if (_shakeCoroutine != null)
            {
                ApplicationManager.instance.StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = ApplicationManager.instance.StartCoroutine(GamePadShake(_allDevices[0] as Gamepad, low, height, time));
        }

        private Coroutine _shakeCoroutine;
        private IEnumerator GamePadShake(Gamepad pad, float low, float height, float time)
        {
            if(pad == null) yield break;
            pad.SetMotorSpeeds(low, height);
            yield return new WaitForSeconds(time);
            pad.SetMotorSpeeds(0f, 0f);
            pad.ResumeHaptics();
            _shakeCoroutine = null;
        }
    }
}