using DrawOpticalFlow;
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
const int minDistance = 7;
// 特徴点の検出範囲を指定するマスク行列。nullだと画像全体で検出。
Mat rangeMask = null;
// 特徴点の検出に使用される近傍領域のサイズ
const int blockSize = 7;
// falseではShi-Tomasi検出器になる
const bool useHarrisDetector = true;
// ハリスのコーナー検出器で使う自由パラメータ。通常0.04~0.06を指定する。
// 小さいとコーナーが多く検出される。大きいとより強い勾配の変化をもつ点が検出される。
const double k = 0.06;

// 最初のフレームの処理
var path = @"";
using var capture = new VideoCapture(path);

var prev = capture.RetrieveMat();
var flow = PairFlow.Init();
for (var i = 0; i < capture.FrameCount - 1; i++)
{
    using var next = capture.RetrieveMat();

    flow.Calc(prev, next);
    flow.Draw(prev);

    prev.Dispose();
    prev = next.Clone();
}
return;

// 直前のフレームの処理
using var framePrev = capture.RetrieveMat();

//// 例: 中心辺りで特徴点検出する場合
//rangeMask = Mat.Zeros(framePrev.Size(), MatType.CV_8UC1);
//var rect = new Rect(100, 100, 200, 200);
//var center = new Point(rect.Left + rangeMask.Width / 2, rect.Top + rangeMask.Height / 2);
//var maskRegion = new Rect(center.X - rect.Width / 2, center.Y - rect.Height / 2, rect.Width, rect.Height);
//rangeMask[maskRegion].SetTo(Scalar.White);

var grayPrev = framePrev.CvtColor(ColorConversionCodes.BGR2GRAY)
        ;
        //.SubMatEx(framePrev);
var featuresPrev = grayPrev.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
using var mask = new Mat(grayPrev.Size(), MatType.CV_8UC3, Scalar.All(0));

for (var i = 0; i < capture.FrameCount - 1; i++)
{
    // オプティカルフロー検出
    using var frameNext = capture.RetrieveMat()
        ;
        //.SubMatEx(framePrev);
;
    var grayNext = frameNext.CvtColor(ColorConversionCodes.BGR2GRAY);
    var featuresNext = grayNext.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
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

    using var prevCornerMat = new Mat(featuresPrev.Length, 1, MatType.CV_32FC2, featuresPrev);
    using var nextCornerMat = new Mat(featuresNext.Length, 1, MatType.CV_32FC2, featuresNext);
    using var matrix = Cv2.EstimateAffinePartial2D(nextCornerMat, prevCornerMat);
    using var adjusted = frameNext.WarpAffine(matrix, frameNext.Size());

    Cv2.ImShow("adjust frame", adjusted);
    Cv2.WaitKey(1);

    // オプティカルフローを検出した特徴点を選別（0：検出せず、1：検出した）
    for (var j = 0; j < status.Length; j++)
    {
        if (status[j] == 0) continue;

        // オプティカルフローを描画
        mask.Line(featuresNext[j].ToPoint(), featuresPrev[j].ToPoint(), colors[j % 100], 2);
        frameNext.Circle((int)featuresNext[j].X, (int)featuresNext[j].Y, 5, colors[j % 100], -1);
    }
    using var drawnMat = frameNext.Add(mask);
    Cv2.ImShow("flow", drawnMat);
    Cv2.WaitKey(1);

    // 次のフレーム、ポイントの準備
    grayPrev = grayNext.Clone();
    if (status.Contains((byte)1))
    {
        featuresPrev = featuresNext;
    }
    else
    { 
        // 特徴点が見つからなかった場合、再度特徴点を検出
        featuresPrev = grayNext.GoodFeaturesToTrack(maxCorners, qualityLevel, minDistance, rangeMask, blockSize, useHarrisDetector, k);
    }
}

public static class MatEx
{
    public static Mat SubMatEx(this Mat target, Mat framePrev)
    {
        return target.SubMat(Rect.FromLTRB(
        (int)(framePrev.Width * 4 / 10d),
        (int)(framePrev.Height * 2 / 10d),
        (int)(framePrev.Width * 6 / 10d),
        (int)(framePrev.Height * 8 / 10d)));
    }
}