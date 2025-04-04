using System.Collections.Generic;

namespace PowerCellStudio
{
    public static class StackExtension
    {
        public static void MoveToTop<T>(this Stack<T> stack, T item)
        {
            if (stack.Contains(item))
            {
                var temp = new Stack<T>();
                T targetItem;
                do
                {
                    targetItem = stack.Pop();
                    if (!targetItem.Equals(item))
                    {
                        temp.Push(targetItem);
                    }
                } while (!targetItem.Equals(item));

                while (stack.Count > 0)
                {
                    temp.Push(stack.Pop());
                }

                while (temp.Count > 0)
                {
                    stack.Push(temp.Pop());
                }

                stack.Push(item);
            }
        }
    }
}