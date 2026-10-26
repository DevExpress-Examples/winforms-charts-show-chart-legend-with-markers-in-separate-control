Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports DevExpress.Utils.Drawing
Imports DevExpress.XtraCharts
Imports DevExpress.XtraGrid.Views.Base

Namespace LegendMarkers

    Public Partial Class frmMain
        Inherits Form

        Public Sub New()
            InitializeComponent()
        End Sub

        ' Actual draw settings the chart used for each series and series point during the last paint.
        Private ReadOnly seriesDrawOptions As Dictionary(Of Series, DrawOptions) = New Dictionary(Of Series, DrawOptions)()

        Private ReadOnly pointColors As Dictionary(Of SeriesPoint, Color) = New Dictionary(Of SeriesPoint, Color)()

        Private Sub frmMain_Load(ByVal sender As Object, ByVal e As System.EventArgs)
            gridControl1.DataSource = chart.Series
        End Sub

        Private Sub chart_CustomDrawSeries(ByVal sender As Object, ByVal e As CustomDrawSeriesEventArgs)
            Dim options As DrawOptions
            If seriesDrawOptions.TryGetValue(e.Series, options) AndAlso options.Color = e.SeriesDrawOptions.Color Then Return
            seriesDrawOptions(e.Series) = CType(e.SeriesDrawOptions.Clone(), DrawOptions)
            gridControl1.Invalidate()
        End Sub

        Private Sub chart_CustomDrawSeriesPoint(ByVal sender As Object, ByVal e As CustomDrawSeriesPointEventArgs)
            Dim color As Color
            If pointColors.TryGetValue(e.SeriesPoint, color) AndAlso color = e.SeriesDrawOptions.Color Then Return
            pointColors(e.SeriesPoint) = e.SeriesDrawOptions.Color
            gridControl1.Invalidate()
        End Sub

        Private Sub gridView1_CustomDrawCell(ByVal sender As Object, ByVal e As RowCellCustomDrawEventArgs)
            If e.Column IsNot colImage Then Return
            Dim series As Series = TryCast(gridView1.GetRow(e.RowHandle), Series)
            Dim options As DrawOptions
            If series Is Nothing OrElse Not seriesDrawOptions.TryGetValue(series, options) Then Return
            e.DefaultDraw()
            Dim markerSize As Size = chart.Legend.MarkerSize
            Dim markerBounds As Rectangle = New Rectangle(e.Bounds.Left + (e.Bounds.Width - markerSize.Width) \ 2, e.Bounds.Top + (e.Bounds.Height - markerSize.Height) \ 2, markerSize.Width, markerSize.Height)
            Call DrawLegendMarker(e.Cache, markerBounds, options, GetMarkerColors(series, options))
            e.Handled = True
        End Sub

        Private Function GetMarkerColors(ByVal series As Series, ByVal options As DrawOptions) As List(Of Color)
            Dim colors As List(Of Color) = New List(Of Color)()
            Dim colorEachView As SeriesViewColorEachSupportBase = TryCast(series.View, SeriesViewColorEachSupportBase)
            If colorEachView IsNot Nothing AndAlso colorEachView.ColorEach Then
                For Each point As SeriesPoint In series.Points
                    Dim color As Color
                    If pointColors.TryGetValue(point, color) Then colors.Add(color)
                Next
            End If

            If colors.Count = 0 Then colors.Add(options.Color)
            Return colors
        End Function

        Private Shared Sub DrawLegendMarker(ByVal cache As GraphicsCache, ByVal bounds As Rectangle, ByVal options As DrawOptions, ByVal colors As List(Of Color))
            Dim lineOptions As LineDrawOptions = TryCast(options, LineDrawOptions)
            If lineOptions IsNot Nothing AndAlso Not(TypeOf options Is AreaDrawOptions) Then
                Dim thickness As Integer = System.Math.Max(1, lineOptions.LineStyle.Thickness)
                Dim y As Integer = bounds.Top + bounds.Height \ 2
                cache.DrawLine(cache.GetPen(colors(0), thickness), New Point(bounds.Left, y), New Point(bounds.Right, y))
                Return
            End If

            ' Split the marker into vertical stripes when the series colors each point individually.
            For i As Integer = 0 To colors.Count - 1
                Dim left As Integer = bounds.Left + bounds.Width * i \ colors.Count
                Dim right As Integer = bounds.Left + bounds.Width * (i + 1) \ colors.Count
                cache.FillRectangle(colors(i), New Rectangle(left, bounds.Top, right - left, bounds.Height))
            Next
        End Sub

        Private Sub chart_ObjectSelected(ByVal sender As Object, ByVal e As HotTrackEventArgs)
            If e.HitInfo.InSeries Then
                gridView1.FocusedRowHandle = gridView1.GetRowHandle(chart.Series.IndexOf(CType(e.Object, Series)))
            End If
        End Sub

        Private Sub gridView1_FocusedRowChanged(ByVal sender As Object, ByVal e As FocusedRowChangedEventArgs)
            chart.SetObjectSelection(gridView1.GetRow(e.FocusedRowHandle))
        End Sub
    End Class
End Namespace
