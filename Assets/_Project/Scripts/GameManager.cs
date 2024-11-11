using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StackDrop
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float dropSpeed = 5f;
    [SerializeField] private float perfectThreshold = 0.1f;
    [SerializeField] private Vector3 blockSize = new Vector3(3f, 0.5f, 3f);
    [SerializeField] private float minBlockSize = 0.5f;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private float boundaryWidth = 5f;
    [SerializeField] private bool alternateAxis = true; // Alternate between X and Z axis movement
    
    [Header("References")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Material blockMaterial;
    [SerializeField] private ParticleSystem perfectEffect;
    
    // Game State
    private List<GameObject> blocks = new List<GameObject>();
    private GameObject currentBlock;
    private bool isMovingPositive = true;
    private bool isDropping;
    private bool isGameOver;
    private int score;
    private Vector3 lastBlockSize;
    private bool movingOnX; // true for X axis, false for Z axis
    private float cameraHeight;
    private Color[] blockColors;
    private int colorIndex;

    private void Start()
    {
        InitializeColors();
        InitializeGame();
    }
    
    private void InitializeColors()
    {
        // Create a gradient of colors for blocks
        blockColors = new Color[] {
            new Color(0.1f, 0.6f, 0.9f), // Light Blue
            new Color(0.2f, 0.7f, 0.8f),
            new Color(0.3f, 0.8f, 0.7f),
            new Color(0.4f, 0.9f, 0.6f),
            new Color(0.5f, 1.0f, 0.5f)  // Mint Green
        };
    }
    
    private void InitializeGame()
    {
        score = 0;
        isGameOver = false;
        lastBlockSize = blockSize;
        movingOnX = true;
        colorIndex = 0;
        cameraHeight = spawnHeight;
        
        UpdateScoreUI();
        gameOverText.gameObject.SetActive(false);
        
        // Create base block
        CreateBaseBlock();
        SpawnNewBlock();
        
        // Position camera
        UpdateCameraPosition();
    }

    private void CreateBaseBlock()
    {
        GameObject baseBlock = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);
        baseBlock.transform.localScale = blockSize;
        baseBlock.GetComponent<Renderer>().material.color = blockColors[0];
        blocks.Add(baseBlock);
    }
    
    private void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RestartGame();
            }
            return;
        }

        if (!isDropping && currentBlock != null)
        {
            MoveBlock();
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartDropping();
            }
        }

        // Smooth camera follow
        UpdateCameraPosition();
    }
    
    private void UpdateCameraPosition()
    {
        float targetHeight = Mathf.Max(spawnHeight, blocks.Count * blockSize.y + 10f);
        cameraHeight = Mathf.Lerp(cameraHeight, targetHeight, Time.deltaTime * 2f);
        
        Vector3 targetPosition = new Vector3(0, cameraHeight, -12f);
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            Time.deltaTime * 2f
        );
        mainCamera.transform.LookAt(new Vector3(0, blocks.Count * blockSize.y, 0));
    }
    
    private void MoveBlock()
    {
        float moveDirection = isMovingPositive ? 1 : -1;
        Vector3 position = currentBlock.transform.position;
        
        if (movingOnX)
        {
            position.x += moveSpeed * moveDirection * Time.deltaTime;
            if (Mathf.Abs(position.x) > boundaryWidth)
            {
                isMovingPositive = !isMovingPositive;
                position.x = Mathf.Sign(position.x) * boundaryWidth;
            }
        }
        else
        {
            position.z += moveSpeed * moveDirection * Time.deltaTime;
            if (Mathf.Abs(position.z) > boundaryWidth)
            {
                isMovingPositive = !isMovingPositive;
                position.z = Mathf.Sign(position.z) * boundaryWidth;
            }
        }
        
        currentBlock.transform.position = position;
    }
    
    private void StartDropping()
    {
        isDropping = true;
        Rigidbody rb = currentBlock.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.velocity = Vector3.down * dropSpeed;
        
        StartCoroutine(CheckForLanding());
    }
    
    private IEnumerator CheckForLanding()
    {
        yield return new WaitForSeconds(0.5f);
        
        while (isDropping)
        {
            Rigidbody rb = currentBlock.GetComponent<Rigidbody>();
            if (Mathf.Abs(rb.velocity.magnitude) < 0.1f)
            {
                HandleBlockLanded();
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void HandleBlockLanded()
    {
        isDropping = false;
        
        if (blocks.Count > 0)
        {
            GameObject previousBlock = blocks[blocks.Count - 1];
            float overlap = CalculateOverlap(currentBlock, previousBlock);
            
            if (overlap <= minBlockSize)
            {
                GameOver();
                return;
            }
            
            ResizeBlock(currentBlock, overlap);
            lastBlockSize = currentBlock.transform.localScale;
            
            bool isPerfect = Mathf.Abs(overlap - previousBlock.transform.localScale.x) < perfectThreshold;
            if (isPerfect)
            {
                SpawnPerfectEffect();
            }
            
            AddScore(isPerfect);
        }
        
        // Lock block position and rotation
        Rigidbody rb = currentBlock.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        currentBlock.transform.rotation = Quaternion.identity;
        
        blocks.Add(currentBlock);
        
        if (alternateAxis)
        {
            movingOnX = !movingOnX;
        }
        
        SpawnNewBlock();
    }
    
    private float CalculateOverlap(GameObject current, GameObject previous)
    {
        if (movingOnX)
        {
            float currentLeft = current.transform.position.x - current.transform.localScale.x / 2;
            float currentRight = current.transform.position.x + current.transform.localScale.x / 2;
            float previousLeft = previous.transform.position.x - previous.transform.localScale.x / 2;
            float previousRight = previous.transform.position.x + previous.transform.localScale.x / 2;
            return Mathf.Min(currentRight, previousRight) - Mathf.Max(currentLeft, previousLeft);
        }
        else
        {
            float currentBack = current.transform.position.z - current.transform.localScale.z / 2;
            float currentFront = current.transform.position.z + current.transform.localScale.z / 2;
            float previousBack = previous.transform.position.z - previous.transform.localScale.z / 2;
            float previousFront = previous.transform.position.z + previous.transform.localScale.z / 2;
            return Mathf.Min(currentFront, previousFront) - Mathf.Max(currentBack, previousBack);
        }
    }
    
    private void ResizeBlock(GameObject block, float overlap)
    {
        Vector3 scale = block.transform.localScale;
        Vector3 position = block.transform.position;
        
        if (movingOnX)
        {
            scale.x = overlap;
            position.x = blocks[blocks.Count - 1].transform.position.x;
        }
        else
        {
            scale.z = overlap;
            position.z = blocks[blocks.Count - 1].transform.position.z;
        }
        
        block.transform.localScale = scale;
        block.transform.position = position;
        
        // Update collider
        BoxCollider collider = block.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.size = Vector3.one;
        }
    }
    
    private void SpawnNewBlock()
    {
        float height = blocks[blocks.Count - 1].transform.position.y + blockSize.y;
        Vector3 spawnPosition;
        
        if (movingOnX)
        {
            spawnPosition = new Vector3(-boundaryWidth, height, blocks[blocks.Count - 1].transform.position.z);
        }
        else
        {
            spawnPosition = new Vector3(blocks[blocks.Count - 1].transform.position.x, height, -boundaryWidth);
        }
        
        currentBlock = Instantiate(blockPrefab, spawnPosition, Quaternion.identity);
        currentBlock.transform.localScale = lastBlockSize;
        
        // Set block color
        Renderer renderer = currentBlock.GetComponent<Renderer>();
        renderer.material = new Material(blockMaterial);
        renderer.material.color = blockColors[colorIndex % blockColors.Length];
        colorIndex++;
        
        Rigidbody rb = currentBlock.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        
        isMovingPositive = true;
        isDropping = false;
    }
    
    private void SpawnPerfectEffect()
    {
        if (perfectEffect != null)
        {
            Vector3 effectPosition = currentBlock.transform.position;
            effectPosition.y += 0.1f;
            ParticleSystem effect = Instantiate(perfectEffect, effectPosition, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration);
        }
    }
    
    private void AddScore(bool isPerfect)
    {
        score += isPerfect ? 100 : 50;
        UpdateScoreUI();
    }
    
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
    
    private void GameOver()
    {
        isGameOver = true;
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
        }
    }
    
    private void RestartGame()
    {
        foreach (GameObject block in blocks)
        {
            Destroy(block);
        }
        blocks.Clear();
        
        if (currentBlock != null)
        {
            Destroy(currentBlock);
        }
        
        InitializeGame();
    }
    }    
}

