using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public Texture2D cursorArrow;

    // Start is called before the first frame update
    void Awake()
    {
        Cursor.SetCursor(cursorArrow, Vector2.zero, CursorMode.ForceSoftware);
        Cursor.visible = true;
    }
}
