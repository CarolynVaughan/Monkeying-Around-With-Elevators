using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CallButtonEvent : UnityEvent<Direction, bool>
{
}

public class CallButtonController : MonoBehaviour
{
    public GameObject DownButton;
    public GameObject UpButton;

    CallButtonEvent OnCallButtonPressed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCallButtonPressedListener(UnityAction<Direction, bool> listener)
    {
        if (OnCallButtonPressed == null) OnCallButtonPressed = new CallButtonEvent();
        OnCallButtonPressed.AddListener(listener);
    }

    // Functions that update button state
    public void ClearCallButton(Direction buttonDirection)
    {
        if (buttonDirection == Direction.Down && DownButton != null)
        {
            DownButton.GetComponent<CallButton>().Selected = false;
            DownButton.GetComponent<CallButton>().Enabled = true;
        }
        else if (buttonDirection == Direction.Up && UpButton != null)
        {
            UpButton.GetComponent<CallButton>().Selected = false;
            UpButton.GetComponent<CallButton>().Enabled = true;
        }
        else
        {
            // Elevator isn't going in a particular direction so clear both buttons
            if (DownButton != null)
            {
                DownButton.GetComponent<CallButton>().Selected = false;
                DownButton.GetComponent<CallButton>().Enabled = true;
            }

            if (UpButton != null)
            {
                UpButton.GetComponent<CallButton>().Selected = false;
                UpButton.GetComponent<CallButton>().Enabled = true;
            }
        }
    }

    public void SelectDirection(Direction buttonDirection)
    {
        if (buttonDirection == Direction.Down && DownButton != null)
        {
            DownButton.GetComponent<CallButton>().Selected = true;
            DownButton.GetComponent<CallButton>().Enabled = false;
        }
        else if (buttonDirection == Direction.Up && UpButton != null)
        {
            UpButton.GetComponent<CallButton>().Selected = true;
            UpButton.GetComponent<CallButton>().Enabled = false;
        }
    }

    // Event Handlers
    public void HandleOnButtonPress(Direction buttonDirection)
    {
        OnCallButtonPressed.Invoke(buttonDirection, true);
    }
}
