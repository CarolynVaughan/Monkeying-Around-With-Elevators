using System;
using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    public GameObject ElevatorTilePrefab;
    public GameObject WallTilePrefab;
    public GameObject DoorPrefab;
    public GameObject CallButtonsPrefab;

    int index;
    public int Index { get => index; set => index = value; }

    List<GameObject> CallButtons;
    List<GameObject> Doors;
    List<GameObject> Tiles;

    bool calledElevatorUp;
    bool calledElevatorDown;

    static readonly int DOOR_OFFSET = -31;

    // Start is called before the first frame update
    void Start()
    {
        CallButtons = new List<GameObject>();
        Doors = new List<GameObject>();
        Tiles = new List<GameObject>();

        Building parentBuilding = GetComponentInParent<Building>();

        // Build floor out of alternating elevator and wall tiles
        for (int i = 0; i < parentBuilding.TilesPerFloor; i += 2)
        {
            float xPosition = parentBuilding.GetXPositionForTileIndex(i);
            Tiles.Add(Instantiate(ElevatorTilePrefab, transform));
            Tiles[i].transform.localPosition = new Vector3(xPosition, 0, 50);

            Doors.Add(Instantiate(DoorPrefab, Tiles[i].transform));
            Doors[Doors.Count - 1].transform.localPosition = new Vector3(0, DOOR_OFFSET, 1);

            if (i + 1 < parentBuilding.TilesPerFloor)
            {
                xPosition += Building.TILE_WIDTH;
                Tiles.Add(Instantiate(WallTilePrefab, transform));
                Tiles[i + 1].transform.localPosition = new Vector3(xPosition, 0, 50);
                Tiles[i + 1].GetComponentInChildren<TMPro.TextMeshPro>().text = Convert.ToString(index + 1);

                CallButtons.Add(Instantiate(CallButtonsPrefab, Tiles[i + 1].transform));
                CallButtonController buttonController = CallButtons[CallButtons.Count - 1].GetComponent<CallButtonController>();
                buttonController.AddCallButtonPressedListener(HandleOnCallButtonPressed);

                if (index == 0)
                {
                    Destroy(buttonController.DownButton);
                    buttonController.DownButton = null;
                }
                else if (index == parentBuilding.numberOfFloors - 1)
                {
                    Destroy(buttonController.UpButton);
                    buttonController.UpButton = null;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Helper Functions
    public bool CalledElevator(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return calledElevatorUp;
            case Direction.Down: return calledElevatorDown;
            default: return false;
        }
    }

    // Elevator Event Handlers
    public void HandleOnCallButtonPressed(Direction direction, bool userInput = false)
    {
        if (direction == Direction.Up) calledElevatorUp = true;
        else if (direction == Direction.Down) calledElevatorDown = true;

        foreach (GameObject callButtonObject in CallButtons)
        {
            callButtonObject.GetComponent<CallButtonController>().SelectDirection(direction);
        }

        GetComponentInParent<Building>().HandleOnCallButtonPressed(index, direction, userInput);
    }

    public void HandleOnElevatorCarArrived(int elevatorIndex, Direction elevatorDirection)
    {
        switch (elevatorDirection)
        {
            case Direction.Up: calledElevatorUp = false; break;
            case Direction.Down: calledElevatorDown = false; break;
            case Direction.Neutral: calledElevatorUp = false; calledElevatorDown = false; break;
        }

        Doors[elevatorIndex].GetComponent<Animator>().SetTrigger("Open");
        Doors[elevatorIndex].GetComponentInChildren<ParticleSystem>().Play();

        foreach (GameObject callButtonObject in CallButtons)
        {
            callButtonObject.GetComponent<CallButtonController>().ClearCallButton(elevatorDirection);
        }
    }

    public void HandleOnElevatorCarLeaving(int elevatorIndex)
    {
        Doors[elevatorIndex].GetComponent<Animator>().SetTrigger("Close");
    }
}
