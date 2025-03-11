using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

public class BallSwipeController : MonoBehaviour
{
    public float moveSpeed = 5f; 
    private Vector2 swipeStart;
    private bool isSwiping = false;
    private GameObject selectedBall = null; 
    [SerializeField] private AudioSource jumpSound;
    [SerializeField] private int movesLeft;
    
    private bool isBallMoving = false;
    private bool moveStarted = false;
    private int totalBalls; 
    private int remainingBalls;
    
    public UIManager uiManager;
    public PowerUps powerUps;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        totalBalls = GameObject.FindGameObjectsWithTag("Ball").Length;
        remainingBalls = totalBalls;
        uiManager.UpdateMovesLeft(movesLeft);
        Debug.Log(remainingBalls);
    }

    private void Update()
    {
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        if (isBallMoving) return;
        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            isSwiping = true;
            Ray ray = Camera.main.ScreenPointToRay(swipeStart);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Ball"))
                {
                    selectedBall = hit.collider.gameObject;

                }
                else
                {
                    Debug.Log("No ball detected at start.");
                    isSwiping = false; 
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            Vector2 swipeEnd = Input.mousePosition;
            Vector2 swipeDirection = swipeEnd - swipeStart;

            if (swipeDirection.magnitude > 0.1f && selectedBall != null) 
            {
                swipeDirection.Normalize();
                movesLeft--;
                uiManager.UpdateMovesLeft(movesLeft);

                if (movesLeft < 0) // Check *after* decrementing
                {
                    StartCoroutine(GameOverAfterDelay());
                    isSwiping = false;
                    selectedBall = null;
                    return; // Exit early if no moves left
                }
                
                moveStarted = true;
                DetermineMoveDirection(swipeDirection);
            }
            else
            {
                Debug.Log("Swipe too short or no ball selected, not registering.");
            }

            isSwiping = false;
            selectedBall = null; // Reset selectedMonster after the swipe
        }
    }

    private void DetermineMoveDirection(Vector2 direction)
    {
        Vector3 moveDirection;

        // Determine cardinal direction
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            moveDirection = direction.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            moveDirection = direction.y > 0 ? Vector3.forward : Vector3.back;
        }

        Debug.Log($"Swipe detected. Moving direction: {moveDirection}");

        if (selectedBall != null)
        {
            StartCoroutine(MoveBall(selectedBall, moveDirection));
            
        }
        else
        {
            Debug.Log("No ball selected to move.");
        }
    }

    private IEnumerator MoveBall(GameObject ball, Vector3 direction)
    {
        isBallMoving = true;
        Vector3 startScale = ball.transform.localScale;
        Vector3 enlargedScale = startScale * 1.2f;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            //bird.transform.rotation = lookRotation * Quaternion.Euler(-90, 0, 0);
        }

        while (true)
        {
            Vector3 nextPosition = ball.transform.position + direction;
            Collider[] colliders = Physics.OverlapSphere(nextPosition, 0.5f);
            bool canMove = false;
            bool isHole = false;
            GameObject targetBlock = null;
            GameObject targetHole = null;

            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Block") && collider.transform.childCount == 0)
                {
                    canMove = true;
                    targetBlock = collider.gameObject;
                    break;
                }
                else if (collider.CompareTag("Block") && collider.transform.childCount > 0)
                {
                    Debug.Log("Obstacle detected: Stopping movement.");
                }
                else if (collider.CompareTag("Hole"))
                {
                    isHole = true;
                    targetHole = collider.gameObject;
                    break;
                }
            }

            // Handle hole interaction
            if (isHole)
            {
                string ballName = ball.name;
                string holeName = targetHole.name;

                Debug.Log($"Ball {ball.name} moving to hole: {targetHole.name}");

                yield return MoveToPosition(ball, targetHole.transform.position + new Vector3(0, 0.2f, 0));

                Destroy(ball);
                //iManager.gameTime += 1;
                remainingBalls--;

                if (remainingBalls == 0)
                {
                    uiManager.TriggerGameWon();
                }
                isBallMoving = false;
                yield break;

                
            }

            if (!canMove)
            {
                Debug.Log("No valid block to move to. Stopping.");
                moveStarted = false;
                isBallMoving = false;
                break;
            }

            if (moveStarted)
            {
                uiManager.UpdateMovesLeft(movesLeft);
                moveStarted = false;
                if (movesLeft < 0)
                {
                    StartCoroutine(GameOverAfterDelay());
                    isBallMoving = false;
                    yield break;
                }
            }

            Quaternion lookRotation = Quaternion.LookRotation(direction);

            CreateTrailEffect(ball);

            yield return MoveToPosition(ball, nextPosition);

            if (targetBlock != null)
            {
                ball.transform.SetParent(targetBlock.transform);
                Vector3 customPosition = targetBlock.transform.position + new Vector3(0, 0.57687f, 0);
                ball.transform.position = customPosition;
            }
        }
        isBallMoving = false;
    }

// Smooth movement function
    private IEnumerator MoveToPosition(GameObject ball, Vector3 targetPosition)
    {
        float moveTime = 0.2f;
        float elapsedBlockTime = 0f;
        Vector3 startPosition = ball.transform.position;
        Vector3 startScale = ball.transform.localScale;
        Vector3 enlargedScale = startScale * 1.2f;

        ball.transform.localScale = enlargedScale;

        while (elapsedBlockTime < moveTime)
        {
            float t = elapsedBlockTime / moveTime;
            t = t * t * (3f - 2f * t); // Smoothstep easing
            ball.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            elapsedBlockTime += Time.deltaTime;
            yield return null;
        }

        ball.transform.position = targetPosition;
        ball.transform.localScale = startScale;
        jumpSound.Play();
    }



    private void CreateTrailEffect(GameObject ball)
    {
        TrailRenderer trail = ball.GetComponent<TrailRenderer>();
        if (!trail)
        {
            trail = ball.AddComponent<TrailRenderer>();
            trail.time = 1f; // Duration of the trail
            trail.startWidth = 0.3f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.white;
            trail.endColor = new Color(1, 1, 1, 0);
        }
    }
    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        uiManager.GameOver();
        Debug.Log("Game Over! No moves left.");
    }
}