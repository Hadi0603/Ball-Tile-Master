using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PowerUps : MonoBehaviour
{
    [Header("Destroy Obstacle PowerUp")]
    [SerializeField] private Button destroyObstacleBtn;
    [SerializeField] private List<GameObject> obstacles;
    
    [Header("Time Slowdown PowerUp")]
    [SerializeField] private Button slowTimeButton;
    [SerializeField] private float slowTimeDuration = 5f;
    [SerializeField] private float slowTimeFactor = 0.5f; // 50% speed
    
    private bool isSlowed = false;

    [Header("Destroy Homes PowerUp")] 
    [SerializeField] private List<GameObject> houses;
    
    public DiscSwipeController swipeController;

    public void DestroyObstacles()
    {
        if (obstacles == null || obstacles.Count == 0)
        {
            Debug.Log("No obstacles found");
            return;
        }

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            if (obstacles[i] != null)
            {
                Destroy(obstacles[i]);
            }
            obstacles.RemoveAt(i);
        }

        destroyObstacleBtn.interactable = false;
    }
    public void ActivateSlowTime()
    {
        if (!isSlowed)
        {
            StartCoroutine(SlowTimeCoroutine());
        }
    }
    private IEnumerator SlowTimeCoroutine()
    {
        isSlowed = true;
        Time.timeScale = slowTimeFactor; // Slow down time
        Time.fixedDeltaTime = Time.timeScale * 0.02f; // Adjust physics time step
        slowTimeButton.interactable = false; // Disable the button while active


        yield return new WaitForSeconds(slowTimeDuration);

        Time.timeScale = 1f; // Return to normal time
        Time.fixedDeltaTime = 0.02f; // Reset physics time step
        isSlowed = false;
    }

    public void DestroyHouses()
    {
        if (houses == null || houses.Count == 0)
        {
            Debug.Log("No houses found");
            return;
        }

        
        for (int i = houses.Count - 1; i >= 0; i--)
        {
            if (houses[i] != null)
            {
                //swipeController.CreatePuff(houses[i]);
                Destroy(houses[i]);
            }
            houses.RemoveAt(i);
        }
    }
}
