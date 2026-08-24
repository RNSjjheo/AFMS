using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AFMSDll
{
    public enum AFMSMathTextType
    {
        Normal = 0,
        Superscript = 1,
        Subscript = 2
    }

    [ToolboxItem(true)]
    public class AFMSMathLabel : Control
    {
        private abstract class MathItem
        {
            public abstract SizeF Measure(Graphics g, AFMSMathLabel owner);
            public abstract void Draw(Graphics g, AFMSMathLabel owner, float x, float baseLine);
        }

        private sealed class LineBreakItem : MathItem
        {
            public override SizeF Measure(Graphics g, AFMSMathLabel owner)
            {
                return SizeF.Empty;
            }

            public override void Draw(Graphics g, AFMSMathLabel owner, float x, float baseLine)
            {
            }
        }

        private sealed class MathLine
        {
            public List<MathItem> Items { get; } = new List<MathItem>();
            public float Width { get; set; }
            public float Height { get; set; }
            public bool HasSuperscript { get; set; }
            public bool HasSubscript { get; set; }
        }

        private sealed class TextItem : MathItem
        {
            public string Text { get; }
            public AFMSMathTextType Type { get; }
            public bool Italic { get; }

            public TextItem(string text, AFMSMathTextType type, bool italic)
            {
                Text = text;
                Type = type;
                Italic = italic;
            }

            public override SizeF Measure(Graphics g, AFMSMathLabel owner)
            {
                using Font font = owner.CreateItemFont(Type, Italic);
                return owner.MeasureText(g, Text, font);
            }

            public override void Draw(Graphics g, AFMSMathLabel owner, float x, float baseLine)
            {
                using Font font = owner.CreateItemFont(Type, Italic);
                using SolidBrush brush = new SolidBrush(owner.ForeColor);
                SizeF size = owner.MeasureText(g, Text, font);
                float y = owner.GetItemY(g, Text, font, Type, baseLine);
                g.DrawString(Text, font, brush, x, y, owner._stringFormat);
            }
        }

        private sealed class LogItem : MathItem
        {
            public string Argument { get; }
            public string BaseValue { get; }
            public bool NaturalLog { get; }

            public LogItem(string argument, string baseValue, bool naturalLog)
            {
                Argument = argument;
                BaseValue = baseValue;
                NaturalLog = naturalLog;
            }

            public override SizeF Measure(Graphics g, AFMSMathLabel owner)
            {
                string function = NaturalLog ? "ln" : "log";
                using Font functionFont = owner.CreateItemFont(AFMSMathTextType.Normal, false);
                using Font subFont = owner.CreateItemFont(AFMSMathTextType.Subscript, false);
                using Font argumentFont = owner.CreateItemFont(AFMSMathTextType.Normal, true);

                SizeF functionSize = owner.MeasureText(g, function, functionFont);
                SizeF baseSize = string.IsNullOrEmpty(BaseValue) ? SizeF.Empty : owner.MeasureText(g, BaseValue, subFont);
                SizeF openSize = owner.MeasureText(g, "(", functionFont);
                SizeF argumentSize = owner.MeasureText(g, Argument, argumentFont);
                SizeF closeSize = owner.MeasureText(g, ")", functionFont);

                float width = functionSize.Width + baseSize.Width + openSize.Width + argumentSize.Width + closeSize.Width;
                float height = Math.Max(functionSize.Height, argumentSize.Height) + (baseSize.Height * 0.35F);
                return new SizeF(width, height);
            }

            public override void Draw(Graphics g, AFMSMathLabel owner, float x, float baseLine)
            {
                string function = NaturalLog ? "ln" : "log";
                using Font functionFont = owner.CreateItemFont(AFMSMathTextType.Normal, false);
                using Font subFont = owner.CreateItemFont(AFMSMathTextType.Subscript, false);
                using Font argumentFont = owner.CreateItemFont(AFMSMathTextType.Normal, true);
                using SolidBrush brush = new SolidBrush(owner.ForeColor);

                DrawPart(function, functionFont, AFMSMathTextType.Normal);
                if (!string.IsNullOrEmpty(BaseValue)) DrawPart(BaseValue, subFont, AFMSMathTextType.Subscript);
                DrawPart("(", functionFont, AFMSMathTextType.Normal);
                DrawPart(Argument, argumentFont, AFMSMathTextType.Normal);
                DrawPart(")", functionFont, AFMSMathTextType.Normal);

                void DrawPart(string text, Font font, AFMSMathTextType type)
                {
                    SizeF size = owner.MeasureText(g, text, font);
                    float y = owner.GetItemY(g, text, font, type, baseLine);
                    g.DrawString(text, font, brush, x, y, owner._stringFormat);
                    x += size.Width;
                }
            }
        }

        private sealed class SqrtItem : MathItem
        {
            public string Expression { get; }

            public SqrtItem(string expression)
            {
                Expression = expression;
            }

            public override SizeF Measure(Graphics g, AFMSMathLabel owner)
            {
                using Font font = owner.CreateItemFont(AFMSMathTextType.Normal, true);
                using Font rootFont = owner.CreateItemFont(AFMSMathTextType.Normal, false);
                SizeF rootSize = owner.MeasureText(g, "√", rootFont);
                SizeF textSize = owner.MeasureText(g, Expression, font);
                return new SizeF(rootSize.Width + textSize.Width + 4F, Math.Max(rootSize.Height, textSize.Height) + 3F);
            }

            public override void Draw(Graphics g, AFMSMathLabel owner, float x, float baseLine)
            {
                using Font font = owner.CreateItemFont(AFMSMathTextType.Normal, true);
                using Font rootFont = owner.CreateItemFont(AFMSMathTextType.Normal, false);
                using SolidBrush brush = new SolidBrush(owner.ForeColor);
                using Pen pen = new Pen(owner.ForeColor, Math.Max(1F, owner.Font.Size / 12F));

                SizeF rootSize = owner.MeasureText(g, "√", rootFont);
                SizeF textSize = owner.MeasureText(g, Expression, font);
                float rootY = owner.GetItemY(g, "√", rootFont, AFMSMathTextType.Normal, baseLine);
                float textY = owner.GetItemY(g, Expression, font, AFMSMathTextType.Normal, baseLine);

                g.DrawString("√", rootFont, brush, x, rootY, owner._stringFormat);
                float textX = x + rootSize.Width;
                g.DrawString(Expression, font, brush, textX, textY, owner._stringFormat);
                g.DrawLine(pen, textX, textY + 1F, textX + textSize.Width + 2F, textY + 1F);
            }
        }

        private sealed class FractionItem : MathItem
        {
            public string Numerator { get; }
            public string Denominator { get; }

            public FractionItem(string numerator, string denominator)
            {
                Numerator = numerator;
                Denominator = denominator;
            }

            public override SizeF Measure(Graphics g, AFMSMathLabel owner)
            {
                using Font font = owner.CreateFractionFont();
                SizeF numeratorSize = owner.MeasureText(g, Numerator, font);
                SizeF denominatorSize = owner.MeasureText(g, Denominator, font);
                return new SizeF(Math.Max(numeratorSize.Width, denominatorSize.Width) + 8F, numeratorSize.Height + denominatorSize.Height + 6F);
            }

            public override void Draw(Graphics g, AFMSMathLabel owner, float x, float baseLine)
            {
                using Font font = owner.CreateFractionFont();
                using SolidBrush brush = new SolidBrush(owner.ForeColor);
                using Pen pen = new Pen(owner.ForeColor, Math.Max(1F, owner.Font.Size / 12F));

                SizeF numeratorSize = owner.MeasureText(g, Numerator, font);
                SizeF denominatorSize = owner.MeasureText(g, Denominator, font);
                float width = Math.Max(numeratorSize.Width, denominatorSize.Width) + 8F;
                float totalHeight = numeratorSize.Height + denominatorSize.Height + 6F;
                float top = baseLine - (totalHeight * 0.62F);
                float numeratorX = x + ((width - numeratorSize.Width) / 2F);
                float denominatorX = x + ((width - denominatorSize.Width) / 2F);
                float lineY = top + numeratorSize.Height + 1F;

                g.DrawString(Numerator, font, brush, numeratorX, top, owner._stringFormat);
                g.DrawLine(pen, x + 2F, lineY, x + width - 2F, lineY);
                g.DrawString(Denominator, font, brush, denominatorX, lineY + 2F, owner._stringFormat);
            }
        }

        private readonly List<MathItem> _items = new List<MathItem>();
        private readonly StringFormat _stringFormat = new StringFormat(StringFormat.GenericTypographic);
        private string _mathFontName = "Cambria Math";
        private float _scriptScale = 0.68F;
        private float _itemSpacing = 1F;
        private float _lineSpacing = 4F;
        private int _subscriptPosition = 50;
        private int _superscriptPosition = 50;
        private ContentAlignment _textAlign = ContentAlignment.MiddleLeft;
        private Point _alignmentOffset = Point.Empty;

        public AFMSMathLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(40, 40, 40);
            Font = new Font("Cambria Math", 14F, FontStyle.Italic, GraphicsUnit.Point);
            Size = new Size(220, 60);
            Padding = new Padding(4);
            TabStop = false;
            _stringFormat.FormatFlags = StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoClip;
        }

        [Category("AFMS Math")]
        [DefaultValue("Cambria Math")]
        public string MathFontName
        {
            get => _mathFontName;
            set { _mathFontName = string.IsNullOrWhiteSpace(value) ? "Cambria Math" : value; Invalidate(); }
        }

        [Category("AFMS Math")]
        [DefaultValue(0.68F)]
        public float ScriptScale
        {
            get => _scriptScale;
            set { _scriptScale = Math.Max(0.4F, Math.Min(1F, value)); Invalidate(); }
        }

        [Category("AFMS Math")]
        [DefaultValue(1F)]
        public float ItemSpacing
        {
            get => _itemSpacing;
            set { _itemSpacing = Math.Max(0F, value); Invalidate(); }
        }

        [Category("AFMS Math")]
        [DefaultValue(4F)]
        public float LineSpacing
        {
            get => _lineSpacing;
            set { _lineSpacing = Math.Max(0F, value); Invalidate(); }
        }

        [Category("AFMS Math")]
        [DefaultValue(50)]
        [Description("아랫첨자 위치입니다. 0은 일반 문자의 세로 중앙과 첨자 상단이 일치하고, 100은 일반 문자 하단과 첨자 상단이 일치합니다.")]
        public int SubscriptPosition
        {
            get => _subscriptPosition;
            set { _subscriptPosition = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        [Category("AFMS Math")]
        [DefaultValue(50)]
        [Description("윗첨자 위치입니다. 0은 일반 문자의 세로 중앙과 첨자 하단이 일치하고, 100은 일반 문자 상단과 첨자 하단이 일치합니다.")]
        public int SuperscriptPosition
        {
            get => _superscriptPosition;
            set { _superscriptPosition = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        [Category("AFMS Math")]
        [DefaultValue(ContentAlignment.MiddleLeft)]
        public ContentAlignment TextAlign
        {
            get => _textAlign;
            set { _textAlign = value; Invalidate(); }
        }

        [Category("AFMS Math")]
        [DefaultValue(typeof(Point), "0, 0")]
        [Description("수식 정렬 위치를 픽셀 단위로 미세 조정합니다. X는 좌우, Y는 상하 이동값입니다.")]
        public Point AlignmentOffset
        {
            get => _alignmentOffset;
            set { _alignmentOffset = value; Invalidate(); }
        }

        [Browsable(false)]
        public int ItemCount => _items.Count;

        public AFMSMathLabel Add(char value, AFMSMathTextType type = AFMSMathTextType.Normal)
        {
            return Add(value.ToString(), type);
        }

        public AFMSMathLabel Add(string value, AFMSMathTextType type = AFMSMathTextType.Normal)
        {
            return AddTextItems(value, type, IsVariableText(value));
        }

        public AFMSMathLabel AddText(string value)
        {
            return AddTextItems(value, AFMSMathTextType.Normal, false);
        }

        public AFMSMathLabel AddVariable(string value, AFMSMathTextType type = AFMSMathTextType.Normal)
        {
            return AddTextItems(value, type, true);
        }

        public AFMSMathLabel AddOperator(string value)
        {
            return AddText(value);
        }

        public AFMSMathLabel AddPower(string value)
        {
            return Add(value, AFMSMathTextType.Superscript);
        }

        public AFMSMathLabel AddSubscript(string value)
        {
            return Add(value, AFMSMathTextType.Subscript);
        }

        public AFMSMathLabel AddLineBreak()
        {
            _items.Add(new LineBreakItem());
            Invalidate();
            return this;
        }

        public AFMSMathLabel NewLine()
        {
            return AddLineBreak();
        }

        public AFMSMathLabel AddLog(string argument, string baseValue = "")
        {
            if (string.IsNullOrWhiteSpace(argument)) return this;
            _items.Add(new LogItem(argument, baseValue ?? string.Empty, false));
            Invalidate();
            return this;
        }

        public AFMSMathLabel AddLn(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument)) return this;
            _items.Add(new LogItem(argument, string.Empty, true));
            Invalidate();
            return this;
        }

        public AFMSMathLabel AddSqrt(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return this;
            _items.Add(new SqrtItem(expression));
            Invalidate();
            return this;
        }

        public AFMSMathLabel AddFraction(string numerator, string denominator)
        {
            if (string.IsNullOrWhiteSpace(numerator) || string.IsNullOrWhiteSpace(denominator)) return this;
            _items.Add(new FractionItem(numerator, denominator));
            Invalidate();
            return this;
        }

        public void ClearMath()
        {
            _items.Clear();
            Invalidate();
        }

        public SizeF GetMathSize()
        {
            using Bitmap bitmap = new Bitmap(1, 1);
            using Graphics g = Graphics.FromImage(bitmap);
            PrepareGraphics(g);
            List<MathLine> lines = BuildLines(g);
            return MeasureLines(lines);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PrepareGraphics(e.Graphics);

            List<MathLine> lines = BuildLines(e.Graphics);
            SizeF totalSize = MeasureLines(lines);
            float y = GetBlockY(totalSize.Height);

            foreach (MathLine line in lines)
            {
                float x = GetLineX(line.Width);
                float baseLine = y + GetLineBaseLine(line.Height, line.HasSuperscript, line.HasSubscript);

                for (int i = 0; i < line.Items.Count; i++)
                {
                    MathItem item = line.Items[i];
                    SizeF itemSize = item.Measure(e.Graphics, this);
                    item.Draw(e.Graphics, this, x, baseLine);
                    x += itemSize.Width;
                    if (i < line.Items.Count - 1) x += ItemSpacing;
                }

                y += line.Height + LineSpacing;
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            Invalidate();
        }

        private AFMSMathLabel AddTextItems(string value, AFMSMathTextType type, bool italic)
        {
            if (string.IsNullOrEmpty(value)) return this;

            string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] parts = normalized.Split('\n');

            for (int i = 0; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i])) _items.Add(new TextItem(parts[i], type, italic));
                if (i < parts.Length - 1) _items.Add(new LineBreakItem());
            }

            Invalidate();
            return this;
        }

        private void PrepareGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        private List<MathLine> BuildLines(Graphics g)
        {
            List<MathLine> lines = new List<MathLine>();
            MathLine line = new MathLine();
            lines.Add(line);

            foreach (MathItem item in _items)
            {
                if (item is LineBreakItem)
                {
                    line = new MathLine();
                    lines.Add(line);
                    continue;
                }

                SizeF size = item.Measure(g, this);
                if (line.Items.Count > 0) line.Width += ItemSpacing;
                line.Items.Add(item);
                line.Width += size.Width;
                line.Height = Math.Max(line.Height, size.Height);

                if (item is TextItem textItem)
                {
                    if (textItem.Type == AFMSMathTextType.Superscript) line.HasSuperscript = true;
                    if (textItem.Type == AFMSMathTextType.Subscript) line.HasSubscript = true;
                }
                else if (item is LogItem logItem && !string.IsNullOrEmpty(logItem.BaseValue))
                {
                    line.HasSubscript = true;
                }
            }

            foreach (MathLine currentLine in lines)
            {
                float minimumHeight = GetMinimumLineHeight(currentLine.HasSuperscript, currentLine.HasSubscript);
                currentLine.Height = Math.Max(currentLine.Height, minimumHeight);
            }

            return lines;
        }

        private SizeF MeasureLines(List<MathLine> lines)
        {
            if (lines.Count == 0) return SizeF.Empty;

            float width = 0F;
            float height = 0F;

            for (int i = 0; i < lines.Count; i++)
            {
                width = Math.Max(width, lines[i].Width);
                height += lines[i].Height;
                if (i < lines.Count - 1) height += LineSpacing;
            }

            return new SizeF(width, height);
        }

        private float GetLineX(float lineWidth)
        {
            float x = Padding.Left;

            if (TextAlign == ContentAlignment.TopCenter || TextAlign == ContentAlignment.MiddleCenter || TextAlign == ContentAlignment.BottomCenter)
                x = (ClientSize.Width - lineWidth) / 2F;
            else if (TextAlign == ContentAlignment.TopRight || TextAlign == ContentAlignment.MiddleRight || TextAlign == ContentAlignment.BottomRight)
                x = ClientSize.Width - Padding.Right - lineWidth;

            return Math.Max(Padding.Left, x) + AlignmentOffset.X;
        }

        private float GetBlockY(float totalHeight)
        {
            float y = Padding.Top;

            if (TextAlign == ContentAlignment.MiddleLeft || TextAlign == ContentAlignment.MiddleCenter || TextAlign == ContentAlignment.MiddleRight)
                y = (ClientSize.Height - totalHeight) / 2F;
            else if (TextAlign == ContentAlignment.BottomLeft || TextAlign == ContentAlignment.BottomCenter || TextAlign == ContentAlignment.BottomRight)
                y = ClientSize.Height - Padding.Bottom - totalHeight;

            return Math.Max(Padding.Top, y) + AlignmentOffset.Y;
        }

        private float GetMinimumLineHeight(bool hasSuperscript, bool hasSubscript)
        {
            float normalHeight = GetNormalTextHeight();
            float scriptHeight = normalHeight * ScriptScale;
            float topExtra = hasSuperscript ? GetScriptExtraHeight(scriptHeight, SuperscriptPosition) : 0F;
            float bottomExtra = hasSubscript ? GetScriptExtraHeight(scriptHeight, SubscriptPosition) : 0F;
            return normalHeight + topExtra + bottomExtra;
        }

        private float GetLineBaseLine(float lineHeight, bool hasSuperscript, bool hasSubscript)
        {
            float normalHeight = GetNormalTextHeight();
            float scriptHeight = normalHeight * ScriptScale;
            float topExtra = hasSuperscript ? GetScriptExtraHeight(scriptHeight, SuperscriptPosition) : 0F;
            float bottomExtra = hasSubscript ? GetScriptExtraHeight(scriptHeight, SubscriptPosition) : 0F;
            float requiredHeight = normalHeight + topExtra + bottomExtra;
            float verticalOffset = Math.Max(0F, lineHeight - requiredHeight) / 2F;
            return verticalOffset + topExtra + (normalHeight * 0.78F);
        }

        private float GetBaseLineHeight()
        {
            return GetNormalTextHeight() * 0.82F;
        }

        private float GetNormalTextHeight()
        {
            using Font font = CreateItemFont(AFMSMathTextType.Normal, true);
            return font.GetHeight();
        }

        private float GetScriptExtraHeight(float scriptHeight, int position)
        {
            float normalHeight = GetNormalTextHeight();
            float ratio = position / 100F;
            return Math.Max(0F, scriptHeight - (normalHeight * 0.5F * (1F - ratio)));
        }

        private float GetItemY(Graphics g, string text, Font font, AFMSMathTextType type, float baseLine)
        {
            float normalHeight = GetNormalTextHeight();
            float normalOriginY = baseLine - (normalHeight * 0.78F);
            if (type == AFMSMathTextType.Normal) return normalOriginY;

            using Font normalFont = CreateItemFont(AFMSMathTextType.Normal, true);
            RectangleF normalGlyphBounds = GetGlyphBounds(g, "V", normalFont);
            RectangleF scriptGlyphBounds = GetGlyphBounds(g, text, font);

            float normalVisualTop = normalOriginY + normalGlyphBounds.Top;
            float normalVisualBottom = normalOriginY + normalGlyphBounds.Bottom;
            float normalVisualCenter = (normalVisualTop + normalVisualBottom) / 2F;

            if (type == AFMSMathTextType.Subscript)
            {
                float ratio = SubscriptPosition / 100F;
                float targetTop = normalVisualCenter + ((normalVisualBottom - normalVisualCenter) * ratio);
                return targetTop - scriptGlyphBounds.Top;
            }

            float superscriptRatio = SuperscriptPosition / 100F;
            float targetBottom = normalVisualCenter - ((normalVisualCenter - normalVisualTop) * superscriptRatio);
            return targetBottom - scriptGlyphBounds.Bottom;
        }

        private RectangleF GetGlyphBounds(Graphics g, string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return RectangleF.Empty;

            using GraphicsPath path = new GraphicsPath();
            float emSize = font.SizeInPoints * g.DpiY / 72F;
            path.AddString(text, font.FontFamily, (int)font.Style, emSize, PointF.Empty, _stringFormat);
            return path.GetBounds();
        }

        private Font CreateItemFont(AFMSMathTextType type, bool italic)
        {
            float size = type == AFMSMathTextType.Normal ? Font.Size : Font.Size * ScriptScale;
            FontStyle style = italic ? FontStyle.Italic : FontStyle.Regular;

            try { return new Font(MathFontName, size, style, GraphicsUnit.Point); }
            catch { return new Font(Font.FontFamily, size, style, GraphicsUnit.Point); }
        }

        private Font CreateFractionFont()
        {
            float size = Font.Size * 0.82F;

            try { return new Font(MathFontName, size, FontStyle.Italic, GraphicsUnit.Point); }
            catch { return new Font(Font.FontFamily, size, FontStyle.Italic, GraphicsUnit.Point); }
        }

        private SizeF MeasureText(Graphics g, string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return SizeF.Empty;
            SizeF size = g.MeasureString(text, font, int.MaxValue, _stringFormat);
            return new SizeF(Math.Max(0F, size.Width - 1F), size.Height);
        }

        private static bool IsVariableText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            foreach (char c in text)
            {
                if (c == '\r' || c == '\n') continue;
                if (!char.IsLetter(c)) return false;
            }

            return true;
        }
    }
}
