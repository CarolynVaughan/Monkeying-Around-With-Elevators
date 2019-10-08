using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ElevatorCarEvent : UnityEvent<int, int, Direction>
{
}

enum MovementPhase { Stopped, SpeedingUp, Steady, SlowingDown }

public class Elevator : MonoBehaviour
{
    public GameObject ShaftTilePrefab;
    public GameObject ElevatorCarPrefab;

    ElevatorCarEvent OnElevatorCarArrived;
    ElevatorCarEvent OnElevatorCarDeparting;

    int index;
    public int Index { get => index; set => index = value; }

    ElevatorState state;
    public ElevatorState State => state;

    bool expectingInput;
    public bool ExpectingInput { get => expectingInput; set => expectingInput = value; }

    int currentFloor = 0;
    public int CurrentFloor => currentFloor;
    Direction currentDirection;
    public Direction CurrentDirection => currentDirection;
    int currentPassengerTotal = 0;
    public int CurrentPassengerTotal { get => currentPassengerTotal; set => currentPassengerTotal = value; }
    int expectedPassengerTotal = 0;
    public int ExpectedPassengerTotal { get => expectedPassengerTotal; set => expectedPassengerTotal = value; }

    List<GameObject> Tiles;
    GameObject Car;
    List<int> Stops;

    static readonly int MINIMUM_SPEED = -10;
    static readonly int MAX_SPEED = 20;

    MovementPhase currentPhase = MovementPhase.Stopped;
    int velocity;

    float waitingTimer;

    // Start is called before the first frame update
    void Start()
    {
        Stops = new List<int>();
        Tiles = new List<GameObject>();

        Building parentBuilding = GetComponentInParent<Building>();

        // Add listeners to events
        OnElevatorCarArrived = new ElevatorCarEvent();
        OnElevatorCarArrived.AddListener(parentBuilding.HandleOnElevatorCarArrived);

        OnElevatorCarDeparting = new ElevatorCarEvent();
        OnElevatorCarDeparting.AddListener(parentBuilding.HandleOnElevatorCarDeparting);

        // Build elevator shaft out of tiles
        for (int i = 0; i < parentBuilding.numberOfFloors; i++)
        {
            Tiles.Add(Instantiate(ShaftTilePrefab, transform));
            Tiles[i].transform.localPosition = new Vector3(0, -Building.MIN_CAMERA_SIZE + Building.TILE_HEIGHT * (i + 0.5f), 1);
        }

        // Create elevator car
        Car = Instantiate(ElevatorCarPrefab, transform);
        Car.transform.localPosition = new Vector3(0, -Building.MIN_CAMERA_SIZE + Building.TILE_HEIGHT * 0.5f + Building.FLOOR_OFFSET, 0);
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case ElevatorState.Moving:
                switch (currentPhase)
                {
                    case MovementPhase.Stopped:
                        // If we're in the moving state but we're in the stopped movement phase, that means we're just starting to move, so set initial velocity and go to the next phase
                        velocity = MINIMUM_SPEED;
                        currentPhase = MovementPhase.SpeedingUp;
                        break;
                    case MovementPhase.SpeedingUp:
                        // Speed up until we hit our max speed
                        velocity += 2;

                        if (velocity >= MAX_SPEED)
                        {
                            velocity = MAX_SPEED;
                            currentPhase = MovementPhase.Steady;
                        }
                        break;
                    case MovementPhase.SlowingDown:
                        // Halve the distance between the car and its final position in each frame until it's one pixel away, then finish on the next frame
                        if (currentDirection == Direction.Up)
                        {
                            float difference = Car.transform.localPosition.y - (Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET);

                            if (difference <= 1) velocity = -(int)difference;
                            else velocity = -(int)(difference / 2);
                        }
                        else if (currentDirection == Direction.Down)
                        {
                            float difference = (Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET) - Car.transform.localPosition.y;

                            if (difference <= 1) velocity = -(int)difference;
                            else velocity = -(int)(difference / 2);
                        }
                        break;
                }

                Car.transform.Translate(0, currentDirection == Direction.Up ? velocity : -velocity, 0);

                if (currentPhase == MovementPhase.Steady)
                {
                    // If we've passed the final position for the car, start moving back towards it
                    if (currentDirection == Direction.Up)
                    {
                        if (Car.transform.localPosition.y > Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET + Building.TILE_HEIGHT * 0.5f) currentFloor++;

                        if (currentFloor == Stops[0] && Car.transform.localPosition.y >= Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET)
                        {
                            currentPhase = MovementPhase.SlowingDown;
                        }
                    }
                    else
                    {
                        if (Car.transform.localPosition.y < Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET - Building.TILE_HEIGHT * 0.5f) currentFloor--;

                        if (currentFloor == Stops[0] && Car.transform.localPosition.y <= Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET)
                        {
                            currentPhase = MovementPhase.SlowingDown;
                        }
                    }
                }
                else if (currentPhase == MovementPhase.SlowingDown)
                {
                    if (System.Math.Abs(Car.transform.localPosition.y - (Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET)) < Building.EPSILON) StopElevator();
                }
                break;
            case ElevatorState.Waiting:
                waitingTimer -= Time.deltaTime;

                if ((expectedPassengerTotal == 0 || currentPassengerTotal == expectedPassengerTotal) && !expectingInput && waitingTimer <= 0)
                {
                    state = ElevatorState.Departing;
                    OnElevatorCarDeparting.Invoke(index, currentFloor, currentDirection);
                }
                break;
        }
    }

    // Helper Functions
    void StopElevator()
    {
        // Update state, position, and moving info
        state = ElevatorState.Waiting;
        waitingTimer = 3f;
        currentPhase = MovementPhase.Stopped;
        Car.transform.localPosition.Set(Car.transform.localPosition.x, Tiles[currentFloor].transform.localPosition.y + Building.FLOOR_OFFSET, Car.transform.localPosition.z);

        // Update next destination (if any) and direction
        if (Stops[0] == currentFloor) Stops.RemoveAt(0);
        if (Stops.Count > 0) currentDirection = Stops[0] > currentFloor ? Direction.Up : Direction.Down;
        else currentDirection = Direction.Neutral;

        OnElevatorCarArrived.Invoke(index, currentFloor, currentDirection);
    }

    public void AddFloorToStops(int floorIndex)
    {
        if (Stops.Count == 0)
        {
            // If the elevator doesn't have any other stops to make, make the provided floor the next stop and start moving towards it if we're not waiting for passengers
            Stops.Add(floorIndex);
            if (floorIndex > currentFloor) currentDirection = Direction.Up;
            else currentDirection = Direction.Down;
            if (state == ElevatorState.Idle) state = ElevatorState.Moving;
        }
        else
        {
            // Only add the floor to list of floors to stop on if we're not already going to be stopping on that floor.
            if (Stops.IndexOf(floorIndex) == -1)
            {
                bool inserted = false;

                if (floorIndex > currentFloor)
                {
                    // Insert the new stop before any stops that come after it (are on a higher floor than it)
                    for (int i = 0; i < Stops.Count; i++)
                    {
                        if (Stops[i] > floorIndex)
                        {
                            Stops.Insert(i, floorIndex);
                            inserted = true;
                            break;
                        }
                    }
                }
                else
                {
                    // Insert the new stop before any stops that come after it (are on a lower floor than it)
                    for (int i = 0; i < Stops.Count; i++)
                    {
                        if (Stops[i] < floorIndex)
                        {
                            Stops.Insert(i, floorIndex);
                            inserted = true;
                            break;
                        }
                    }
                }

                // Make sure to add the new stop at the end if we didn't insert it already
                if (!inserted) Stops.Add(floorIndex);
            }
        }
    }

    public void PutObjectInCar(GameObject gameObject)
    {
        gameObject.transform.parent = Car.transform;
    }

    // Event Handlers
    public void SendToFloor(int floorIndex, Direction desiredDirection)
    {
        if (floorIndex == currentFloor)
        {
            if (state == ElevatorState.Idle)
            {
                currentDirection = desiredDirection;
                state = ElevatorState.Waiting;
                OnElevatorCarArrived.Invoke(index, currentFloor, currentDirection);
            }
            else if (state == ElevatorState.Moving)
            {
                AddFloorToStops(floorIndex);
            }
        }
        else AddFloorToStops(floorIndex);
    }

    public void HandleOnDoorsClosed()
    {
        if (Stops.Count > 0)
        {
            if (Stops[0] > currentFloor) currentDirection = Direction.Up;
            else currentDirection = Direction.Down;
            state = ElevatorState.Moving;
        }
        else
        {
            currentDirection = Direction.Neutral;
            state = ElevatorState.Idle;
            GetComponentInParent<Building>().HandleOnElevatorIdle(index);
        }
    }
}
