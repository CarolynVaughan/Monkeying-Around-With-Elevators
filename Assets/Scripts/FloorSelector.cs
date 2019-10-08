using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class FloorSelectorEvent : UnityEvent<int, int>
{
}

public class FloorSelector : MonoBehaviour
{
    public List<GameObject> FloorButtons;

    int currentFloor = 0;
    int currentElevator = 0;

    FloorSelectorEvent OnFloorSelected;
    FloorSelectorEvent OnFloorSelectionCancelled;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Data Update Functions
    public void UpdateCurrentFloorAndElevator(int elevatorIndex, int floorIndex)
    {
        if (currentFloor != floorIndex)
        {
            FloorButtons[currentFloor].GetComponent<Button>().interactable = true;
            FloorButtons[floorIndex].GetComponent<Button>().interactable = false;
            currentFloor = floorIndex;
        }

        currentElevator = elevatorIndex;
    }

    public void UpdateNumberOfFloors(int numberOfFloors)
    {
        for (var i = 0; i < FloorButtons.Count; i++)
        {
            FloorButtons[i].GetComponent<Button>().interactable = i < numberOfFloors && i != currentFloor;
        }
    }

    // Functions to add listeners to events
    public void AddFloorSelectedListener(UnityAction<int, int> listener)
    {
        if (OnFloorSelected == null) OnFloorSelected = new FloorSelectorEvent();
        OnFloorSelected.AddListener(listener);
    }

    public void AddFloorSelectionCancelledListener(UnityAction<int, int> listener)
    {
        if (OnFloorSelectionCancelled == null) OnFloorSelectionCancelled = new FloorSelectorEvent();
        OnFloorSelectionCancelled.AddListener(listener);
    }

    // Event Handlers
    public void HandleOnCancelButtonPressed()
    {
        gameObject.SetActive(false);
        OnFloorSelectionCancelled.Invoke(currentElevator, currentFloor);
    }

    public void HandleOnFloorButtonPressed(int floorIndex)
    {
        gameObject.SetActive(false);
        OnFloorSelected.Invoke(currentElevator, floorIndex);
    }
}
