namespace TestEntity
{
   public struct Archetype
   {
      public ulong componentMask;

      public bool IsMatch(ulong other)
      {
         return componentMask & other == componentMask;
      }

      public bool IsMatch(Archetype other)
      {
         return IsMatch(other.componentMask);
      }

      public bool IsSupersetTo(ulong other)
      {
         return (componentMask & other) == other;
      }

      public bool IsSupersetTo(Archetype other)
      {
         return (componentMask & other.componentMask) == other.componentMask;
      }
   }
}