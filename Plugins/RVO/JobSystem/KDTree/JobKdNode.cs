using Unity.Mathematics;

namespace RVO.JobSystem
{
    internal struct JobKdNode
    {
        public int begin;
        public int end;
        public int left;
        public int right;
        public int axis;
        public float split;
        public float3 min;
        public float3 max;
        public byte isLeaf;
    }
}
