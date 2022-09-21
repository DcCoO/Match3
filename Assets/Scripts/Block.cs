using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public int colorIndex;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteRenderer lighting;
    public Vector2Int position;

    static readonly int timeScaleID = Shader.PropertyToID("_TimeScale");

    public void Setup(int colorIndex, Color color, Vector2Int position)
    {
        this.colorIndex = colorIndex;
        spriteRenderer.color = color;
        this.position = position;
    }

    public bool IsNeighbour(Block block) =>
        (Mathf.Abs(position.x - block.position.x) == 0 && Mathf.Abs(position.y - block.position.y) == 1) ||
        (Mathf.Abs(position.x - block.position.x) == 1 && Mathf.Abs(position.y - block.position.y) == 0);

    public Block GetAdjacentBlock()
    {
        int random = Random.Range(0, 4);
        if (random == 0 && position.x > 0) return Match3Controller.instance.grid[position.y, position.x - 1];
        if (random == 1 && position.x < Match3Controller.instance.gridSize.x - 1) return Match3Controller.instance.grid[position.y, position.x + 1];
        if (random == 2 && position.y > 0) return Match3Controller.instance.grid[position.y - 1, position.x];
        if (random == 3 && position.y < Match3Controller.instance.gridSize.y - 1) return Match3Controller.instance.grid[position.y + 1, position.x];
        return GetAdjacentBlock();
    }

    private void OnMouseDown()
    {
        if (Match3Controller.instance.allowInput)
        {
            Match3Controller.instance.SelectBlock(this);
        }
    }

    public void SetSelected(bool state) => spriteRenderer.material.SetFloat(timeScaleID, state ? 3f : 0);

    public void SetLayer(bool ahead)
    {
        if (ahead)
        {
            spriteRenderer.sortingOrder = 100;
            lighting.sortingOrder = 101;
        }
        else
        {
            spriteRenderer.sortingOrder = 0;
            lighting.sortingOrder = 1;
        }
    }
    

    public bool SameColor(Block block) => block.colorIndex == colorIndex;
}
