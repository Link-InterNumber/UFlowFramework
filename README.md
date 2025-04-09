# Unity Project Template (2022.3.53f1)

![Unity Version](https://img.shields.io/badge/Unity-2022.3.53f1%20LTS-blue?logo=unity)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

## 🚨 环境要求

### 强制依赖包
必须通过 **Package Manager** 安装以下官方插件包：

| 包名称 | 用途 | 安装验证方式 |
|--------|------|--------------|
| [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/index.html) | 资源动态加载系统 | 检查 `Window > Asset Management > Addressables` 菜单是否存在 |
| [Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/index.html) | 多语言本地化系统 | 确认 `Project Settings > Localization` 配置面板 |
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
   
### 可能你不需要以上这些功能
对于项目中已经使用了**其他或者自定义**的资源加载、本地化管理方案的项目，可以自行修改/删除对应脚本。

例如资源加载可以新增实现对应接口的脚本来接入需要的加载方式，具体方案请根据项目情况进行开发。

# Unity Project Third-Party Assets

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