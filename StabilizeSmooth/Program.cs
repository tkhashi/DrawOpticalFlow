using OpenCvSharp;

// 入力ビデオを読み込む
var basePath = @"";
var path = @"";

using var capture = new VideoCapture(Path.Combine(basePath, path));

var frameCount = (int)capture.Get(VideoCaptureProperties.FrameCount);
var width = capture.Get(VideoCaptureProperties.FrameWidth);
var height = capture.Get(VideoCaptureProperties.FrameHeight);

//// 出力ビデオを設定
var savePath = Path.Combine(basePath, $"{Path.GetFileNameWithoutExtension(path)}_stabilize_smooth_smoothEndRadius0.mp4");
using var writer = new VideoWriter(savePath, FourCC.H264, capture.Fps, new Size(width, height));

// 最初のフレームを読み込む
var previousFrame = new Mat();
capture.Read(previousFrame);

var previousGray = previousFrame.CvtColor(ColorConversionCodes.BGR2GRAY);

// 前のフレームで追跡する特徴点を見つける
var previousPoints = Cv2.GoodFeaturesToTrack(previousGray, 100, 0.3, 30, null, 7, true, 0.06);
//Mat rangeMask = null;
//using var rangeMask = Mat.Zeros(previousFrame.Size(), MatType.CV_8UC1).ToMat();
//var maskRegion = new Rect(0, 0, previousFrame.Width, previousFrame.Height);
//rangeMask[maskRegion].SetTo(Scalar.White);
//var previousPoints = AKAZE.Create().Detect(previousGray, rangeMask).Select(x => x.Pt).ToArray();

// 累積的な軌道を計算するための配列
var trajectoryX = new double[frameCount];
var trajectoryY = new double[frameCount];
var trajectoryA = new double[frameCount];

double cumulativeX = 0;
double cumulativeY = 0;
double cumulativeA = 0;

for (int i = 0; i < frameCount - 1; i++)
{
    // 次のフレームを読み込む
    var currentFrame = new Mat();
    if (!capture.Read(currentFrame))
    {
        break;
    }

    // フレームをグレースケールに変換
    var currentGray = new Mat();
    Cv2.CvtColor(currentFrame, currentGray, ColorConversionCodes.BGR2GRAY);

    // オプティカルフロー（特徴点の追跡）を計算
    var currentPoints = new Point2f[]{};
    Cv2.CalcOpticalFlowPyrLK(previousGray, currentGray, previousPoints, ref currentPoints, out var status, out _);

    // 有効な点のみをフィルタリング
    var validPreviousPoints = new List<Point2f>();
    var validCurrentPoints = new List<Point2f>();
    for (int j = 0; j < status.Length; j++)
    {
        if (status[j] == 1)
        {
            validPreviousPoints.Add(previousPoints[j]);
            validCurrentPoints.Add(currentPoints[j]);
        }
    }

    // 変換行列を見つける
    //var transformMatrix = (validPreviousPoints.Count is 0 || validCurrentPoints.Count is 0) ? null : Cv2.GetAffineTransform(validPreviousPoints, validCurrentPoints);
    var transformMatrix = (validPreviousPoints.Count is 0 || validCurrentPoints.Count is 0) ?
        null :
        Cv2.EstimateAffine2D(InputArray.Create(validPreviousPoints), InputArray.Create(validCurrentPoints));

    double dx;
    double dy;
    double da;
    // 変換行列から平行移動と回転を抽出
    if (transformMatrix?.Empty() ?? true)
    {
        dx = 0;
        dy = 0;
        da = 0;
    }
    else
    {
        dx = transformMatrix.At<double>(0, 2);
        dy = transformMatrix.At<double>(1, 2);
        da = Math.Atan2(transformMatrix.At<double>(1, 0), transformMatrix.At<double>(0, 0));
    }
    Console.WriteLine(status.Count(x => x == 1));

    // 累積的な軌道を計算
    cumulativeX += dx;
    cumulativeY += dy;
    cumulativeA += da;

    trajectoryX[i] = cumulativeX;
    trajectoryY[i] = cumulativeY;
    trajectoryA[i] = cumulativeA;

    // 次のフレームに移動
    previousGray = currentGray.Clone();
    if (status.Count(x => x == 1) < 2)
    {
        previousPoints = Cv2.GoodFeaturesToTrack(previousGray, 100, 0.3, 30, null, 7, true, 0.06);
    }
    else
    {
        previousPoints = currentPoints;
    }
}

// 平滑化のためのウィンドウサイズを設定
var smoothingRadius = 30; // ウィンドウサイズは実際の動画によります

// 平滑化された軌道を計算
var smoothedTrajectoryX = new double[frameCount];
var smoothedTrajectoryY = new double[frameCount];
var smoothedTrajectoryA = new double[frameCount];

for (var i = 0; i < frameCount; i++)
{
    var count = 0;
    for (var j = 0; j <= smoothingRadius; j++)
    {
        if (i + j >= 0 && i + j < frameCount)
        {
            smoothedTrajectoryX[i] += trajectoryX[i + j];
            smoothedTrajectoryY[i] += trajectoryY[i + j];
            smoothedTrajectoryA[i] += trajectoryA[i + j];
            count++;
        }
    }

    smoothedTrajectoryX[i] /= count;
    smoothedTrajectoryY[i] /= count;
    smoothedTrajectoryA[i] /= count;
}

// 平滑化された軌道と実際の軌道との差を計算
var smoothTransforms = new double[frameCount, 3];
for (var i = 0; i < frameCount; i++)
{
    smoothTransforms[i, 0] = smoothedTrajectoryX[i] - trajectoryX[i];
    smoothTransforms[i, 1] = smoothedTrajectoryY[i] - trajectoryY[i];
    smoothTransforms[i, 2] = smoothedTrajectoryA[i] - trajectoryA[i];
}

// ストリームを最初のフレームにリセット
capture.Set(VideoCaptureProperties.PosFrames, 0);

// n-1フレームに変換を適用
for (var i = 0; i < frameCount - 1; i++)
{
    // 次のフレームを読み込む
    var frame = new Mat();
    if (!capture.Read(frame))
    {
        break;
    }

    // 新しい変換行列を抽出
    var dx = smoothTransforms[i, 0];
    var dy = smoothTransforms[i, 1];
    var da = smoothTransforms[i, 2];

    // 新しい値に基づいて変換行列を再構築
    var transformMatrix = new Mat(2, 3, MatType.CV_64F);
    transformMatrix.Set(0, 0, Math.Cos(da));
    transformMatrix.Set(0, 1, -Math.Sin(da));
    transformMatrix.Set(1, 0, Math.Sin(da));
    transformMatrix.Set(1, 1, Math.Cos(da));
    transformMatrix.Set(0, 2, dx);
    transformMatrix.Set(1, 2, dy);

    // フレームにアフィン変換を適用
    var stabilizedFrame = new Mat();
    Cv2.WarpAffine(frame, stabilizedFrame, transformMatrix, new Size(width, height));

    //// フレームをファイルに書き込む
    //writer.Write(stabilizedFrame);
    Cv2.ImShow("r", stabilizedFrame);
    Cv2.WaitKey(20);
}