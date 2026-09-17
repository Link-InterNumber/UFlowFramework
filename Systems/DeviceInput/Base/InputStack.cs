using System;
using System.Collections.Generic;

namespace UFlowFramework
{
    public class InputStack<TKey> : IDisposable
    {
        private Stack<InputPage<TKey>> _pages;
        
        public InputStack()
        {
            _pages = new Stack<InputPage<TKey>>();
        }

        public void Dispose()
        {
            _pages.Clear();
            _pages = null;
        }

        public InputPage<TKey> PushPage()
        {
            if (_pages == null) throw new ObjectDisposedException(nameof(InputStack<TKey>));
            var page = new InputPage<TKey>();
            _pages.Push(page);
            return page;
        }
        
        public void PopPage()
        {
            if (_pages == null) throw new ObjectDisposedException(nameof(InputStack<TKey>));
            if (_pages.Count <= 0) return;
            var page = _pages.Pop();
            page?.Dispose();
        }
        
        public InputPage<TKey> GetCurrentPage()
        {
            if (_pages == null) throw new ObjectDisposedException(nameof(InputStack<TKey>));
            if (_pages.Count <= 0) return null;
            return _pages.Peek();
        }
        
        public void AddListener(TKey actionKey, Action<InputEvent<TKey>> callback)
        {
            var currentPage = GetCurrentPage();
            if (currentPage != null)
                currentPage.AddListener(actionKey, callback);
        }
        
        public void RemoveListener(TKey actionKey, Action<InputEvent<TKey>> callback)
        {
            var currentPage = GetCurrentPage();
            if (currentPage != null)
                currentPage.RemoveListener(actionKey, callback);
        }
        
        public void RemoveAllListener(TKey actionKey)
        {
            var currentPage = GetCurrentPage();
            if (currentPage != null)
                currentPage.RemoveAllListener(actionKey);
        }

        public void ClearPeekPage()
        {
            var currentPage = GetCurrentPage();
            if (currentPage != null)
                currentPage.ClearAll();
        }

        public void Dispatch(in InputEvent<TKey> input)
        {
            GetCurrentPage()?.Handle(in input);
        }
    }
}