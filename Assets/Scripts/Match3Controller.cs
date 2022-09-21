using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Match3Controller : MonoBehaviour
{

    public static Match3Controller instance;

    public bool allowInput = true;

    [SerializeField] Transform gridParent;
    public Vector2Int gridSize;
    public int colorSize;
    public int gridHeight;
    [SerializeField] GameObject blockPrefab;
    [SerializeField] Color[] colors;
    public Block[,] grid = null;
    Queue<Block> blockPool = new Queue<Block>();
    Vector2 faraway = 1000 * Vector2.one;

    Block currentBlock;
    bool isSimulating;

    private void Awake() => instance = this;

    private void OnDestroy() => instance = null;

    void Start() => InitGrid();

    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector2(-gridHeight / 2, -gridHeight / 2), new Vector2(-gridHeight / 2, gridHeight / 2));
        Gizmos.DrawLine(new Vector2(-gridHeight / 2, gridHeight / 2), new Vector2(gridHeight / 2, gridHeight / 2));
        Gizmos.DrawLine(new Vector2(gridHeight / 2, gridHeight / 2), new Vector2(gridHeight / 2, -gridHeight / 2));
        Gizmos.DrawLine(new Vector2(gridHeight / 2, -gridHeight / 2), new Vector2(-gridHeight / 2, -gridHeight / 2));
    }*/

    public void InitGrid()
    {
        StopAllCoroutines();
        isSimulating = false;
        if (grid != null)
        {
            for (int i = 0; i < grid.GetLength(0); ++i)
            {
                for (int j = 0; j < grid.GetLength(1); ++j)
                {
                    PoolBlock(grid[i, j]);
                }
            }
        }

        grid = new Block[gridSize.y, gridSize.x];

        float scale = (gridSize.x >= gridHeight || gridSize.y >= gridHeight) ? ((gridHeight - 1) / (float)Mathf.Max(gridSize.x, gridSize.y)) : 1;

        Vector2 offset = new Vector2(
            gridSize.x % 2 == 0 ? ((gridSize.x) / 2 - 0.5f) : ((gridSize.x - 1) / 2),
            gridSize.y % 2 == 0 ? ((gridSize.y) / 2 - 0.5f) : ((gridSize.y - 1) / 2)
        ) * scale; ;

        for (int i = 0; i < gridSize.y; ++i)
        {
            for (int j = 0; j < gridSize.x; ++j)
            {
                Block block = InstantiateBlock();
                //block.transform.position = new Vector2(j, i) * scale - offset;
                block.transform.position = GetPositionOfCell(j, i);
                block.transform.localScale = scale * Vector2.one;
                grid[i, j] = block;
                int index = Random.Range(0, colorSize);
                if (i == 0 && j > 0)
                    while (index == grid[i, j - 1].colorIndex) index = Random.Range(0, colorSize);
                if (j == 0 && i > 0)
                    while (index == grid[i - 1, j].colorIndex) index = Random.Range(0, colorSize);
                if (i > 0 && j > 0)
                    while (index == grid[i - 1, j].colorIndex || index == grid[i, j - 1].colorIndex) index = Random.Range(0, colorSize);

                block.Setup(index, colors[index], new Vector2Int(j, i));
            }
        }
    }

    public Vector2 GetPositionOfCell(int x, int y)
    {
        float scale = (gridSize.x >= gridHeight || gridSize.y >= gridHeight) ? ((gridHeight - 1) / (float)Mathf.Max(gridSize.x, gridSize.y)) : 1;
        Vector2 offset = new Vector2(
            gridSize.x % 2 == 0 ? ((gridSize.x) / 2 - 0.5f) : ((gridSize.x - 1) / 2),
            gridSize.y % 2 == 0 ? ((gridSize.y) / 2 - 0.5f) : ((gridSize.y - 1) / 2)
        ) * scale; ;
        return new Vector2(x, y) * scale - offset;
    }

    public float GetScale() => (gridSize.x >= gridHeight || gridSize.y >= gridHeight) ? ((gridHeight - 1) / (float)Mathf.Max(gridSize.x, gridSize.y)) : 1;
    

    public void SelectBlock(Block block)
    {
        if (currentBlock == null)
        {
            currentBlock = block;
            currentBlock.SetSelected(true);
        }
        else
        {
            if (block.IsNeighbour(currentBlock)) SwapBlocks(block, currentBlock);
            else
            {
                currentBlock.SetSelected(false);
                currentBlock = block;
                currentBlock.SetSelected(true);
            }
        }
    }

    public void SwapBlocks(Block b1, Block b2)
    {
        allowInput = false;
        b1.SetSelected(false);
        b2.SetSelected(false);
        currentBlock = null;
        StartCoroutine(SwapRoutine(b1, b2));
    }

    IEnumerator SwapRoutine(Block b1, Block b2)
    {
        Vector2Int aux = b1.position;
        b1.position = b2.position;
        b2.position = aux;

        b1.SetLayer(true);

        Vector2 pos1 = b1.transform.position, pos2 = b2.transform.position;
        for (float t = 0; t < 1; t += Time.deltaTime * 4)
        {
            b1.transform.position = Vector2.Lerp(pos1, pos2, t);
            b2.transform.position = Vector2.Lerp(pos2, pos1, t);
            yield return null;
        }

        b1.transform.position = pos2;
        b2.transform.position = pos1;
        b1.SetLayer(false);

        grid[b1.position.y, b1.position.x] = b1;
        grid[b2.position.y, b2.position.x] = b2;

        yield return StartCoroutine(DestroyPieces(b1, b2));

        allowInput = true;
    }

    IEnumerator DestroyPieces(Block b1, Block b2)
    {
        List<Block> blocksToDestroy = new List<Block>();

        //horizontal check b1
        int left = b1.position.x, right = left;
        while (left > 0 && grid[b1.position.y, left - 1].colorIndex == b1.colorIndex) --left;
        while (right < gridSize.x - 1 && grid[b1.position.y, right + 1].colorIndex == b1.colorIndex) ++right;
        if (right - left >= 2) for (int i = left; i <= right; ++i) blocksToDestroy.Add(grid[b1.position.y, i]);

        //horizontal check b2
        left = b2.position.x; right = left;
        while (left > 0 && grid[b2.position.y, left - 1].colorIndex == b2.colorIndex) --left;
        while (right < gridSize.x - 1 && grid[b2.position.y, right + 1].colorIndex == b2.colorIndex) ++right;
        if (right - left >= 2) for (int i = left; i <= right; ++i) blocksToDestroy.Add(grid[b2.position.y, i]);

        //vertical check b1
        int bottom = b1.position.y, top = bottom;
        while (bottom > 0 && grid[bottom - 1, b1.position.x].colorIndex == b1.colorIndex) --bottom;
        while (top < gridSize.y - 1 && grid[top + 1, b1.position.x].colorIndex == b1.colorIndex) ++top;
        if (top - bottom >= 2) for (int i = bottom; i <= top; ++i) blocksToDestroy.Add(grid[i, b1.position.x]);

        //vertical check b2
        bottom = b2.position.y; top = bottom;
        while (bottom > 0 && grid[bottom - 1, b2.position.x].colorIndex == b2.colorIndex) --bottom;
        while (top < gridSize.y - 1 && grid[top + 1, b2.position.x].colorIndex == b2.colorIndex) ++top;
        if (top - bottom >= 2) for (int i = bottom; i <= top; ++i) blocksToDestroy.Add(grid[i, b2.position.x]);

        blocksToDestroy = blocksToDestroy.GroupBy(x => x.position).Select(x => x.First()).ToList();

        HashSet<int> columns = new HashSet<int>();

        UIController.instance.AddScore(blocksToDestroy.Count);
        foreach (Block block in blocksToDestroy)
        {
            grid[block.position.y, block.position.x] = null;
            columns.Add(block.position.x);
            PoolBlock(block);
        }

        foreach (int column in columns) yield return StartCoroutine(UpdateColumn(column));
    }

    IEnumerator UpdateColumn(int column)
    {
        int nullPos = -1;
        int blockPos = -1;
        for (int i = 0; i < gridSize.y; ++i) if (grid[i, column] == null) { nullPos = i; break; }

        if (nullPos == -1) yield break;

        for (int i = nullPos; i < gridSize.y; ++i) if (grid[i, column] != null) { blockPos = i; break; }

        if (blockPos != -1)
        {
            float x = grid[blockPos, column].transform.position.x;
            List<float> ys = new List<float>();
            for (int i = blockPos; i < gridSize.y; ++i) ys.Add(grid[i, column].transform.position.y);

            float dy = Mathf.Abs(GetPositionOfCell(blockPos, column).x - GetPositionOfCell(nullPos, column).x);

            for (float t = 0; t < 1; t += Time.deltaTime * 4)
            {
                for (int i = blockPos; i < gridSize.y; ++i)
                {
                    grid[i, column].transform.position = Vector2.Lerp(new Vector2(x, ys[i - blockPos]), new Vector2(x, ys[i - blockPos] - dy), t);
                }
                yield return null;
            }

            for (int i = blockPos; i < gridSize.y; ++i)
            {
                grid[i, column].transform.position = new Vector2(x, ys[i - blockPos] - dy);
                grid[i, column].position = new Vector2Int(column, nullPos + (i - blockPos));
                grid[nullPos + (i - blockPos), column] = grid[i, column];
                grid[i, column] = null;
            }
        }

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < gridSize.y; ++i)
        {
            if(grid[i, column] == null)
            {
                Block block = InstantiateBlock();
                block.transform.localScale = GetScale() * Vector2.one;
                block.transform.position = GetPositionOfCell(column, i);
                int index = Random.Range(0, colorSize);
                if (column == 0)
                {
                    while ((i > 0 && grid[i - 1, column].colorIndex == index) || (grid[i, column + 1] != null && grid[i, column + 1].colorIndex == index)) index = Random.Range(0, colorSize);
                }
                else if (column == gridSize.x - 1)
                {
                    while ((i > 0 && grid[i - 1, column].colorIndex == index) || (grid[i, column - 1] != null && grid[i, column - 1].colorIndex == index)) index = Random.Range(0, colorSize);
                }
                else
                {
                    while ( 
                           (i > 0 && grid[i - 1, column].colorIndex == index) ||
                           (grid[i, column - 1] != null && grid[i, column - 1].colorIndex == index) ||
                           (grid[i, column + 1] != null && grid[i, column + 1].colorIndex == index))
                        index = Random.Range(0, colorSize);
                }
                block.Setup(index, colors[index], new Vector2Int(column, i));
                grid[i, column] = block;
            }
        }
        PrintGrid();
    }

    public Block InstantiateBlock()
    {
        if (blockPool.Count > 0) return blockPool.Dequeue();
        return Instantiate(blockPrefab, faraway, Quaternion.identity, gridParent).GetComponent<Block>();
    }

    public void PrintGrid()
    {
        string s = "";
        for(int i = gridSize.y - 1; i >= 0; --i)
        {
            for (int j = 0; j < gridSize.x; ++j) s += grid[i, j] == null ? "o" : "x";
            if(i > 0) s += "\n";
        }
        print(s);
    }

    public void PoolBlock(Block block)
    {
        blockPool.Enqueue(block);
        block.transform.position = faraway;
    }

    public void Simulate(bool fast)
    {
        if (!isSimulating)
        {
            allowInput = false;
            isSimulating = true;
            StartCoroutine(SimulationRoutine(fast));
        }
        else
        {
            allowInput = true;
            isSimulating = false;
        }
    }


    IEnumerator SimulationRoutine(bool fast)
    {
        int swaps = 0;
        UIController.instance.UpdateSimulation(swaps);
        yield return null;

        while (isSimulating)
        {
            Block b1 = grid[Random.Range(0, grid.GetLength(0)), Random.Range(0, grid.GetLength(1))];
            Block b2 = b1.GetAdjacentBlock();
            FastSwap(b1, b2);
            swaps++;
            UIController.instance.UpdateSimulation(swaps);
            if (fast) yield return null;
            else yield return new WaitForSeconds(1);
        }
    }

    public void FastSwap(Block b1, Block b2)
    {
        if (currentBlock != null) currentBlock.SetSelected(false);
        currentBlock = null;
        Vector2Int aux = b1.position;
        b1.position = b2.position;
        b2.position = aux;
        Vector2 pos1 = b1.transform.position, pos2 = b2.transform.position;
        b1.transform.position = pos2;
        b2.transform.position = pos1;
        grid[b1.position.y, b1.position.x] = b1;
        grid[b2.position.y, b2.position.x] = b2;

        FastDestroy(b1, b2);
    }


    void FastDestroy(Block b1, Block b2)
    {
        List<Block> blocksToDestroy = new List<Block>();

        //horizontal check b1
        int left = b1.position.x, right = left;
        while (left > 0 && grid[b1.position.y, left - 1].colorIndex == b1.colorIndex) --left;
        while (right < gridSize.x - 1 && grid[b1.position.y, right + 1].colorIndex == b1.colorIndex) ++right;
        if (right - left >= 2) for (int i = left; i <= right; ++i) blocksToDestroy.Add(grid[b1.position.y, i]);

        //horizontal check b2
        left = b2.position.x; right = left;
        while (left > 0 && grid[b2.position.y, left - 1].colorIndex == b2.colorIndex) --left;
        while (right < gridSize.x - 1 && grid[b2.position.y, right + 1].colorIndex == b2.colorIndex) ++right;
        if (right - left >= 2) for (int i = left; i <= right; ++i) blocksToDestroy.Add(grid[b2.position.y, i]);

        //vertical check b1
        int bottom = b1.position.y, top = bottom;
        while (bottom > 0 && grid[bottom - 1, b1.position.x].colorIndex == b1.colorIndex) --bottom;
        while (top < gridSize.y - 1 && grid[top + 1, b1.position.x].colorIndex == b1.colorIndex) ++top;
        if (top - bottom >= 2) for (int i = bottom; i <= top; ++i) blocksToDestroy.Add(grid[i, b1.position.x]);

        //vertical check b2
        bottom = b2.position.y; top = bottom;
        while (bottom > 0 && grid[bottom - 1, b2.position.x].colorIndex == b2.colorIndex) --bottom;
        while (top < gridSize.y - 1 && grid[top + 1, b2.position.x].colorIndex == b2.colorIndex) ++top;
        if (top - bottom >= 2) for (int i = bottom; i <= top; ++i) blocksToDestroy.Add(grid[i, b2.position.x]);

        blocksToDestroy = blocksToDestroy.GroupBy(x => x.position).Select(x => x.First()).ToList();

        HashSet<int> columns = new HashSet<int>();

        UIController.instance.AddScore(blocksToDestroy.Count);
        foreach (Block block in blocksToDestroy)
        {
            grid[block.position.y, block.position.x] = null;
            columns.Add(block.position.x);
            PoolBlock(block);
        }

        foreach (int column in columns) FastUpdateColumn(column);
    }

    void FastUpdateColumn(int column)
    {
        int nullPos = -1;
        int blockPos = -1;
        for (int i = 0; i < gridSize.y; ++i) if (grid[i, column] == null) { nullPos = i; break; }

        if (nullPos == -1) return;

        for (int i = nullPos; i < gridSize.y; ++i) if (grid[i, column] != null) { blockPos = i; break; }

        if (blockPos != -1)
        {
            float x = grid[blockPos, column].transform.position.x;
            List<float> ys = new List<float>();
            for (int i = blockPos; i < gridSize.y; ++i) ys.Add(grid[i, column].transform.position.y);

            float dy = Mathf.Abs(GetPositionOfCell(blockPos, column).x - GetPositionOfCell(nullPos, column).x);

            for (int i = blockPos; i < gridSize.y; ++i)
            {
                grid[i, column].transform.position = new Vector2(x, ys[i - blockPos] - dy);
                grid[i, column].position = new Vector2Int(column, nullPos + (i - blockPos));
                grid[nullPos + (i - blockPos), column] = grid[i, column];
                grid[i, column] = null;
            }
        }

        for (int i = 0; i < gridSize.y; ++i)
        {
            if (grid[i, column] == null)
            {
                Block block = InstantiateBlock();
                block.transform.localScale = GetScale() * Vector2.one;
                block.transform.position = GetPositionOfCell(column, i);
                int index = Random.Range(0, colorSize);
                if (column == 0)
                {
                    while ((i > 0 && grid[i - 1, column].colorIndex == index) || (grid[i, column + 1] != null && grid[i, column + 1].colorIndex == index)) index = Random.Range(0, colorSize);
                }
                else if (column == gridSize.x - 1)
                {
                    while ((i > 0 && grid[i - 1, column].colorIndex == index) || (grid[i, column - 1] != null && grid[i, column - 1].colorIndex == index)) index = Random.Range(0, colorSize);
                }
                else
                {
                    while (
                           (i > 0 && grid[i - 1, column].colorIndex == index) ||
                           (grid[i, column - 1] != null && grid[i, column - 1].colorIndex == index) ||
                           (grid[i, column + 1] != null && grid[i, column + 1].colorIndex == index))
                        index = Random.Range(0, colorSize);
                }
                block.Setup(index, colors[index], new Vector2Int(column, i));
                grid[i, column] = block;
            }
        }
    }
}
