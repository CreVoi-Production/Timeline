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
            button1 = new Button();
            trackBar1 = new TrackBar();
            trackBarTime = new TrackBar();
            labelTime = new Label();
            button2 = new Button();
            button3 = new Button();
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarTime).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.Info;
            panel1.Location = new Point(26, 21);
            panel1.Name = "panel1";
            panel1.Size = new Size(554, 185);
            panel1.TabIndex = 0;
            panel1.Paint += TimelinePanel_Paint;
            // 
            // button1
            // 
            button1.Location = new Point(26, 274);
            button1.Name = "button1";
            button1.Size = new Size(134, 55);
            button1.TabIndex = 1;
            button1.Text = "Add";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button_AddObject;
            // 
            // trackBar1
            // 
            trackBar1.Location = new Point(26, 212);
            trackBar1.Name = "trackBar1";
            trackBar1.Size = new Size(554, 56);
            trackBar1.TabIndex = 0;
            trackBar1.Scroll += trackBar1_Scroll;
            // 
            // trackBarTime
            // 
            trackBarTime.Location = new Point(26, 300);
            trackBarTime.Name = "trackBarTime";
            trackBarTime.Size = new Size(745, 56);
            trackBarTime.TabIndex = 2;
            trackBarTime.Scroll += TrackBar;
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
            button2.Location = new Point(306, 274);
            button2.Name = "button2";
            button2.Size = new Size(134, 55);
            button2.TabIndex = 2;
            button2.Text = "Play";
            button2.UseVisualStyleBackColor = true;
            button2.Click += Button_Play;
            // 
            // button3
            // 
            button3.Location = new Point(446, 274);
            button3.Name = "button3";
            button3.Size = new Size(134, 55);
            button3.TabIndex = 3;
            button3.Text = "Stop";
            button3.UseVisualStyleBackColor = true;
            button3.Click += Button_Stop;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(594, 353);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(trackBar1);
            Controls.Add(button1);
            Controls.Add(panel1);
            Name = "Form1";
            Text = "Timeline";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarTime).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Button button1;
        private TrackBar trackBar1;
        private Button button2;
        private Button button3;
    }
}
