// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.InputSystem.Controls;
//
// namespace PowerCellStudio
// {
//     public class PCDevice : IPlayerDevice
//     {
//         private Keyboard _keyboard;
//         private Mouse _mouse;
//         private HashSet<ButtonControl> _buttonControls = new HashSet<ButtonControl>();
//         public HashSet<ButtonControl> buttonControls => _buttonControls;
//
//         public PCDevice(Keyboard keyboard, Mouse mouse)
//         {
//             _keyboard = keyboard;
//             _mouse = mouse;
//             Reset();
//         }
//
//         private bool _isDefault = true;
//         public bool isDefault { get; }
//
//         public void Reset()
//         {
//             _isDefault = true;
//             _buttonControls.Clear();
//             _leftStick = _keyboard.zKey;
//             _rightStick = _keyboard.xKey;
//             _upMoveInput = _keyboard.wKey;
//             _downMoveInput = _keyboard.sKey;
//             _leftMoveInput = _keyboard.aKey;
//             _rightMoveInput = _keyboard.dKey;
//             _upInput = _keyboard.upArrowKey;
//             _downInput = _keyboard.downArrowKey;
//             _leftInput = _keyboard.leftArrowKey;
//             _rightInput = _keyboard.rightArrowKey;
//             _triggerL = _keyboard.leftCtrlKey;
//             _triggerR = _keyboard.spaceKey;
//             _triggerLT = _keyboard.leftShiftKey;
//             _triggerRT = _keyboard.leftAltKey;
//             _confirmInput = _mouse.leftButton;
//             _cancelInput = _mouse.rightButton;
//             _action1Input = _keyboard.qKey;
//             _action2Input = _keyboard.eKey;
//             _startInput = _keyboard.escapeKey;
//             _selectInput = _keyboard.tabKey;
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
//             _buttonControls.Add(_upMoveInput);
//             _buttonControls.Add(_downMoveInput);
//             _buttonControls.Add(_leftMoveInput);
//             _buttonControls.Add(_rightMoveInput);
//         }
//
//         private ButtonControl _upMoveInput;
//         private ButtonControl _downMoveInput;
//         private ButtonControl _leftMoveInput;
//         private ButtonControl _rightMoveInput;
//
//         public Vector2 moveInput
//         {
//             get
//             {
//                 var result = Vector2.zero;
//                 if (_upMoveInput.isPressed)
//                     result.y += 1f;
//
//                 if (_downMoveInput.isPressed)
//                     result.y -= 1f;
//
//                 if (_leftMoveInput.isPressed)
//                     result.x -= 1f;
//
//                 if (_rightMoveInput.isPressed)
//                     result.x += 1f;
//                 return result;
//             }
//         }
//
//         public Vector2 lookInput => _mouse.delta.ReadValue();
//         private ButtonControl _leftStick;
//
//         public ButtonControl leftStick
//         {
//             get => _leftStick;
//         }
//
//         private ButtonControl _rightStick;
//
//         public ButtonControl rightStick
//         {
//             get => _rightStick;
//         }
//
//         private ButtonControl _upInput;
//
//         public ButtonControl upInput
//         {
//             get => _upInput;
//         }
//
//         private ButtonControl _downInput;
//
//         public ButtonControl downInput
//         {
//             get => _downInput;
//         }
//
//         private ButtonControl _leftInput;
//
//         public ButtonControl leftInput
//         {
//             get => _leftInput;
//         }
//
//         private ButtonControl _rightInput;
//
//         public ButtonControl rightInput
//         {
//             get => _rightInput;
//         }
//
//         private ButtonControl _triggerL;
//
//         public ButtonControl triggerL
//         {
//             get => _triggerL;
//         }
//
//         private ButtonControl _triggerR;
//
//         public ButtonControl triggerR
//         {
//             get => _triggerR;
//         }
//
//         private ButtonControl _triggerLT;
//
//         public ButtonControl triggerLT
//         {
//             get => _triggerLT;
//         }
//
//         private ButtonControl _triggerRT;
//
//         public ButtonControl triggerRT
//         {
//             get => _triggerRT;
//         }
//
//         public ButtonControl confirmInput { get; }
//
//         private ButtonControl _confirmInput;
//
//         public ButtonControl comfirmInput
//         {
//             get => _confirmInput;
//         }
//
//         private ButtonControl _cancelInput;
//
//         public ButtonControl cancelInput
//         {
//             get => _cancelInput;
//         }
//
//         private ButtonControl _action1Input;
//
//         public ButtonControl action1Input
//         {
//             get => _action1Input;
//         }
//
//         private ButtonControl _action2Input;
//
//         public ButtonControl action2Input
//         {
//             get => _action2Input;
//         }
//
//         private ButtonControl _startInput;
//
//         public ButtonControl startInput
//         {
//             get => _startInput;
//         }
//
//         private ButtonControl _selectInput;
//
//         public ButtonControl selectInput
//         {
//             get => _selectInput;
//         }
//
//         private bool IsDeviceButton(ButtonControl newInput)
//         {
//             if (newInput == null || (newInput.device != _keyboard.device && newInput.device != _mouse.device))
//             {
//                 return false;
//             }
//
//             _buttonControls.Remove(newInput);
//             _isDefault = false;
//             return true;
//         }
//
//         public bool RemapMoveUpInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _upMoveInput = newInput;
//             return true;
//         }
//
//         public bool RemapMoveDownInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _downMoveInput = newInput;
//             return true;
//         }
//
//         public bool RemapMoveLeftInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _leftMoveInput = newInput;
//             return true;
//         }
//
//         public bool RemapLeftStick(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _leftStick = newInput;
//             return true;
//         }
//
//         public bool RemapRightStick(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _rightStick = newInput;
//             return true;
//         }
//
//         public bool RemapMoveRightInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _rightMoveInput = newInput;
//             return true;
//         }
//
//         public bool RemapUpInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _upInput = newInput;
//             return true;
//         }
//
//         public bool RemapDownInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _downInput = newInput;
//             return true;
//         }
//
//         public bool RemapLeftInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _leftInput = newInput;
//             return true;
//         }
//
//         public bool RemapRightInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _rightInput = newInput;
//             return true;
//         }
//
//         public bool RemapTriggerL(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _triggerL = newInput;
//             return true;
//         }
//
//         public bool RemapTriggerR(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _triggerR = newInput;
//             return true;
//         }
//
//         public bool RemapTriggerLT(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _triggerLT = newInput;
//             return true;
//         }
//
//         public bool RemapTriggerRT(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _triggerRT = newInput;
//             return true;
//         }
//         
//         public bool RemapConfirmInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _confirmInput = newInput;
//             return true;
//         }
//
//         public bool RemapCancelInput(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _cancelInput = newInput;
//             return true;
//         }
//
//         public bool RemapAction1Input(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _action1Input = newInput;
//             return true;
//         }
//
//         public bool RemapAction2Input(ButtonControl newInput)
//         {
//             IsDeviceButton(newInput);
//             _action2Input = newInput;
//             return true;
//         }
//
//         public PlayerInputSetting GetPlayerInputSetting()
//         {
//             var setting = new PlayerInputSetting();
//             setting.deviceName = _keyboard.device.name;
//             setting.isDefault = _isDefault;
//             if(setting.isDefault)
//             {
//                 return setting;
//             }
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.MoveUp,
//                 Name = _upMoveInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.MoveDown,
//                 Name = _downMoveInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.MoveLeft,
//                 Name = _leftMoveInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.MoveRight,
//                 Name = _rightMoveInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.LeftStick,
//                 Name = _leftStick.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.RightStick,
//                 Name = _rightStick.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Up,
//                 Name = _upInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Down,
//                 Name = _downInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Left,
//                 Name = _leftInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Right,
//                 Name = _rightInput.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerL,
//                 Name = _triggerL.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerR,
//                 Name = _triggerR.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerLT,
//                 Name = _triggerLT.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.TriggerRT,
//                 Name = _triggerRT.displayName
//             });
//             // setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             // {
//             //     playerInput = EPlayerInput.Comfirm,
//             //     Name = _comfirmInput.displayName
//             // });
//             // setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             // {
//             //     playerInput = EPlayerInput.Cancel,
//             //     Name = _cancelInput.displayName
//             // });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Action1,
//                 Name = _action1Input.displayName
//             });
//             setting.playerInputSettingDatas.Add(new PlayerInputSettingData()
//             {
//                 playerInput = EPlayerInput.Action2,
//                 Name = _action2Input.displayName
//             });
//             return setting;
//         }
//
//         public void SetPlayerInputSetting(PlayerInputSetting playerInputSetting)
//         {
//             if(playerInputSetting == null || playerInputSetting.isDefault)
//             {
//                 Reset();
//                 return;
//             }
//             foreach (var data in playerInputSetting.playerInputSettingDatas)
//             {
//                 switch (data.playerInput)
//                 {
//                     case EPlayerInput.MoveUp:
//                         RemapMoveUpInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.MoveDown:
//                         RemapMoveDownInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.MoveLeft:
//                         RemapMoveLeftInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.MoveRight:
//                         RemapMoveRightInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.LeftStick:
//                         RemapLeftStick(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.RightStick:
//                         RemapRightStick(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.Up:
//                         RemapUpInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.Down:
//                         RemapDownInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.Left:
//                         RemapLeftInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.Right:
//                         RemapRightInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.TriggerL:
//                         RemapTriggerL(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.TriggerR:
//                         RemapTriggerR(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.TriggerLT:
//                         RemapTriggerLT(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.TriggerRT:
//                         RemapTriggerRT(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     // case EPlayerInput.Comfirm:
//                     //     RemapComfirmInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                     //     break;
//                     // case EPlayerInput.Cancel:
//                     //     RemapCancelInput(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                     //     break;
//                     case EPlayerInput.Action1:
//                         RemapAction1Input(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                     case EPlayerInput.Action2:
//                         RemapAction2Input(_keyboard.FindKeyOnCurrentKeyboardLayout(data.Name));
//                         break;
//                 }
//             }
//         }
//
//         public ButtonControl GetPressKey()
//         {
//             foreach (var key in _keyboard.allKeys)
//             {
//                 if (key.isPressed)
//                 {
//                     return key;
//                 }
//             }
//             return null;
//         }
//     }
// }