namespace ExportProgressDialog
{
    partial class Form2
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelMessage;

        private void InitializeComponent()
        {
            progressBar = new ProgressBar();
            labelMessage = new Label();
            panel1 = new Panel();
            button1 = new Button();
            button2 = new Button();
            label1 = new Label();
            label2 = new Label();
            panel2 = new Panel();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // progressBar
            // 
            progressBar.Location = new Point(12, 12);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(260, 23);
            progressBar.TabIndex = 0;
            // 
            // labelMessage
            // 
            labelMessage.AutoSize = true;
            labelMessage.Location = new Point(12, 50);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(82, 13);
            labelMessage.TabIndex = 1;
            labelMessage.Text = "Processing...";
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.MenuHighlight;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(53, 10);
            panel1.TabIndex = 0;
            panel1.Paint += ExportProgressing_panel1;
            // 
            // button1
            // 
            button1.Location = new Point(10, 64);
            button1.Name = "button1";
            button1.Size = new Size(87, 36);
            button1.TabIndex = 1;
            button1.Text = "Pause";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Pause_button1;
            // 
            // button2
            // 
            button2.Location = new Point(103, 64);
            button2.Name = "button2";
            button2.Size = new Size(87, 36);
            button2.TabIndex = 2;
            button2.Text = "Cancel";
            button2.UseVisualStyleBackColor = true;
            button2.Click += Cancel_button2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(5, 36);
            label1.Name = "label1";
            label1.Size = new Size(17, 20);
            label1.TabIndex = 3;
            label1.Text = "0";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(269, 36);
            label2.Name = "label2";
            label2.Size = new Size(49, 20);
            label2.TabIndex = 4;
            label2.Text = "100 %";
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.ActiveBorder;
            panel2.Controls.Add(panel1);
            panel2.Location = new Point(10, 23);
            panel2.Name = "panel2";
            panel2.Size = new Size(298, 10);
            panel2.TabIndex = 1;
            // 
            // Form2
            // 
            BackColor = SystemColors.ButtonHighlight;
            ClientSize = new Size(320, 111);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(panel2);
            Name = "Form2";
            Text = "ExportProgressing";
            panel2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private Panel panel1;
        private Button button1;
        private Button button2;
        private Label label1;
        private Label label2;
        private Panel panel2;
    }
}