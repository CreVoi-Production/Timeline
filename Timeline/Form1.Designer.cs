namespace Timeline
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TrackBar trackBarTime;
        private System.Windows.Forms.Label labelTime;


        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            panel2 = new Panel();
            button1 = new Button();
            trackBarTime = new TrackBar();
            labelTime = new Label();
            button2 = new Button();
            button3 = new Button();
            label3 = new Label();
            label1 = new Label();
            label2 = new Label();
            button4 = new Button();
            button5 = new Button();
            hScrollBar1 = new HScrollBar();
            button6 = new Button();
            button7 = new Button();
            ((System.ComponentModel.ISupportInitialize)trackBarTime).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.Desktop;
            panel1.Location = new Point(26, 29);
            panel1.Name = "panel1";
            panel1.Size = new Size(834, 185);
            panel1.TabIndex = 0;
            panel1.Paint += Timeline_panel1;
            // 
            // panel2
            // 
            panel2.BackColor = Color.Red;
            panel2.Location = new Point(26, 242);
            panel2.Name = "panel2";
            panel2.Size = new Size(100, 5);
            panel2.TabIndex = 1;
            panel2.Paint += PlaybackBar_panel2;
            // 
            // button1
            // 
            button1.Location = new Point(26, 350);
            button1.Name = "button1";
            button1.Size = new Size(104, 29);
            button1.TabIndex = 1;
            button1.Text = "Add";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Add_button1;
            // 
            // trackBarTime
            // 
            trackBarTime.Location = new Point(0, 0);
            trackBarTime.Name = "trackBarTime";
            trackBarTime.Size = new Size(104, 56);
            trackBarTime.TabIndex = 0;
            // 
            // labelTime
            // 
            labelTime.AutoSize = true;
            labelTime.Location = new Point(26, 370);
            labelTime.Name = "labelTime";
            labelTime.Size = new Size(46, 17);
            labelTime.TabIndex = 3;
            labelTime.Text = "Time: 0";
            // 
            // button2
            // 
            button2.Location = new Point(426, 350);
            button2.Name = "button2";
            button2.Size = new Size(104, 29);
            button2.TabIndex = 2;
            button2.Text = "Play";
            button2.UseVisualStyleBackColor = true;
            button2.Click += Play_button2;
            // 
            // button3
            // 
            button3.Location = new Point(536, 350);
            button3.Name = "button3";
            button3.Size = new Size(104, 29);
            button3.TabIndex = 3;
            button3.Text = "Stop";
            button3.UseVisualStyleBackColor = true;
            button3.Click += Stop_button3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(26, 259);
            label3.Name = "label3";
            label3.Size = new Size(165, 20);
            label3.TabIndex = 0;
            label3.Text = "Playback Time: 00:00:00";
            label3.Click += PlaybackTime_label3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(26, 304);
            label1.Name = "label1";
            label1.Size = new Size(136, 20);
            label1.TabIndex = 4;
            label1.Text = "End Time : 00:00:00";
            label1.Click += EndTime_label1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(26, 282);
            label2.Name = "label2";
            label2.Size = new Size(142, 20);
            label2.TabIndex = 5;
            label2.Text = "Start Time : 00:00:00";
            label2.Click += StartTime_label2;
            // 
            // button4
            // 
            button4.Location = new Point(646, 350);
            button4.Name = "button4";
            button4.Size = new Size(104, 29);
            button4.TabIndex = 6;
            button4.Text = "Reset";
            button4.UseVisualStyleBackColor = true;
            button4.Click += Reset_button4;
            // 
            // button5
            // 
            button5.Location = new Point(136, 350);
            button5.Name = "button5";
            button5.Size = new Size(104, 29);
            button5.TabIndex = 7;
            button5.Text = "Clean";
            button5.UseVisualStyleBackColor = true;
            button5.Click += Clean_button5;
            // 
            // hScrollBar1
            // 
            hScrollBar1.Location = new Point(26, 217);
            hScrollBar1.Name = "hScrollBar1";
            hScrollBar1.Size = new Size(834, 17);
            hScrollBar1.TabIndex = 9;
            hScrollBar1.Scroll += Timeline_hScrollBar1;
            // 
            // button6
            // 
            button6.Location = new Point(756, 350);
            button6.Name = "button6";
            button6.Size = new Size(104, 29);
            button6.TabIndex = 8;
            button6.Text = "Export";
            button6.UseVisualStyleBackColor = true;
            button6.Click += Export_button6;
            // 
            // button7
            // 
            button7.Location = new Point(136, 385);
            button7.Name = "button7";
            button7.Size = new Size(104, 29);
            button7.TabIndex = 10;
            button7.Text = "Delete";
            button7.UseVisualStyleBackColor = true;
            button7.Click += Delete_button7;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(881, 417);
            Controls.Add(button7);
            Controls.Add(hScrollBar1);
            Controls.Add(button6);
            Controls.Add(button5);
            Controls.Add(button4);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(panel1);
            Controls.Add(label3);
            Controls.Add(panel2);
            Name = "Form1";
            Text = "Timeline";
            ((System.ComponentModel.ISupportInitialize)trackBarTime).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Button button1;
        private Button button2;
        private Button button3;
        private Label label3;
        private Panel panel2;
        private Label label1;
        private Label label2;
        private Button button4;
        private Button button5;
        private HScrollBar hScrollBar1;
        private Button button6;
        private Button button7;
    }
}
