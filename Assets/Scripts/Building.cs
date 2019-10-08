using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Direction { Up, Down, Neutral };
public enum Side { Left, Right };
public enum ElevatorState { Idle, Moving, Waiting, Departing };

struct CallRecord
{
    public int floorIndex;
    public Direction desiredDirection;
}

public class Building : MonoBehaviour
{
    [Range(2, 20)]
    public int numberOfFloors;

    [Range(1, 10)]
    public int numberOfElevators;

    [Range(1, 250)]
    public int maximumTotalMonkeys;

    public float minimumTimeBetweenMonkeys = 0.5f;
    public float maximumTimeBetweenMonkeys = 2f;

    public GameObject ElevatorPrefab;
    public GameObject FloorPrefab;
    public GameObject MonkeyPrefab;

    public Camera MainCamera;
    public Button NormalViewButton;
    public Button CutawayViewButton;
    public Button DecreaseZoomButton;
    public Button IncreaseZoomButton;

    public GameObject FloorSelectorObject;

    List<GameObject> Elevators;
    List<GameObject> Floors;
    List<GameObject> Monkeys;

    List<CallRecord> CallQueue;
    int userCalledFloor = -1;
    Direction userCalledDirection = Direction.Neutral;

    int tilesPerFloor;
    public int TilesPerFloor => tilesPerFloor;
    int topEdge;
    public int TopEdge => topEdge;
    int bottomEdge;
    public int BottomEdge => bottomEdge;
    int leftEdge;
    public int LeftEdge => leftEdge;
    int rightEdge;
    public int RightEdge => rightEdge;

    public static readonly float EPSILON = 0.0000001f;
    public static readonly int TILE_WIDTH = 512;
    public static readonly int TILE_HEIGHT = 512;
    public static readonly int FLOOR_OFFSET = 41;
    public static readonly int MIN_CAMERA_SIZE = 980;
    public static readonly int MID_CAMERA_SIZE = 1960;
    public static readonly int MAX_CAMERA_SIZE = 2940;
    public static readonly int MONKEY_OFFSET = 100;
    public static readonly int MONKEY_HEIGHT = 150;
    public static readonly float DOOR_ANIMATION_DURATION = 1f;
    static readonly int CAMERA_SPEED = 20;

    float monkeySpawnTimer = -1f;

    // Start is called before the first frame update
    void Start()
    {
        Elevators = new List<GameObject>();
        Floors = new List<GameObject>();
        Monkeys = new List<GameObject>();

        CallQueue = new List<CallRecord>();

        // Calculate how many tiles wide the building should be
        if (numberOfElevators == 1) tilesPerFloor = 2;
        else tilesPerFloor = numberOfElevators * 2 - 1;

        // Calculate edges of the building to use as camera movement constraints
        topEdge = numberOfFloors * TILE_HEIGHT - MIN_CAMERA_SIZE;
        bottomEdge = -MIN_CAMERA_SIZE;
        leftEdge = -(tilesPerFloor * TILE_WIDTH) / 2;
        rightEdge = (tilesPerFloor * TILE_WIDTH) / 2;

        // Create elevators
        for (int i = 0; i < numberOfElevators; i++)
        {
            float xPosition = GetXPositionForTileIndex(i * 2);
            Elevators.Add(Instantiate(ElevatorPrefab, transform));
            Elevators[i].transform.localPosition = new Vector3(xPosition, 0, 100);
            Elevators[i].GetComponent<Elevator>().Index = i;
        }

        // Create floors
        for (int i = 0; i < numberOfFloors; i++)
        {
            Floors.Add(Instantiate(FloorPrefab, transform));
            Floors[i].transform.localPosition = new Vector3(0, -MIN_CAMERA_SIZE + TILE_HEIGHT * (i + 0.5f), 0);
            Floors[i].GetComponent<Floor>().Index = i;
        }

        // UI elements set up
        NormalViewButton.interactable = false;

        FloorSelector floorSelectorScript = FloorSelectorObject.GetComponent<FloorSelector>();
        floorSelectorScript.UpdateNumberOfFloors(numberOfFloors);
        floorSelectorScript.AddFloorSelectionCancelledListener(HandleOnFloorSelectionCancelled);
        floorSelectorScript.AddFloorSelectedListener(HandleOnFloorSelected);
        FloorSelectorObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Monkey generation
        if (monkeySpawnTimer <= 0)
        {
            // Only add another monkey if there's room in the building
            if (Monkeys.Count < maximumTotalMonkeys)
            {
                // Monkey should appear on a random floor from a randomly chosen side of the building
                int entryFloor = Random.Range(0, numberOfFloors);
                Side side = (Side)Random.Range(0, 2);
                float xPosition = 0;
                if (side == Side.Left)
                {
                    xPosition = leftEdge - MONKEY_OFFSET;
                }
                else
                {
                    xPosition = rightEdge + MONKEY_OFFSET;
                }

                GameObject monkeyObject = Instantiate(MonkeyPrefab, Floors[entryFloor].transform);
                monkeyObject.transform.localPosition = new Vector3(xPosition, -(TILE_HEIGHT - MONKEY_HEIGHT - FLOOR_OFFSET) / 2, 0);
                Monkey monkeyScript = monkeyObject.GetComponent<Monkey>();
                monkeyScript.Initialize(entryFloor, side == Side.Left ? 0 : tilesPerFloor - 1, side, this);

                Monkeys.Add(monkeyObject);
            }

            // Determine when the next monkey should spawn
            monkeySpawnTimer = Random.Range(0, (maximumTimeBetweenMonkeys - minimumTimeBetweenMonkeys)) + minimumTimeBetweenMonkeys;
        }
        else monkeySpawnTimer -= Time.deltaTime;

        // Camera movement
        float zoomAdjustment = -Input.mouseScrollDelta.y * 5;

        if ((zoomAdjustment < 0 && MainCamera.orthographicSize > MIN_CAMERA_SIZE) || (zoomAdjustment > 0 && MainCamera.orthographicSize < MAX_CAMERA_SIZE))
        {
            if (MainCamera.orthographicSize + zoomAdjustment < MIN_CAMERA_SIZE)
            {
                UpdateCameraSize(MIN_CAMERA_SIZE);
            }
            else if (MainCamera.orthographicSize + zoomAdjustment > MAX_CAMERA_SIZE)
            {
                UpdateCameraSize(MAX_CAMERA_SIZE);
            }
            else
            {
                UpdateCameraSize(MainCamera.orthographicSize + zoomAdjustment);
            }

            DecreaseZoomButton.interactable = MainCamera.orthographicSize > MIN_CAMERA_SIZE;
            IncreaseZoomButton.interactable = MainCamera.orthographicSize < MAX_CAMERA_SIZE;
        }

        float orthographicWidth = (MainCamera.orthographicSize / 9) * 16;
        float xMovement = Input.GetAxis("Horizontal") * CAMERA_SPEED;
        float yMovement = Input.GetAxis("Vertical") * CAMERA_SPEED;

        if (xMovement != 0 || yMovement != 0)
        {
            // Don't allow scrolling past the edges
            if ((xMovement < 0 && MainCamera.transform.localPosition.x - orthographicWidth <= leftEdge) || (xMovement > 0 && MainCamera.transform.localPosition.x + orthographicWidth >= rightEdge))
            {
                xMovement = 0;
            }
            else if (xMovement < 0 && MainCamera.transform.localPosition.x - orthographicWidth + xMovement <= leftEdge)
            {
                xMovement = leftEdge - (MainCamera.transform.localPosition.x - orthographicWidth);
            }
            else if (xMovement > 0 && MainCamera.transform.localPosition.x + orthographicWidth >= rightEdge)
            {
                xMovement = rightEdge - (MainCamera.transform.localPosition.x + orthographicWidth);
            }

            if ((yMovement < 0 && MainCamera.transform.localPosition.y - MainCamera.orthographicSize <= bottomEdge) || (yMovement > 0 && MainCamera.transform.localPosition.y + MainCamera.orthographicSize >= topEdge))
            {
                yMovement = 0;
            }
            else if (yMovement < 0 && MainCamera.transform.localPosition.y - MainCamera.orthographicSize + xMovement <= bottomEdge)
            {
                yMovement = bottomEdge - (MainCamera.transform.localPosition.y - MainCamera.orthographicSize);
            }
            else if (yMovement > 0 && MainCamera.transform.localPosition.y + MainCamera.orthographicSize >= topEdge)
            {
                yMovement = topEdge - (MainCamera.transform.localPosition.y + MainCamera.orthographicSize);
            }

            MainCamera.transform.Translate(xMovement, yMovement, 0);
        }
    }

    // Helper Functions
    public int GetXPositionForTileIndex(int tileIndex)
    {
        if (numberOfElevators == 1)
        {
            if (tileIndex == 0) return -TILE_WIDTH / 2;

            return TILE_WIDTH / 2;
        }

        return TILE_WIDTH * (tileIndex - (tilesPerFloor / 2));
    }

    public int GetWaitingElevatorIndex(int floorIndex, Direction direction)
    {
        foreach (GameObject elevatorObject in Elevators)
        {
            Elevator elevator = elevatorObject.GetComponent<Elevator>();
            if (elevator.State == ElevatorState.Waiting && elevator.CurrentFloor == floorIndex && (elevator.CurrentDirection == direction || elevator.CurrentDirection == Direction.Neutral)) return elevator.Index;
        }

        return -1;
    }

    void UpdateCameraSize(float newSize)
    {
        MainCamera.orthographicSize = newSize;

        float maxY = topEdge - MainCamera.orthographicSize;
        float minY = MainCamera.orthographicSize - MIN_CAMERA_SIZE;

        if (maxY < minY) maxY = minY;   // If building is shorter than the window, have the building's bottom rest on the bottom of the window

        if (MainCamera.transform.localPosition.y < minY) MainCamera.transform.Translate(0, minY - MainCamera.transform.localPosition.y, 0);
        else if (MainCamera.transform.localPosition.y > maxY) MainCamera.transform.Translate(0, maxY - MainCamera.transform.localPosition.y, 0);
    }

    // Elevator Event Handlers
    public void HandleOnElevatorCarArrived(int elevatorIndex, int floorIndex, Direction elevatorDirection)
    {
        Floors[floorIndex].GetComponent<Floor>().HandleOnElevatorCarArrived(elevatorIndex, elevatorDirection);

        foreach (GameObject monkeyObject in Monkeys)
        {
            monkeyObject.GetComponent<Monkey>().HandleOnElevatorCarArrived(elevatorIndex, floorIndex, elevatorDirection);
        }

        if (userCalledFloor == floorIndex && (userCalledDirection == elevatorDirection || elevatorDirection == Direction.Neutral))
        {
            // Clear out record of user input and show the floor selector
            userCalledFloor = -1;
            userCalledDirection = Direction.Neutral;
            Elevators[elevatorIndex].GetComponent<Elevator>().ExpectingInput = true;
            StartCoroutine(ShowFloorSelector(elevatorIndex, floorIndex));
        }
    }

    public void HandleOnElevatorCarDeparting(int elevatorIndex, int floorIndex, Direction elevatorDirection)
    {
        Elevator elevator = Elevators[elevatorIndex].GetComponent<Elevator>();

        Floors[elevator.CurrentFloor].GetComponent<Floor>().HandleOnElevatorCarLeaving(elevatorIndex);
        elevator.Invoke("HandleOnDoorsClosed", DOOR_ANIMATION_DURATION);
    }

    public void HandleOnElevatorIdle(int elevatorIndex)
    {
        if (CallQueue.Count > 0)
        {
            Elevators[elevatorIndex].GetComponent<Elevator>().SendToFloor(CallQueue[0].floorIndex, CallQueue[0].desiredDirection);
            CallQueue.RemoveAt(0);
        }
    }

    public void HandleOnCallButtonPressed(int floorIndex, Direction direction, bool userInput)
    {
        if (userInput)
        {
            // Note what floor the user hit the call button on and which direction they selected so we know when to show the floor selector
            userCalledFloor = floorIndex;
            userCalledDirection = direction;
        }

        bool elevatorSent = false;

        // Figure out which floors have an elevator that isn't in use on them
        int[] idleElevators = new int[numberOfFloors];
        for (int i = 0; i < numberOfFloors; i++) idleElevators[i] = -1;

        foreach (GameObject elevatorObject in Elevators)
        {
            Elevator elevator = elevatorObject.GetComponent<Elevator>();
            if (elevator.State == ElevatorState.Idle && idleElevators[elevator.CurrentFloor] == -1)  // Only need to record the index of the first idle elevator we find per floor
            {
                idleElevators[elevator.CurrentFloor] = elevator.Index;
            }
        }

        // Try to find an elevator that isn't in use to send, starting at the target floor and expanding the search to adjacent floors from there
        int searchDistance = 0;

        while (!elevatorSent && (floorIndex + searchDistance < numberOfFloors || floorIndex - searchDistance > -1))
        {
            if (floorIndex + searchDistance < numberOfFloors && idleElevators[floorIndex + searchDistance] > -1)
            {
                Elevators[idleElevators[floorIndex + searchDistance]].GetComponent<Elevator>().SendToFloor(floorIndex, direction);
                elevatorSent = true;
            }
            else if (floorIndex - searchDistance > -1 && idleElevators[floorIndex - searchDistance] > -1)
            {
                Elevators[idleElevators[floorIndex - searchDistance]].GetComponent<Elevator>().SendToFloor(floorIndex, direction);
                elevatorSent = true;
            }

            searchDistance++;
        }

        if (!elevatorSent)  // No empty elevators to send
        {
            // Try to find an in use elevator that is going in the right direction and will pass this floor
            foreach (GameObject elevatorObject in Elevators)
            {
                Elevator elevator = elevatorObject.GetComponent<Elevator>();
                if (elevator.CurrentDirection == direction && ((direction == Direction.Up && elevator.CurrentFloor < floorIndex) || (direction == Direction.Down && elevator.CurrentFloor > floorIndex))) elevator.SendToFloor(floorIndex, direction);
            }
        }

        if (!elevatorSent)
        {
            // No available elevator to send, add the call to a queue to be handled once an elevator becomes free
            CallRecord record = new CallRecord();
            record.floorIndex = floorIndex;
            record.desiredDirection = direction;
            CallQueue.Add(record);
        }
    }

    public void HandleOnFloorSelectionCancelled(int elevatorIndex, int floorIndex)
    {
        Elevators[elevatorIndex].GetComponent<Elevator>().ExpectingInput = false;
    }

    public void HandleOnFloorSelected(int elevatorIndex, int floorIndex)
    {
        Elevator elevator = Elevators[elevatorIndex].GetComponent<Elevator>();
        elevator.SendToFloor(floorIndex, elevator.CurrentFloor < floorIndex ? Direction.Up : Direction.Down);
        elevator.GetComponent<Elevator>().ExpectingInput = false;
    }

    // Monkey Event Handlers
    public void HandleOnMonkeyLeftBuilding(GameObject monkey)
    {
        Monkeys.Remove(monkey);
        Destroy(monkey);
    }

    public void HandleOnMonkeyEnteringElevator(int elevatorIndex)
    {
        Elevators[elevatorIndex].GetComponent<Elevator>().ExpectedPassengerTotal++;
    }

    public void HandleOnMonkeyEnteredElevator(GameObject monkey, int elevatorIndex, int floorIndex)
    {
        Elevator elevator = Elevators[elevatorIndex].GetComponent<Elevator>();

        elevator.PutObjectInCar(monkey);
        monkey.transform.Translate(0, 0, -(monkey.transform.localPosition.z + 1));
        elevator.CurrentPassengerTotal++;
        elevator.AddFloorToStops(floorIndex);
    }

    public void HandleOnMonkeyLeftElevator(GameObject monkey, int elevatorIndex, int floorIndex)
    {
        monkey.transform.parent = Floors[floorIndex].transform;
        monkey.transform.Translate(0, 0, -(monkey.transform.localPosition.z));

        Elevator elevator = Elevators[elevatorIndex].GetComponent<Elevator>();

        elevator.CurrentPassengerTotal--;
        elevator.ExpectedPassengerTotal--;
    }

    // UI Event Handlers
    public void HandleOnClickNormalView()
    {
        NormalViewButton.interactable = false;
        CutawayViewButton.interactable = true;
        foreach (GameObject floor in Floors)
        {
            floor.transform.Translate(0, 0, 100);
        }
    }

    public void HandleOnClickCutawayView()
    {
        CutawayViewButton.interactable = false;
        NormalViewButton.interactable = true;
        foreach (GameObject floor in Floors)
        {
            floor.transform.Translate(0, 0, -100);
        }
    }

    public void HandleOnClickZoomButton(bool increase)
    {
        if (increase)
        {
            if (MainCamera.orthographicSize >= MID_CAMERA_SIZE)
            {
                UpdateCameraSize(MAX_CAMERA_SIZE);
                IncreaseZoomButton.interactable = false;
            }
            else
            {
                UpdateCameraSize(MID_CAMERA_SIZE);
                DecreaseZoomButton.interactable = true;
            }
        }
        else
        {
            if (MainCamera.orthographicSize <= MID_CAMERA_SIZE)
            {
                UpdateCameraSize(MIN_CAMERA_SIZE);
                DecreaseZoomButton.interactable = false;
            }
            else
            {
                UpdateCameraSize(MID_CAMERA_SIZE);
                IncreaseZoomButton.interactable = true;
            }
        }
    }

    IEnumerator ShowFloorSelector(int elevatorIndex, int floorIndex)
    {
        yield return new WaitForSeconds(DOOR_ANIMATION_DURATION);

        if (!FloorSelectorObject.activeInHierarchy)   // Don't try to show the floor selector if it's already open
        {
            FloorSelectorObject.SetActive(true);
            FloorSelectorObject.GetComponent<FloorSelector>().UpdateCurrentFloorAndElevator(elevatorIndex, floorIndex);
        }
    }
}
