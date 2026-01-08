using UnityEngine;
using System.IO;

public class TakeScreenshot : MonoBehaviour
{
    public Camera targetCamera; // シーン内で割り当て

    public void Capture(string savePath)
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("[TakeScreenshot] No camera assigned.");
            return;
        }

        int width = Screen.width;
        int height = Screen.height;
        RenderTexture rt = new RenderTexture(width, height, 24);
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);

        targetCamera.targetTexture = rt;
        targetCamera.Render();

        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);

        Debug.Log("[TakeScreenshot] Screenshot saved to: " + savePath);
    }
}
