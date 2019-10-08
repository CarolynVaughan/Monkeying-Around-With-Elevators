using System;
using UnityEngine;

public enum MonkeyState { Idle, Moving, CallingElevator, WaitingForElevator, EnteringElevator, RidingElevator, WaitingForDoors, LeavingElevator };

public class Monkey : MonoBehaviour
{
    public TMPro.TextMeshPro textMesh;

    int currentFloor = 0;
    public int CurrentFloor { get => currentFloor; set => currentFloor = value; }

    int currentTile = 0;

    MonkeyState state = MonkeyState.Idle;
    public MonkeyState State { get => state; set => ChangeState(value); }

    int destinationFloor;
    int destinationElevator;
    float destinationX;
    float destinationY;
    float idleTimer = 0;

    Building parentBuilding;

    static readonly int MOVE_SPEED = 10;
    static readonly int BUTTON_OFFSET = 50;
    static readonly int ELEVATOR_OFFSET = 50;
    static readonly float JUMP_ANIMATION_DURATION = 0.333333f;

    public void Initialize(int floorIndex, int tileIndex, Side side, Building building)
    {
        parentBuilding = building;

        currentFloor = floorIndex;
        currentTile = tileIndex;

        // Monkey starts off moving into the building from one of the sides
        ChangeState(MonkeyState.Moving);

        if (side == Side.Left)
        {
            destinationX = transform.localPosition.x + Building.MONKEY_OFFSET + Building.TILE_WIDTH / 2;
        }
        else
        {
            destinationX = transform.localPosition.x - Building.MONKEY_OFFSET - Building.TILE_WIDTH / 2;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case MonkeyState.Idle:
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0) ChooseNextAction();
                break;
            case MonkeyState.Moving:
                MoveTowardDestinationX();

                if (System.Math.Abs(transform.localPosition.x - destinationX) < Building.EPSILON)
                {
                    if (destinationX < parentBuilding.LeftEdge || destinationX > parentBuilding.RightEdge) parentBuilding.HandleOnMonkeyLeftBuilding(gameObject);
                    else ChangeState(MonkeyState.Idle);
                }
                break;
            case MonkeyState.CallingElevator:
                if (GetComponentInParent<Floor>().CalledElevator(destinationFloor > currentFloor ? Direction.Up : Direction.Down))
                {
                    // Stop trying to call an elevator if one has already been called
                    ChangeState(MonkeyState.WaitingForElevator);
                }
                else
                {
                    MoveTowardDestinationX();

                    if (System.Math.Abs(transform.localPosition.x - destinationX) < Building.EPSILON)
                    {
                        ChangeState(MonkeyState.WaitingForElevator);
                        GetComponent<Animator>().SetTrigger("Jump");
                        Invoke("FinishCallingElevator", JUMP_ANIMATION_DURATION);
                    }
                }
                break;
            case MonkeyState.EnteringElevator:
                if (System.Math.Abs(transform.localPosition.x - destinationX) > Building.EPSILON)
                {
                    MoveTowardDestinationX();
                }
                else
                {
                    MoveTowardDestinationY();

                    if (System.Math.Abs(transform.localPosition.y - destinationY) < Building.EPSILON)
                    {
                        ChangeState(MonkeyState.RidingElevator);
                        parentBuilding.HandleOnMonkeyEnteredElevator(gameObject, destinationElevator, destinationFloor);
                    }
                }
                break;
            case MonkeyState.LeavingElevator:
                MoveTowardDestinationY();

                if (System.Math.Abs(transform.localPosition.y - destinationY) < Building.EPSILON)
                {
                    parentBuilding.HandleOnMonkeyLeftElevator(gameObject, destinationElevator, destinationFloor);
                    ChangeState(MonkeyState.Idle);
                }
                break;
        }
    }

    // Helper Functions
    bool InMovingState()
    {
        if (state == MonkeyState.Moving) return true;
        if (state == MonkeyState.CallingElevator) return true;
        if (state == MonkeyState.EnteringElevator) return true;
        if (state == MonkeyState.LeavingElevator) return true;

        return false;
    }

    void ChangeState(MonkeyState newState)
    {
        switch (newState)
        {
            case MonkeyState.Idle:
                if (InMovingState()) GetComponent<Animator>().SetTrigger("Stand");
                idleTimer = UnityEngine.Random.Range(0f, 1f);
                break;
            case MonkeyState.Moving:
            case MonkeyState.CallingElevator:
            case MonkeyState.EnteringElevator:
            case MonkeyState.LeavingElevator:
                if (!InMovingState()) GetComponent<Animator>().SetTrigger("Walk");
                break;
            case MonkeyState.WaitingForElevator:
            case MonkeyState.RidingElevator:
                if (InMovingState()) GetComponent<Animator>().SetTrigger("Stand");
                break;
        }

        state = newState;
    }

    void MoveTowardDestinationX()
    {
        float moveAmount = MOVE_SPEED;
        if (destinationX < transform.localPosition.x)
        {
            if (transform.localPosition.x - destinationX < moveAmount) moveAmount = transform.localPosition.x - destinationX;
            transform.Translate(-moveAmount, 0, 0);
        }
        else if (destinationX > transform.localPosition.x)
        {
            if (destinationX - transform.localPosition.x < moveAmount) moveAmount = destinationX - transform.localPosition.x;
            transform.Translate(moveAmount, 0, 0);
        }
    }

    void MoveTowardDestinationY()
    {
        float moveAmount = MOVE_SPEED;
        if (destinationY < transform.localPosition.y)
        {
            if (transform.localPosition.y - destinationY < moveAmount) moveAmount = transform.localPosition.y - destinationY;
            transform.Translate(0, -moveAmount, 0);
        }
        else if (destinationY > transform.localPosition.y)
        {
            if (destinationY - transform.localPosition.y < moveAmount) moveAmount = destinationY - transform.localPosition.y;
            transform.Translate(0, moveAmount, 0);
        }
    }

    void FinishCallingElevator()
    {
        GetComponentInParent<Floor>().HandleOnCallButtonPressed(destinationFloor > currentFloor ? Direction.Up : Direction.Down);
    }

    void GetOffElevator()
    {
        currentFloor = destinationFloor;
        textMesh.text = "";
        destinationY = transform.localPosition.y - ELEVATOR_OFFSET;
        ChangeState(MonkeyState.LeavingElevator);
    }

    // AI Functions
    void ChooseNextAction()
    {
        int choice = UnityEngine.Random.Range(0, 10);

        switch (choice)
        {
            case 0:
                // Stay idle
                ChangeState(MonkeyState.Idle);
                break;
            case 1:
            case 2:
                // Leave the building
                ChangeState(MonkeyState.Moving);
                if (transform.localPosition.x < 0) destinationX = parentBuilding.LeftEdge - Building.MONKEY_OFFSET;
                else destinationX = parentBuilding.RightEdge + Building.MONKEY_OFFSET;
                break;
            case 3:
            case 4:
            case 5:
            case 6:
                // Change Floors
                do
                {
                    destinationFloor = UnityEngine.Random.Range(0, parentBuilding.numberOfFloors);
                }
                while (destinationFloor == currentFloor);

                textMesh.text = Convert.ToString(destinationFloor + 1);

                Direction desiredDirection = destinationFloor > currentFloor ? Direction.Up : Direction.Down;
                int elevatorIndex = parentBuilding.GetWaitingElevatorIndex(currentFloor, desiredDirection);

                if (elevatorIndex > -1)
                {
                    // Go to the elevator that's already waiting
                    ChangeState(MonkeyState.WaitingForElevator);
                    HandleOnElevatorCarArrived(elevatorIndex, currentFloor, desiredDirection);
                }
                else if (GetComponentInParent<Floor>().CalledElevator(desiredDirection))
                {
                    // An elevator is already coming to this floor so don't try to call one
                    ChangeState(MonkeyState.WaitingForElevator);
                }
                else
                {
                    ChangeState(MonkeyState.CallingElevator);

                    int tileX = parentBuilding.GetXPositionForTileIndex(currentTile);

                    if (currentTile % 2 == 0)   // Need to move over a tile if we're on an elevator tile
                    {
                        if (currentTile == 0) tileX += Building.TILE_WIDTH;
                        else tileX -= Building.TILE_WIDTH;
                    }

                    destinationX = desiredDirection == Direction.Up ? tileX - BUTTON_OFFSET : tileX + BUTTON_OFFSET;
                }
                break;
            case 7:
            case 8:
            case 9:
                // Move to a different tile
                int destinationTile;
                do
                {
                    destinationTile = UnityEngine.Random.Range(0, parentBuilding.TilesPerFloor);
                }
                while (destinationTile == currentTile);

                ChangeState(MonkeyState.Moving);
                destinationX = parentBuilding.GetXPositionForTileIndex(destinationTile);
                break;
        }
    }

    // Event Handlers
    void OnDestroy()
    {
        parentBuilding = null;  // Reference to the parent building is no longer needed after object is destroyed and could create problems with circular references
    }

    public void HandleOnElevatorCarArrived(int elevatorIndex, int floorIndex, Direction elevatorDirection)
    {
        Direction desiredDirection = destinationFloor > currentFloor ? Direction.Up : Direction.Down;

        if (floorIndex == currentFloor && (state == MonkeyState.WaitingForElevator || state == MonkeyState.CallingElevator) && (elevatorDirection == desiredDirection || elevatorDirection == Direction.Neutral))
        {
            // Get on the elevator
            destinationElevator = elevatorIndex;
            destinationX = parentBuilding.GetXPositionForTileIndex(elevatorIndex * 2);
            destinationY = transform.localPosition.y + ELEVATOR_OFFSET;
            ChangeState(MonkeyState.EnteringElevator);
            parentBuilding.HandleOnMonkeyEnteringElevator(destinationElevator);
        }
        else if (floorIndex == destinationFloor && state == MonkeyState.RidingElevator && elevatorIndex == destinationElevator)
        {
            ChangeState(MonkeyState.WaitingForDoors);
            Invoke("GetOffElevator", Building.DOOR_ANIMATION_DURATION);   // Need to wait for doors to open before getting off
        }
    }
}
