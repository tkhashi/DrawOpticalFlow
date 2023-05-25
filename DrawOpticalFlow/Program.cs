using OpenCvSharp;

var rnd = new Random();
var colors = Enumerable
    .Range(0, 100)
    .Select(i => new Scalar(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)))
    .ToArray();

// Shi-Tomasiのコーナー検出パラメータ
const int maxCorners = 100;
const double qualityLevel = 0.3;
const int minDistance = 7;
const int blockSize = 7;
const bool useHarrisDetector = false;
const double k = 0.04;

// 最初のフレームの処理
using var capture = new VideoCapture(@"C:\Users\k_tak\Downloads\534-21-62-2下流調査-ガタツキ部のみ.mp4");
using var frameNext = new Mat();
var endFlag = capture.Read(frameNext);
var grayPrev = frameNext.CvtColor(ColorConversionCodes.BGR2GRAY);
var featuresPrev =
    grayPrev.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, null, blockSize, useHarrisDetector, k);
using var mask = new Mat(frameNext.Size(), MatType.CV_8UC3, Scalar.All(0));

while (endFlag)
{
    // オプティカルフロー検出
    var featuresNext = new Point2f[] { };
    var grayNext = frameNext.CvtColor(ColorConversionCodes.BGR2GRAY);
    Cv2.CalcOpticalFlowPyrLK(
        grayPrev,
        grayNext,
        featuresPrev,
        ref featuresNext,
        out var status,
        out _,
        new Size(15, 15),
        2,
        TermCriteria.Both(10, 0.03));

    // オプティカルフローを検出した特徴点を選別（0：検出せず、1：検出した）
    for (var i = 0; i < status.Length; i++)
        if (status[i] == 1)
        {
            // オプティカルフローを描画
            mask.Line(featuresNext[i].ToPoint(), featuresPrev[i].ToPoint(), colors[i % 100], 2);
            frameNext.Circle((int)featuresNext[i].X, (int)featuresNext[i].Y, 5, colors[i % 100], -1);
        }

    using var drawnMat = frameNext.Add(mask);
    Cv2.ImShow("flow", drawnMat);
    Cv2.WaitKey(1);

    // 次のフレーム、ポイントの準備
    grayPrev = grayNext.Clone();
    featuresPrev = status.Contains((byte)1)
        ? featuresNext
        : Cv2.GoodFeaturesToTrack(grayPrev, maxCorners, qualityLevel, minDistance, null, blockSize, useHarrisDetector,
            k); // 特徴点が見つからなかった場合、再度特徴点を検出

    endFlag = capture.Read(frameNext);
}