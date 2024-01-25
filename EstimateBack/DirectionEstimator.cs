using System.Diagnostics;
using EstimateBack.Interface;
using OpenCvSharp;

namespace EstimateBack;

public class DirectionEstimator : IDirectionEstimator
{
    public IQuadrant FirstQuadrant { get; }
    public IQuadrant SecondQuadrant { get; }
    public IQuadrant ThirdQuadrant { get; }
    public IQuadrant ForthQuadrant { get; }

    /// <summary>
    /// オプティカルフローを用いて移動ベクトルを算出し進行方向を決定する
    /// </summary>
    /// <param name="prev"></param>
    /// <param name="next"></param>
    public DirectionEstimator(Mat prev, Mat next)
    {
        var blockHeight = prev.Rows / 2;
        var blockWidth = prev.Cols / 2;

        var firstBlock = new Rect(1 * blockWidth, 0 * blockHeight, blockWidth, blockHeight);
        var secondBlock = new Rect(0 * blockWidth, 0 * blockHeight, blockWidth, blockHeight);
        var thirdBlock = new Rect(0 * blockWidth, 1 * blockHeight, blockWidth, blockHeight);
        var forthBlock = new Rect(1 * blockWidth, 1 * blockHeight, blockWidth, blockHeight);
        FirstQuadrant = new Quadrant(Quad.First, new Mat(prev, firstBlock), new Mat(next, firstBlock));
        SecondQuadrant = new Quadrant(Quad.Second, new Mat(prev, secondBlock), new Mat(next, secondBlock));
        ThirdQuadrant = new Quadrant(Quad.Third, new Mat(prev, thirdBlock), new Mat(next, thirdBlock));
        ForthQuadrant = new Quadrant(Quad.Forth, new Mat(prev, forthBlock), new Mat(next, forthBlock));
    }

    public Direction EstimateDirection()
    {
        var first = FirstQuadrant.GetDirection();
        var second = SecondQuadrant.GetDirection();
        var third = ThirdQuadrant.GetDirection();
        var forth = ForthQuadrant.GetDirection();

        var isFirstBack = first == CardinalDirection.LeftDown;
        var isSecondBack = second == CardinalDirection.RightDown;
        var isThirdBack = third == CardinalDirection.RightUp;
        var isForthBack = forth == CardinalDirection.LeftUp;

        // 全部前進判定
        //if (isFirstBack &&
        //    isSecondBack &&
        //    isThirdBack &&
        //    isForthBack)
        //{
        //    Debug.WriteLine("後退");
        //    return Direction.Backward;
        //}

        //// 対角の象限に着目して判定
        //if (isFirstBack && isThirdBack ||
        //    isSecondBack && isForthBack)
        //{
        //    Debug.WriteLine("後退");
        //    return Direction.Backward;
        //}

        var cnt = new List<bool>(){isFirstBack, isSecondBack, isThirdBack, isForthBack}
            .Count(x => x);
        if (cnt >= 2)
        {
            return Direction.Backward;
        }


        return Direction.Forward;
    }
}
