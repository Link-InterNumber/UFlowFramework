using Unity.Mathematics;

namespace PowerCellStudio
{
    public partial class FastAStar
    {
        private static readonly int2[] SquareDirections =
        {
            new int2(0, 1), //上
            new int2(-1, 1), //左上
            new int2(-1, 0), //左
            new int2(-1, -1), //左下
            new int2(0, -1), //下
            new int2(1, -1), //右下
            new int2(1, 0), //右
            new int2(1, 1), //右上
        };

        private static readonly int2[] HexEvenRowDirections =
        {
            new int2(0, 1), //上
            new int2(-1, 1), //左上
            new int2(-1, 0), //左
            new int2(-1, -1), //左下
            new int2(0, -1), //下
            new int2(1, 0), //右
        };

        private static readonly int2[] HexOddRowDirections =
        {
            new int2(0, 1), //上
            new int2(-1, 0), //左
            new int2(0, -1), //下
            new int2(1, -1), //右下
            new int2(1, 0), //右
            new int2(1, 1), //右上
        };
    }
}