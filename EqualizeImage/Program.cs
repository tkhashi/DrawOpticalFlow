using OpenCvSharp;

var path = @"C:\Users\k_tak\OneDrive\画像\Screenpresso\2023-06-14_15h49_40.png";
var src = new Mat(path);
var normalized = NormalizeBrightness(src);
Cv2.ImShow("result", normalized);
Cv2.WaitKey();

Mat NormalizeBrightness(Mat inputImage)
{
    // 入力画像をグレースケールに変換
    var grayImage = new Mat();
    Cv2.CvtColor(inputImage, grayImage, ColorConversionCodes.BGR2GRAY);

    // ヒストグラム平坦化を実行
    var equalizedImage = new Mat();
    Cv2.EqualizeHist(grayImage, equalizedImage);

    // グレースケール画像を元のカラー画像に変換
    var outputImage = new Mat();
    Cv2.CvtColor(equalizedImage, outputImage, ColorConversionCodes.GRAY2BGR);
    var result = new Mat();
    Cv2.Resize(outputImage, result, new Size( inputImage.Width / 2, inputImage.Height / 2));

    return result;
}