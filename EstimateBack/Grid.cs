using EstimateBack.Interface;
using OpenCvSharp;

namespace EstimateBack;

public class Grid : IGrid
{
    public Mat PrevMat { get; }
    public Mat NextMat { get; }
    private readonly float _originX;
    private readonly float _originY;

    //private Point2f[] _prevFeatures = {new Point2f()};
    //private Point2f[] _nextFeatures = {new Point2f()};
    //private byte[]? _prevStatus;
    private static readonly Random Rnd = new();
    private static readonly Scalar[] Colors = Enumerable
        .Range(0, 100)
        .Select(i => new Scalar(Rnd.Next(0, 256), Rnd.Next(0, 256), Rnd.Next(0, 256)))
        .ToArray();

    public Grid(Mat prev, Mat next)
    {
        PrevMat = prev;
        NextMat = next;
        _originX = prev.Width / 2f;
        _originY = prev.Height / 2f;
    }

    public CardinalDirection GetOpticalFlowDirection()
    {
        const int maxCorners = 100;
        const double qualityLevel = 0.3;
        const int minDistance = 7;
        Mat? rangeMask = null;
        const int blockSize = 7;
        const bool useHarrisDetector = false;
        const double k = 0.04;

        using var mask = new Mat(PrevMat.Size(), MatType.CV_8UC3, Scalar.All(0));
        var prevFeatures = PrevMat.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
        if (prevFeatures.Length == 0) return CardinalDirection.None;


        var nextFeatures = new List<Point2f>().ToArray();
        Cv2.CalcOpticalFlowPyrLK(
            PrevMat,
            NextMat,
            prevFeatures,
            ref nextFeatures,
            out var status,
            out var errors,
            new Size(21, 21),
            2,
            TermCriteria.Both(10, 0.03));

        // 一番多い方向を代表方向として採用
        var direction = prevFeatures.Zip(nextFeatures).Select(x =>
        {
            var (prevFeature, nextFeature) = (x.First, x.Second);
            var delta = nextFeature - prevFeature;

           return delta.X switch
            {
                0 when delta.Y == 0 => CardinalDirection.None,
                >= 0 when delta.Y >= 0 => CardinalDirection.RightDown,
                >= 0 when delta.Y <= 0 => CardinalDirection.RightUp,
                <= 0 when delta.Y >= 0 => CardinalDirection.LeftDown,
                <= 0 when delta.Y <= 0 => CardinalDirection.LeftUp,
                _ => CardinalDirection.None,
            };
        })
            .Max();

        //using var grayMask = new Mat(PrevMat.Size(), MatType.CV_8UC1, Scalar.All(0));
        //for (var j = 0; j < status?.Length; j++)
        //{
        //    if (status[j] == 0) continue;

        //    // オプティカルフローを描画
        //    grayMask.Line(nextFeatures[j].ToPoint(), prevFeatures[j].ToPoint(), Colors[j % 100], 2);
        //    NextMat.Circle((int)nextFeatures[j].X, (int)nextFeatures[j].Y, 5, Colors[j % 100], -1);
        //}
        //using var drawnMat = NextMat.Add(grayMask);
        //Cv2.ImShow("draw", drawnMat);
        //Cv2.WaitKey(1);

        return direction;
    }
}
