using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSDataViewer
{
    public sealed class InfoItem
    {
        /// <summary>
        /// 프로그램에서 항목을 식별하는 고유 ID입니다.
        /// 표시 이름이 변경되어도 ID는 변경하지 않는 것이 좋습니다.
        /// </summary>
        public string Name { get; set; }
        public string Value1 { get; set; } = string.Empty;
        public string Value2 { get; set; } = string.Empty;
        public Color? ValueColor { get; set; }
        public bool BoldValue1 { get; set; } = false;
        public bool BoldValue2{ get; set; } = false;
    }

    public sealed class InfoSection
    {
        /// <summary>
        /// 카테고리를 식별하는 고유 ID입니다.
        /// </summary>
        public string Title { get; set; }
        public string ValueHeader { get; set; } = string.Empty;
        public List<InfoItem> Items { get; } = [];
    }

    [ToolboxItem(true)]
    public class StatusInfoCard : AFMSPanel
    {
        private readonly TableLayoutPanel _mainLayout = new();
        private readonly Dictionary<string, InfoSection> _sections = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _sectionOrder = [];

        private readonly Dictionary<string, Label> _valueLabels = new(StringComparer.OrdinalIgnoreCase);

        private readonly Font _normalFont = new("맑은 고딕", 8.0F, FontStyle.Regular);
        private readonly Font _boldFont = new("맑은 고딕", 8.0F, FontStyle.Bold);
        private readonly Color _headerBackColor = Color.Transparent;//ColorTranslator.FromHtml("#F8FAFC");
        private readonly Color _headerForeColor = ColorTranslator.FromHtml("#334155");
        private readonly Color _nameForeColor = ColorTranslator.FromHtml("#64748B");
        private readonly Color _valueForeColor = ColorTranslator.FromHtml("#1E293B");
        private readonly Color _lineColor = ColorTranslator.FromHtml("#E2E8F0");

        public StatusInfoCard()
        {
            BorderRadius = 10;
            BorderThickness = 1;
            BorderColor = ColorTranslator.FromHtml("#D6DEE8");

            // 내부 컨트롤이 둥근 테두리를 덮지 않게 합니다.
            Padding = new Padding(5);
            AutoScroll = true;

            _mainLayout.Dock = DockStyle.Top;
            _mainLayout.AutoSize = true;
            _mainLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _mainLayout.Margin = Padding.Empty;
            _mainLayout.Padding = Padding.Empty;
            _mainLayout.ColumnCount = 1;
            _mainLayout.RowCount = 0;
            _mainLayout.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            _mainLayout.BackColor = Color.Transparent;//Color.White;

            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Controls.Add(_mainLayout);
        }

        /// <summary>
        /// 카테고리를 추가합니다.
        /// </summary>
        public void AddSection(InfoSection section)
        {
            ArgumentNullException.ThrowIfNull(section);
            ValidateSection(section);

            if (!_sections.TryAdd(section.Title, section))
            {
                throw new InvalidOperationException($"이미 존재하는 카테고리입니다: {section.Title}");
            }

            _sectionOrder.Add(section.Title);

            RebuildView();
        }

        private void RebuildView()
        {
            SuspendLayout();
            _mainLayout.SuspendLayout();

            try
            {
                ClearGeneratedControls();

                foreach (string sectionId in _sectionOrder)
                {
                    if (!_sections.TryGetValue(sectionId,out InfoSection? section)) continue;

                    TableLayoutPanel sectionControl = CreateSectionControl(section);

                    int rowIndex = _mainLayout.RowCount;

                    _mainLayout.RowCount++;
                    _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _mainLayout.Controls.Add(sectionControl, 0, rowIndex);
                }
            }
            finally
            {
                _mainLayout.ResumeLayout(true);
                ResumeLayout(true);
            }
        }

        private TableLayoutPanel CreateSectionControl(InfoSection section)
        {
            TableLayoutPanel sectionLayout = new()
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,

                ColumnCount = 1,
                RowCount = section.Items.Count + 2,

                Margin = Padding.Empty,
                Padding = Padding.Empty,

                BackColor = Color.Transparent,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            }; 

            sectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            // 카테고리 헤더
            sectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            sectionLayout.Controls.Add(CreateSectionHeader(section.Title,section.ValueHeader), 0, 0);

            for (int index = 0; index < section.Items.Count; index++)
            {
                InfoItem item = section.Items[index];

                sectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

                TableLayoutPanel row = CreateItemRow(section.Title, item);
                row.BackColor = index % 2 == 0 ? Color.Transparent : DllColorHelper.HexToColor("#FBFCFD");
                sectionLayout.Controls.Add(row, 0, index + 1);
            }

            sectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 5F));
            return sectionLayout;
        }

        private Control CreateSectionHeader(string title, string valueHeader)
        {
            TableLayoutPanel row = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,

                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            row.BackColor = Color.Transparent;// _headerBackColor;

            Label titleLabel = CreateLabel(title, ContentAlignment.MiddleLeft, _headerForeColor, _boldFont);

            titleLabel.Padding = new Padding(6, 0, 0, 0);

            Label valueHeaderLabel = CreateLabel(valueHeader, ContentAlignment.MiddleRight, _headerForeColor, _boldFont);

            valueHeaderLabel.Padding = new Padding(0, 0, 6, 0);

            Panel separator = new Panel();
            separator.Dock = DockStyle.Fill;
            separator.Margin = Padding.Empty;
            separator.BackColor = _lineColor;

            row.Controls.Add(titleLabel, 0, 0);
            row.Controls.Add(valueHeaderLabel, 1, 0);
            row.Controls.Add(separator, 0, 1);
            row.SetColumnSpan(separator, 2);

            return row;
        }

        private TableLayoutPanel CreateItemRow(string sectionId, InfoItem item)
        {
            TableLayoutPanel row = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,

                Margin = new Padding(8,0,0,0),
                Padding = Padding.Empty,

                BackColor = Color.White
            };

            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label nameLabel = CreateLabel(item.Name, ContentAlignment.MiddleLeft, _nameForeColor, _normalFont);

            nameLabel.Padding = new Padding(6, 0, 0, 0);

            Label valueLabel1 = CreateLabel(item.Value1, ContentAlignment.MiddleRight, item.ValueColor ?? _nameForeColor, item.BoldValue1 ? _boldFont : _normalFont);
            Label valueLabel2 = CreateLabel(item.Value2, ContentAlignment.MiddleRight, item.ValueColor ?? _nameForeColor, item.BoldValue2 ? _boldFont : _normalFont);
            
            valueLabel1.Padding = new Padding(0, 0, 6, 0);
            valueLabel2.Padding = new Padding(0, 0, 6, 0);

            row.Controls.Add(nameLabel, 0, 0);
            row.Controls.Add(valueLabel1, 1, 0);

            if (item.Value2 == string.Empty)
            {
                row.SetColumnSpan(valueLabel1, 2);
            }
            else
            {
                row.Controls.Add(valueLabel2, 2, 0);
            }

            //row.Controls.Add(separator, 0, 1);
            //row.SetColumnSpan(separator, 2);

            _valueLabels[CreateItemKey(sectionId, item.Name)] = valueLabel1;

            return row;
        }

        private static Label CreateLabel(string text, ContentAlignment alignment,Color foreColor, Font font)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,

                Margin = Padding.Empty,
                Padding = Padding.Empty,

                Text = text,
                TextAlign = alignment,

                ForeColor = foreColor,
                BackColor = Color.Transparent,
                Font = font
            };
        }

        private void ClearGeneratedControls()
        {
            _valueLabels.Clear();

            while (_mainLayout.Controls.Count > 0)
            {
                Control control = _mainLayout.Controls[0];

                _mainLayout.Controls.RemoveAt(0);
                control.Dispose();
            }

            _mainLayout.RowStyles.Clear();
            _mainLayout.RowCount = 0;
        }

        private static void ValidateSection(InfoSection section)
        {
            if (string.IsNullOrWhiteSpace(section.Title))
            {
                throw new ArgumentException("카테고리 ID가 필요합니다.");
            }

            HashSet<string> itemIds = new(StringComparer.OrdinalIgnoreCase);

            foreach (InfoItem item in section.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    throw new ArgumentException($"항목 ID가 비어 있습니다. 카테고리: {section.Title}");
                }

                if (!itemIds.Add(item.Name))
                {
                    throw new ArgumentException($"중복된 항목 ID입니다: {section.Title}/{item.Name}");
                }
            }
        }

        private static string CreateItemKey(string sectionId, string itemId)
        {
            return $"{sectionId}\u001F{itemId}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _normalFont.Dispose();
                _boldFont.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
