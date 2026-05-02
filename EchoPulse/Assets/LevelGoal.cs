using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    public string completionMessage = "Exit reached";
    public bool showCompletionMessage = true;
    public bool isComplete;

    private Font guiFont;
    private Texture2D backgroundTexture;

    public bool IsComplete
    {
        get { return isComplete; }
    }

    public void CompleteGoal()
    {
        isComplete = true;
    }

    private void Awake()
    {
        guiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        backgroundTexture = Texture2D.whiteTexture;
    }

    private void OnGUI()
    {
        if (!showCompletionMessage || !isComplete || guiFont == null)
        {
            return;
        }

        const int width = 360;
        const int height = 64;
        Rect rect = new Rect((Screen.width - width) * 0.5f, 32f, width, height);

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0.05f, 0.04f, 0.82f);
        GUI.DrawTexture(rect, backgroundTexture);
        GUI.color = oldColor;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.font = guiFont;
        style.fontSize = 28;
        style.normal.textColor = new Color(0.65f, 1f, 0.78f, 1f);

        GUI.Label(rect, completionMessage, style);
    }
}
