// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem.Controls;
//
// namespace PowerCellStudio
// {
//     public interface IPlayerDevice
//     {
//         public bool isDefault { get; }
//         public void Reset();
//         public HashSet<ButtonControl> buttonControls { get; }
//         public Vector2 moveInput { get; }
//         public Vector2 lookInput { get; }
//         public ButtonControl leftStick { get; }
//         public ButtonControl rightStick { get; }
//         public ButtonControl upInput { get; }
//         public ButtonControl downInput { get; }
//         public ButtonControl leftInput { get; }
//         public ButtonControl rightInput { get; }
//         public ButtonControl triggerL { get; }
//         public ButtonControl triggerR { get; }
//         public ButtonControl triggerLT { get; }
//         public ButtonControl triggerRT { get; }
//         public ButtonControl confirmInput { get; }
//         public ButtonControl cancelInput { get; }
//         public ButtonControl actio1Input { get; }
//         public ButtonControl action2Input { get; }
//         public ButtonControl startInput { get; }
//         public ButtonControl selectInput { get; }
//         public ButtonControl GetPressKey();
//
//         public bool RemapUpInput(ButtonControl newInput);
//         public bool RemapDownInput(ButtonControl newInput);
//         public bool RemapLeftInput(ButtonControl newInput);
//         public bool RemapRightInput(ButtonControl newInput);
//         public bool RemapTriggerL(ButtonControl newInput);
//         public bool RemapTriggerR(ButtonControl newInput);
//         public bool RemapTriggerLT(ButtonControl newInput);
//         public bool RemapTriggerRT(ButtonControl newInput);
//         public bool RemapConfirmInput(ButtonControl newInput);
//         public bool RemapCancelInput(ButtonControl newInput);
//         public bool RemapAction1Input(ButtonControl newInput);
//         public bool RemapAction2Input(ButtonControl newInput);
//         
//         public PlayerInputSetting GetPlayerInputSetting();
//         public void SetPlayerInputSetting(PlayerInputSetting playerInputSetting);
//     }
// }