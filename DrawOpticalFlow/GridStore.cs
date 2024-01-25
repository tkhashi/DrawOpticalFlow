using OpenCvSharp;

namespace DrawOpticalFlow;

/// <summary>
/// 画像任意の行×列に分割
/// </summary>
public class GridStore
{
    private readonly PairFlow _flowService;
    private readonly Mat[,] _prevMats;
    private readonly Mat _drawnMat = new();
    private Point2f[,] _prevFeature;
    private Point2f[,] _nextFeature;
    private readonly int _row;
    private readonly int _col;
    private float[] _error;

    /// <summary>
    /// 初期化時に分割
    /// </summary>
    /// <param name="target"></param>
    /// <param name="col"></param>
    /// <param name="row"></param>
    public GridStore(Mat initialMat, int col, int row)
    {
        _flowService = PairFlow.Init();
        _prevMats = new Mat[col, row];
        _row = row;
        _col = col;

        _prevMats = initialMat.Grid(col, row);
    }

    /// <summary>
    /// 各Gridのオプティカルフローを求める
    /// </summary>
    public void Estimate(Mat next)
    {
        var h = Enumerable.Repeat(new Mat(), _col).ToArray();
        var v = Enumerable.Repeat(new Mat(), _row).ToArray();

        var nextMats = next.Grid(_col, _row);

        for (var c = 0; c < _col; c++)
        {
            // グリッドの一列をそれぞれ計算しVConcatする
            for (var r = 0; r < _row; r++)
            {
                (_prevFeature[c, r], _nextFeature[c, r]) = _flowService.Calc(_prevMats[c, r], nextMats[c, r]);
                var d = _flowService.Draw(_prevMats[c, r]);

                v[r] = d;
            }

            Cv2.VConcat(v, h[c]);
            h[c] = h[c].Clone();
        }
        Cv2.HConcat(h, _drawnMat);

        for (var c = 0; c < _col; c++)
        for (var r = 0; r < _row; r++)
        {
            _prevMats[c, r].Dispose();
            _prevMats[c, r] = nextMats[c, r].Clone();
        }
    }

    /// <summary>
    /// Estimateした結果を描画
    /// </summary>
    public void Draw()
    {
        Cv2.ImShow("drawn", _drawnMat);
        Cv2.WaitKey(1);
        _drawnMat.Dispose();
    }

    public void isForward()
    {


    }

    /// <summary>
    /// Gridのオプティカルフローベクトルを平均して一つのベクトルにする
    /// </summary>
    public void AverageOpticalFlow(Point2f[] prevs, Point2f[] nexts)
    {
        //Mat status = new Mat();
        //Mat errors = new Mat();

        //Cv2.CalcOpticalFlowPyrLK(grayPrev, grayNext, prevFeatures, nextFeatures, status, errors);

        //double sumX = 0.0;
        //double sumY = 0.0;
        //int count = 0;

        //for (int i = 0; i < status.Rows; i++)
        //{
        //    if (IsInsideRegion(nextFeatures[i], region) && status.At<byte>(i) == 1)
        //    {
        //        double[] flowVector = errors.Get<Point2f>(i);
        //        sumX += flowVector[0];
        //        sumY += flowVector[1];
        //        count++;
        //    }
        //}

        //if (count > 0)
        //{
        //    double averageX = sumX / count;
        //    double averageY = sumY / count;
        //    return new double[] { averageX, averageY };
        //}
        //else
        //{
        //    return null;
        //}
    }

    /// <summary>
    /// 一つのGridを更にcol*rowグリッドに分割
    /// </summary>
    /// <returns></returns>
    public GridStore Grid()
    {
        //TODO: 仮
        return this;
    }

}