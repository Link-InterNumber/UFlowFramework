namespace TestEntity
{
   // public interface IEntity : IIndex
   // {
   //    public bool isDestroyed { get; set; }
   // }

   public struct Entity : IIndex
   {
      private long _index;
      public long index => _index;
      public bool isDestroyed;
      public ulong chunkIndex;
      public long indexInChunk;

      public Archetype archetype;
   }
}