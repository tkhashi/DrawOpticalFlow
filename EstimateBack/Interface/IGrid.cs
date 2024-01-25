using OpenCvSharp;

namespace EstimateBack.Interface;

/// <summary>
/// 時間的に連続した二枚の画像を扱うオブジェクト
/// </summary>
public interface IGrid
{
    /// <summary>
    /// オプティカルフロー算出用の画像
    /// </summary>
    public Mat PrevMat { get; }
    /// <summary>
    /// オプティカルフロー算出用の画像
    /// </summary>
    public Mat NextMat { get; }

    /// <summary>
    /// 二枚の画像からオプティカルフローから移動ベクトルを算出し、方向を返す
    /// </summary>
    /// <returns></returns>
    public CardinalDirection GetOpticalFlowDirection();

    ///// <summary>
    ///// オプティカルフロー算出用に次のフレームをセット
    ///// </summary>
    ///// <param name="next"></param>
    //public void SetNextMat(Mat next);
}