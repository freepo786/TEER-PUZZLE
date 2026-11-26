using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeerPuzzle
{
    /// <summary>
    /// One cell of the grid. Column 0..width-1 goes left to right,
    /// row 0..height-1 goes top to bottom.
    /// </summary>
    [Serializable]
    public class Cell
    {
        public int column;
        public int row;

        public Cell() { }

        public Cell(int column, int row)
        {
            this.column = column;
            this.row = row;
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(column, row);
        }
    }

    /// <summary>
    /// One arrow, described as a path of cells from tail to head.
    /// The last cell is the head; the exit direction is from the
    /// second-last cell towards the last cell.
    /// </summary>
    [Serializable]
    public class ArrowData
    {
        // Index into the active theme's arrow color palette.
        public int color;

        // Cells of the path, tail first, head last. Every step moves
        // exactly one cell orthogonally (up / down / left / right).
        public List<Cell> nodalPoints = new List<Cell>();

        public ArrowData() { }

        public ArrowData(int color, List<Cell> nodalPoints)
        {
            this.color = color;
            this.nodalPoints = nodalPoints;
        }
    }

    /// <summary>
    /// One level, loaded from JSON in Resources/Levels with
    /// JsonUtility. No dictionaries, so it stays JsonUtility-compatible.
    /// Example file (level_001.json):
    /// {"id":1,"pack":"easy","width":5,"height":5,"lives":3,
    ///  "arrows":[{"color":0,"nodalPoints":[
    ///  {"column":1,"row":4},{"column":1,"row":3},{"column":2,"row":3}]}]}
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int id;
        public string pack;
        public int width;
        public int height;
        public int lives;
        public List<ArrowData> arrows = new List<ArrowData>();
    }
}
