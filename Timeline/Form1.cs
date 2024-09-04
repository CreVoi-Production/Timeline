using System;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace Timeline
{
    public partial class Form1 : Form
    {
        private Timeline _timeline;
        private System.Windows.Forms.Timer _playbackTimer;
        private TimeSpan _currentPlaybackTime;
        private bool _isPlaying;
        private AudioPlayer _audioPlayer;

        public Form1()
        {
            InitializeComponent();

            _timeline = new Timeline();
            _playbackTimer = new System.Windows.Forms.Timer();
            _playbackTimer.Interval = 100; // �~���b�P�ʁA�����ł�100ms���ƂɍX�V
            _playbackTimer.Tick += PlaybackTimer_Tick;

            // TrackBar �̏����ݒ�
            UpdateTrackBar(TimeSpan.Zero, TimeSpan.FromSeconds(10)); // 10�b�̃^�C�����C��

            _audioPlayer = new AudioPlayer();

            // �h���b�O���h���b�v�̐ݒ�
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
        }

        //�@�h���b�O���h���b�v�̎���
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        //�@�h���b�O���h���b�v�̎���
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var filePath in filePaths)
            {
                // �����t�@�C���̃��[�h
                _audioPlayer.Load(filePath);

                // �����t�@�C�������������[�h����Ă��邩�m�F
                if (_audioPlayer.TotalTime != TimeSpan.Zero)
                {
                    // �^�C�����C���I�u�W�F�N�g�Ƃ��Ēǉ�
                    var timelineObject = LoadAudioFile(filePath);
                    _timeline.AddObject(timelineObject);

                    // �^�C�����C���̒����������t�@�C���̒����ɍ��킹�čX�V
                    UpdateTrackBar(TimeSpan.Zero, _audioPlayer.TotalTime);

                    // �^�C�����C�����ĕ`��
                    panel1.Invalidate();
                }
                else
                {
                    MessageBox.Show($"�����t�@�C���̓ǂݍ��݂Ɏ��s���܂���: {filePath}", "�G���[", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //�Đ��o�[�̈ʒu���X�V
        private void UpdatePlaybackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            // �^�C�����C���̑S�̂̕����s�N�Z���P�ʂŎ擾
            int timelineWidth = panel1.Width; // `panel1` �̓^�C�����C���̕\���̈�

            // ���݂̍Đ��ʒu�Ɋ�Â��ăo�[�̕����v�Z
            double percentage = currentTime.TotalMilliseconds / totalTime.TotalMilliseconds;
            int newWidth = (int)(timelineWidth * percentage);

            // �Đ��o�[�̈ʒu���X�V
            panelPlaybackBar.Width = newWidth;
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            var currentTime = _audioPlayer.CurrentTime;

            // �^�C�����C�����I���n�_�ɒB���������m�F
            if (currentTime >= _audioPlayer.TotalTime)
            {
                // �Đ����~
                StopPlayback();
            }
            else
            {
                // TrackBar �ƃ��x�����X�V
                UpdateTrackBar(currentTime, _audioPlayer.TotalTime);
                labelPlaybackTime.Text = $"Playback Time: {currentTime.ToString(@"hh\:mm\:ss")}";

                // �Đ��o�[���X�V
                UpdatePlaybackBar(currentTime, _audioPlayer.TotalTime);
            }
        }

        private void StopPlayback()
        {
            // �^�C�}�[���~
            _playbackTimer.Stop();

            // �����Đ����~
            _audioPlayer.Stop();

            // TrackBar ���I���n�_�ɃZ�b�g
            UpdateTrackBar(_audioPlayer.TotalTime, _audioPlayer.TotalTime);
        }

        private void TimelinePanel_Paint(object sender, PaintEventArgs e)
        {
            // �^�C�����C���I�u�W�F�N�g��`�悷�鏈��
            foreach (var obj in _timeline.GetObjects())
            {
                DrawTimelineObject(e.Graphics, obj);
            }
        }

        private void DrawTimelineObject(Graphics g, TimelineObject obj)
        {
            // �I�u�W�F�N�g�̈ʒu�ƃT�C�Y���v�Z���ĕ`��
            int x = (int)obj.StartTime.TotalMilliseconds;  // �~���b���s�N�Z���ɕϊ�
            int width = (int)obj.Duration.TotalMilliseconds;  // �~���b���s�N�Z���ɕϊ�
            int y = obj.Layer * 20;  // ���C���[���Ƃ�Y���W��ς���
            int height = 20;

            g.FillRectangle(Brushes.Blue, x, y, width, height);
            g.DrawRectangle(Pens.Black, x, y, width, height);
        }

        private void Button_AddObject(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files|*.wav;*.mp3"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                _audioPlayer.Load(filePath);

                // �����t�@�C�����^�C�����C���I�u�W�F�N�g�Ƃ��Ēǉ�
                var timelineObject = new TimelineObject(TimeSpan.Zero, _audioPlayer.TotalTime, 0);
                _timeline.AddObject(timelineObject);

                // �^�C�����C���̒����������t�@�C���̒����ɍ��킹�čX�V
                UpdateTrackBar(TimeSpan.Zero, _audioPlayer.TotalTime);

                // �^�C�����C�����ĕ`��
                panel1.Invalidate();
            }
        }

        private TimelineObject LoadAudioFile(string filePath)
        {
            // �����t�@�C���̒������擾
            TimeSpan duration;
            using (var reader = new AudioFileReader(filePath))
            {
                duration = reader.TotalTime;
            }

            // �����ł́A�J�n���Ԃ�0�A���C���[��0�ɐݒ�
            return new TimelineObject(TimeSpan.Zero, duration, 0);
        }

        private void UpdateTrackBar(TimeSpan currentTime, TimeSpan totalTime)
        {
            trackBarTime.Minimum = 0;
            trackBarTime.Maximum = (int)totalTime.TotalMilliseconds;
            trackBarTime.Value = (int)currentTime.TotalMilliseconds;
            labelTime.Text = $"Time: {currentTime.ToString(@"hh\:mm\:ss")}";
        }

        private void Button_Play(object sender, EventArgs e)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                _audioPlayer.Play();
                _playbackTimer.Start();
            }
        }

        private void Button_Stop(object sender, EventArgs e)
        {
            _isPlaying = false;
            _audioPlayer.Stop();
            _playbackTimer.Stop();
            _currentPlaybackTime = TimeSpan.Zero;
            UpdateTrackBar(_currentPlaybackTime, TimeSpan.FromSeconds(60));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }

    public class Timeline
    {
        private List<TimelineObject> _objects = new List<TimelineObject>();

        public void AddObject(TimelineObject obj)
        {
            _objects.Add(obj);
        }

        public void RemoveObject(TimelineObject obj)
        {
            _objects.Remove(obj);
        }

        public List<TimelineObject> GetObjects()
        {
            return _objects;
        }
    }

    public class TimelineObject
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int Layer { get; set; }

        public TimelineObject(TimeSpan startTime, TimeSpan duration, int layer)
        {
            StartTime = startTime;
            Duration = duration;
            Layer = layer;
        }
    }

    public class AudioPlayer
    {
        private List<IWavePlayer> _wavePlayers;
        private List<WaveStream> _waveStreams;

        public AudioPlayer()
        {
            _wavePlayers = new List<IWavePlayer>();
            _waveStreams = new List<WaveStream>();
        }

        public void Load(string filePath)
        {
            var wavePlayer = new WaveOutEvent();
            var waveStream = new AudioFileReader(filePath);

            wavePlayer.Init(waveStream);
            _wavePlayers.Add(wavePlayer);
            _waveStreams.Add(waveStream);
        }

        public void Play()
        {
            foreach (var player in _wavePlayers)
            {
                player.Play();
            }
        }

        public void Stop()
        {
            foreach (var player in _wavePlayers)
            {
                player.Stop();
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                if (_waveStreams.Count > 0)
                    return _waveStreams[0].CurrentTime;
                return TimeSpan.Zero;
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                if (_waveStreams.Count > 0)
                    return _waveStreams[0].TotalTime;
                return TimeSpan.Zero;
            }
        }
    }
}
