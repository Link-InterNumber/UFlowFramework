using System;

namespace LinkState
{
   public interface ILinkStateOwner : IDisposable
   {
      public int StateIndex { get; set; }
   }
}