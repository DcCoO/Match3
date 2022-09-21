using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    public Vector2 rowRange, columnRange, colorRange;
    int rows = 4, columns = 4, colors = 4, score = 0;
    public Button rowsPlus, rowsMinus, columnsPlus, columnsMinus, colorsPlus, colorsMinus;
    public Text rowsText, columnsText, colorsText;
    public Text simulateSlowText, simulateFastText;
    public Text scoreText;
    public Text simulationCount;

    public bool isSimulating;

    private void Awake() => instance = this;

    private void OnDestroy() => instance = null;

    void Start()
    {
        rowsText.text = $"{rows}";
        columnsText.text = $"{columns}";
        colorsText.text = $"{colors}";
    }

    public void ResetScore()
    {
        score = 0;
        simulationCount.text = "0";
        UpdateScore();
    }

    public void AddScore(int addition)
    {
        score += addition;
        UpdateScore();
    }

    public void UpdateScore() => scoreText.text = $"{score}";  
    
    public void UpdateSimulation(int swaps) => simulationCount.text = $"{swaps}";

    public void AdjustRows(int offset)
    {
        rows += offset;
        rowsText.text = $"{rows}";
        rowsMinus.interactable = rows > rowRange.x;
        rowsPlus.interactable = rows < rowRange.y;
        Match3Controller.instance.gridSize = new Vector2Int(columns, rows);
        Match3Controller.instance.InitGrid();
        ResetScore();
    }

    public void AdjustColumns(int offset)
    {
        columns += offset;
        columnsText.text = $"{columns}";
        columnsMinus.interactable = columns > columnRange.x;
        columnsPlus.interactable = columns < columnRange.y;
        Match3Controller.instance.gridSize = new Vector2Int(columns, rows);
        Match3Controller.instance.InitGrid();
        ResetScore();
    }

    public void AdjustColors(int offset)
    {
        colors += offset;
        colorsText.text = $"{colors}";
        colorsMinus.interactable = colors > colorRange.x;
        colorsPlus.interactable = colors < colorRange.y;
        Match3Controller.instance.colorSize = colors;
        Match3Controller.instance.InitGrid();
        ResetScore();
    }

    public void ResetGame()
    {
        Match3Controller.instance.InitGrid();
        ResetScore();
    }

    public void Simulate(bool fast)
    {
        if (!isSimulating)
        {
            simulateSlowText.text = simulateFastText.text = "stop";           
        }
        else
        {
            simulateSlowText.text = "simu slow";
            simulateFastText.text = "simu fast";
        }
        Match3Controller.instance.Simulate(fast);
        isSimulating = !isSimulating;
    }    
}
