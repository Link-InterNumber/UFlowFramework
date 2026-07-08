# Unity Project Template (2022.3.53f1)

![Unity Version](https://img.shields.io/badge/Unity-2022.3.53f1%20LTS-blue?logo=unity)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

# There are unverified features in the main branch, use the release branch!

# github

[https://github.com/Link-InterNumber/UFlowFramework](https://github.com/Link-InterNumber/UFlowFramework)

# Document

[Document](Doc/English/0.InfoOfUFlow.md)

## 📦 Package Information

| Item | Value |
|------|-------|
| Package Name | `com.powercellstudio.uflowframework` |
| Current Version | `1.7.1` |
| Unity Version | `2022.3` or later |
| License | [MIT License](LICENSE) |
| Recommended Branch | `release` |

## 🚀 Installation

### Option 1: Copy into Your Project

Download or clone the repository, then copy the `Assets/UFlowFramework` folder into the `Assets` directory of your Unity project.

### Option 2: Install from Git via Package Manager

In Unity, open **Package Manager > Add package from git URL...** and enter:

```text
https://github.com/Link-InterNumber/UFlowFramework.git?path=Assets/UFlowFramework
```

> For stable usage, use the `release` branch or specify a tag/commit.

## ✨ UFlow Framework Features

UFlow is a modular development framework for Unity projects. It provides a set of commonly used systems and tools to help projects quickly build their foundation, reduce repetitive development work, and improve extensibility and maintainability.

- **Modular Architecture**: Provides a module hub, business module lifecycle management, and event registration/release mechanisms to help split project logic by feature.
- **Asset Management System**: Supports Addressables, AssetBundle, Resources, and other loading modes, with unified interfaces for asset loading, caching, and release.
- **Page-Window UI System**: Manages UI through the parent-child relationship between Page and Window, supporting page stack switching, window opening/closing, and UI code skeleton generation.
- **Configuration Table Tools**: Supports Excel/CSV configuration import, generates configuration classes and binary configuration assets, and supports index-chunk based reading and custom build pipeline integration.
- **Event and Async Systems**: Provides type-safe events, end-of-frame merged event dispatching, and coroutine/task-style asynchronous execution helpers.
- **Data and Localization**: Includes persistent data, runtime data management, version migration, and multilingual support based on Unity Localization.
- **Common Game Systems**: Includes object pooling, audio management, red point notifications, guidance, time utilities, finite state machines, UI list update tools, and more.
- **Tools and Extensibility**: Provides logging code generation, build tools, binary serialization, DataChunk, reflection utilities, visual effects helpers, network communication tools, and other extensions.

## 🚨 Environment Requirements

### Mandatory Dependency Packages
The following official plugin packages must be installed via **Package Manager**:

| Package Name | Purpose | Installation Verification |
|--------------|---------|---------------------------|
| [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html) | Dynamic resource loading system | Check if the `Window > Asset Management > Addressables` menu exists |
| [Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/index.html) | Multilingual localization system | Confirm the `Window > Asset Management > Localization Tables` configuration panel |
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


## 🤝 Contributing

Issues and Pull Requests are welcome for bug reports, fixes, and documentation improvements. Before submitting changes, it is recommended to:

1. Create a feature branch based on the `release` branch.
2. Confirm that the Unity version and dependency package versions meet the requirements.
3. Run basic validation for changes related to runtime logic, asset loading, configuration tables, UI, or build pipelines.
4. If public APIs or usage workflows are changed, update both Chinese and English documentation.

## 🐞 Feedback

- GitHub Issues: [https://github.com/Link-InterNumber/UFlowFramework/issues](https://github.com/Link-InterNumber/UFlowFramework/issues)
- QQ Group: 676959424

## 📄 License

This project is open-sourced under the [MIT License](LICENSE). You are free to use, modify, and distribute it, provided that the original copyright and license notices are retained.

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