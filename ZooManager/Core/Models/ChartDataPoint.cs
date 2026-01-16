namespace ZooManager.Core.Models
{
    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public double BarHeight { get; set; } // Berechnete Höhe in Pixeln
        public string ToolTipText => $"{Label}: {Value}";
    }
}