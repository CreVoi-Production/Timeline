using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ExportProgressDialog
{
    public partial class Form2 : Form
    {
        private Panel progressPanel; // パネルの名前を変更

        public Form2()
        {
            InitializeComponent();
            progressPanel = new Panel();
            this.Controls.Add(progressPanel); // progressPanel をフォームに追加
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void ExportProgressing_panel1(object sender, PaintEventArgs e)
        {

        }

        public void UpdateProgress(double progress, string message)
        {
            if (progressPanel.Parent != null)
            {
                if (progressPanel.InvokeRequired)
                {
                    progressPanel.Invoke(new Action(() => UpdateProgress(progress, message)));
                }
                else
                {
                    // 親パネルの幅を取得
                    int containerWidth = progressPanel.Parent.Width;

                    // 進行度に応じて幅を計算
                    progressPanel.Width = (int)(containerWidth * (progress / 100.0));
                    progressPanel.Invalidate(); // 再描画を要求

                    // メッセージを更新
                    labelMessage.Text = message;
                }
            }
        }

        private void Pause_button1(object sender, EventArgs e)
        {

        }

        private void Cancel_button2(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
