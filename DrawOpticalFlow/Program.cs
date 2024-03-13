using System.Collections;
using System.Runtime.InteropServices;
using DrawOpticalFlow;
using OpenCvSharp;

var rnd = new Random();
var colors = Enumerable
    .Range(0, 100)
    .Select(i => new Scalar(rnd.Next(0, 256), rnd.Next(0, 256), rnd.Next(0, 256)))
    .ToArray();

// 検出する特徴点の最高数
const int maxCorners = 1000;
// 特徴点のスコアの閾値。0~1で1が最高。
const double qualityLevel = 0.2;
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
const double k = 0.04;

// 最初のフレームの処理
//const string path = @"C:\Users\k_tak\Downloads\534-21-62-2下流調査_ガタツキ~後退.mp4";
//const string path = @"C:\Users\k_tak\Downloads\534-21-62-2下流調査-ガタツキ部のみ.mp4";

//const string path = @"C:\Users\k_tak\Downloads\Camera01-selected\VID_20230825_013822_10_011 - コピー_trimmed.mp4";
const string path = @"C:\Users\k_tak\Downloads\boring_trimmed.mp4";
using var capture = new VideoCapture(path);


const int col = 5*2;
const int row = 5*2;
//var store = new GridStore(capture.RetrieveMat(), col, row);
//for (var i = 0; i < capture.FrameCount - 1; i++)
//{
//    store.Estimate(capture.RetrieveMat());
//    store.Draw();
//}

//return;



//var prev = capture.RetrieveMat().Grid(col, row);
//var flow = PairFlow.Init();
//using var drawnMats = new Mat();
//for (var i = 0; i < capture.FrameCount - 1; i++)
//{
//    var h = Enumerable.Repeat(new Mat(), col).ToArray();
//    var v = Enumerable.Repeat(new Mat(), row).ToArray();
//    var next = capture.RetrieveMat().Grid(col, row);
//    for (var c = 0; c < col; c++)
//    {
//        for (var r = 0; r < row; r++)
//        {
//            flow.Calc(prev[c, r], next[c, r]);
//            var d = flow.Draw(prev[c, r]);

//            v[r] = d;
//        }

//        Cv2.VConcat(v, h[c]);
//        h[c] = h[c].Clone();
//    }
//    Cv2.HConcat(h, drawnMats);
//    Cv2.ImShow("drawn", drawnMats);
//    Cv2.WaitKey();

//    //flow.Calc(prev, next);
//    //flow.Draw(prev);

//    for (var c = 0; c < col; c++)
//    for (var r = 0; r < row; r++)
//    {
//        prev[c, r].Dispose();
//        prev[c, r] = next[c, r].Clone();
//    }
//}
//return;

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
var mask = new Mat(grayPrev.Size(), MatType.CV_8UC3, Scalar.All(0));

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

    adjusted.Circle(new Point(adjusted.Width / 2, adjusted.Height / 2), 3, Scalar.Red, 1, LineTypes.AntiAlias);
    //Cv2.ImShow("adjust frame", adjusted);
    //Cv2.WaitKey(1);

    // オプティカルフローを検出した特徴点を選別（0：検出せず、1：検出した）
    for (var j = 0; j < status.Length; j++)
    {
        if (status[j] == 0) continue;

        // オプティカルフローを描画
        mask.Line(featuresNext[j].ToPoint(), featuresPrev[j].ToPoint(), colors[j % 100], 1, LineTypes.AntiAlias);
        frameNext.Circle((int)featuresNext[j].X, (int)featuresNext[j].Y, 5, colors[j % 100], -1);
    }
    using var drawnMat = frameNext.Add(mask);
    Cv2.ImShow("flow", drawnMat.ToMat().Resize(Size.Zero, 0.5, 0.5));
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
        mask.Dispose();
        mask = new Mat(grayPrev.Size(), MatType.CV_8UC3, Scalar.All(0));
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

    /// <summary>
    /// 格子状に分割
    /// </summary>
    /// <param name="target"></param>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public static Mat[,] Grid(this Mat target, int col, int row)
    {
        var matMatrix = new Mat[col,row];
        for (var c = 0; c < col; c++)
        {
            for (var r = 0; r < row; r++)
            {
                var x = (double)target.Width / col * c;
                var y = (double)target.Height / row * r;
                var rect = new Rect((int)x, (int)y, target.Width / col, target.Height / row);
                matMatrix[c,r] = target.SubMat(rect);
            } 
        }


        return matMatrix; 
    }
}