using UnityEngine;
using UnityEngine.UI;

public class ImagePreview : MonoBehaviour
{
	private int initialSize = 4;
	private int maxSize = 2048;

	private RawImage targetImage;
	private Texture2D dynamicTexture;
	private int currentSize;
	private int frameCounter;

	void Start()
	{
		if (targetImage == null) {
			targetImage = GetComponent<RawImage>();
		}
		InitializeTexture();
	}

	//void Update()
	//{
	//	frameCounter++;
	//	const int framesBetweenUpdates = 100;
	//	if (frameCounter >= framesBetweenUpdates) {
	//		UpdateImage();
	//		frameCounter = 0;
	//	}
	//}

	private void InitializeTexture()
	{
		currentSize = initialSize;
		dynamicTexture = new Texture2D(currentSize, currentSize);
		dynamicTexture.filterMode = FilterMode.Point;

		if (targetImage != null) {
			targetImage.texture = dynamicTexture;
		}

		// Generate initial colors
		Color[] initialColors = GenerateRandomColors(currentSize * currentSize);
		UpdateTextureWithColors(initialColors, currentSize);
	}

	public void UpdateTextureWithColors(Color[] colors, int size)
	{
		if (dynamicTexture == null || colors.Length != size * size) {
			Debug.LogError("Color array size doesn't match texture dimensions!");
			return;
		}

		// Resize texture if needed
		if (dynamicTexture.width != size || dynamicTexture.height != size) {
			dynamicTexture.Reinitialize(size, size);
		}

		dynamicTexture.SetPixels(colors);
		dynamicTexture.Apply();

		currentSize = size;
	}

	private void UpdateImage()
	{
		// Determine new size
		int newSize = currentSize;
		if (currentSize * 2 <= maxSize) {
			newSize = currentSize * 2;
		}

		// Generate new colors
		Color[] newColors = GenerateRandomColors(newSize * newSize);
		UpdateTextureWithColors(newColors, newSize);

		Debug.Log($"Image updated! Size: {newSize}x{newSize}");
	}

	private Color[] GenerateRandomColors(int colorCount)
	{
		Color[] colors = new Color[colorCount];

		for (int i = 0; i < colorCount; i++) {
			colors[i] = new Color(
				Random.value,
				Random.value,
				Random.value,
				1f
			);
		}

		return colors;
	}

	// Public method to update with custom colors
	public void UpdateWithCustomColors(Color[] colors, int size)
	{
		if (colors.Length != size * size) {
			Debug.LogError($"Color array length ({colors.Length}) doesn't match size² ({size * size})");
			return;
		}

		UpdateTextureWithColors(colors, size);
	}

	void OnDestroy()
	{
		if (dynamicTexture != null) {
			Destroy(dynamicTexture);
		}
	}
}
