namespace EstimateBack.Interface;

/// <summary>
/// 時間的に連続した二枚の画像から前進しているか後退しているかを判定するサービスクラス
/// </summary>
public interface IDirectionEstimator
{
    /// <summary>
    /// 第一象限
    /// </summary>
    public IQuadrant FirstQuadrant { get; }
    /// <summary>
    /// 第二象限
    /// </summary>
    public IQuadrant SecondQuadrant { get; }
    /// <summary>
    /// 第三象限
    /// </summary>
    public IQuadrant ThirdQuadrant { get; }
    /// <summary>
    /// 第四象限
    /// </summary>
    public IQuadrant ForthQuadrant { get; }

    // 入力画像を2 * 2の四象限に分割する
    // 象限内で6 * 4のグリッドに分割する
    // それぞれのグリッドごとでオプティカルフローを計算し求められた移動ベクトルを求める
    // 各象限ごとにグリッドの移動ベクトルを平均して代表方向を決める
    // 方向を右上、右下、左上、左下のいずれかに分類する
    // 第一象限が右上、第二象限が左上、第三象限が左下、第四象限が右下であればDirection.Backwardを返し、それ以外の場合はDirection.Forwardを返す
    public Direction EstimateDirection();
}