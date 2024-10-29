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
            button8 = new Button();
            trackBar1 = new TrackBar();
            trackBar2 = new TrackBar();
            trackBar3 = new TrackBar();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            textBox1 = new TextBox();
            label7 = new Label();
            comboBox1 = new ComboBox();
            label8 = new Label();
            textBox2 = new TextBox();
            textBox3 = new TextBox();
            textBox4 = new TextBox();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            textBox5 = new TextBox();
            label10 = new Label();
            button9 = new Button();
            trackBar4 = new TrackBar();
            label9 = new Label();
            comboBox2 = new ComboBox();
            button10 = new Button();
            button11 = new Button();
            panel3 = new Panel();
            ((System.ComponentModel.ISupportInitialize)trackBarTime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBar2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBar3).BeginInit();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar4).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.Desktop;
            panel1.Location = new Point(26, 29);
            panel1.Name = "panel1";
            panel1.Size = new Size(792, 185);
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
            button1.Location = new Point(35, 414);
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
            button2.Location = new Point(384, 414);
            button2.Name = "button2";
            button2.Size = new Size(104, 29);
            button2.TabIndex = 2;
            button2.Text = "Play";
            button2.UseVisualStyleBackColor = true;
            button2.Click += Play_button2;
            // 
            // button3
            // 
            button3.Location = new Point(494, 414);
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
            button4.Location = new Point(604, 414);
            button4.Name = "button4";
            button4.Size = new Size(104, 29);
            button4.TabIndex = 6;
            button4.Text = "Reset";
            button4.UseVisualStyleBackColor = true;
            button4.Click += Reset_button4;
            // 
            // button5
            // 
            button5.Location = new Point(145, 414);
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
            hScrollBar1.Size = new Size(792, 17);
            hScrollBar1.TabIndex = 9;
            hScrollBar1.Scroll += Timeline_hScrollBar1;
            // 
            // button6
            // 
            button6.Location = new Point(714, 414);
            button6.Name = "button6";
            button6.Size = new Size(104, 29);
            button6.TabIndex = 8;
            button6.Text = "Export";
            button6.UseVisualStyleBackColor = true;
            button6.Click += Export_button6;
            // 
            // button7
            // 
            button7.Location = new Point(145, 449);
            button7.Name = "button7";
            button7.Size = new Size(104, 29);
            button7.TabIndex = 10;
            button7.Text = "Delete";
            button7.UseVisualStyleBackColor = true;
            button7.Click += Delete_button7;
            // 
            // button8
            // 
            button8.Location = new Point(13, 290);
            button8.Name = "button8";
            button8.Size = new Size(312, 29);
            button8.TabIndex = 11;
            button8.Text = "Synthesize";
            button8.UseVisualStyleBackColor = true;
            button8.Click += Synthesize_button8;
            // 
            // trackBar1
            // 
            trackBar1.Location = new Point(131, 104);
            trackBar1.Maximum = 8;
            trackBar1.Minimum = 2;
            trackBar1.Name = "trackBar1";
            trackBar1.Size = new Size(194, 56);
            trackBar1.TabIndex = 12;
            trackBar1.Value = 4;
            trackBar1.Scroll += ReadingSpeed_trackBar1;
            // 
            // trackBar2
            // 
            trackBar2.Location = new Point(131, 166);
            trackBar2.Maximum = 3;
            trackBar2.Minimum = -3;
            trackBar2.Name = "trackBar2";
            trackBar2.Size = new Size(194, 56);
            trackBar2.TabIndex = 13;
            trackBar2.Scroll += VoiceHeight_trackBar2;
            // 
            // trackBar3
            // 
            trackBar3.Location = new Point(131, 228);
            trackBar3.Name = "trackBar3";
            trackBar3.Size = new Size(194, 56);
            trackBar3.TabIndex = 14;
            trackBar3.Value = 5;
            trackBar3.Scroll += Intonation_trackBar3;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(13, 104);
            label4.Name = "label4";
            label4.Size = new Size(106, 20);
            label4.TabIndex = 15;
            label4.Text = "ReadingSpeed";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(13, 166);
            label5.Name = "label5";
            label5.Size = new Size(41, 20);
            label5.TabIndex = 16;
            label5.Text = "Pitch";
            label5.Click += label5_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(13, 230);
            label6.Name = "label6";
            label6.Size = new Size(77, 20);
            label6.TabIndex = 17;
            label6.Text = "Intonation";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(131, 20);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(194, 27);
            textBox1.TabIndex = 18;
            textBox1.Text = "おはよう";
            textBox1.TextChanged += TextEntry_textBox1;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(13, 23);
            label7.Name = "label7";
            label7.Size = new Size(35, 20);
            label7.TabIndex = 19;
            label7.Text = "Text";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "四国めたん", "ずんだもん", "春日部つむぎ", "雨晴はう", "波音リツ", "玄野武宏", "白上虎太郎", "青山龍星", "冥鳴ひまり", "九州そら", "もち子さん", "剣崎雌雄", "WhiteCUL", "後鬼", "No.7", "ちび式じい", "櫻歌ミコ", "小夜/SAYO", "ナースロボ＿タイプＴ", "†聖騎士 紅桜†", "雀松朱司", "麒ヶ島宗麟", "春歌ナナ", "猫使アル", "猫使ビィ", "中国うさぎ", "栗田まろん", "あいえるたん", "満別花丸", "琴詠ニア" });
            comboBox1.Location = new Point(131, 63);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(194, 28);
            comboBox1.TabIndex = 20;
            comboBox1.Text = "四国めたん";
            comboBox1.SelectedIndexChanged += TTSCharacter_comboBox1;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(13, 66);
            label8.Name = "label8";
            label8.Size = new Size(62, 20);
            label8.TabIndex = 21;
            label8.Text = "Speaker";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(13, 127);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(106, 27);
            textBox2.TabIndex = 22;
            textBox2.Text = "1";
            textBox2.TextChanged += ReadingSpeed_textBox2;
            // 
            // textBox3
            // 
            textBox3.Location = new Point(13, 189);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(106, 27);
            textBox3.TabIndex = 23;
            textBox3.Text = "0";
            textBox3.TextChanged += VoiceHeight_textBox3;
            // 
            // textBox4
            // 
            textBox4.Location = new Point(13, 250);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(106, 27);
            textBox4.TabIndex = 24;
            textBox4.Text = "1";
            textBox4.TextChanged += textBox4_TextChanged;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(comboBox1);
            groupBox1.Controls.Add(textBox4);
            groupBox1.Controls.Add(button8);
            groupBox1.Controls.Add(textBox3);
            groupBox1.Controls.Add(trackBar1);
            groupBox1.Controls.Add(textBox2);
            groupBox1.Controls.Add(trackBar2);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(trackBar3);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(textBox1);
            groupBox1.Controls.Add(label6);
            groupBox1.Location = new Point(828, 19);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(331, 329);
            groupBox1.TabIndex = 25;
            groupBox1.TabStop = false;
            groupBox1.Text = "TTS";
            groupBox1.Enter += TTS_groupBox1;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textBox5);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(button9);
            groupBox2.Controls.Add(trackBar4);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(comboBox2);
            groupBox2.Location = new Point(828, 354);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(331, 159);
            groupBox2.TabIndex = 26;
            groupBox2.TabStop = false;
            groupBox2.Text = "VC";
            groupBox2.Enter += VC_groupBox2;
            // 
            // textBox5
            // 
            textBox5.Location = new Point(13, 83);
            textBox5.Name = "textBox5";
            textBox5.Size = new Size(106, 27);
            textBox5.TabIndex = 26;
            textBox5.Text = "0";
            textBox5.TextChanged += VCVoiceHeight_textBox5;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(18, 60);
            label10.Name = "label10";
            label10.Size = new Size(41, 20);
            label10.TabIndex = 25;
            label10.Text = "Pitch";
            // 
            // button9
            // 
            button9.Location = new Point(13, 122);
            button9.Name = "button9";
            button9.Size = new Size(312, 29);
            button9.TabIndex = 25;
            button9.Text = "Recording";
            button9.UseVisualStyleBackColor = true;
            button9.Click += Recording_button9;
            // 
            // trackBar4
            // 
            trackBar4.Location = new Point(131, 60);
            trackBar4.Maximum = 12;
            trackBar4.Minimum = -12;
            trackBar4.Name = "trackBar4";
            trackBar4.Size = new Size(194, 56);
            trackBar4.TabIndex = 25;
            trackBar4.Scroll += VCVoiceHeight_trackBar4;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(18, 31);
            label9.Name = "label9";
            label9.Size = new Size(62, 20);
            label9.TabIndex = 25;
            label9.Text = "Speaker";
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Items.AddRange(new object[] { "あみたろ", "ずんだもん" });
            comboBox2.Location = new Point(131, 26);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(194, 28);
            comboBox2.TabIndex = 25;
            comboBox2.Text = "ずんだもん";
            comboBox2.SelectedIndexChanged += VCCharacter_comboBox2;
            // 
            // button10
            // 
            button10.Location = new Point(714, 448);
            button10.Name = "button10";
            button10.Size = new Size(104, 29);
            button10.TabIndex = 27;
            button10.Text = "Send";
            button10.UseVisualStyleBackColor = true;
            button10.Click += Send_button10;
            // 
            // button11
            // 
            button11.Location = new Point(714, 483);
            button11.Name = "button11";
            button11.Size = new Size(104, 29);
            button11.TabIndex = 28;
            button11.Text = "Local";
            button11.UseVisualStyleBackColor = true;
            button11.Click += Local_button11;
            // 
            // panel3
            // 
            panel3.BackColor = Color.Gainsboro;
            panel3.Location = new Point(26, 242);
            panel3.Name = "panel3";
            panel3.Size = new Size(751, 5);
            panel3.TabIndex = 2;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(1174, 525);
            Controls.Add(button11);
            Controls.Add(button10);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
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
            Controls.Add(panel3);
            Name = "Form1";
            Text = "MiVoi";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)trackBarTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBar2).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBar3).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar4).EndInit();
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
        private Button button8;
        private TrackBar trackBar1;
        private TrackBar trackBar2;
        private TrackBar trackBar3;
        private Label label4;
        private Label label5;
        private Label label6;
        private TextBox textBox1;
        private Label label7;
        private ComboBox comboBox1;
        private Label label8;
        private TextBox textBox2;
        private TextBox textBox3;
        private TextBox textBox4;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Label label9;
        private ComboBox comboBox2;
        private Button button9;
        private Button button10;
        private TrackBar trackBar4;
        private TextBox textBox5;
        private Label label10;
        private Button button11;
        private Panel panel3;
    }
}
