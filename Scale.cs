using UnityEngine;

public class Scale {
    static Vector2 DEFAULT_DIMENSION = new Vector2(1366.0f, 768.0f);
    public static Vector2 GetScaledSize(Vector2 origin) {
        return new Vector2(GetScaledWidth(origin.x), GetScaledHeight(origin.y));
    }

    public static float GetScaledWidth(float origin) {
        return (Screen.safeArea.width * origin) / DEFAULT_DIMENSION.x;
    }

    public static float GetScaledHeight(float origin) {
        return (Screen.safeArea.height * origin) / DEFAULT_DIMENSION.y;
    }
}