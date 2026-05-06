// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Pool;
//
// namespace PowerCellStudio
// {
//     public partial class AssetsBundleManager
//     {
//         private class AssetsBundleRef
//         {
//             public AssetBundle Bundle => _bundle;
//             public int RefCount => _refCount;
//
//             private AssetBundle _bundle;
//
//             private int _refCount = 0;
//
//             // public bool AutoDispose = false;
//             public bool Alive = true;
//             private Coroutine _unloadCoroutine;
//             private AssetsBundleManager _assetsBundleManager;
//
//             public AssetsBundleRef(AssetBundle bundle, AssetsBundleManager assetsBundleManager)
//             {
//                 _bundle = bundle;
//                 _refCount = 0;
//                 _assetsBundleManager = assetsBundleManager;
//             }
//
//             public void DeRef()
//             {
//                 _refCount -= 1;
//                 if (RefCount <= AssetsBundleManager.disposeRefLine)
//                 {
//                     WaitToUnloadBundle();
//                 }
//             }
//
//             public void Restore()
//             {
//                 Alive = true;
//                 if (_unloadCoroutine != null)
//                 {
//                     _assetsBundleManager._coroutineRunner.StopCoroutine(_unloadCoroutine);
//                     _unloadCoroutine = null;
//                 }
//
//                 if (RefCount <= AssetsBundleManager.disposeRefLine)
//                 {
//                     _refCount = 0;
//                 }
//             }
//
//             public void AddRef()
//             {
//                 Alive = true;
//                 if (_unloadCoroutine != null)
//                 {
//                     _assetsBundleManager._coroutineRunner.StopCoroutine(_unloadCoroutine);
//                     _unloadCoroutine = null;
//                 }
//
//                 if (RefCount <= AssetsBundleManager.disposeRefLine)
//                 {
//                     _refCount = 1;
//                 }
//                 else
//                 {
//                     _refCount += 1;
//                 }
//             }
//
//             public void ForceUnload()
//             {
//                 _refCount = AssetsBundleManager.disposeRefLine - 1;
//                 // AutoDispose = true;
//                 WaitToUnloadBundle();
//             }
//
//             public void WaitToUnloadBundle()
//             {
//                 if (!Alive || _refCount > AssetsBundleManager.disposeRefLine || _unloadCoroutine != null)
//                     return;
//                 //  启动计时器
//                 if (_assetsBundleManager._coroutineRunner && AssetsBundleManager.delayUnloadDuration > 0)
//                     _unloadCoroutine = _assetsBundleManager._coroutineRunner.StartCoroutine(WaitToUnloadHandle());
//                 else
//                 {
//                     Alive = false;
//                     _assetsBundleManager?.UnloadAssetsBundle(this);
//                 }
//             }
//
//             private IEnumerator WaitToUnloadHandle()
//             {
//                 yield return new WaitForSecondsRealtime(AssetsBundleManager.delayUnloadDuration);
//                 Alive = false;
//                 _assetsBundleManager?.UnloadAssetsBundle(this);
//             }
//         }
//
//         private class BundleDependenceStack : IDisposable
//         {
//             private HashSet<string> _bundleSet;
//
//             private List<List<string>> _stack;
//
//             public int layerCount => _stack.Count;
//
//             public BundleDependenceStack()
//             {
//                 _bundleSet = HashSetPool<string>.Get();
//                 _stack = new List<List<string>>();
//             }
//
//             public bool Contains(string bundleName)
//             {
//                 return _bundleSet.Contains(bundleName);
//             }
//
//             public void Push(int layerIndex, string bundleName)
//             {
//                 // if (_bundleSet.Contains(bundleName)) return;
//                 while (_stack.Count < layerIndex + 1)
//                 {
//                     _stack.Add(ListPool<string>.Get());
//                 }
//
//                 _stack[layerIndex].Add(bundleName);
//                 _bundleSet.Add(bundleName);
//             }
//
//             // public void Pop()
//             // {
//             //     var list = _stack[layerCount -  1];
//             //     _stack.RemoveAt(layerCount -  1);
//             //     return list;
//             // }
//
//             public List<string> GetBundleNamesByLayer(int layerIndex)
//             {
//                 if (layerIndex < 0 || layerIndex >= _stack.Count) return new List<string>();
//                 return _stack[layerIndex];
//             }
//
//             public void Dispose()
//             {
//                 HashSetPool<string>.Release(_bundleSet);
//                 _bundleSet = null;
//                 foreach (var list in _stack)
//                 {
//                     ListPool<string>.Release(list);
//                 }
//
//                 _stack.Clear();
//                 _stack = null;
//             }
//         }
//     }
// }