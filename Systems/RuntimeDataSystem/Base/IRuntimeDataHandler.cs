using System.Collections.Generic;
using System.Linq;

namespace PowerCellStudio
{
    public interface IRuntimeDataHandler
    {
        public void InitData();
    }
    
    public interface IRuntimeDataHandler<T> : IRuntimeDataHandler
        where T : ICloneT<T>
    {
        // private sealed class BagData : RuntimeDataDic<int, RItem> { }

        public void AddData(T rData);

        public void RemoveData(T rData);

        public int GetKey(T rData);
        
        public T GetData(int key);

        public void AddListener(OnRuntimeDataChange<T> action);

        public void RemoveListener(OnRuntimeDataChange<T> action);

        public List<T> GetAllData();
    }

    public abstract class RuntimeDataHandlerBase<T, KT> : IRuntimeDataHandler<T>
        where T : struct, ICloneT<T>
        where KT : RuntimeDataManager.RuntimeDataDic<int,T>, new()
    {
        public virtual void InitData()
        {
            if(RuntimeDataManager.instance.GetRuntimeStorage<KT>() != null) return;
            var storage = new KT();
            RuntimeDataManager.instance.AddRuntimeStorage(storage);
        }

        public virtual void AddData(T rData)
        {
            var storage = GetStorage();
            if (storage == null) return;
            var key = GetKey(rData);
            storage.ReplaceData(key, rData);
        }

        public virtual void RemoveData(T rData)
        {
            var storage = GetStorage();
            if (storage == null) return;
            var key = GetKey(rData);
            storage.Remove(key);
        }

        private KT GetStorage()
        {
            return RuntimeDataManager.instance.GetRuntimeStorage<KT>();
        }

        public abstract int GetKey(T rData);

        public virtual T GetData(int key)
        {
            var storage = GetStorage();
            if (storage == null) return default(T);
            return storage.GetData(key);
        }

        public virtual void AddListener(OnRuntimeDataChange<T> action)
        {
            var storage = GetStorage();
            if (storage == null) return;
            storage.AddListener(action);
        }

        public virtual void RemoveListener(OnRuntimeDataChange<T> action)
        {
            var storage = GetStorage();
            if (storage == null) return;
            storage.RemoveListener(action);
        }

        public virtual List<T> GetAllData()
        {
            var storage = GetStorage();
            if (storage == null) return new List<T>();
            return storage.ToList();
        }
    }
}