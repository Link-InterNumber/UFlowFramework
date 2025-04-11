/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnjoyGameClub.TextLifeFramework.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EnjoyGameClub.TextLifeFramework.Core
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [ExecuteInEditMode]
    [Serializable]
    public partial class TextLife : UIBehaviour
    {
        #region Constants
        /// <summary>
        /// 标签匹配正则表达式模式
        /// </summary>
        /// <remarks>
        /// Regular expression pattern for matching custom tags in text
        /// </remarks>
        private const string TAG_REGEX_PATTERN = @"<(\w+)>([\s\S]*?)<@\1>";

        #endregion

        #region Serialized Fields
        /// <summary>
        /// 原始文本内容（支持文本区域编辑）
        /// </summary>
        /// <remarks>
        /// Original text content with text area editing support
        /// </remarks>
        [TextArea(3, 10)] public string Text;
        /// <summary>
        /// 匹配到的动画过程列表
        /// </summary>
        /// <remarks>
        /// List of matched animation processes
        /// </remarks>
        public ProcessList MatchProcesses = new();
        /// <summary>
        /// 全局应用的动画过程列表
        /// </summary>
        /// <remarks>
        /// List of globally applied animation processes
        /// </remarks>
        public ProcessList GlobalProcesses = new();
        /// <summary>
        /// 持久化存储的动画过程列表
        /// </summary>
        /// <remarks>
        /// List of persisted animation processes
        /// </remarks>
        public ProcessList _persistedProcesses = new();

        #endregion

        #region Private Fields

        private TMP_Text _tempText;
        private Dictionary<string, AnimationProcess> _activeProcessMap = new();
        private List<MatchData> _matchedProcessDataList = new();
        private Vector3[] _originalVertices;
        private Color32[] _originalColor;
        private Character[] _characters;
        private float _animationTime;

        // Start
        private string _originalText;
        private string _runtimeText;
        private string _tmpFormattedText;
        private string _processedText;
        public bool Debug;
        private int _globalProcessesCount;

        #endregion

        #region Public Properties

        /// <summary>
        /// 已注册的动画过程类型字典（标签与类型映射）
        /// </summary>
        /// <remarks>
        /// Dictionary of registered animation process types (tag -> type mapping)
        /// </remarks>
        public static Dictionary<string, Type> RegisteredProcessTypes { get; private set; } = new();

        #endregion


        #region Unity Lifecycle

        protected override void Start()
        {
            InitTMPTextComponent();
            ResetAnimation();
            InitStateMachine();
            InitModelProcesses();
            CleanProcesses();
            RefreshActiveProcesses();
        }

        private void Update()
        {
            stateMachine?.UpdateState();
            RefreshGlobalProcesses();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// 重置动画时间线
        /// </summary>
        /// <remarks>
        /// Reset animation timeline
        /// </remarks>
        public void ResetAnimation()
        {
            Parallel.ForEach(_persistedProcesses.ProcessesList, animationProcess => { animationProcess?.Reset(); });
        }
        
        /// <summary>
        /// 添加全局动画过程
        /// </summary>
        /// <remarks>
        /// Add global animation process
        /// </remarks>
        /// <param name="process">要添加的动画过程实例</param>
        public void AddGlobalProcess(AnimationProcess process)
        {
            if (GlobalProcesses.ProcessesList.Any(activeProcess => activeProcess.MatchTag == process.MatchTag))
            {
                return;
            }
        
            GlobalProcesses.ProcessesList.Add(process);
            RefreshActiveProcesses();
        }
        
        /// <summary>
        /// 移除全局动画过程
        /// </summary>
        /// <remarks>
        /// Remove global animation process by match name
        /// </remarks>
        /// <param name="matchName">匹配标签名称</param>
        public void RemoveGlobalProcess(string matchName)
        {
            GlobalProcesses.ProcessesList =
                GlobalProcesses.ProcessesList.Where(process => process.MatchTag != matchName).ToList();
            RefreshActiveProcesses();
        }


        /// <summary>
        /// 通过类型移除全局动画过程
        /// </summary>
        /// <remarks>
        /// Remove global animation process by type
        /// </remarks>
        /// <typeparam name="T">动画过程类型</typeparam>
        public void RemoveGlobalProcess<T>() where T : AnimationProcess
        {
            GlobalProcesses.ProcessesList =
                GlobalProcesses.ProcessesList.Where(process => process.GetType() != typeof(T)).ToList();
            RefreshActiveProcesses();
        }

        /// <summary>
        /// 通过类型移除全局动画过程
        /// </summary>
        /// <remarks>
        /// Remove global animation process by type
        /// </remarks>
        /// <param name="type">动画过程类型</param>
        public void RemoveGlobalProcess(Type type)
        {
            if (!type.IsSubclassOf(typeof(AnimationProcess)))
            {
                return;
            }
        
            GlobalProcesses.ProcessesList =
                GlobalProcesses.ProcessesList.Where(process => process.GetType() != type).ToList();
            RefreshActiveProcesses();
        }

        /// <summary>
        /// 创建指定类型的动画过程
        /// </summary>
        /// <remarks>
        /// Create animation process of specified type
        /// </remarks>
        /// <param name="type">动画过程类型</param>
        /// <returns>动画过程实例</returns>
        public AnimationProcess CreateProcess(Type type)
        {
            if (!type.IsSubclassOf(typeof(AnimationProcess)))
            {
                return null;
            }

            if (TryGetProcess(out AnimationProcess process))
            {
                return process;
            }

            process = Activator.CreateInstance(type) as AnimationProcess;
            process?.Create();
            _persistedProcesses.ProcessesList.Add(process);
            return process;
        }

        /// <summary>
        /// 创建指定类型的动画过程
        /// </summary>
        /// <remarks>
        /// Create animation process of specified type
        /// </remarks>
        /// <typeparam name="T">动画过程类型</typeparam>
        /// <returns>动画过程实例</returns>
        public T CreateProcess<T>() where T : AnimationProcess
        {
            return CreateProcess(typeof(T)) as T;
        }


        /// <summary>
        /// 获取指定类型的动画过程
        /// </summary>
        /// <remarks>
        /// Get animation process of specified type
        /// </remarks>
        /// <typeparam name="T">动画过程类型</typeparam>
        /// <returns>动画过程实例</returns>
        public T GetProcess<T>() where T : AnimationProcess
        {
            return GetProcess(typeof(T)) as T;
        }
        /// <summary>
        /// 获取指定类型的动画过程
        /// </summary>
        /// <remarks>
        /// Get animation process of specified type
        /// </remarks>
        /// <param name="type">动画过程类型</param>
        /// <returns>动画过程实例</returns>
        public AnimationProcess GetProcess(Type type)
        {
            return _persistedProcesses.ProcessesList.FirstOrDefault(persistedProcess =>
                persistedProcess.GetType() == type);
        }
        /// <summary>
        /// 尝试获取动画流程
        /// </summary>
        /// <remarks>
        /// Get animation process of specified type
        /// </remarks>
        /// <typeparam name="T">动画过程类型</typeparam>
        /// <returns>是否成功获取</returns>
        public bool TryGetProcess<T>(out T process) where T : AnimationProcess
        {
            process = GetProcess<T>();
            return process != null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 重新解析文本中的标签并更新动画状态
        /// </summary>
        /// <remarks>
        /// Re-parses text tags and updates animation states.
        /// </remarks>
        /// <param name="originalText">需要处理的原始文本/Raw text to process</param>
        private void ReMatchText(string originalText)
        {
            _runtimeText = originalText;
            Color color = _tempText.color;
            _tempText.color = new Color(0, 0, 0, 0);
            var str = StartMatchTag(_runtimeText);
            _tmpFormattedText = str.toComponentString;
            _processedText = str.toProcessesString;
            _tempText.SetText(_tmpFormattedText);
            _tempText.color = color;
            _tempText.ForceMeshUpdate();
            RefreshActiveProcesses();
            RecordOriginalVertexData();
            BuildCharacterData();
        }

        
        /// <summary>
        /// 记录动画的累计播放时间
        /// </summary>
        /// <remarks>
        /// Records animation timeline progression. 
        /// </remarks>
        private void RecordAnimationTime()
        {
            _animationTime += Time.deltaTime;
        }

        /// <summary>
        /// 初始化TMP文本组件并配置渲染回调
        /// </summary>
        /// <remarks>
        /// Initializes the TextMeshPro component and configures rendering callbacks.
        /// </remarks>
        private void InitTMPTextComponent()
        {
            _tempText = GetComponent<TMP_Text>();
            _tempText.OnPreRenderText += info => { RecordOriginalVertexData(); };
        }

        /// <summary>
        /// 刷新当前激活的动画过程列表
        /// </summary>
        /// <remarks>
        /// Updates active animation processes based on matched tags
        /// </remarks>
        private void RefreshActiveProcesses()
        {
            if (stateMachine.State.Name == "noneState")
            {
                return;
            }

            MatchProcesses.ProcessesList.Clear();
            // Load match processes.
            var activeTags = new HashSet<string>(_matchedProcessDataList.Select(x => x.MatchTag));
            foreach (var matchTag in activeTags)
            {
                if (MatchProcesses.ProcessesList.FirstOrDefault(p => p.MatchTag == matchTag) != null)
                {
                    continue;
                }

                var existing = _persistedProcesses.ProcessesList.FirstOrDefault(p => p.MatchTag == matchTag);
                if (_persistedProcesses.ProcessesList.FirstOrDefault(p => p.MatchTag == matchTag) != null)
                {
                    MatchProcesses.ProcessesList.Add(existing);
                    continue;
                }

                if (RegisteredProcessTypes.TryGetValue(matchTag, out var processType))
                {
                    var process = CreateProcess(processType);
                    MatchProcesses.ProcessesList.Add(process);
                }
            }

            _activeProcessMap = MatchProcesses.ProcessesList.ToDictionary(p => p.MatchTag);
        }
        /// <summary>
        /// 刷新全局动画过程列表
        /// </summary>
        /// <remarks>
        /// Updates global animation processes and ensures uniqueness
        /// </remarks>
        private void RefreshGlobalProcesses()
        {
            if (GlobalProcesses.ProcessesList.Count == _globalProcessesCount)
            {
                return;
            }

            GlobalProcesses.ProcessesList = GlobalProcesses
                .ProcessesList.GroupBy(item => item.GetType()) 
                .Select(g => g.Last()) 
                .ToList();
            for (int i = 0; i < GlobalProcesses.ProcessesList.Count; i++)
            {
                var process = GlobalProcesses.ProcessesList[i];
                var persistProcess = GetProcess(process.GetType());
                if (persistProcess != null)
                {
                    GlobalProcesses.ProcessesList[i] = persistProcess;
                }
                else
                {
                    _persistedProcesses.ProcessesList.Add(process);
                    process.Create();
                }
            }

            _globalProcessesCount = GlobalProcesses.ProcessesList.Count;
        }
        /// <summary>
        /// 检查调试文本是否发生变化
        /// </summary>
        /// <remarks>
        /// Checks if debug text has been modified
        /// </remarks>
        private bool HasDebugTextChanged() => _runtimeText != Text;
        /// <summary>
        /// 检查TMP文本是否发生变化
        /// </summary>
        /// <remarks>
        /// Checks if TMP text has been modified
        /// </remarks>
        private bool HasTMPTextChanged()
        {
            return  _tempText.text != _originalText &&
                    _tempText.text != _tmpFormattedText;
        }

        /// <summary>
        /// 初始化动画过程模型
        /// </summary>
        /// <remarks>
        /// Initializes animation process model types
        /// </remarks>
        private void InitModelProcesses()
        {
            RegisteredProcessTypes.Clear();
            IEnumerable<Type> processTypes = Assembly.GetAssembly(typeof(AnimationProcess))
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AnimationProcess)));
            foreach (Type type in processTypes)
            {
                FieldInfo matchTagField = type.GetField("MatchTag", BindingFlags.Public | BindingFlags.Instance);
                if (matchTagField != null && matchTagField.FieldType == typeof(string))
                {
                    AnimationProcess instance = (AnimationProcess)Activator.CreateInstance(type);
                    string matchTag = (string)matchTagField.GetValue(instance);
                    if (!string.IsNullOrEmpty(matchTag))
                    {
                        if (!RegisteredProcessTypes.ContainsKey(matchTag))
                        {
                            RegisteredProcessTypes.Add(matchTag, type);
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"Duplicate matchTag detected: {matchTag} in {type.Name}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清理无效的动画过程
        /// </summary>
        /// <remarks>
        /// Cleans up invalid animation processes
        /// </remarks>
        private void CleanProcesses()
        {
            for (int i = _persistedProcesses.ProcessesList.Count - 1; i >= 0; i--)
            {
                if (_persistedProcesses.ProcessesList[i] == null)
                {
                    _persistedProcesses.ProcessesList.RemoveAt(i);
                }
            }

            for (int i = GlobalProcesses.ProcessesList.Count - 1; i >= 0; i--)
            {
                if (GlobalProcesses.ProcessesList[i] == null)
                {
                    GlobalProcesses.ProcessesList.RemoveAt(i);
                }
            }

            _persistedProcesses.ProcessesList = _persistedProcesses
                .ProcessesList.GroupBy(item => item.GetType()) // 按类型分组
                .Select(g => g.Last()) // 选最后一个
                .ToList();
        }

        /// <summary>
        /// 记录原始顶点数据
        /// </summary>
        /// <remarks>
        /// Records original vertex data for animation
        /// </remarks>
        private void RecordOriginalVertexData()
        {
            if (_tempText.textInfo.characterCount <= 0)
            {
                _tempText.textInfo.ClearMeshInfo(true);
                return;
            }

            var charInfo = _tempText.textInfo.characterInfo[0];
            var meshInfo = _tempText.textInfo.meshInfo[charInfo.materialReferenceIndex];
            _originalVertices = new Vector3[meshInfo.vertices.Length];
            _originalColor = new Color32[meshInfo.colors32.Length];
            meshInfo.vertices.CopyTo(_originalVertices, 0);
            meshInfo.colors32.CopyTo(_originalColor, 0);
        }
        /// <summary>
        /// 开始匹配文本中的标签
        /// </summary>
        /// <remarks>
        /// Starts matching tags in the text
        /// </remarks>
        /// <param name="originalString">原始文本</param>
        /// <returns>包含组件文本和处理文本的元组</returns>
        private (string toComponentString, string toProcessesString) StartMatchTag(string originalString)
        {
            // Prepare data structures
            List<Match> matchesList = new List<Match>();
            _matchedProcessDataList.Clear();
            var overflowMode = _tempText.overflowMode;
            _tempText.overflowMode = TextOverflowModes.Overflow;
            _tempText.SetText(originalString);
            _tempText.ForceMeshUpdate();
            string processedText = _tempText.GetParsedText();
            _tempText.overflowMode = overflowMode;


            string tmpFormattedText = originalString;
            // Regex matching to determine recursion depth
            var matchCollection = Regex.Matches(processedText, TAG_REGEX_PATTERN, RegexOptions.Singleline);
            for (int i = 0; i < matchCollection.Count; i++)
            {
                MatchTag(matchesList, processedText, ref processedText, ref tmpFormattedText);
            }

            return (tmpFormattedText, processedText);
        }

        /// <summary>
        /// 匹配文本中的标签
        /// </summary>
        /// <remarks>
        /// Matches tags in the text
        /// </remarks>
        /// <param name="matchesList">匹配列表</param>
        /// <param name="matchingString">待匹配文本</param>
        /// <param name="processedText">处理后的文本</param>
        /// <param name="tmpFormattedText">格式化后的文本</param>
        /// <returns>是否匹配成功</returns>
        private bool MatchTag(List<Match> matchesList, string matchingString, ref string processedText,
            ref string tmpFormattedText)
        {
            // Regex matching
            var match = Regex.Match(matchingString, TAG_REGEX_PATTERN);
            if (match == Match.Empty)
            {
                return false;
            }

            matchesList.Add(match);

            // Recursive processing
            string innerString = match.Groups[2].Value;
            bool hasInnerMatch = MatchTag(matchesList, innerString, ref processedText, ref tmpFormattedText);
            if (!hasInnerMatch)
            {
                int contentStartIndex = matchesList[0].Index;
                int contentEndIndex = contentStartIndex + innerString.Length;
                foreach (var m in matchesList)
                {
                    string matchTag = m.Groups[1].Value;
                    ReplaceTagInText(ref processedText, matchTag);
                    ReplaceTagInText(ref tmpFormattedText, matchTag);
                    _matchedProcessDataList.Add(new MatchData()
                    {
                        MatchTag = matchTag,
                        Content = innerString,
                        StartIndex = contentStartIndex,
                        EndIndex = contentEndIndex,
                    });
                }


                matchesList.Clear();
            }

            return true;
        }

        /// <summary>
        /// 替换文本中的标签
        /// </summary>
        /// <remarks>
        /// Replaces tags in the text
        /// </remarks>
        /// <param name="text">目标文本</param>
        /// <param name="tag">标签名称</param>
        private static void ReplaceTagInText(ref string text, string tag)
        {
            Regex regexFront = new Regex($"<{tag}>", RegexOptions.Singleline);
            Regex regexBack = new Regex($"<@{tag}>", RegexOptions.Singleline);
            text = regexFront.Replace(text, "", 1);
            text = regexBack.Replace(text, "", 1);
        }
        /// <summary>
        /// 构建字符数据
        /// </summary>
        /// <remarks>
        /// Builds character data for animation
        /// </remarks>
        private void BuildCharacterData()
        {
            int characterCount = _tempText.textInfo.characterCount;
            _characters = new Character[characterCount];

            // 构建Character数据
            Parallel.For(0, characterCount, i =>
            {
                var currentCharInfo = _tempText.textInfo.characterInfo[i];
                var currentCharMeshInfo = _tempText.textInfo.meshInfo[currentCharInfo.materialReferenceIndex];
                int startVertexIndex = currentCharInfo.vertexIndex;
                int nextVertexIndex = startVertexIndex + 4;
                bool visible = currentCharInfo.isVisible;
                // Build per-character data
                var data = new Character
                {
                    Transform = new Transform()
                    {
                        Vertices = new Vector3[4],
                        TMPComponent = _tempText
                    },
                    VerticesColor = new Color32[4],
                    CharIndex = i,
                    StartIndex = startVertexIndex,
                    EndIndex = nextVertexIndex,
                    MeshInfo = currentCharMeshInfo,
                    TotalCount = characterCount,
                    Visible = visible,
                    CharacterInfo = currentCharInfo,
                    TMPComponent = _tempText
                };
                // 初始化一次，记录某些数据。
                Array.Copy(_originalVertices, startVertexIndex, data.Transform.Vertices, 0, 4);
                Array.Copy(_originalColor, startVertexIndex, data.VerticesColor, 0, 4);
                data.Transform.InitData();
                _characters[i] = data;
            });
        }

        /// <summary>
        /// 执行动画过程
        /// </summary>
        /// <remarks>
        /// Executes animation processes
        /// </remarks>
        private void ExecuteProcess()
        {
            int characterCount = _characters.Length;
            Parallel.For(0, characterCount, i =>
            {
                // Copy position and color data
                Array.Copy(_originalVertices, _characters[i].StartIndex, _characters[i].Transform.Vertices, 0, 4);
                Array.Copy(_originalColor, _characters[i].StartIndex, _characters[i].VerticesColor, 0, 4);
                _characters[i].Transform.InitData();
            });
            
            // Match process.
            foreach (var processData in _matchedProcessDataList)
            {
                if (!_activeProcessMap.TryGetValue(processData.MatchTag, out AnimationProcess progress)) continue;

                int startIndex = processData.StartIndex;
                int endIndex = processData.EndIndex;
                for (int i = startIndex; i < endIndex; i++)
                {
                    // Safety check
                    if (i >= _characters.Length)
                    {
                        break;
                    }

                    if (!_characters[i].Visible)
                    {
                        continue;
                    }

                    _characters[i] = progress.Progress(_animationTime, Time.deltaTime, _characters[i]);
                }
            }
            
            // Global process.
            foreach (var variableGlobalProcess in GlobalProcesses.ProcessesList)
            {
                for (int i = 0; i < _characters.Length; i++)
                {
                    if (!_characters[i].Visible)
                    {
                        continue;
                    }
                    _characters[i] =
                        variableGlobalProcess.Progress(_animationTime, Time.deltaTime, _characters[i]);
                }
            }

            // Update vertices data.
            for (int i = 0; i < _characters.Length; i++)
            {
                if (!_characters[i].Visible)
                {
                    continue;
                }
                _characters[i].Transform.RecordData();
                for (int k = 0; k < _characters[i].Transform.Vertices.Length; k++)
                {
                    var character = _characters[i];
                    int index = character.StartIndex + k;
                    character.MeshInfo.vertices[index] =
                        character.Transform.Vertices[k];
                    character.MeshInfo.colors32[index] = character.VerticesColor[k];
                }
                
            }
            
            _tempText.UpdateVertexData();
        }

        #endregion
    }
}