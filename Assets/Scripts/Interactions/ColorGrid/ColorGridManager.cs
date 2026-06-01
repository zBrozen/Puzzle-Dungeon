using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ColorGridManager : MonoBehaviour
{
    [Header("--- COLONNE 0 (FOND / TOP) ---")]
    public GameObject[] col0_Back = new GameObject[6];

    [Header("--- COLONNE 1 (MILIEU) ---")]
    public GameObject[] col1_Mid = new GameObject[6];

    [Header("--- COLONNE 2 (DEVANT / BOTTOM) ---")]
    public GameObject[] col2_Front = new GameObject[6];

    [Header("Settings")]
    public Material[] colorMaterials;
    public GameObject buttonVisualPrefab;
    public GameObject whitePlatform; 

    private ColorBlock[,] grid; 
    private ColorBlock currentBlock;
    private HashSet<int> usedColors = new HashSet<int>();
    private bool buttonPressed = false;
    private Vector2Int buttonPos;

    private int totalColumns = 3;
    private int totalLines = 6;

    void Start()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);
        if (whitePlatform != null) whitePlatform.SetActive(false);
        SetupManualGrid();
    }

    void SetupManualGrid()
    {
        grid = new ColorBlock[totalColumns, totalLines];
        for (int l = 0; l < totalLines; l++) grid[0, l] = GetOrAddBlock(col0_Back[l], 0, l);
        for (int l = 0; l < totalLines; l++) grid[1, l] = GetOrAddBlock(col1_Mid[l], 1, l);
        for (int l = 0; l < totalLines; l++) grid[2, l] = GetOrAddBlock(col2_Front[l], 2, l);
        GenerateColors();
    }

    ColorBlock GetOrAddBlock(GameObject go, int c, int l)
    {
        if (go == null) return null;
        ColorBlock block = go.GetComponent<ColorBlock>();
        if (block == null) block = go.AddComponent<ColorBlock>();
        block.gameObject.name = $"Block_{c}_{l}";
        return block;
    }

    void GenerateColors()
    {
        int[,] colors = new int[totalColumns, totalLines];
        bool possible = false;
        int attempts = 0;
        int colorCount = colorMaterials.Length;

        while (!possible && attempts < 2000)
        {
            attempts++;
            for (int c = 0; c < totalColumns; c++)
                for (int l = 0; l < totalLines; l++) colors[c, l] = -1;

            buttonPos = new Vector2Int(Random.Range(0, 2), Random.Range(0, totalLines));

            List<Vector2Int> path = new List<Vector2Int> {
                new Vector2Int(2, Random.Range(0, 3)), 
                buttonPos,
                new Vector2Int(2, Random.Range(3, 6))
            };

            List<int> shuffledColorIDs = Enumerable.Range(0, colorCount).OrderBy(x => Random.value).ToList();

            int[] counts = new int[colorCount];
            for (int i = 0; i < path.Count; i++) {
                int cID = shuffledColorIDs[i % shuffledColorIDs.Count];
                colors[path[i].x, path[i].y] = cID;
                counts[cID]++;
            }

            for (int c = 0; c < totalColumns; c++) {
                for (int l = 0; l < totalLines; l++) {
                    if (colors[c, l] == -1) {
                        List<int> valid = new List<int>();
                        for (int i = 0; i < colorCount; i++) {
                            if (IsDiagonalSafe(c, l, i, colors) && counts[i] < 4) 
                                valid.Add(i);
                        }

                        int chosen = -1;
                        if (valid.Count > 0) {
                            foreach (int v in valid) if (counts[v] < 2) { chosen = v; break; }
                            if (chosen == -1) chosen = valid[Random.Range(0, valid.Count)];
                        } else {
                            for (int i = 0; i < colorCount; i++)
                                if (IsDiagonalSafe(c, l, i, colors)) { chosen = i; break; }
                            if (chosen == -1) chosen = Random.Range(0, colorCount);
                        }

                        colors[c, l] = chosen;
                        counts[chosen]++;
                    }
                }
            }

            if (CheckSolvability(colors, buttonPos)) possible = true;
        }

        foreach (var b in grid) if (b != null) {
            foreach (Transform child in b.transform) if (child.name.Contains("Button")) Destroy(child.gameObject);
        }

        for (int c = 0; c < totalColumns; c++) {
            for (int l = 0; l < totalLines; l++) {
                if (grid[c, l] != null) {
                    grid[c, l].colorMaterials = colorMaterials;
                    grid[c, l].meshRenderer = grid[c, l].GetComponent<MeshRenderer>();
                    grid[c, l].SetColor(colors[c, l]);
                    grid[c, l].OnPlayerEnter += HandlePlayerEnter;

                    if (c == buttonPos.x && l == buttonPos.y) {
                        if (buttonVisualPrefab != null) {
                            // Calculate top surface of the mesh for perfect placement
                            float topY = grid[c, l].meshRenderer.bounds.max.y;
                            Vector3 spawnPos = grid[c, l].transform.position;
                            spawnPos.y = topY + 0.01f; // Slight offset to avoid Z-fighting

                            GameObject btn = Instantiate(buttonVisualPrefab, spawnPos, Quaternion.identity, grid[c, l].transform);
                            btn.name = "ButtonVisual";
                        }
                    }
                }
            }
        }
    }

    bool IsDiagonalSafe(int col, int line, int color, int[,] cur)
    {
        int[] dc = { -1, -1, 1, 1 };
        int[] dl = { -1, 1, -1, 1 };
        for (int i = 0; i < 4; i++) {
            int nc = col + dc[i];
            int nl = line + dl[i];
            if (nc >= 0 && nc < totalColumns && nl >= 0 && nl < totalLines)
                if (cur[nc, nl] == color) return false;
        }
        return true;
    }

    bool CheckSolvability(int[,] colors, Vector2Int target)
    {
        Queue<(Vector2Int pos, int mask, bool pressed)> queue = new Queue<(Vector2Int, int, bool)>();
        HashSet<(Vector2Int, int, bool)> visited = new HashSet<(Vector2Int, int, bool)>();

        for (int l = 0; l < totalLines; l++)
            queue.Enqueue((new Vector2Int(2, l), 1 << colors[2, l], (2 == target.x && l == target.y)));

        while (queue.Count > 0) {
            var state = queue.Dequeue();
            if (visited.Contains(state)) continue;
            visited.Add(state);

            if (state.pressed && state.pos.x == 2 && (state.pos.y == 4 || state.pos.y == 5))
                return true;

            for (int dc = -1; dc <= 1; dc++) {
                for (int dl = -1; dl <= 1; dl++) {
                    if (dc == 0 && dl == 0) continue;
                    Vector2Int next = new Vector2Int(state.pos.x + dc, state.pos.y + dl);
                    if (next.x >= 0 && next.x < totalColumns && next.y >= 0 && next.y < totalLines) {
                        int nextColor = colors[next.x, next.y];
                        if ((state.mask & (1 << nextColor)) == 0) {
                            bool nowPressed = state.pressed || (next == target);
                            queue.Enqueue((next, state.mask | (1 << nextColor), nowPressed));
                        }
                    }
                }
            }
        }
        return false;
    }

    void HandlePlayerEnter(ColorBlock block)
    {
        if (currentBlock == block) return;
        if (!usedColors.Contains(block.colorID)) {
            usedColors.Add(block.colorID);
            block.SetAsCurrent(true);
            TriggerPermanentFall(block.colorID, block);
        } else block.SetAsCurrent(true);
        currentBlock = block;
        if (!buttonPressed && grid[buttonPos.x, buttonPos.y] == block) PressButton();
    }

    void TriggerPermanentFall(int colorID, ColorBlock safeBlock)
    {
        for (int c = 0; c < totalColumns; c++)
            for (int l = 0; l < totalLines; l++)
                if (grid[c, l] != null && grid[c, l].colorID == colorID && grid[c, l] != safeBlock)
                    grid[c, l].Fall();
    }

    public void ResetPuzzle()
    {
        usedColors.Clear();
        currentBlock = null;
        buttonPressed = false;
        if (whitePlatform != null) whitePlatform.SetActive(false);
        for (int c = 0; c < totalColumns; c++)
            for (int l = 0; l < totalLines; l++)
                if (grid[c, l] != null) { grid[c, l].Rise(); grid[c, l].SetAsCurrent(false); }
    }

    public void RegeneratePuzzle()
    {
        ResetPuzzle();
        // Unsubscribe from events to avoid duplicates when GenerateColors adds them again
        for (int c = 0; c < totalColumns; c++)
            for (int l = 0; l < totalLines; l++)
                if (grid[c, l] != null) grid[c, l].OnPlayerEnter -= HandlePlayerEnter;
        
        GenerateColors();
    }

    void PressButton()
    {
        if (buttonPressed) return;
        buttonPressed = true;
        if (whitePlatform != null) whitePlatform.SetActive(true);
    }
}
