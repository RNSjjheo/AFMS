using AFMSDll;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class CheckBoxArray : TableLayoutPanel
    {
        private readonly List<AFMSCheckBox> _Items = new List<AFMSCheckBox>();

        public IReadOnlyList<AFMSCheckBox> Items => _Items;
        public IEnumerable<AFMSCheckBox> CheckedItems => _Items.Where(x => x.Checked);

        public CheckBoxArray()
        {
            Dock = DockStyle.Fill;
            AutoSize = false;
            RowCount = 1;
            ColumnCount = 0;
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            BackColor = Color.Transparent;
            Padding = new Padding(0);
            Margin = new Padding(0);
        }

        public AFMSCheckBox Add(string text)
        {
            return Add(text, null);
        }

        public AFMSCheckBox Add(string text, object tag)
        {
            AFMSCheckBox checkBox = new AFMSCheckBox();
            checkBox.Text = text;
            checkBox.Tag = tag;
            checkBox.Dock = DockStyle.Fill;
            checkBox.Margin = new Padding(3, 3, 3, 3);

            Add(checkBox);

            return checkBox;
        }

        public void Add(AFMSCheckBox checkBox)
        {
            if (checkBox == null) return;
            if (_Items.Contains(checkBox)) return;

            SuspendLayout();

            _Items.Add(checkBox);

            ColumnCount = _Items.Count;
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, checkBox.Width + checkBox.Margin.Horizontal));
            Controls.Add(checkBox, _Items.Count - 1, 0);

            ResumeLayout();
        }

        public bool Remove(AFMSCheckBox checkBox)
        {
            if (checkBox == null || !_Items.Contains(checkBox)) return false;

            Controls.Remove(checkBox);
            _Items.Remove(checkBox);

            RebuildLayout();

            checkBox.Dispose();

            return true;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _Items.Count) return false;

            return Remove(_Items[index]);
        }

        public void ClearItems()
        {
            SuspendLayout();

            foreach (AFMSCheckBox checkBox in _Items)
            {
                Controls.Remove(checkBox);
                checkBox.Dispose();
            }

            _Items.Clear();
            Controls.Clear();
            ColumnStyles.Clear();
            ColumnCount = 0;

            ResumeLayout();
        }

        public AFMSCheckBox GetItem(int index)
        {
            if (index < 0 || index >= _Items.Count) return null;

            return _Items[index];
        }

        public AFMSCheckBox FindByTag(object tag)
        {
            return _Items.FirstOrDefault(x => Equals(x.Tag, tag));
        }

        public void SetChecked(object tag, bool value)
        {
            AFMSCheckBox checkBox = FindByTag(tag);

            if (checkBox != null) checkBox.Checked = value;
        }

        public void SetAllChecked(bool value)
        {
            foreach (AFMSCheckBox checkBox in _Items) checkBox.Checked = value;
        }

        private void RebuildLayout()
        {
            SuspendLayout();

            Controls.Clear();
            ColumnStyles.Clear();

            ColumnCount = _Items.Count;

            for (int i = 0; i < _Items.Count; i++)
            {
                AFMSCheckBox checkBox = _Items[i];

                ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, checkBox.Width + checkBox.Margin.Horizontal));
                Controls.Add(checkBox, i, 0);
            }

            ResumeLayout();
        }
    }
}