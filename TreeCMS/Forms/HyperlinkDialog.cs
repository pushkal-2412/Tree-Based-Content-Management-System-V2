using System;
using System.Drawing;
using System.Windows.Forms;
using TreeCMS.Models;

namespace TreeCMS.Forms
{
    /// <summary>
    /// Modal dialog to input and edit a Hyperlink (Title and URL).
    /// </summary>
    public class HyperlinkDialog : Form
    {
        private Label lblTitle;
        private TextBox txtTitle;
        private Label lblUrl;
        private TextBox txtUrl;
        private Button btnOk;
        private Button btnCancel;

        public HyperlinkItem Hyperlink { get; private set; }

        public HyperlinkDialog() : this(null)
        {
        }

        public HyperlinkDialog(HyperlinkItem existingItem)
        {
            InitializeComponent();

            if (existingItem != null)
            {
                this.Text = "Edit Hyperlink";
                txtTitle.Text = existingItem.Title;
                txtUrl.Text = existingItem.Url;
            }
            else
            {
                this.Text = "Add Hyperlink";
            }
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.txtTitle = new TextBox();
            this.lblUrl = new Label();
            this.txtUrl = new TextBox();
            this.btnOk = new Button();
            this.btnCancel = new Button();
            this.SuspendLayout();

            // Form properties
            this.ClientSize = new Size(460, 210);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.White;

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new Point(25, 20);
            this.lblTitle.Text = "Link Title / Display Name:";
            this.lblTitle.ForeColor = Color.FromArgb(40, 40, 40);

            // txtTitle
            this.txtTitle.Location = new Point(28, 45);
            this.txtTitle.Size = new Size(405, 28);

            // lblUrl
            this.lblUrl.AutoSize = true;
            this.lblUrl.Location = new Point(25, 85);
            this.lblUrl.Text = "URL (e.g. https://www.google.com):";
            this.lblUrl.ForeColor = Color.FromArgb(40, 40, 40);

            // txtUrl
            this.txtUrl.Location = new Point(28, 110);
            this.txtUrl.Size = new Size(405, 28);

            // btnOk
            this.btnOk.Text = "Save";
            this.btnOk.Size = new Size(95, 32);
            this.btnOk.Location = new Point(230, 155);
            this.btnOk.BackColor = Color.FromArgb(0, 120, 215);
            this.btnOk.ForeColor = Color.White;
            this.btnOk.FlatStyle = FlatStyle.Flat;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.Click += new EventHandler(BtnOk_Click);

            // btnCancel
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Size = new Size(95, 32);
            this.btnCancel.Location = new Point(338, 155);
            this.btnCancel.BackColor = Color.FromArgb(235, 235, 235);
            this.btnCancel.ForeColor = Color.FromArgb(50, 50, 50);
            this.btnCancel.FlatStyle = FlatStyle.Flat;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.DialogResult = DialogResult.Cancel;

            // Add controls
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.txtTitle);
            this.Controls.Add(this.lblUrl);
            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);

            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text.Trim();
            string url = txtUrl.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a link title.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a valid URL.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUrl.Focus();
                return;
            }

            // Prepend https:// if protocol is missing
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            Hyperlink = new HyperlinkItem(title, url);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
