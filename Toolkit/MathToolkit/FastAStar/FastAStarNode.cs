using Unity.Mathematics;

namespace PowerCellStudio
{
    public partial class FastAStar
    {
        public struct FastAStarNode
        {
            public int2 pos;

            /// <summary>
            /// 深度
            /// </summary>
            public int G;

            /// <summary>
            /// 距离终点长度
            /// </summary>
            public int H;

            /// <summary>
            /// 从起点到这里，有几次拐弯
            /// </summary>
            public int I;

            public int F => G + H + I;
            
            public int parentIndex;
        }
    }
}