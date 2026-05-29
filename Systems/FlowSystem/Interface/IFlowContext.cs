using System;
using System.Collections.Generic;

namespace PowerCellStudio
{
    /// <summary>
    /// 记录当前场景流转的上下文数据和黑板值
    /// </summary>
    public interface IFlowContext : IDisposable
    {
        /// <summary>
        /// 自定义上下文数据
        /// </summary>
        public object contextData { get; set; }
        
        public ISceneFlow currentFlow { get; }
        public ISceneFlow previousFlow { get; }
        
        public bool isFlowCompleted { get; }
        public bool isFlowFailed { get; }

        internal Dictionary<string, object> sharedValues { get; }
        
        public void StartFlow(ISceneFlow nextFlow);
        public void CompleteFlow();
        public void FailFlow(string reason = null);

        /// <summary>
        /// 设置一个触发器的状态，触发器可以被流程中的条件判断使用
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isOn"></param>
        public void SetTrigger(string key, bool isOn);

        /// <summary>
        /// 检查一个触发器是否被设置为true
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>        
        public bool IsTriggerOn(string key);
        
        /// <summary>
        /// 检查一个触发器是否开启，检查后会自动重置为false
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool CheckTrigger(string key);
        
        /// <summary>
        /// 写入黑板值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetValue(string key, object value);
        
        /// <summary>
        /// 获取黑板值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue<T>(string key, T defaultValue = default);
        
        public bool ClearValue(string key);
        
        public void ClearAllValues();
        
        /// <summary>
        /// 写入一个在所有上下文中共享的黑板值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetSharedValue(string key, object value);
        
        /// <summary>
        /// 获取一个在所有上下文中共享的黑板值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSharedValue<T>(string key, T defaultValue = default);
        
        public bool ClearSharedValue(string key);
    }
}