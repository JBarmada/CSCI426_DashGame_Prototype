using UnityEngine;

public class ControlsUI : MonoBehaviour
{
    [Header("Position")]
    public float xOffset = 20f;
    public float yOffset = 20f;
    
    [Header("Styling")]
    public Color keyBackgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
    public Color keyBorderColor = new Color(0.4f, 0.4f, 0.5f, 1f);
    public Color keyTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color labelColor = new Color(0.7f, 0.85f, 1f, 1f);
    public Color panelColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
    
    [Header("Sizing")]
    public int keySize = 36;
    public int keySpacing = 4;
    public int sectionSpacing = 24;
    public int fontSize = 14;
    public int labelFontSize = 12;

    private GUIStyle keyStyle;
    private GUIStyle labelStyle;
    private GUIStyle panelStyle;
    private Texture2D keyTex;
    private Texture2D keyBorderTex;
    private Texture2D panelTex;
    private bool stylesInitialized = false;

    void InitStyles()
    {
        if (stylesInitialized) return;
        
        // Create textures for the pixel-art look
        keyTex = MakeTexture(2, 2, keyBackgroundColor);
        keyBorderTex = MakeTexture(2, 2, keyBorderColor);
        panelTex = MakeTexture(2, 2, panelColor);

        // Key style
        keyStyle = new GUIStyle(GUI.skin.box);
        keyStyle.normal.background = keyTex;
        keyStyle.normal.textColor = keyTextColor;
        keyStyle.fontSize = fontSize;
        keyStyle.fontStyle = FontStyle.Bold;
        keyStyle.alignment = TextAnchor.MiddleCenter;
        keyStyle.border = new RectOffset(1, 1, 1, 1);

        // Label style
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = labelColor;
        labelStyle.fontSize = labelFontSize;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        // Panel style
        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTex;
        panelStyle.border = new RectOffset(2, 2, 2, 2);

        stylesInitialized = true;
    }

    Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point; // Pixel-art crisp look
        return tex;
    }

    void OnGUI()
    {
        InitStyles();

        float x = xOffset;
        float y = yOffset;

        // Calculate total width for background panel
        float moveWidth = keySize * 3 + keySpacing * 2;
        float dashWidth = keySize * 3;
        float resetWidth = 50; // Wider for "RESET" label
        float totalWidth = moveWidth + dashWidth + resetWidth + sectionSpacing * 2 + 30;
        float totalHeight = keySize * 2 + keySpacing + 30;

        // Background panel
        GUI.Box(new Rect(x - 10, y - 10, totalWidth, totalHeight + 10), "", panelStyle);

        // === MOVE Section ===
        float sectionX = x;
        
        // "MOVE" label
        GUI.Label(new Rect(sectionX, y, moveWidth, 20), "MOVE", labelStyle);
        y += 24;

        // W key (top center)
        DrawKey(sectionX + keySize + keySpacing, y, "W");
        
        // A, S, D keys (bottom row)
        float bottomY = y + keySize + keySpacing;
        DrawKey(sectionX, bottomY, "A");
        DrawKey(sectionX + keySize + keySpacing, bottomY, "S");
        DrawKey(sectionX + (keySize + keySpacing) * 2, bottomY, "D");

        // === DASH Section ===
        sectionX += moveWidth + sectionSpacing;
        y = yOffset;
        
        // "DASH" label
        GUI.Label(new Rect(sectionX, y, dashWidth, 20), "DASH", labelStyle);
        y += 24;

        // Spacebar (wide key)
        DrawKey(sectionX, y + keySize * 0.3f, "SPACE", dashWidth, keySize * 0.7f);

        // === RESET Section ===
        sectionX += dashWidth + sectionSpacing;
        y = yOffset;
        
        // "RESET" label (wider to fit text)
        float resetLabelWidth = 50;
        GUI.Label(new Rect(sectionX - 7, y, resetLabelWidth, 20), "RESET", labelStyle);
        y += 24;

        // R key
        DrawKey(sectionX, y + keySize * 0.3f, "R");
    }

    void DrawKey(float x, float y, string text, float width = -1, float height = -1)
    {
        if (width < 0) width = keySize;
        if (height < 0) height = keySize;

        // Draw border (slightly larger rect behind)
        GUI.color = keyBorderColor;
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), "", keyStyle);

        // Draw key background
        GUI.color = keyBackgroundColor;
        GUI.Box(new Rect(x, y, width, height), "", keyStyle);

        // Draw highlight (top edge)
        GUI.color = new Color(1f, 1f, 1f, 0.2f);
        GUI.DrawTexture(new Rect(x + 2, y + 2, width - 4, 3), Texture2D.whiteTexture);

        // Draw shadow (bottom edge)
        GUI.color = new Color(0f, 0f, 0f, 0.3f);
        GUI.DrawTexture(new Rect(x + 2, y + height - 4, width - 4, 2), Texture2D.whiteTexture);

        // Draw text
        GUI.color = Color.white;
        GUIStyle textStyle = new GUIStyle(keyStyle);
        textStyle.fontSize = text.Length > 1 ? fontSize - 4 : fontSize;
        textStyle.normal.background = null;
        GUI.Label(new Rect(x, y, width, height), text, textStyle);
    }

    void OnDestroy()
    {
        // Clean up textures
        if (keyTex != null) Destroy(keyTex);
        if (keyBorderTex != null) Destroy(keyBorderTex);
        if (panelTex != null) Destroy(panelTex);
    }
}
