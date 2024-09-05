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
            panelPlaybackBar = new Panel();
            button1 = new Button();
            trackBarTime = new TrackBar();
            labelTime = new Label();
            button2 = new Button();
            button3 = new Button();
            labelPlaybackTime = new Label();
            label1 = new Label();
            label2 = new Label();
            button4 = new Button();
            ((System.ComponentModel.ISupportInitialize)trackBarTime).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.Info;
            panel1.Location = new Point(26, 21);
            panel1.Name = "panel1";
            panel1.Size = new Size(694, 185);
            panel1.TabIndex = 0;
            panel1.Paint += TimelinePanel_Paint;
            // 
            // panelPlaybackBar
            // 
            panelPlaybackBar.BackColor = Color.Red;
            panelPlaybackBar.Location = new Point(26, 261);
            panelPlaybackBar.Name = "panelPlaybackBar";
            panelPlaybackBar.Size = new Size(100, 5);
            panelPlaybackBar.TabIndex = 1;
            // 
            // button1
            // 
            button1.Location = new Point(26, 324);
            button1.Name = "button1";
            button1.Size = new Size(134, 55);
            button1.TabIndex = 1;
            button1.Text = "Add";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button_AddObject;
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
            button2.Location = new Point(306, 324);
            button2.Name = "button2";
            button2.Size = new Size(134, 55);
            button2.TabIndex = 2;
            button2.Text = "Play";
            button2.UseVisualStyleBackColor = true;
            button2.Click += Button_Play;
            // 
            // button3
            // 
            button3.Location = new Point(446, 324);
            button3.Name = "button3";
            button3.Size = new Size(134, 55);
            button3.TabIndex = 3;
            button3.Text = "Stop";
            button3.UseVisualStyleBackColor = true;
            button3.Click += Button_Stop;
            // 
            // labelPlaybackTime
            // 
            labelPlaybackTime.AutoSize = true;
            labelPlaybackTime.Location = new Point(26, 223);
            labelPlaybackTime.Name = "labelPlaybackTime";
            labelPlaybackTime.Size = new Size(165, 20);
            labelPlaybackTime.TabIndex = 0;
            labelPlaybackTime.Text = "Playback Time: 00:00:00";
            labelPlaybackTime.Click += Label_PlaybackTime;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(584, 246);
            label1.Name = "label1";
            label1.Size = new Size(136, 20);
            label1.TabIndex = 4;
            label1.Text = "End Time : 00:00:00";
            label1.Click += Label_EndTime;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(578, 223);
            label2.Name = "label2";
            label2.Size = new Size(142, 20);
            label2.TabIndex = 5;
            label2.Text = "Start Time : 00:00:00";
            label2.Click += Label_StartTime;
            // 
            // button4
            // 
            button4.Location = new Point(586, 324);
            button4.Name = "button4";
            button4.Size = new Size(134, 55);
            button4.TabIndex = 6;
            button4.Text = "Reset";
            button4.UseVisualStyleBackColor = true;
            button4.Click += Button_Reset;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(745, 384);
            Controls.Add(button4);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(panel1);
            Controls.Add(labelPlaybackTime);
            Controls.Add(panelPlaybackBar);
            Name = "Form1";
            Text = "Timeline";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)trackBarTime).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Button button1;
        private Button button2;
        private Button button3;
        private Label labelPlaybackTime;
        private Panel panelPlaybackBar;
        private Label label1;
        private Label label2;
        private Button button4;
    }
}
