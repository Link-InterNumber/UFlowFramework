# Unity Project Template (2022.3.53f1)

![Unity Version](https://img.shields.io/badge/Unity-2022.3.53f1%20LTS-blue?logo=unity)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

## 🚨 Environment Requirements

### Mandatory Dependency Packages
The following official plugin packages must be installed via **Package Manager**:

| Package Name | Purpose | Installation Verification |
|--------------|---------|---------------------------|
| [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html) | Dynamic resource loading system | Check if the `Window > Asset Management > Addressables` menu exists |
| [Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/index.html) | Multilingual localization system | Confirm the `Project Settings > Localization` configuration panel |
| [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) | Advanced text rendering solution | Import `TMP Essential Resources` |

### Installation Verification Steps
1. Open the Package Manager (`Window > Package Manager`)
2. Search for the following packages in the `Unity Registry`:
    - `com.unity.addressables` (Recommended version 1.21.4+)
    - `com.unity.localization` (Recommended version 1.4.2+)
    - `com.unity.textmeshpro` (Recommended version 3.0.6+)
3. When using TextMeshPro for the first time:
   ```csharp
   // Call this in any initialization code
   TMPro.TMP_Settings.LoadDefaultSettings(); 
   ```

### You May Not Need These Features
For projects that already use **other or custom** resource loading or localization management solutions, you can modify/delete the corresponding scripts as needed.

For example, to implement a custom resource loading method, you can create scripts that implement the required interfaces. Specific solutions should be developed based on the project requirements.

# Unity Project Third-Party Assets

This project uses the following open-source/third-party resources. Special thanks and acknowledgments:

---

## 📦 Core Framework & Network Communication
### [NetCoreServer](https://github.com/chronoxor/NetCoreServer)
- **Type**: High-performance cross-platform network server library
- **Purpose**: Implementation of TCP/UDP/SSL communication modules
- **License**: MIT License

---

## 🖥️ UI Components
### [uGUI-Hypertext](https://github.com/setchi/uGUI-Hypertext)
- **Type**: Rich text interaction component
- **Purpose**: Create UGUI text with hyperlink support
- **License**: MIT License

### [TextLife](https://flowus.cn/enjoygameclub/share/fa2ac259-3498-4282-8200-3caeef47caef)
- **Type**: UI text component
- **Purpose**: Generate text with special effects
- **License**: MPL-2.0

---

## 🧠 Algorithms and Data Structures
### [KDTree](https://github.com/viliwonka/KDTree)
- **Type**: Spatial partitioning data structure
- **Purpose**: Efficient nearest neighbor search algorithm implementation
- **License**: MIT License

---

## ✒️ Font Resources
### [Smiley Sans](https://github.com/atelier-anchor/smiley-sans)
- **Type**: Open-source Chinese font
- **Style**: Modern geometric sans-serif
- **Character Set**: Supports Simplified Chinese
- **License**: SIL Open Font License

### [Zihun Biantaoti](https://izihun.com/shangyongziti/7495.html)
- **Type**: Open-source Chinese font
- **Feature**: Handwriting-style artistic font
- **Authorization**: Zihun Network created the open-source font "Zihun Biantaoti" based on Smiley Sans. It is also open-source and free for commercial use.

---

## 🔧 Development Tools
### [PlayableGraph Monitor](https://github.com/SolarianZ/UnityPlayableGraphMonitorTool)
- **Type**: Timeline/Animation system debugging tool
- **Purpose**: Visualize the PlayableGraph structure
- **License**: MIT License

---

## 📜 License Notes
This project complies with the licensing agreements of all third-party resources:
- MIT Licensed resources can be freely modified/distributed
- MPL-2.0 requires modified files to be marked, and derivative works must be open-sourced
- SIL OFL fonts require copyright notices to be retained

---

🙏 **Special Thanks** to all the authors and contributors of open-source projects for their outstanding work!

# 使用文档

[使用文档](Doc/English/0.InfoOfUFlow.md)