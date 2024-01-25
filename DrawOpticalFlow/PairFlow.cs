using OpenCvSharp;

namespace DrawOpticalFlow;

public class PairFlow
{
    private Mat _nextMat = new();
    private Point2f[] _prevFeatures = {new Point2f()};
    private Point2f[] _nextFeatures = {new Point2f()};
    private byte[]? _prevStatus;
    private float[] _prevError;

    private static readonly Random Rnd = new();
    private static readonly Scalar[] Colors = Enumerable
        .Range(0, 100)
        .Select(i => new Scalar(Rnd.Next(0, 256), Rnd.Next(0, 256), Rnd.Next(0, 256)))
        .ToArray();

    public static PairFlow Init() => new();

    public (Point2f prevFeature, Point2f nextFeature) Calc(Mat prevMat, Mat nextMat)
    {
        using var grayPrev = prevMat.CvtColor(ColorConversionCodes.BGR2GRAY).EqualizeHist();
        //_nextMat.Dispose();
        _nextMat = nextMat;

        const int maxCorners = 1;
        const double qualityLevel = 0.3;
        const int minDistance = 7;
        Mat? rangeMask = null;
        const int blockSize = 7;
        const bool useHarrisDetector = false;
        const double k = 0.04;

        using var mask = new Mat(grayPrev.Size(), MatType.CV_8UC3, Scalar.All(0));
        using var grayNext = nextMat.CvtColor(ColorConversionCodes.BGR2GRAY).EqualizeHist();
        // 初回と連続する特徴点（オプティカルフロー）が見つからなかったとき
        if (_prevStatus == null || !_prevStatus.Contains((byte)1))
        {
            _prevFeatures = grayPrev.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
            if (_prevFeatures.Length == 0) return (_prevFeatures.FirstOrDefault(), _nextFeatures.FirstOrDefault());
        }
        else if( _prevStatus.Contains((byte)1))
        {
            _prevFeatures = _nextFeatures;
        }
        Console.WriteLine(_prevStatus?.Count(x => x is 1));

        Cv2.CalcOpticalFlowPyrLK(
            grayPrev,
            grayNext,
            _prevFeatures,
            ref _nextFeatures,
            out var status,
            out var errors,
            new Size(21, 21),
            2,
            TermCriteria.Both(10, 0.03));

            _prevStatus = status;
            _prevError = errors;

            return (_prevFeatures.FirstOrDefault(), _nextFeatures.FirstOrDefault());
    }

    public Mat Draw(Mat prevMat)
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
        //Cv2.ImShow("flow", drawnMat);
        //Cv2.WaitKey();

        return drawnMat;
        drawnMat.Dispose();
        mask.Dispose();
    }
}