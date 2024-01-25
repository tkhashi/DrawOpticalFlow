using System.Collections.Concurrent;
using OpenCvSharp;

namespace EstimateBack;

public static class MatExtensions
{
    public static Mat[,] Blocking(this Mat origin, int row, int col)
    {
        var blockHeight = origin.Height / row;
        var blockWidth = origin.Width / col;

        var blocks = new Mat[blockHeight, blockWidth];
        for (var i = 0; i < col; i++)
        {
            for (var j = 0; j < row; j++)
            {
                var blockRect = new Rect(i * blockWidth, j * blockHeight, blockWidth, blockHeight);
                blocks[i, j] = new Mat(origin, blockRect);
            }
        }

        return blocks;
    }
}