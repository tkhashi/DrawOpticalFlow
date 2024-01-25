using OpenCvSharp;

namespace EstimateBack.Interface;

/// <summary>
/// 象限単位での処理を行うオブジェクト
/// </summary>
public interface IQuadrant
{
    public Quad Quad { get; }
    /// <summary>
    /// 象限内で分割されたIGridの多次元配列
    /// </summary>
    //public IGrid[,] Grids { get; }
    public ICollection<IGrid> Grids { get; }

    /// <summary>
    /// 象限内のIGridの方向を平均した結果の方向を返す
    /// </summary>
    /// <returns></returns>
    public CardinalDirection GetDirection();

    ///// <summary>
    ///// オプティカルフロー算出用に次のフレームをIGridに分割してセット
    ///// </summary>
    ///// <param name="next"></param>
    //public void SetNextMat(Mat next);
}