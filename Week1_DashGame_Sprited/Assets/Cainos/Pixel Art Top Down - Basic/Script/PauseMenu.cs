using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    /// <summary>
    /// Simple pause menu that appears when pressing Escape.
    /// Shows a Quit button to exit the game.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Styling")]
        public Color panelColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        public Color buttonColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        public Color buttonHoverColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        public Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        
        [Header("Sizing")]
        public int panelWidth = 200;
        public int panelHeight = 120;
        public int buttonWidth = 150;
        public int buttonHeight = 40;
        public int fontSize = 18;
        public int titleFontSize = 22;

        private bool isPaused = false;
        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle titleStyle;
        private Texture2D panelTex;
        private Texture2D buttonTex;
        private Texture2D buttonHoverTex;
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

        void InitStyles()
        {
            if (stylesInitialized) return;

            // Create textures
            panelTex = MakeTexture(2, 2, panelColor);
            buttonTex = MakeTexture(2, 2, buttonColor);
            buttonHoverTex = MakeTexture(2, 2, buttonHoverColor);

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

            // Center the panel on screen
            float panelX = (Screen.width - panelWidth) / 2f;
            float panelY = (Screen.height - panelHeight) / 2f;

            // Draw panel background
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "", panelStyle);

            // Draw title
            GUI.Label(new Rect(panelX, panelY + 15, panelWidth, 30), "PAUSED", titleStyle);

            // Draw Quit button
            float buttonX = panelX + (panelWidth - buttonWidth) / 2f;
            float buttonY = panelY + 55;

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
            
            // Make sure time scale is restored if this object is destroyed
            Time.timeScale = 1f;
        }
    }
}
