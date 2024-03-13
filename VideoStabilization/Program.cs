using OpenCvSharp;

const string videoPath = @"C:\Users\k_tak\Downloads\無題の動画 ‐ Clipchampで作成.mp4";
using var cap = new VideoCapture(videoPath);

using var cur = new Mat();
using var curGrey = new Mat();
using var prev = new Mat();
using var prevGrey = new Mat();

cap.Read(prev);
Cv2.CvtColor(prev, prevGrey, ColorConversionCodes.BGR2GRAY);

// クロップするボーダーの幅
const int horizontalBorderCrop = 20;

while (true)
{
    if (cap.Read(cur) == false) break;

    Cv2.CvtColor(cur, curGrey, ColorConversionCodes.BGR2GRAY);

    // 画像中の特徴点を検出
    var prevCorner2D = Cv2.GoodFeaturesToTrack(prevGrey, 200, 0.1, 30, null, 7, false, 0.04);
    Point2f[] curCorners2D = { };

    using var prevCornersMat = new Mat(prevCorner2D.Length, 1, MatType.CV_32FC2, prevCorner2D);
    // Optical Flowを計算
    Cv2.CalcOpticalFlowPyrLK(prevGrey, curGrey, prevCorner2D, ref curCorners2D, out _, out _);

    // 変換行列を計算
    using var curCorner2DMat = new Mat(curCorners2D.Length, 1, MatType.CV_32FC2, curCorners2D);

    //using var T = Cv2.EstimateAffinePartial2D(curCorner2DMat, prevCornersMat);
    using var T = prevCorner2D.ComputeTransformMatrix(curCorners2D);
    var cur2 = new Mat();
    Cv2.WarpAffine(cur, cur2, T, cur.Size());

    // 画像をクロップ
    cur2 = new Mat(cur2, new Rect(horizontalBorderCrop, 0, cur2.Cols - horizontalBorderCrop * 2, cur2.Rows));

    // 画像のサイズを調整
    Cv2.Resize(cur2, cur2, cur.Size());

    // 変換前後の画像を並べて表示
    using Mat canvas = Mat.Zeros(cur.Rows, cur.Cols * 2 + 10, cur.Type());
    cur.CopyTo(new Mat(canvas, new Rect(0, 0, cur.Cols, cur.Rows)));
    cur2.CopyTo(new Mat(canvas, new Rect(cur2.Cols + 10, 0, cur2.Cols, cur.Rows)));

    if (canvas.Cols > 1920) Cv2.Resize(canvas, canvas, new Size(canvas.Cols / 2, canvas.Rows / 2));

    Cv2.ImShow("変換前と変換後", canvas);
    Cv2.WaitKey(20);

    cur2.Dispose();
}

public static class Transform
{
public static Mat ComputeTransformMatrix(this Point2f[] curCorners2D, Point2f[] prevCorner2D)
{
    // 1つ目の点の移動ベクトルを計算します
    Point2f translation = curCorners2D[0] - prevCorner2D[0];

    // 平行移動の行列を計算します
    Mat translationMat = Mat.Eye(2, 3, MatType.CV_32F);
    translationMat.At<float>(0, 2) = translation.X;
    translationMat.At<float>(1, 2) = translation.Y;

    return translationMat;}
}

