using DNTCaptcha.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace DNTCaptcha.Core.Providers
{
    /// <summary>
    /// The default captcha image provider
    /// </summary>
    public class CaptchaImageProvider : ICaptchaImageProvider
    {
        private readonly IRandomNumberProvider _randomNumberProvider;
        private readonly List<string> colors = new List<string>();

        /// <summary>
        /// The default captcha image provider
        /// </summary>
        public CaptchaImageProvider(IRandomNumberProvider randomNumberProvider)
        {
            randomNumberProvider.CheckArgumentNull(nameof(randomNumberProvider));

            _randomNumberProvider = randomNumberProvider;

            SetColors();
        }

        /// <summary>
        /// Creates the captcha image.
        /// </summary>
        public byte[] DrawCaptcha(string message, string foreColor, string backColor, float fontSize, string fontName)
        {
            var fColor = ColorTranslator.FromHtml(foreColor);
            var bColor = string.IsNullOrWhiteSpace(backColor) ?
                Color.Transparent : ColorTranslator.FromHtml(backColor);

            var captchaFont = new Font(fontName, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

            var captchaSize = measureString(message, captchaFont);

            const int margin = 8;
            var height = (int)captchaSize.Height + margin;
            var width = (int)captchaSize.Width + margin;

            var rectF = new Rectangle(0, 0, width: width, height: height);
            using (var pic = new Bitmap(width: width, height: height))
            {
                using (var graphics = Graphics.FromImage(pic))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.High;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

                    using (var font = captchaFont)
                    {
                        using (var format = new StringFormat())
                        {
                            format.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                            var rect = drawRoundedRectangle(graphics, rectF, 15, new Pen(bColor) { Width = 1.1f }, bColor);
                            graphics.DrawString(message, font, new SolidBrush(fColor), rect, format);

                            using (var stream = new MemoryStream())
                            {
                                distortImage(height, width, pic);
                                pic.Save(stream, ImageFormat.Png);
                                return stream.ToArray();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates the captcha image with noise.
        /// </summary>
        public byte[] DrawCaptcha(string message, float fontSize, string fontName)
        {
            message = message.Replace(",", string.Empty);
            const int margin = 8;
            var captchaFont = new Font(fontName, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            var captchaSize = measureString(message, captchaFont);
            var height = (int)captchaSize.Height + margin;
            var width = (int)captchaSize.Width + margin;

            using (var pic = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                using (var graphics = Graphics.FromImage(pic))
                {
                    using (var backgroundBrush = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.SmallCheckerBoard, Color.DimGray, Color.WhiteSmoke))
                        graphics.FillRectangle(backgroundBrush, 0, 0, width, height);

                    var horizontalPosition = 0;
                    var characterSpacing = (width / message.Length) - 1;
                    foreach (var item in message)
                    {
                        var brush = new SolidBrush(ColorTranslator.FromHtml(colors[_randomNumberProvider.Next(0, colors.Count - 1)]));
                        var maxVerticalPosition = height - Convert.ToInt32(graphics.MeasureString(item.ToString(), captchaFont).Height);
                        graphics.DrawString(item.ToString(), captchaFont, brush, horizontalPosition, _randomNumberProvider.Next(0, maxVerticalPosition));
                        horizontalPosition += characterSpacing + _randomNumberProvider.Next(-1, 1);
                    }

                    for (var i = 0; i < 30; i++)
                    {
                        var start = _randomNumberProvider.Next(1, 4);
                        var brush = new SolidBrush(ColorTranslator.FromHtml(colors[_randomNumberProvider.Next(0, colors.Count - 1)]));
                        graphics.FillEllipse(brush, _randomNumberProvider.Next(start, width), _randomNumberProvider.Next(1, height), _randomNumberProvider.Next(1, 4), _randomNumberProvider.Next(2, 5));

                        var x0 = _randomNumberProvider.Next(0, width);
                        var y0 = _randomNumberProvider.Next(0, height);
                        var x1 = _randomNumberProvider.Next(0, width);
                        var y1 = _randomNumberProvider.Next(0, height);
                        graphics.DrawLine(Pens.Black, x0, y0, x1, x1);
                    }
                }

                using (var stream = new MemoryStream())
                {
                    distortImage(height, width, pic);
                    pic.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        private static Rectangle drawRoundedRectangle(Graphics gfx, Rectangle bounds, int cornerRadius, Pen drawPen, Color fillColor)
        {
            int strokeOffset = Convert.ToInt32(Math.Ceiling(drawPen.Width));
            bounds = Rectangle.Inflate(bounds, -strokeOffset, -strokeOffset);
            drawPen.EndCap = drawPen.StartCap = LineCap.Round;
            GraphicsPath gfxPath = new GraphicsPath();
            gfxPath.AddArc(bounds.X, bounds.Y, cornerRadius, cornerRadius, 180, 90);
            gfxPath.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y, cornerRadius, cornerRadius, 270, 90);
            gfxPath.AddArc(bounds.X + bounds.Width - cornerRadius, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            gfxPath.AddArc(bounds.X, bounds.Y + bounds.Height - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            gfxPath.CloseAllFigures();
            gfx.FillPath(new SolidBrush(fillColor), gfxPath);
            gfx.DrawPath(drawPen, gfxPath);

            return bounds;
        }

        private static SizeF measureString(string text, Font f)
        {
            using (var bmp = new Bitmap(1, 1))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    return g.MeasureString(text, f);
                }
            }
        }

        private void distortImage(int height, int width, Bitmap pic)
        {
            using (var copy = (Bitmap)pic.Clone())
            {
                double distort = _randomNumberProvider.Next(1, 6) * (_randomNumberProvider.Next(10) == 1 ? 1 : -1);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Adds a simple wave
                        int newX = (int)(x + (distort * Math.Sin(Math.PI * y / 84.0)));
                        int newY = (int)(y + (distort * Math.Cos(Math.PI * x / 44.0)));
                        if (newX < 0 || newX >= width) newX = 0;
                        if (newY < 0 || newY >= height) newY = 0;
                        pic.SetPixel(x, y, copy.GetPixel(newX, newY));
                    }
                }
            }
        }

        private void SetColors()
        {
            colors.Add("#F0F8FF");  // AliceBlue
            colors.Add("#F0FFFF");  // Azure
            colors.Add("#0000FF");  // Blue
            colors.Add("#A52A2A");  // Brown
            colors.Add("#5F9EA0");  // CadetBlue
            colors.Add("#00008B");  // DarkBlue
            colors.Add("#008B8B");  // DarkCyan
            colors.Add("#B8860B");  // DarkGoldenrod
            colors.Add("#006400");  // DarkGreen
            colors.Add("#BDB76B");  // DarkKhaki
            colors.Add("#8B008B");  // DarkMagenta
            colors.Add("#556B2F");  // DarkOliveGreen
            colors.Add("#FF8C00");  // DarkOrange
            colors.Add("#9932CC");  // DarkOrchid
            colors.Add("#8B0000");  // DarkRed
            colors.Add("#E9967A");  // DarkSalmon
            colors.Add("#8FBC8B"); // DarkSeaGreen
            colors.Add("#483D8B"); // DarkSlateBlue
            colors.Add("#00CED1"); // DarkTurquoise
            colors.Add("#9400D3"); // DarkViolet
            colors.Add("#FF1493"); // DeepPink
            colors.Add("#00BFFF"); // DeepSkyBlue
            colors.Add("#1E90FF"); // DodgerBlue
            colors.Add("#B22222"); // Firebrick
            colors.Add("#FFFAF0"); // FloralWhite
            colors.Add("#228B22"); // ForestGreen
            colors.Add("#FF00FF"); // Fuchsia
            colors.Add("#DCDCDC"); // Gainsboro
            colors.Add("#F8F8FF"); // GhostWhite
            colors.Add("#FFD700"); // Gold
            colors.Add("#DAA520"); // Goldenrod
            colors.Add("#008000"); // Green
            colors.Add("#ADFF2F"); // GreenYellow
            colors.Add("#F0FFF0"); // Honeydew
            colors.Add("#FF69B4"); // HotPink
            colors.Add("#CD5C5C"); // IndianRed
            colors.Add("#4B0082"); // Indigo
            colors.Add("#FFFFF0"); // Ivory
            colors.Add("#F0E68C"); // Khaki
            colors.Add("#E6E6FA"); // Lavender
            colors.Add("#FFF0F5"); // LavenderBlush
            colors.Add("#7CFC00"); // LawnGreen
            colors.Add("#FFFACD"); // LemonChiffon
            colors.Add("#ADD8E6"); // LightBlue
            colors.Add("#F08080"); // LightCoral
            colors.Add("#E0FFFF"); // LightCyan
            colors.Add("#FAFAD2"); // LightGoldenrodYellow
            colors.Add("#90EE90"); // LightGreen
            colors.Add("#FFB6C1"); // LightPink
            colors.Add("#FFA07A"); // LightSalmon
            colors.Add("#20B2AA"); // LightSeaGreen
            colors.Add("#87CEFA"); // LightSkyBlue
            colors.Add("#B0C4DE"); // LightSteelBlue
            colors.Add("#FFFFE0"); // LightYellow
            colors.Add("#00FF00"); // Lime
            colors.Add("#32CD32"); // LimeGreen
            colors.Add("#FAF0E6"); // Linen
            colors.Add("#FF00FF"); // Magenta
            colors.Add("#800000"); // Maroon
            colors.Add("#66CDAA"); // MediumAquamarine
            colors.Add("#0000CD"); // MediumBlue
            colors.Add("#BA55D3"); // MediumOrchid
            colors.Add("#9370DB"); // MediumPurple
            colors.Add("#3CB371"); // MediumSeaGreen
            colors.Add("#7B68EE"); // MediumSlateBlue
            colors.Add("#00FA9A"); // MediumSpringGreen
            colors.Add("#48D1CC"); // MediumTurquoise
            colors.Add("#C71585"); // MediumVioletRed
            colors.Add("#191970"); // MidnightBlue
            colors.Add("#F5FFFA"); // MintCream
            colors.Add("#FFE4E1"); // MistyRose
            colors.Add("#FFE4B5"); // Moccasin
            colors.Add("#FFDEAD"); // NavajoWhite
            colors.Add("#000080"); // Navy
            colors.Add("#FDF5E6"); // OldLace
            colors.Add("#808000"); // Olive
            colors.Add("#6B8E23"); // OliveDrab
            colors.Add("#FFA500"); // Orange
            colors.Add("#FF4500"); // OrangeRed
            colors.Add("#DA70D6"); // Orchid
            colors.Add("#EEE8AA"); // PaleGoldenrod
            colors.Add("#98FB98"); // PaleGreen
            colors.Add("#AFEEEE"); // PaleTurquoise
            colors.Add("#DB7093"); // PaleVioletRed
            colors.Add("#FFEFD5"); // PapayaWhip
            colors.Add("#FFDAB9"); // PeachPuff
            colors.Add("#CD853F"); // Peru
            colors.Add("#FFC0CB"); // Pink
            colors.Add("#DDA0DD"); // Plum
            colors.Add("#B0E0E6"); // PowderBlue
            colors.Add("#800080"); // Purple
            colors.Add("#FF0000"); // Red
            colors.Add("#BC8F8F"); // RosyBrown
            colors.Add("#4169E1"); // RoyalBlue
            colors.Add("#8B4513"); // SaddleBrown
            colors.Add("#FA8072"); // Salmon
            colors.Add("#F4A460"); // SandyBrown
            colors.Add("#2E8B57"); // SeaGreen
            colors.Add("#FFF5EE"); // SeaShell
            colors.Add("#A0522D"); // Sienna
            colors.Add("#C0C0C0"); // Silver
            colors.Add("#87CEEB"); // SkyBlue
            colors.Add("#6A5ACD"); // SlateBlue
            colors.Add("#FFFAFA"); // Snow
            colors.Add("#00FF7F"); // SpringGreen
            colors.Add("#4682B4"); // SteelBlue
            colors.Add("#D2B48C"); // Tan
            colors.Add("#008080"); // Teal
            colors.Add("#D8BFD8"); // Thistle
            colors.Add("#FF6347"); // Tomato
            colors.Add("#FFFFFF"); // Transparent
            colors.Add("#40E0D0"); // Turquoise
            colors.Add("#EE82EE"); // Violet
            colors.Add("#F5DEB3"); // Wheat
            colors.Add("#FFFF00"); // Yellow
            colors.Add("#9ACD32"); // YellowGreen
        }
    }
}