using UnityEngine;

public class CallButton : MonoBehaviour
{
    public Direction buttonDirection;
    public Sprite normalSprite;
    public Sprite selectedSprite;

    bool selected;
    public bool Selected
    {
        get => selected; set
        {
            selected = value;

            if (value) GetComponent<SpriteRenderer>().sprite = selectedSprite;
            else GetComponent<SpriteRenderer>().sprite = normalSprite;
        }
    }

    bool isEnabled;
    public bool Enabled { get => isEnabled; set => isEnabled = value; }

    // Start is called before the first frame update
    void Start()
    {
        isEnabled = true;
        selected = false;
    }

    // Event Handlers
    void OnMouseDown()
    {
        if (isEnabled) GetComponentInParent<CallButtonController>().HandleOnButtonPress(buttonDirection);
    }
}
