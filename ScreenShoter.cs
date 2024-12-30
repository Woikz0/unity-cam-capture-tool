using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ObjectScreenshotToSprite : MonoBehaviour
{
    public Camera renderCamera; // Render için kullanılacak kamera
    public string savePath = "Assets/Sprites/"; // Kaydedilecek klasör
    public List<GameObject> allObjects = new List<GameObject>();

    void Start()
    {
        // Sahnedeki tüm aktif GameObject'leri bul

        foreach (GameObject obj in allObjects)
        {
            obj.SetActive(true);
            // Render işlemi için ekran görüntüsünü al
            if (obj.activeInHierarchy)
            {
                TakeScreenshot(obj);
                obj.SetActive(false);
            }
        }
    }

    void TakeScreenshot(GameObject obj)
    {
        // Geçici bir RenderTexture oluştur
        RenderTexture renderTexture = new RenderTexture(512, 512, 16);
        renderCamera.targetTexture = renderTexture;
        AdjustCameraForObject(obj);

        // Kamerayı objeye odakla
        // renderCamera.transform.position = obj.transform.position - renderCamera.transform.forward * 10f;
        // renderCamera.transform.LookAt(obj.transform);

        // Kamerayı sahneyi render etmesi için zorlama
        renderCamera.Render();

        // RenderTexture'tan Texture2D'ye aktar
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        // RenderTexture bağlantılarını temizle
        renderCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        // PNG olarak kaydet
        byte[] bytes = texture.EncodeToPNG();
        string fileName = savePath + obj.name + ".png";

        // Klasör yoksa oluştur
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        File.WriteAllBytes(fileName, bytes);
        Debug.Log($"Kaydedildi: {fileName}");

        // Unity editörüne kaydedilen dosyaları yeniden yükle
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
    void AdjustCameraForObject(GameObject obj)
    {
        // Objeyi ortalamak için objenin Bounds bilgilerini al
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"Object {obj.name} Renderer içermiyor, atlanıyor.");
            return;
        }

        Bounds bounds = renderer.bounds;

        // Kamerayı objeye doğru konumlandır
        Vector3 objectCenter = bounds.center;
        float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        renderCamera.transform.position = objectCenter - renderCamera.transform.forward * objectSize * 2f;
        renderCamera.transform.LookAt(objectCenter);

        // Kameranın ortografik veya perspektif boyutunu ayarla
        if (renderCamera.orthographic)
        {
            renderCamera.orthographicSize = objectSize ; // Ortografik boyut
        }
        else
        {
            float fov = renderCamera.fieldOfView;
            float distance = Vector3.Distance(renderCamera.transform.position, objectCenter);
            float frustumHeight = 2.0f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            renderCamera.fieldOfView = Mathf.Atan2(objectSize, distance) * Mathf.Rad2Deg * 2f; // Perspektif boyut
        }
    }
}
