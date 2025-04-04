// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.InputSystem.Controls;
//
// namespace PowerCellStudio
// {
//     public class GamePadDevice : IPlayerDevice
//     {
//         Gamepad _gamepad;
//         
//         private HashSet<ButtonControl> _buttonControls = new HashSet<ButtonControl>();
//         public HashSet<ButtonControl> buttonControls => _buttonControls;
//
//         public GamePadDevice(Gamepad gamepad)
//         {
//             _gamepad = gamepad;
//             Reset();
//         }
//
//         private bool _isDefault = false;
//         public bool isDefault { get; }
//
//         public void Reset()
//         {
//             _isDefault = true;
//             _buttonControls.Clear();
//             _leftStick = _gamepad.leftStickButton;
//             _rightStick = _gamepad.rightStickButton;
//             _upInput = _gamepad.dpad.up;
//             _downInput = _gamepad.dpad.down;
//             _leftInput = _gamepad.dpad.left;
//             _rightInput = _gamepad.dpad.right;
//             _triggerL = _gamepad.leftTrigger;
//             _triggerR = _gamepad.rightTrigger;
//             _triggerLT = _gamepad.leftTrigger;
//             _triggerRT = _gamepad.rightTrigger;
//             if (Application.platform == RuntimePlatform.Switch)
//             {
//                 _confirmInput = _gamepad.buttonEast;
//                 _cancelInput = _gamepad.buttonSouth;
//             }
//             else
//             {
//                 _confirmInput = _gamepad.buttonSouth;
//                 _cancelInput = _gamepad.buttonEast;
//             }
//
//             _action1Input = _gamepad.buttonWest;
//             _action2Input = _gamepad.buttonNorth;
//             _startInput = _gamepad.startButton;
//             _selectInput = _gamepad.selectButton;
//             _buttonControls.Add(_upInput);
//             _buttonControls.Add(_downInput);
//             _buttonControls.Add(_leftInput);
//             _buttonControls.Add(_rightInput);
//             _buttonControls.Add(_triggerL);
//             _buttonControls.Add(_triggerR);
//             _buttonControls.Add(_triggerLT);
//             _buttonControls.Add(_triggerRT);
//             _buttonControls.Add(_confirmInput);
//             _buttonControls.Add(_cancelInput);
//             _buttonControls.Add(_action1Input);
//             _buttonControls.Add(_action2Input);
//             _buttonControls.Add(_startInput);
//             _buttonControls.Add(_selectInput);
//         }
//
//         public Vector2 moveInput { get => _gamepad.leftStick.value; }
//         public Vector2 lookInput { get => _gamepad.rightStick.value; }
//         private ButtonControl _leftStick;
//         public ButtonControl leftStick { get => _leftStick; }
//         private ButtonControl _rightStick;
//         public ButtonControl rightStick { get => _rightStick; }
//         private ButtonControl _upInput;
//         public ButtonControl upInput { get => _upInput; }
//         private ButtonControl _downInput;
//         public ButtonControl downInput { get => _downInput; }
//         private ButtonControl _leftInput;
//         public ButtonControl leftInput { get => _leftInput; }
//         private ButtonControl _rightInput;
//         public ButtonControl rightInput { get => _rightInput; }
//         private ButtonControl _triggerL;
//         public ButtonControl triggerL { get => _triggerL;}
//         private ButtonControl _triggerR;
//         public ButtonControl triggerR { get => _triggerR;}
//         private ButtonControl _triggerLT;
//         public ButtonControl triggerLT { get => _triggerLT;}
//         private ButtonControl _triggerRT;
//         public ButtonControl triggerRT { get => _triggerRT;}
//         private ButtonControl _confirmInput;
//         public ButtonControl confirmInput { get => _confirmInput; }
//         private ButtonControl _cancelInput;
//         public ButtonControl cancelInput { get => _cancelInput; }
//         private ButtonControl _action1Input;
//         public ButtonControl action1Input { get => _action1Input; }
//         private ButtonControl _action2Input;
//         public ButtonControl action2Input { get => _action2Input; }
//         private ButtonControl _startInput;
//         public ButtonControl startInput { get => _startInput;}
//         private ButtonControl _selectInput;
//         public ButtonControl selectInput { get => _selectInput; }
//
//         public ButtonControl GetPressKey()
//         {
//             foreach (var gamepadAllControl in _gamepad.allControls)
//             {
//                 if (gamepadAllControl is ButtonControl button && button.isPressed)
//                 {
//                     return button;
//                 }
//             }
//             return null;
//         }
//         
//         private bool IsDeviceButton(ButtonControl newInput)
//         {
//             if (newInput ==null || newInput.device != _gamepad.device)
//             {
//                 return false;
//             }
//             _buttonControls.Remove(newInput);
//             _isDefault = false;
//             return true;
//         }
//         
//         public bool RemapLeftStick(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _leftStick = newInput;
//             return true;
//         }
//         
//         public bool RemapRightStick(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _rightStick = newInput;
//             return true;
//         }
//
//         public bool RemapUpInput(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _upInput = newInput;
//             return true;
//         }
//         
//         public bool RemapDownInput(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _downInput = newInput;
//             return true;
//         }
//         
//         public bool RemapLeftInput(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _leftInput = newInput;
//             return true;
//         }
//         
//         public bool RemapRightInput(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _rightInput = newInput;
//             return true;
//         }
//         
//         public bool RemapTriggerL(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _triggerL = newInput;
//             return true;
//         }
//         
//         public bool RemapTriggerR(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _triggerR = newInput;
//             return true;
//         }
//         
//         public bool RemapTriggerLT(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _triggerLT = newInput;
//             return true;
//         }
//         
//         public bool RemapTriggerRT(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _triggerRT = newInput;
//             return true;
//         }
//         
//         public bool RemapConfirmInput(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _confirmInput = newInput;
//             return true;
//         }
//         
//         public bool RemapCancelInput(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _cancelInput = newInput;
//             return true;
//         }
//         
//         public bool RemapAction1Input(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _action1Input = newInput;
//             return true;
//         }
//         
//         public bool RemapAction2Input(ButtonControl newInput)
//         {
//             if (!IsDeviceButton(newInput)) return false;
//             _action2Input = newInput;
//             return true;
//         }
//
//         public PlayerInputSetting GetPlayerInputSetting()
//         {
//             var setting = new PlayerInputSetting();
//             setting.isDefault = _isDefault;
//             setting.deviceName = _gamepad.device.name;
//             if(setting.isDefault) return setting;
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.LeftStick,
//                 Name = _leftStick.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.RightStick,
//                 Name = _rightStick.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Up,
//                 Name = _upInput.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Down,
//                 Name = _downInput.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Left,
//                 Name = _leftInput.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Right,
//                 Name = _rightInput.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerL,
//                 Name = _triggerL.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerR,
//                 Name = _triggerR.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerLT,
//                 Name = _triggerLT.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerRT,
//                 Name = _triggerRT.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Comfirm,
//                 Name = _confirmInput.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Cancel,
//                 Name = _cancelInput.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Action1,
//                 Name = _action1Input.name
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Action2,
//                 Name = _action2Input.name
//             });
//             return setting;
//         }
//
//         public void SetPlayerInputSetting(PlayerInputSetting playerInputSetting)
//         {
//             if (playerInputSetting == null || playerInputSetting.isDefault)
//             {
//                 Reset();
//                 return;
//             }
//             foreach (var playerInputSettingData in playerInputSetting.playerInputSettingDatas)
//             {
//                 switch (playerInputSettingData.playerInput)
//                 {
//                     case EPlayerInput.Up:
//                         RemapUpInput(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.Down:
//                         RemapDownInput(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.Left:
//                         RemapLeftInput(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.Right:
//                         RemapRightInput(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.TriggerL:
//                         RemapTriggerL(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.TriggerR:
//                         RemapTriggerR(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.TriggerLT:
//                         RemapTriggerLT(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.TriggerRT:
//                         RemapTriggerRT(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.Comfirm:
//                         RemapConfirmInput(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.Cancel:
//                         RemapCancelInput(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.Action1:
//                         RemapAction1Input(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.Action2:
//                         RemapAction2Input(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.LeftStick:
//                         RemapLeftStick(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     case EPlayerInput.RightStick:
//                         RemapRightStick(_gamepad.allControls.FirstOrDefault(o => o.name == playerInputSettingData.Name) as ButtonControl);
//                         break;
//                     default:
//                         break;
//                 }
//             }
//         }
//     }
// }