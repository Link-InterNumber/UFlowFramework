using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UFlowFramework
{
    public class InputStack<TKey> : IDisposable
    {
        private Stack<IInputPage<TKey>> _pages;
        private ObjectPool<IInputPage<TKey>> _pagePool;
        
        public InputStack(Func<IInputPage<TKey>> pageFactory)
        {
            _pages = new Stack<IInputPage<TKey>>();
            _pagePool = new ObjectPool<IInputPage<TKey>>(
                pageFactory, 
                null, 
                page => page.ClearAll(),
                page => page.Dispose(), 
                false, 
                5, 
                10);
        }

        public void Dispose()
        {
            while (_pages.Count > 0)
            {
                var page = _pages.Pop();
                page?.Dispose();
            }
            _pages = null;
            _pagePool?.Dispose();
        }

        public void PushPage()
        {
            if (_pages == null) throw new ObjectDisposedException(nameof(InputStack<TKey>));
            var page = _pagePool.Get();
            _pages.Push(page);
        }
        
        public void PopPage()
        {
            if (_pages == null) throw new ObjectDisposedException(nameof(InputStack<TKey>));
            if (_pages.Count <= 0) return;
            var page = _pages.Pop();
            _pagePool.Release(page);
        }
        
        internal IInputPage<TKey> GetCurrentPage()
        {
            if (_pages == null) throw new ObjectDisposedException(nameof(InputStack<TKey>));
            if (_pages.Count <= 0) return null;
            return _pages.Peek();
        }

        public bool HasListenerRightNow(TKey actionKey)
        {
            var currentPage = GetCurrentPage();
            if (currentPage != null)
                return currentPage.HasListener(actionKey);
            return false;
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
            var currentPage = GetCurrentPage();
            if (currentPage == null || currentPage.isEmpty) return;
            currentPage.Handle(input);
        }
    }
}