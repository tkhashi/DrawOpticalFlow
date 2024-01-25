using System.Diagnostics;
using EstimateBack;
using OpenCvSharp;

//var path = @"C:\Users\k_tak\Downloads\534-21-62-2下流調査-ガタツキ部のみ.mp4";
var path = @"C:\Users\k_tak\Downloads\K-Proデータ-selected\534-21-62-2.MP4";
using var capture = VideoCapture.FromFile(path);


var basePath = @"C:\Users\k_tak\Downloads";
var savePath = Path.Combine(basePath, $"{Path.GetFileNameWithoutExtension(path)}_reduce_backward.mp4");
var width = capture.Get(VideoCaptureProperties.FrameWidth);
var height = capture.Get(VideoCaptureProperties.FrameHeight);
using var writer = new VideoWriter(savePath, FourCC.H264, capture.Fps, new Size(width, height));

var prev = capture.RetrieveMat().CvtColor(ColorConversionCodes.BGR2GRAY).EqualizeHist();
for (var i = 0 - 1; i < capture.FrameCount; i++)
{
    using var next = capture.RetrieveMat();
    if (next.Empty()) return;

    using var nextGray = next.CvtColor(ColorConversionCodes.BGR2GRAY).EqualizeHist();
    var estimator = new DirectionEstimator(prev, nextGray);
    var direction = estimator.EstimateDirection();

    if (direction is Direction.Forward)
    {
        writer.Write(next);
    }


    // 次のループ用
    prev.Dispose();
    prev = nextGray.Clone();
}
