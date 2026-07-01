using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RetroNoiseOverlay : MonoBehaviour
{
    [Header("Cài đặt Nhiễu (Noise)")]
    [Range(0f, 1f)]
    [Tooltip("Độ đậm nhạt của hạt nhiễu (0.1 đến 0.3 là đẹp nhất)")]
    public float noiseIntensity = 0.15f;
    
    [Tooltip("Kích thước hạt nhiễu (Càng nhỏ hạt càng to, ví dụ: 64, 128)")]
    public int textureSize = 128;

    [Tooltip("Tốc độ chớp giật (Frames Per Second)")]
    public float fps = 24f;

    private RawImage rawImage;
    private Texture2D noiseTexture;
    private float timer = 0f;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        rawImage.raycastTarget = false; // Xuyên chuột qua
        
        GenerateNoiseTexture();
        UpdateColor();
    }

    void GenerateNoiseTexture()
    {
        noiseTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        noiseTexture.filterMode = FilterMode.Point; // Hạt vuông vức, đậm chất PS1/VHS
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            float val = Random.value;
            pixels[i] = new Color(val, val, val, 1f); // Ảnh xám trắng đen
        }
        
        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
        
        rawImage.texture = noiseTexture;
    }

    void Update()
    {
        UpdateColor();

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            float offsetX = Random.Range(0f, 1f);
            float offsetY = Random.Range(0f, 1f);
            
            float tilingX = Screen.width / (float)textureSize;
            float tilingY = Screen.height / (float)textureSize;
            
            rawImage.uvRect = new Rect(offsetX, offsetY, tilingX, tilingY);
        }
    }

    void UpdateColor()
    {
        if (rawImage != null)
        {
            Color c = rawImage.color;
            c.a = noiseIntensity;
            rawImage.color = c;
        }
    }
}
