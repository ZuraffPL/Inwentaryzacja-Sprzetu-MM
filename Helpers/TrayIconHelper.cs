using System.Drawing;
using System.Drawing.Drawing2D;

namespace InwentaryzacjaSprzetu.Helpers
{
    /// <summary>
    /// Generuje programatyczną ikonę dzwonka dla system traya.
    /// Nie wymaga pliku .ico — ikona rysowana jest przez GDI+.
    /// </summary>
    internal static class TrayIconHelper
    {
        // Przechowujemy bitmap jako static, żeby GDI handle nie wygasł przed końcem programu
        private static Bitmap? _bitmap;
        private static Bitmap? _bitmapAlert;

        /// <summary>Ikona dzwonka w kolorze ambrowym (stan normalny — brak aktywnych alertów).</summary>
        public static Icon CreateBellIcon() => BuildIcon(hasAlert: false);

        /// <summary>Ikona dzwonka z czerwoną kropką (stan alertu — są aktywne powiadomienia).</summary>
        public static Icon CreateBellAlertIcon() => BuildIcon(hasAlert: true);

        private static Icon BuildIcon(bool hasAlert)
        {
            const int S = 32;

            ref Bitmap? bmpRef = ref (hasAlert ? ref _bitmapAlert : ref _bitmap);

            if (bmpRef != null)
            {
                var hOld = bmpRef.GetHicon();
                return Icon.FromHandle(hOld);
            }

            var bmp = new Bitmap(S, S);
            bmpRef = bmp;

            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);

            // ── tło — ambrowe zaokrąglone koło ────────────────────────────────
            var bgColor = hasAlert
                ? Color.FromArgb(200, 30, 0)   // czerwony gdy alert
                : Color.FromArgb(180, 80, 0);   // ciemny bursztyn normalny

            using (var bgBrush = new SolidBrush(bgColor))
                g.FillEllipse(bgBrush, 1, 1, S - 3, S - 3);

            // ── dzwonek (biały) ────────────────────────────────────────────────
            // Kopuła dzwonka (wypełniony półokrąg)
            using (var bellBrush = new SolidBrush(Color.White))
            {
                // Uszko/hak na górze
                using var handlePen = new Pen(Color.White, 2f);
                g.DrawArc(handlePen, 12, 4, 8, 6, 180, 180);

                // Korpus dzwonka — trapez zaokrąglony przez GraphicsPath
                using var path = new GraphicsPath();
                path.AddArc(5, 10, 22, 12, 180, 180);   // górny łuk (kopuła)
                path.AddLine(27, 16, 27, 21);             // prawa ściana
                path.AddLine(27, 21, 5, 21);              // dolna krawędź
                path.AddLine(5, 21, 5, 16);               // lewa ściana
                path.CloseFigure();
                g.FillPath(bellBrush, path);

                // Mały prostokąt — podstawa dzwonka
                g.FillRectangle(bellBrush, 5f, 20f, 22f, 3f);

                // Kołatka (kulka na dole)
                g.FillEllipse(bellBrush, 13f, 23f, 6f, 5f);
            }

            // ── czerwona kropka z wykrzyknikiem (gdy aktywne alerty) ──────────
            if (hasAlert)
            {
                using var dotBrush = new SolidBrush(Color.FromArgb(255, 50, 50));
                g.FillEllipse(dotBrush, 19, 2, 11, 11);

                using var font  = new Font("Arial", 7f, FontStyle.Bold, GraphicsUnit.Point);
                using var tBrush = new SolidBrush(Color.White);
                g.DrawString("!", font, tBrush, 21.5f, 3f);
            }

            var hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
    }
}
