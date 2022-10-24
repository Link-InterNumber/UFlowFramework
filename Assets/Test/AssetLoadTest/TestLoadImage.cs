using System.Collections;
using LinkFrameWork.AssetsManage;
using UnityEngine;
using UnityEngine.UI;

namespace Test.AssetLoadTest
{
    public class TestLoadImage : MonoBehaviour
    {
        public Button btn;
        private void Start()
        {
            // ApplicationManager.Instance.StartCoroutine(LoadImage());
            LoadImageTest();
        }

        private void LoadImageTest()
        {
            AssetLoader.LoadSpriteAsync("Assets/Res/Textures/TestImg.jpg", transform.GetComponent<Image>(), arg0 => arg0.SetNativeSize());
        }

        public IEnumerator LoadImage()
        {
            AssetLoader.LoadSprite("Assets/Res/Textures/TestImg.jpg",transform.GetComponent<Image>());
            // AssetsBundleManager.Instance.AsyncLoadAssetsBundle("testtexture", bundle =>
            // {
            //     var a =bundle.LoadAsset<Sprite>("Assets/Res/Textures/TestImg.jpg");
            //     transform.GetComponent<Image>().sprite = a;
            //     GetComponent<Image>().SetNativeSize();
            // });
            yield return null;
            // if (AssetsBundleManager.Instance.GetAssetBundle("testtexture", out var ab))
            // {
            //     foreach (var allAssetName in ab.GetAllAssetNames())
            //     {
            //         Debug.Log(allAssetName);
            //     }
            //     foreach (var allSceneName in ab.GetAllScenePaths())
            //     {
            //         Debug.Log(allSceneName);
            //     }
            //     GetComponent<Image>().sprite = ab.LoadAsset<Sprite>("Assets/Res/Textures/TestImg.jpg");
            //     GetComponent<Image>().SetNativeSize();
            // }
        }
    }
}