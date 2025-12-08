using System;

namespace PowerCellStudio
{
    public interface IOpenWindowRequest
    {
        Type currentWindowType { get; }
        bool isPreLoad { get; }
        AssetLoadStatus assetLoadStatus { get; }
        void Load();
        void SetOpenData(object sourceData, Action beforeOpen);
        void OnLoaded(Action onLoaded);
    }
}