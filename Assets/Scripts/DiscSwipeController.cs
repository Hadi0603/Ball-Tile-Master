using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

public class DiscSwipeController : MonoBehaviour
{
    public float moveSpeed = 5f; 
    private Vector2 swipeStart;
    private bool isSwiping = false;
    private GameObject selectedBird = null; 
    [SerializeField] private GameObject puff;
    [SerializeField] private Camera mainCamera;
    [SerializeField] Canvas gameCanvas;
    [SerializeField] private AudioSource jumpSound;
    [SerializeField] AudioSource popSound;
    [SerializeField] private int movesLeft;
    
    private bool moveStarted = false;
    private int totalBirds; 
    private int remainingBirds;
    
    public UIManager uiManager;
    public PowerUps powerUps;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        totalBirds = GameObject.FindGameObjectsWithTag("Bird").Length;
        remainingBirds = totalBirds;
        uiManager.UpdateMovesLeft(movesLeft);
        Debug.Log(remainingBirds);
    }

    private void Update()
    {
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            isSwiping = true;

            // Check if the starting position is a disc
            Ray ray = Camera.main.ScreenPointToRay(swipeStart);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Bird"))
                {
                    selectedBird = hit.collider.gameObject;
                    Debug.Log($"Bird selected at start: {selectedBird.name}");
                }
                else
                {
                    Debug.Log("No bird detected at start.");
                    isSwiping = false; 
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            Vector2 swipeEnd = Input.mousePosition;
            Vector2 swipeDirection = swipeEnd - swipeStart;

            if (swipeDirection.magnitude > 0.1f && selectedBird != null) 
            {
                swipeDirection.Normalize();
                movesLeft--;
                uiManager.UpdateMovesLeft(movesLeft);

                if (movesLeft < 0) // Check *after* decrementing
                {
                    powerUps.DestroyHouses();
                    StartCoroutine(GameOverAfterDelay());
                    isSwiping = false;
                    selectedBird = null;
                    return; // Exit early if no moves left
                }
                
                moveStarted = true;
                DetermineMoveDirection(swipeDirection);
            }
            else
            {
                Debug.Log("Swipe too short or no monster selected, not registering.");
            }

            isSwiping = false;
            selectedBird = null; // Reset selectedMonster after the swipe
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

        if (selectedBird != null)
        {
            StartCoroutine(MoveBird(selectedBird, moveDirection));
            
        }
        else
        {
            Debug.Log("No bird selected to move.");
        }
    }

    private IEnumerator MoveBird(GameObject bird, Vector3 direction)
{
    Vector3 startScale = bird.transform.localScale;
    Vector3 enlargedScale = startScale * 1.2f;

    if (direction != Vector3.zero)
    {
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        //bird.transform.rotation = lookRotation * Quaternion.Euler(-90, 0, 0);
    }

    while (true)
    {
        Vector3 nextPosition = bird.transform.position + direction;
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
            string birdName = bird.name;
            string holeName = targetHole.name;
            
            Debug.Log($"Bird {bird.name} moving to hole: {targetHole.name}");

            yield return MoveToPosition(bird, targetHole.transform.position + new Vector3(0, 0.2f, 0));

            Destroy(bird);
            //iManager.gameTime += 1;
            remainingBirds--;

            if (remainingBirds == 0)
            {
                uiManager.TriggerGameWon();
            }
            yield break;

            /*if (!birdName.Equals(holeName.Replace("Hole", "Bird")))
            {
                yield return MoveToPosition(bird, targetHole.transform.position + new Vector3(0, 0.2f, 0));
                uiManager.GameOver();
                Debug.Log($"Bird {bird.name} stopped behind hole: {targetHole.name}");
                yield break;
            }
            else
            {
                Debug.Log($"Bird {bird.name} moving to hole: {targetHole.name}");

                yield return MoveToPosition(bird, targetHole.transform.position + new Vector3(0, 0.2f, 0));

                Destroy(bird);
                uiManager.gameTime += 1;
                remainingBirds--;

                if (remainingBirds == 0)
                {
                    uiManager.TriggerGameWon();
                }
                yield break;
            }*/
        }

        if (!canMove)
        {
            Debug.Log("No valid block to move to. Stopping.");
            moveStarted = false;
            break;
        }

        if (moveStarted)
        {
            uiManager.UpdateMovesLeft(movesLeft);
            moveStarted = false;
            if (movesLeft < 0)
            {
                powerUps.DestroyHouses();
                StartCoroutine(GameOverAfterDelay());
                yield break;
            }
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        //bird.transform.rotation = lookRotation * Quaternion.Euler(-90, 0, 0);

        CreateTrailEffect(bird);

        yield return MoveToPosition(bird, nextPosition);

        if (targetBlock != null)
        {
            bird.transform.SetParent(targetBlock.transform);
            Vector3 customPosition = targetBlock.transform.position + new Vector3(0, 0.57687f, 0);
            bird.transform.position = customPosition;
        }
    }
}

// Smooth movement function
private IEnumerator MoveToPosition(GameObject bird, Vector3 targetPosition)
{
    float moveTime = 0.2f;
    float elapsedBlockTime = 0f;
    Vector3 startPosition = bird.transform.position;
    Vector3 startScale = bird.transform.localScale;
    Vector3 enlargedScale = startScale * 1.2f;

    bird.transform.localScale = enlargedScale;

    while (elapsedBlockTime < moveTime)
    {
        float t = elapsedBlockTime / moveTime;
        t = t * t * (3f - 2f * t); // Smoothstep easing
        bird.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
        elapsedBlockTime += Time.deltaTime;
        yield return null;
    }

    bird.transform.position = targetPosition;
    bird.transform.localScale = startScale;
    jumpSound.Play();
}



    private void CreateTrailEffect(GameObject bird)
    {
        TrailRenderer trail = bird.GetComponent<TrailRenderer>();
        if (!trail)
        {
            trail = bird.AddComponent<TrailRenderer>();
            trail.time = 1f; // Duration of the trail
            trail.startWidth = 0.3f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.white;
            trail.endColor = new Color(1, 1, 1, 0);
        }
    }
    private Vector2 ScreenToCanvasPosition(Vector3 screenPosition, Canvas canvas)
    {
        Vector2 canvasPosition;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Convert screen position to Canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            screenPosition, 
            canvas.worldCamera, 
            out canvasPosition
        );

        return canvasPosition;
    }


    
    private GameObject GetBirdAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Bird"))
                return collider.gameObject;
        }
        return null;
    }
    private IEnumerator GameOverAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        uiManager.GameOver();
        Debug.Log("Game Over! No moves left.");
    }
}