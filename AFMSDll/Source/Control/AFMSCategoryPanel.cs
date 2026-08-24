using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AFMSDll
{
    [ToolboxItem(true)]
    [DefaultProperty(nameof(CategoryText))]
    public class AFMSCategoryPanel : AFMSPanel
    {
        private readonly Label _categoryLabel;
        private readonly TableLayoutPanel _contentLayout;

        private string _categoryText = "카테고리";
        private int _headerHeight = 32;
        private Color _headerBackColor = Color.FromArgb(245, 247, 250);
        private Color _headerForeColor = Color.FromArgb(55, 62, 72);
        private Color _dividerColor = Color.FromArgb(225, 229, 235);

        public AFMSCategoryPanel()
        {
            Padding = Padding.Empty;
            BackColor = Color.White;
            BorderColor = Color.FromArgb(205, 211, 220);
            BorderThickness = 0.5F;
            BorderRadius = 8;
            Size = new Size(220, 120);

            _categoryLabel = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = _headerForeColor,
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                Text = _categoryText,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 8, 0)
            };

            _contentLayout = new TableLayoutPanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 2,
                RowCount = 0,
                Margin = Padding.Empty,
                Padding = new Padding(10, 8, 10, 8),
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };

            _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));

            Controls.Add(_contentLayout);
            Controls.Add(_categoryLabel);

            LayoutInternalControls();
        }

        [Category("AFMS Appearance")]
        [DefaultValue("카테고리")]
        public string CategoryText
        {
            get => _categoryText;
            set
            {
                _categoryText = value ?? string.Empty;
                _categoryLabel.Text = _categoryText;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(32)]
        public int HeaderHeight
        {
            get => _headerHeight;
            set
            {
                _headerHeight = Math.Max(20, value);
                LayoutInternalControls();
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderBackColor
        {
            get => _headerBackColor;
            set
            {
                _headerBackColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HeaderForeColor
        {
            get => _headerForeColor;
            set
            {
                _headerForeColor = value;
                _categoryLabel.ForeColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color DividerColor
        {
            get => _dividerColor;
            set
            {
                _dividerColor = value;
                Invalidate();
            }
        }

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Font HeaderFont
        {
            get => _categoryLabel.Font;
            set
            {
                _categoryLabel.Font = value;
                Invalidate();
            }
        }

        [Category("AFMS Layout")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public TableLayoutPanel ContentLayout => _contentLayout;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Label CategoryLabel => _categoryLabel;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInternalControls();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            if (ClientSize.Width <= 1 || ClientSize.Height <= 1) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            RectangleF outerRect = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);

            using GraphicsPath outerPath = CreateRoundedPath(outerRect, BorderRadius);
            GraphicsState state = e.Graphics.Save();

            e.Graphics.SetClip(outerPath);

            using SolidBrush headerBrush = new SolidBrush(HeaderBackColor);
            e.Graphics.FillRectangle(headerBrush, 0F, 0F, ClientSize.Width, HeaderHeight);

            e.Graphics.Restore(state);

            using Pen dividerPen = new Pen(DividerColor, 1F);
            e.Graphics.DrawLine(dividerPen, 1F, HeaderHeight - 0.5F, ClientSize.Width - 1F, HeaderHeight - 0.5F);
        }

        private void LayoutInternalControls()
        {
            if (_categoryLabel == null || _contentLayout == null) return;

            _categoryLabel.SetBounds(1, 1, Math.Max(0, ClientSize.Width - 2), Math.Max(0, HeaderHeight - 1));

            int contentTop = HeaderHeight;
            int contentHeight = Math.Max(0, ClientSize.Height - contentTop - 1);

            _contentLayout.SetBounds(1, contentTop, Math.Max(0, ClientSize.Width - 2), contentHeight);

            _categoryLabel.BringToFront();
        }
    }
}
