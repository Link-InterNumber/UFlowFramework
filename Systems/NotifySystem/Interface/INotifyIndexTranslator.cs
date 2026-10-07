using System;

namespace PowerCellStudio
{
    public interface INotifyIndexTranslator<T>
    {
        public int ToIndex(T data);

        public T ByIndex(int index);

        public int GetNotifyCount();
    }
}