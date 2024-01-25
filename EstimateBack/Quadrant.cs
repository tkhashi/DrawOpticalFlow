using System.Diagnostics;
using EstimateBack.Interface;
using OpenCvSharp;

namespace EstimateBack;

public class Quadrant : IQuadrant
{
    public Quad Quad { get; }

    public ICollection<IGrid> Grids { get; } = new List<IGrid>();

    private const int Row = 3;
    private const int Col = 4;

    /// <summary>
    /// 一つの象限における前後のMat
    /// </summary>
    /// <param name="quad">象限</param>
    /// <param name="prev"></param>
    /// <param name="next"></param>
    public Quadrant(Quad quad, Mat prev, Mat next)
    {
        Quad = quad;

        var prevBlocks = prev.Blocking(Row, Col);
        var nextBlocks = next.Blocking(Row, Col);

        var (skipCol, skipRow) = quad switch
        {
            Quad.First => (0, 3),
            Quad.Second => (0, 0),
            Quad.Third => (3, 0),
            _ => (3, 3),
        };
        for (var i = 0; i < Col; i++)
        for (var j = 0; j < Row; j++)
        {
            if (i == skipCol) continue;
            if (j == skipRow) continue;
            Grids.Add(new Grid(prevBlocks[i, j], nextBlocks[i, j]));
        }


    }

    public CardinalDirection GetDirection()
    {
        var dir = Grids.Select(x => x.GetOpticalFlowDirection()).Max();

        return dir;
    }

    //public void SetNextMat(Mat next)
    //{
       
    //}
}
