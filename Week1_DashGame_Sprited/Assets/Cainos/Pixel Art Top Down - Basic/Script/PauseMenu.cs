using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    /// <summary>
    /// Simple pause menu that appears when pressing Escape.
    /// Shows a volume slider and Quit button.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Styling")]
        public Color panelColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        public Color buttonColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        public Color buttonHoverColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        public Color sliderBackgroundColor = new Color(0.15f, 0.15f, 0.2f, 1f);
        public Color sliderFillColor = new Color(0.4f, 0.6f, 0.9f, 1f);
        public Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        public Color labelColor = new Color(0.7f, 0.85f, 1f, 1f);
        
        [Header("Sizing")]
        public int panelWidth = 220;
        public int panelHeight = 180;
        public int buttonWidth = 150;
        public int buttonHeight = 40;
        public int sliderWidth = 150;
        public int sliderHeight = 20;
        public int fontSize = 18;
        public int titleFontSize = 22;
        public int labelFontSize = 14;

        private bool isPaused = false;
        private float masterVolume = 1f;
        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle sliderBackgroundStyle;
        private GUIStyle sliderThumbStyle;
        private Texture2D panelTex;
        private Texture2D buttonTex;
        private Texture2D buttonHoverTex;
        private Texture2D sliderBackgroundTex;
        private Texture2D sliderFillTex;
        private Texture2D sliderThumbTex;
        private bool stylesInitialized = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        void TogglePause()
        {
            isPaused = !isPaused;
            
            // Pause/unpause the game
            Time.timeScale = isPaused ? 0f : 1f;
        }

        void Start()
        {
            // Initialize slider position from current AudioListener setting
            // Since we use squared curve, take square root to get slider position
            masterVolume = Mathf.Sqrt(AudioListener.volume);
        }

        void InitStyles()
        {
            if (stylesInitialized) return;

            // Create textures
            panelTex = MakeTexture(2, 2, panelColor);
            buttonTex = MakeTexture(2, 2, buttonColor);
            buttonHoverTex = MakeTexture(2, 2, buttonHoverColor);
            sliderBackgroundTex = MakeTexture(2, 2, sliderBackgroundColor);
            sliderFillTex = MakeTexture(2, 2, sliderFillColor);
            sliderThumbTex = MakeTexture(2, 2, textColor);

            // Panel style
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTex;
            panelStyle.border = new RectOffset(2, 2, 2, 2);

            // Button style
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = buttonTex;
            buttonStyle.hover.background = buttonHoverTex;
            buttonStyle.active.background = buttonHoverTex;
            buttonStyle.normal.textColor = textColor;
            buttonStyle.hover.textColor = textColor;
            buttonStyle.active.textColor = textColor;
            buttonStyle.fontSize = fontSize;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            // Title style
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.normal.textColor = textColor;
            titleStyle.fontSize = titleFontSize;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            // Label style
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = labelColor;
            labelStyle.fontSize = labelFontSize;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            // Slider styles
            sliderBackgroundStyle = new GUIStyle();
            sliderBackgroundStyle.normal.background = sliderBackgroundTex;

            sliderThumbStyle = new GUIStyle();
            sliderThumbStyle.normal.background = sliderThumbTex;
            sliderThumbStyle.fixedWidth = 12;
            sliderThumbStyle.fixedHeight = sliderHeight + 4;

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
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        void OnGUI()
        {
            if (!isPaused) return;

            InitStyles();

            // Calculate content layout first to determine panel height
            float contentStartY = 15;  // Top padding
            float titleHeight = 30;
            float volumeLabelY = 55;
            float volumeLabelHeight = 20;
            float sliderOffsetY = volumeLabelY + 25;
            float buttonOffsetY = sliderOffsetY + sliderHeight + 20;
            float bottomPadding = 15;
            
            // Calculate actual panel height needed to fit all content
            float actualPanelHeight = buttonOffsetY + buttonHeight + bottomPadding;

            // Center the panel on screen
            float panelX = (Screen.width - panelWidth) / 2f;
            float panelY = (Screen.height - actualPanelHeight) / 2f;

            // Draw panel background (now covers everything)
            GUI.Box(new Rect(panelX, panelY, panelWidth, actualPanelHeight), "", panelStyle);

            // Draw title
            GUI.Label(new Rect(panelX, panelY + contentStartY, panelWidth, titleHeight), "PAUSED", titleStyle);

            // === Volume Slider ===
            float sliderX = panelX + (panelWidth - sliderWidth) / 2f;
            float sliderY = panelY + sliderOffsetY;

            // Volume label with percentage
            int volumePercent = Mathf.RoundToInt(masterVolume * 100);
            GUI.Label(new Rect(panelX, panelY + volumeLabelY, panelWidth, volumeLabelHeight), $"VOLUME: {volumePercent}%", labelStyle);

            // Draw slider background
            GUI.Box(new Rect(sliderX, sliderY, sliderWidth, sliderHeight), "", sliderBackgroundStyle);

            // Draw filled portion
            float fillWidth = sliderWidth * masterVolume;
            if (fillWidth > 0)
            {
                GUI.color = sliderFillColor;
                GUI.DrawTexture(new Rect(sliderX, sliderY, fillWidth, sliderHeight), sliderFillTex);
                GUI.color = Color.white;
            }

            // Draw the slider (invisible, just for interaction)
            masterVolume = GUI.HorizontalSlider(
                new Rect(sliderX, sliderY, sliderWidth, sliderHeight),
                masterVolume,
                0f,
                1f,
                GUIStyle.none,
                sliderThumbStyle
            );

            // Apply volume change (squared for logarithmic feel - matches human hearing perception)
            AudioListener.volume = masterVolume * masterVolume;

            // === Quit Button ===
            float buttonX = panelX + (panelWidth - buttonWidth) / 2f;
            float buttonY = panelY + buttonOffsetY;

            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "QUIT", buttonStyle))
            {
                QuitGame();
            }
        }

        void QuitGame()
        {
            // Restore time scale before quitting
            Time.timeScale = 1f;

            #if UNITY_EDITOR
            // Stop playing in the editor
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            // Quit the built application
            Application.Quit();
            #endif
        }

        void OnDestroy()
        {
            // Clean up textures
            if (panelTex != null) Destroy(panelTex);
            if (buttonTex != null) Destroy(buttonTex);
            if (buttonHoverTex != null) Destroy(buttonHoverTex);
            if (sliderBackgroundTex != null) Destroy(sliderBackgroundTex);
            if (sliderFillTex != null) Destroy(sliderFillTex);
            if (sliderThumbTex != null) Destroy(sliderThumbTex);
            
            // Make sure time scale is restored if this object is destroyed
            Time.timeScale = 1f;
        }
    }
}
