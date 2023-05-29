using OpenCvSharp;

namespace DrawOpticalFlow;

public class PairFlow
{
    private Mat _nextMat = new();
    private Point2f[] _prevFeatures = {new Point2f()};
    private Point2f[] _nextFeatures = {new Point2f()};
    private byte[]? _prevStatus;

    private static readonly Random Rnd = new();
    private static readonly Scalar[] Colors = Enumerable
        .Range(0, 100)
        .Select(i => new Scalar(Rnd.Next(0, 256), Rnd.Next(0, 256), Rnd.Next(0, 256)))
        .ToArray();

    public static PairFlow Init() => new();

    public void Calc(Mat prevMat, Mat nextMat)
    {
        using var grayPrev = prevMat.CvtColor(ColorConversionCodes.BGR2GRAY);
        _nextMat.Dispose();
        _nextMat = nextMat;

        const int maxCorners = 100;
        const double qualityLevel = 0.3;
        const int minDistance = 7;
        Mat? rangeMask = null;
        const int blockSize = 7;
        const bool useHarrisDetector = true;
        const double k = 0.06;

        using var mask = new Mat(grayPrev.Size(), MatType.CV_8UC3, Scalar.All(0));
        using var grayNext = nextMat.CvtColor(ColorConversionCodes.BGR2GRAY);
        // 初回と連続する特徴点（オプティカルフロー）が見つからなかったとき
        if (_prevStatus == null || !_prevStatus.Contains((byte)1))
        {
            _prevFeatures = grayPrev.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
        }
        else if( _prevStatus.Contains((byte)1))
        {
            _prevFeatures = _nextFeatures;
        }

        Cv2.CalcOpticalFlowPyrLK(
            grayPrev,
            grayNext,
            _prevFeatures,
            ref _nextFeatures,
            out var status,
            out _,
            new Size(21, 21),
            2,
            TermCriteria.Both(10, 0.03));

            _prevStatus = status;
    }

    public void Draw(Mat prevMat)
    {
        using var mask = new Mat(prevMat.Size(), MatType.CV_8UC3, Scalar.All(0));
        prevMat.Dispose();
        // オプティカルフローを検出した特徴点を選別（0：検出せず、1：検出した）
        for (var j = 0; j < _prevStatus?.Length; j++)
        {
            if (_prevStatus[j] == 0) continue;

            // オプティカルフローを描画
            mask.Line(_nextFeatures[j].ToPoint(), _prevFeatures[j].ToPoint(), Colors[j % 100], 2);
            _nextMat.Circle((int)_nextFeatures[j].X, (int)_nextFeatures[j].Y, 5, Colors[j % 100], -1);
        }
        using var drawnMat = _nextMat.Add(mask);
        Cv2.ImShow("flow", drawnMat);
        Cv2.WaitKey(1);

        drawnMat.Dispose();
        mask.Dispose();
    }
}