using OpenCvSharp;

var rnd = new Random();
var colors = Enumerable
    .Range(0, 100)
    .Select(i => new Scalar(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)))
    .ToArray();

// 検出する特徴点の最高数
const int maxCorners = 100;
// 特徴点のスコアの閾値。0~1で1が最高。
const double qualityLevel = 0.3;
// 特徴点間の最小距離
const int minDistance = 100;
//  特徴点の検出範囲を指定するマスク行列。nullだと画像全体で検出。
Mat rangeMask = null;
// 特徴点の検出に使用される近傍領域のサイズ
const int blockSize = 7;
// falseではShi-Tomasi検出器になる
const bool useHarrisDetector = true;
// ハリスのコーナー検出器で使う自由パラメータ。通常0.04~0.06を指定する。
// 小さいとコーナーが多く検出される。大きいとより強い勾配の変化をもつ点が検出される。
const double k = 0.04;

// 最初のフレームの処理
var path = @"";
using var capture = new VideoCapture(path);
using var frameNext = new Mat();
var endFlag = capture.Read(frameNext);
var grayPrev = frameNext.CvtColor(ColorConversionCodes.BGR2GRAY);
var featuresPrev =
    grayPrev.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
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
        err:out _,
        winSize:new Size(21, 21),
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
    Cv2.WaitKey();

    // 次のフレーム、ポイントの準備
    grayPrev = grayNext.Clone();
    if (status.Contains((byte)1))
    {
        featuresPrev = featuresNext;
    }
    else
    { 
        // 特徴点が見つからなかった場合、再度特徴点を検出
        featuresPrev = grayPrev.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
    }

    endFlag = capture.Read(frameNext);
}