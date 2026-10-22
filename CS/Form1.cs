using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Utils.Drawing;
using DevExpress.XtraCharts;
using DevExpress.XtraGrid.Views.Base;

namespace LegendMarkers
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }
        // Actual draw settings the chart used for each series and series point during the last paint.
        readonly Dictionary<Series, DrawOptions> seriesDrawOptions = new Dictionary<Series, DrawOptions>();
        readonly Dictionary<SeriesPoint, Color> pointColors = new Dictionary<SeriesPoint, Color>();

        void frmMain_Load(object sender, System.EventArgs e)
        {
            gridControl1.DataSource = chart.Series;
        }

        void chart_CustomDrawSeries(object sender, CustomDrawSeriesEventArgs e)
        {
            DrawOptions options;
            if (seriesDrawOptions.TryGetValue(e.Series, out options) && options.Color == e.SeriesDrawOptions.Color)
                return;
            seriesDrawOptions[e.Series] = (DrawOptions)e.SeriesDrawOptions.Clone();
            gridControl1.Invalidate();
        }

        void chart_CustomDrawSeriesPoint(object sender, CustomDrawSeriesPointEventArgs e)
        {
            Color color;
            if (pointColors.TryGetValue(e.SeriesPoint, out color) && color == e.SeriesDrawOptions.Color)
                return;
            pointColors[e.SeriesPoint] = e.SeriesDrawOptions.Color;
            gridControl1.Invalidate();
        }

        void gridView1_CustomDrawCell(object sender, RowCellCustomDrawEventArgs e)
        {
            if (e.Column != colImage)
                return;
            Series series = gridView1.GetRow(e.RowHandle) as Series;
            DrawOptions options;
            if (series == null || !seriesDrawOptions.TryGetValue(series, out options))
                return;
            e.DefaultDraw();
            Size markerSize = chart.Legend.MarkerSize;
            Rectangle markerBounds = new Rectangle(
                e.Bounds.Left + (e.Bounds.Width - markerSize.Width) / 2,
                e.Bounds.Top + (e.Bounds.Height - markerSize.Height) / 2,
                markerSize.Width, markerSize.Height);
            DrawLegendMarker(e.Cache, markerBounds, options, GetMarkerColors(series, options));
            e.Handled = true;
        }

        List<Color> GetMarkerColors(Series series, DrawOptions options)
        {
            List<Color> colors = new List<Color>();
            SeriesViewColorEachSupportBase colorEachView = series.View as SeriesViewColorEachSupportBase;
            if (colorEachView != null && colorEachView.ColorEach)
            {
                foreach (SeriesPoint point in series.Points)
                {
                    Color color;
                    if (pointColors.TryGetValue(point, out color))
                        colors.Add(color);
                }
            }
            if (colors.Count == 0)
                colors.Add(options.Color);
            return colors;
        }

        static void DrawLegendMarker(GraphicsCache cache, Rectangle bounds, DrawOptions options, List<Color> colors)
        {
            LineDrawOptions lineOptions = options as LineDrawOptions;
            if (lineOptions != null && !(options is AreaDrawOptions))
            {
                int thickness = System.Math.Max(1, lineOptions.LineStyle.Thickness);
                int y = bounds.Top + bounds.Height / 2;
                cache.DrawLine(cache.GetPen(colors[0], thickness), new Point(bounds.Left, y), new Point(bounds.Right, y));
                return;
            }
            // Split the marker into vertical stripes when the series colors each point individually.
            for (int i = 0; i < colors.Count; i++)
            {
                int left = bounds.Left + bounds.Width * i / colors.Count;
                int right = bounds.Left + bounds.Width * (i + 1) / colors.Count;
                cache.FillRectangle(colors[i], new Rectangle(left, bounds.Top, right - left, bounds.Height));
            }
        }

        private void chart_ObjectSelected(object sender, HotTrackEventArgs e)
        {
            if (e.HitInfo.InSeries)
            {
                gridView1.FocusedRowHandle = gridView1.GetRowHandle(chart.Series.IndexOf(((Series)e.Object)));
            }
        }

        private void gridView1_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            chart.SetObjectSelection(gridView1.GetRow(e.FocusedRowHandle));
        }
    }
}
