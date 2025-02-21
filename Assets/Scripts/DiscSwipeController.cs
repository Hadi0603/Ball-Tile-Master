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

    private System.Collections.IEnumerator MoveBird(GameObject bird, Vector3 direction)
{
    Vector3 startScale = bird.transform.localScale;
    Vector3 enlargedScale = startScale * 1.2f;
    if (direction != Vector3.zero)
    {
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        bird.transform.rotation = lookRotation * Quaternion.Euler(-90, 0, -90);
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
                //CheckAndDestroyMatchingMonsters(monster.transform.position);
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

            if (!birdName.Equals(holeName.Replace("Hole", "Bird")))
            {
                float moveDuration = 0.3f;
                float elapsedTime = 0f;

                Vector3 initialPosition = bird.transform.position;
                Vector3 holePosition = targetHole.transform.position + new Vector3(0, 0.5f, 0);

                while (elapsedTime < moveDuration)
                {
                    float t = elapsedTime / moveDuration;
                    t = t * t * (3f - 2f * t); // Smoothstep easing
                    bird.transform.position = Vector3.Lerp(initialPosition, holePosition, t);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                bird.transform.position = holePosition;
                uiManager.GameOver();
                Debug.Log($"Bird {bird.name} stopped behind hole: {targetHole.name}");
                yield break;
            }
            else
            {
                Debug.Log($"Bird {bird.name} moving to hole: {targetHole.name}");

                float moveDuration = 0.3f;
                float elapsedTime = 0f;

                Vector3 initialPosition = bird.transform.position;
                Vector3 holePosition = targetHole.transform.position + new Vector3(0, 0.1f, 0);

                while (elapsedTime < moveDuration)
                {
                    float t = elapsedTime / moveDuration;
                    t = t * t * (3f - 2f * t); // Smoothstep easing
                    bird.transform.position = Vector3.Lerp(initialPosition, holePosition, t);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                bird.transform.position = holePosition;
                Debug.Log($"Bird {bird.name} destroyed in hole: {targetHole.name}");
                Destroy(bird);
                uiManager.gameTime += 1;
                //CreatePuff(targetHole);
                remainingBirds--;
                Debug.Log(remainingBirds);
                if (remainingBirds == 0)
                {
                    uiManager.TriggerGameWon();
                }
                

                yield break;
            }
        }

        if (!canMove)
        {
            Debug.Log("No valid block to move to. Stopping.");
            moveStarted = false;
            yield break;
        }
        if (moveStarted)
        {
            //movesLeft--;
            uiManager.UpdateMovesLeft(movesLeft);
            moveStarted = false; // Reset the flag *after* the successful move start
            if (movesLeft < 0)
            {
                powerUps.DestroyHouses();
                StartCoroutine(GameOverAfterDelay());
                yield break;
            }
        }
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        bird.transform.rotation = lookRotation * Quaternion.Euler(-90, 0, -90);

        CreateTrailEffect(bird);
        float moveTime = 0.2f;
        float elapsedBlockTime = 0f;

        Vector3 startPosition = bird.transform.position;

        // Scale up for a juicy effect
        bird.transform.localScale = enlargedScale;

        while (elapsedBlockTime < moveTime)
        {
            float t = elapsedBlockTime / moveTime;
            t = t * t * (3f - 2f * t); // Smoothstep easing
            bird.transform.position = Vector3.Lerp(startPosition, nextPosition, t);

            elapsedBlockTime += Time.deltaTime;
            yield return null;
        }

        /*movesLeft--;
        uiManager.UpdateMovesLeft(movesLeft);
        if (movesLeft <= 0 && remainingMonsters > 0)
        {
            uiManager.GameOver();
        }*/

        bird.transform.position = nextPosition;
        bird.transform.localScale = startScale;
        jumpSound.Play();
        

        if (targetBlock != null)
        {
            bird.transform.SetParent(targetBlock.transform);
            Vector3 customPosition = targetBlock.transform.position + new Vector3(0, 0.53f, 0);
            bird.transform.position = customPosition;

            Debug.Log($"Bird {bird.name} positioned at custom position: {bird.transform.position}");
        }
    }
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

    /*public void CreatePuff(GameObject holeObject)
    {
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(holeObject.transform.position);
        GameObject puffObject = Instantiate(puff, gameCanvas.transform);
        popSound.Play();
        RectTransform rectTransform = puffObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Convert screen position to Canvas space
            rectTransform.anchoredPosition = ScreenToCanvasPosition(screenPosition, gameCanvas);
            rectTransform.localScale = Vector3.one; // Ensure correct scale
        }

        Destroy(puffObject, 1f);
    }*/
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
    /*private void CheckAndDestroyMatchingMonsters(Vector3 position)
    {
        HashSet<GameObject> uniqueMatchingMonsters = new HashSet<GameObject>();

        // Check in horizontal direction first (you can choose to check vertical first if preferred)
        AddMatchingMonsters(Vector3.right, position, uniqueMatchingMonsters); // Check right
        AddMatchingMonsters(Vector3.left, position, uniqueMatchingMonsters);  // Check left

        // If no matching discs found horizontally, check vertically
        if (uniqueMatchingMonsters.Count == 0)
        {
            AddMatchingMonsters(Vector3.forward, position, uniqueMatchingMonsters);  // Check forward
            AddMatchingMonsters(Vector3.back, position, uniqueMatchingMonsters);     // Check back
        }

        // If there are 3 or more matching discs (including the original), destroy them
        if (uniqueMatchingMonsters.Count >= 3)
        {
            uniqueMatchingMonsters.Add(GetMonsterAtPosition(position)); // Include the original disc

            foreach (GameObject monster in uniqueMatchingMonsters)
            {
                Destroy(monster);
                CreatePuff(monster); // Optional puff effect
                remainingMonsters--; // Correct decrement
            }

            Debug.Log($"Destroyed {uniqueMatchingMonsters.Count} monsters. Remaining: {remainingMonsters}");

            // Check if all discs are removed
            if (remainingMonsters == 0)
            {
                uiManager.TriggerGameWon();
            }
        }
    }*/


    private void AddMatchingBirds(Vector3 direction, Vector3 startPosition, HashSet<GameObject> matches)
    {
        Vector3 nextPosition = startPosition + direction;
        GameObject nextBird = GetBirdAtPosition(nextPosition);

        while (nextBird != null && nextBird.name == GetBirdAtPosition(startPosition).name)
        {
            matches.Add(nextBird);
            nextPosition += direction;
            nextBird = GetBirdAtPosition(nextPosition);
        }
    }

// Helper function to get the disc at a specific position
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