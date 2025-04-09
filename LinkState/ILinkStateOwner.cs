using System;

namespace LinkState
{
   public interface ILinkStateOwner
   {
      public int StateIndex { get; set; }
   }
}