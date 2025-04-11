/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) Ruoy  
 */

using System;
namespace EnjoyGameClub.TextLifeFramework.Core
{
    /// <summary>
    /// 动画处理的基类，用于编写TextLife框架中的文字动画过程。
    /// 提供了文字动画的时间控制和时间缩放功能。
    /// </summary>
    /// <remarks>
    /// The base class for handling animation processes in the TextLife framework.
    /// Provides timing and scaling functionalities for character animations.
    /// </remarks>
    [Serializable]
    public abstract class AnimationProcess
    {
        /// <summary>
        /// 是否启用动画处理。
        /// </summary>
        /// <remarks>
        /// Determines whether the animation process is enabled.
        /// </remarks>
        public bool Enable = true;

        /// <summary>
        /// 匹配该动画处理的标签，自动由派生类设置。
        /// </summary>
        /// <remarks>
        /// The tag used to match this animation process. Automatically set by the derived class.
        /// </remarks>
        [ReadOnly] public string MatchTag = "";

        /// <summary>
        /// 时间缩放系数，用于控制动画的时间流逝速度。
        /// </summary>
        /// <remarks>
        /// Scale factor for the animation time, controlling the speed of animation.
        /// </remarks>
        public float TimeScale = 1;

        /// <summary>
        /// 每个字符的时间偏移，用于控制字符的动画起始时间。
        /// </summary>
        /// <remarks>
        /// Offset time applied per character in the animation sequence.
        /// </remarks>
        public float CharacterTimeOffset;

        /// <summary>
        /// 用于时间偏移计算的常量参数。
        /// </summary>
        /// <remarks>
        /// Constant parameter used for time offset calculations.
        /// </remarks>
        protected const float TIME_OFFSET_PARAM = 0.001f;

        // 定时器相关变量
        protected float Time = 0;
        protected float DeltaTime;
        private float _resetTimePoint = 0;
        private float _systemTime = 0;

        /// <summary>
        /// 初始化动画处理对象，并自动为其设置标签。
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of the <see cref="AnimationProcess"/> class.
        /// Automatically assigns a tag to the animation process.
        /// </remarks>
        protected AnimationProcess()
        {
            SetTag();
        }

        /// <summary>
        /// 设置动画标签，调用派生类的实现。
        /// </summary>
        /// <remarks>
        /// Sets the tag for the animation process, calling the derived class implementation.
        /// </remarks>
        public void SetTag()
        {
            MatchTag = OnSetTag();
        }

        /// <summary>
        /// 派生类实现该方法以设置自定义的标签。
        /// </summary>
        /// <remarks>
        /// The derived class implements this method to set a custom tag.
        /// </remarks>
        protected abstract string OnSetTag();

        /// <summary>
        /// 创建动画处理，调用派生类的创建逻辑。
        /// </summary>
        /// <remarks>
        /// Creates the animation process, calling the derived class creation logic.
        /// </remarks>
        public void Create()
        {
            OnProcessCreate();
        }

        /// <summary>
        /// 派生类可以重写此方法来实现自定义的创建逻辑。
        /// </summary>
        /// <remarks>
        /// The derived class can override this method to implement custom creation logic.
        /// </remarks>
        protected virtual void OnProcessCreate()
        {
        }

        /// <summary>
        /// 重置动画处理，设置重置时间并调用派生类的重置逻辑。
        /// </summary>
        /// <remarks>
        /// Resets the animation process, sets the reset time, and calls the derived class reset logic.
        /// </remarks>
        public void Reset()
        {
            _resetTimePoint = _systemTime;
            OnReset();
        }

        /// <summary>
        /// 派生类可以重写此方法来实现自定义的重置逻辑。
        /// </summary>
        /// <remarks>
        /// The derived class can override this method to implement custom reset logic.
        /// </remarks>
        protected virtual void OnReset()
        {
        }

        /// <summary>
        /// 进度更新函数，根据当前时间和字符信息更新动画进度。
        /// </summary>
        /// <param name="time">当前时间。</param>
        /// <param name="deltaTime">每帧的时间差。</param>
        /// <param name="character">当前处理的字符。</param>
        /// <returns>字符。</returns>
        /// <remarks>
        /// Progress update function that updates the animation progress based on the current time and character information.
        /// </remarks>
        public Character Progress(float time, float deltaTime, Character character)
        {
            if (!Enable)
            {
                return character;
            }

            _systemTime = time;
            Time = time - _resetTimePoint;
            Time += character.CharIndex * -CharacterTimeOffset * TIME_OFFSET_PARAM;
            Time *= TimeScale;
            DeltaTime = deltaTime * TimeScale;
            return OnProgress(character);
        }

        /// <summary>
        /// 派生类可以重写此方法来处理动画进度更新。
        /// </summary>
        /// <param name="character">当前处理的字符。</param>
        /// <returns>更新后的字符。</returns>
        /// <remarks>
        /// The derived class can override this method to handle animation progress updates.
        /// </remarks>
        protected virtual Character OnProgress(Character character)
        {
            return character;
        }
    }
}
