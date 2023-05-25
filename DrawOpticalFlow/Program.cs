using OpenCvSharp;

var path = @"";
var capture = new VideoCapture(path);
var rng = new Random();

// Shi-Tomasiのコーナー検出パラメータ
var maxCorners = 100;
var qualityLevel = 0.3;
var minDistance = 7;
var blockSize = 7;

// Lucas-Kanade法のパラメータ
var winSize = new Size(15, 15);
var maxLevel = 2;
var criteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 10, 0.03);

// ランダムに色を１００個生成（値0～255の範囲で100行3列のランダムなndarrayを生成）
var colors = new Scalar[100];
for (int i = 0; i < colors.Length; i++)
{
    colors[i] = new Scalar(rng.Next(0, 256), rng.Next(0, 256), rng.Next(0, 256));
}

// 最初のフレームの処理
var frame = new Mat();
bool endFlag = capture.Read(frame);
var grayPrev = new Mat();
bool useHarrisDetector = false;
double k = 0.04;
Cv2.CvtColor(frame, grayPrev, ColorConversionCodes.BGR2GRAY);
var featuresPrev = Cv2.GoodFeaturesToTrack(grayPrev, maxCorners, qualityLevel, minDistance, null, blockSize, useHarrisDetector, k);
var mask = new Mat(frame.Size(), MatType.CV_8UC3, Scalar.All(0));

while (endFlag)
{
    // グレースケールに変換
    var grayNext = new Mat();
    Cv2.CvtColor(frame, grayNext, ColorConversionCodes.BGR2GRAY);

    // オプティカルフロー検出
    Point2f[] featuresNext = new Point2f[] { };
    byte[] status;
    float[] err;
    double minEigThreshold = 0.0001;
    OpticalFlowFlags flags = OpticalFlowFlags.None;
    Cv2.CalcOpticalFlowPyrLK(grayPrev, grayNext, featuresPrev, ref featuresNext, out status, out err, winSize, maxLevel, criteria, flags, minEigThreshold);

    // オプティカルフローを検出した特徴点を選別（0：検出せず、1：検出した）
    var goodPrev = new List<Point2f>();
    var goodNext = new List<Point2f>();
    for (int i = 0; i < status.Length; i++)
    {
        if (status[i] == 1)
        {
            goodPrev.Add(featuresPrev[i]);
            goodNext.Add(featuresNext[i]);
        }
    }

    // オプティカルフローを描画
    for (int i = 0; i < goodNext.Count; i++)
    {
        var nextPoint = goodNext[i];
        var prevPoint = goodPrev[i];
        Cv2.Line(mask, nextPoint.ToPoint(), prevPoint.ToPoint(), colors[i % 100], 2);
        Cv2.Circle(frame, (int)nextPoint.X, (int)nextPoint.Y, 5, colors[i % 100], -1);
    }

    var img = new Mat();
    Cv2.Add(frame, mask, img);

    // ウィンドウに表示
    Cv2.ImShow("window", img);

    // ESCキー押下で終了
    if ((Cv2.WaitKey(30) & 0xff) == 27)
        break;

    // 次のフレーム、ポイントの準備
    grayPrev = grayNext.Clone();
    if (goodNext.Count > 0)
    {
        featuresPrev = goodNext.ToArray();
    }
    else
    {
        // 特徴点が見つからなかった場合、再度特徴点を検出
        featuresPrev = Cv2.GoodFeaturesToTrack(grayPrev, maxCorners, qualityLevel, minDistance, null, blockSize, useHarrisDetector, k);
    }

    endFlag = capture.Read(frame);
}

// 終了処理
Cv2.DestroyAllWindows();
capture.Release();

