# Unity Project Template (2022.3.53f1)

![Unity Version](https://img.shields.io/badge/Unity-2022.3.53f1%20LTS-blue?logo=unity)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

# main 分支存在未验证功能，使用 release 分支！

# 仓库原址

[https://github.com/Link-InterNumber/UFlowFramework](https://github.com/Link-InterNumber/UFlowFramework)

# 使用文档

[使用文档](Doc/中文/0.框架介绍Info.md)

## 📦 包信息

| 项目 | 内容 |
|------|------|
| Package Name | `com.powercellstudio.uflowframework` |
| 当前版本 | `1.7.1` |
| Unity 版本 | `2022.3` 或更高版本 |
| 许可证 | [MIT License](LICENSE) |
| 推荐分支 | `release` |

## 🚀 安装方式

### 方式一：复制到项目

下载或克隆仓库后，将 `Assets/UFlowFramework` 文件夹复制到目标 Unity 项目的 `Assets` 目录下。

### 方式二：通过 Package Manager 从 Git 安装

在 Unity 的 **Package Manager > Add package from git URL...** 中输入：

```text
https://github.com/Link-InterNumber/UFlowFramework.git?path=Assets/UFlowFramework
```

> 如果需要稳定版本，建议使用 `release` 分支或指定 tag/commit。

## ✨ UFlow 框架特性

UFlow 是面向 Unity 项目的模块化开发框架，提供一组常用系统和工具，帮助项目快速搭建基础架构、降低重复开发成本，并提升功能扩展和维护效率。

- **模块化架构**：提供模块中枢、业务模块生命周期、事件注册与释放机制，便于按功能拆分项目逻辑。
- **资源管理系统**：支持 Addressables、AssetBundle、Resources 等加载方式，并通过统一接口管理资源加载、缓存和释放。
- **Page-Window UI 系统**：基于 Page 与 Window 的父子层级管理 UI，支持页面栈切换、窗口打开关闭和 UI 代码骨架生成。
- **配置表工具**：支持 Excel/CSV 配置导入，生成配置类和二进制配置资源，支持按块索引读取和自定义构建流程接入。
- **事件与异步系统**：提供类型安全的事件机制、帧末尾合并触发事件，以及协程/任务风格的异步执行辅助。
- **数据与本地化**：内置持久化数据、运行时数据管理、版本迁移和基于 Unity Localization 的多语言支持。
- **常用游戏系统**：包含对象池、音频管理、红点系统、引导系统、时间工具、有限状态机、UI 列表更新工具等模块。
- **工具与扩展能力**：提供日志生成、构建打包、二进制序列化、DataChunk、反射、表现效果和网络通信等辅助工具。

## 🚨 环境要求
 
 ### 强制依赖包
 必须通过 **Package Manager** 安装以下官方插件包：
 
 | 包名称 | 用途 | 安装验证方式 |
 |--------|------|--------------|
 | [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html) | 资源动态加载系统 | 检查 `Window > Asset Management > Addressables` 菜单是否存在 |
 | [Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/index.html) | 多语言本地化系统 | 确认 `Window > Asset Management > Localization Tables` 配置面板 |
 | [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) | 高级文本渲染方案 | 需导入 `TMP Essential Resources` |
 
 ### 安装验证步骤
 1. 打开 Package Manager (`Window > Package Manager`)
 2. 在 `Unity Registry` 中搜索以下包：
     - `com.unity.addressables` (建议版本 1.21.4+)
     - `com.unity.localization` (建议版本 1.4.2+)
     - `com.unity.textmeshpro` (建议版本 3.0.6+)
 3. 首次使用 TextMeshPro 时需：
   ```csharp
    // 在任意初始化代码中调用
    TMPro.TMP_Settings.LoadDefaultSettings(); 
   ```

## 🤝 参与贡献

欢迎通过 Issue 或 Pull Request 反馈问题、提交修复或补充文档。提交修改前建议：

1. 基于 `release` 分支创建功能分支。
2. 确认 Unity 版本和依赖包版本满足要求。
3. 对涉及运行时逻辑、资源加载、配置表、UI 或构建流程的修改进行基础验证。
4. 如果修改了公开 API 或使用流程，请同步更新中文和英文文档。

## 🐞 问题反馈

- GitHub Issues：[https://github.com/Link-InterNumber/UFlowFramework/issues](https://github.com/Link-InterNumber/UFlowFramework/issues)
- 技术交流 QQ 群：676959424

## 📄 开源许可证

本项目采用 [MIT License](LICENSE) 开源。你可以自由使用、修改和分发本项目，但需要保留原始版权和许可证声明。

### 可能你不需要以上这些功能

对于项目中已经使用了**其他或者自定义**的资源加载、本地化管理方案的项目，可以自行修改/删除对应脚本。

例如资源加载可以新增实现对应接口的脚本来接入需要的加载方式，具体方案请根据项目情况进行开发。

# 第三方插件/资源

本项目使用了以下开源/第三方资源，特此声明并致谢：

---

## 📦 核心框架 & 网络通信
### [NetCoreServer](https://github.com/chronoxor/NetCoreServer)
 - **类型**: 高性能跨平台网络服务器库
 - **用途**: TCP/UDP/SSL 通信模块实现
 - **许可证**: MIT License

---

## 🖥️ UI 组件
### [uGUI-Hypertext](https://github.com/setchi/uGUI-Hypertext)
 - **类型**: 富文本交互组件
 - **用途**: 创建支持超链接的UGUI文本
 - **许可证**: MIT License

### [TextLife](https://flowus.cn/enjoygameclub/share/fa2ac259-3498-4282-8200-3caeef47caef)
 - **类型**: UI文本组件
 - **用途**: 生成带特效的文本
 - **许可证**: MPL-2.0

---

## 🧠 算法与数据结构
### [KDTree](https://github.com/viliwonka/KDTree)
 - **类型**: 空间分区数据结构
 - **用途**: 高效近邻搜索算法实现
 - **许可证**: MIT License

---

## ✒️ 字体资源
### [得意黑 Smiley Sans](https://github.com/atelier-anchor/smiley-sans)
 - **类型**: 开源中文字体
 - **风格**: 现代几何风格黑体
 - **字符集**: 支持简体中文
 - **许可证**: SIL Open Font License

### [字魂扁桃体](https://izihun.com/shangyongziti/7495.html)
 - **类型**: 开源中文字体
 - **特征**: 手写风格艺术字体
 - **授权**: 字魂网对得意黑进行二次创作，发布开源字体「字魂扁桃体」，同样是开源并永久免费商用

---

## 🔧 开发工具
### [PlayableGraph Monitor](https://github.com/SolarianZ/UnityPlayableGraphMonitorTool)
 - **类型**: Timeline/动画系统调试工具
 - **用途**: 可视化PlayableGraph结构
 - **许可证**: MIT License

---

## 📜 许可证说明
 本项目遵循各第三方资源的授权协议：
 - MIT Licensed 资源可自由修改/再分发
 - MPL-2.0 修改文件需标注修改内容，衍生作品需开源
 - SIL OFL 字体需保留版权声明

---

🙏 **特别感谢** 所有开源项目作者及贡献者的杰出工作！