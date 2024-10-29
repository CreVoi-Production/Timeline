using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ExportProgressDialog
{
    public partial class Form2 : Form
    {
        private Panel progressPanel; // �p�l���̖��O��ύX

        public Form2()
        {
            InitializeComponent();
            progressPanel = new Panel();
            this.Controls.Add(progressPanel); // progressPanel ���t�H�[���ɒǉ�
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
                    // �e�p�l���̕����擾
                    int containerWidth = progressPanel.Parent.Width;

                    // �i�s�x�ɉ����ĕ����v�Z
                    progressPanel.Width = (int)(containerWidth * (progress / 100.0));
                    progressPanel.Invalidate(); // �ĕ`���v��

                    // ���b�Z�[�W���X�V
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
